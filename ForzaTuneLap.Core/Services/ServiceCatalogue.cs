using System.Globalization;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

public enum StatutSuppression
{
    Supprime,

    /// <summary>Rien n'a été supprimé : des réglages et des chronos seraient perdus et l'utilisateur doit confirmer.</summary>
    ConfirmationRequise
}

/// <param name="NombreDeReglages">Réglages supprimés (ou qui le seraient, avec ConfirmationRequise).</param>
/// <param name="NombreDeChronos">Chronos supprimés (ou qui le seraient).</param>
public sealed record ResultatSuppressionVoiture(StatutSuppression Statut, int NombreDeReglages, int NombreDeChronos);

public sealed record CorrespondanceMarque(Marque Personnalisee, Marque Officielle);

public sealed record CorrespondanceVoiture(Voiture Personnalisee, Voiture Officielle);

/// <summary>Marques et voitures personnalisées qui existent désormais dans le catalogue officiel.</summary>
public sealed record FusionsPossibles(
    IReadOnlyList<CorrespondanceMarque> Marques,
    IReadOnlyList<CorrespondanceVoiture> Voitures)
{
    public bool EstVide => Marques.Count == 0 && Voitures.Count == 0;
}

public sealed record ResultatFusion(
    int NombreDeVoituresFusionnees,
    int NombreDeVoituresDeplacees,
    int NombreDeReglagesDeplaces,
    int NombreDeReglagesRenommes,
    bool MarqueSupprimee);

/// <summary>
/// Ajout, modification et suppression des marques et voitures PERSONNALISÉES (absentes du fichier de données),
/// et fusion avec le catalogue officiel quand il les rattrape.
/// Les marques et voitures officielles ne sont jamais modifiables. Les identifiants personnalisés
/// commencent à <see cref="ConstantesReferentiel.PremierIdentifiantPersonnalise"/>.
/// À appeler après l'initialisation de la base : <see cref="DetecterFusionsAsync"/> indique alors s'il y a des fusions à proposer.
/// </summary>
public sealed class ServiceCatalogue
{
    private const CompareOptions OptionsDeComparaison =
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols;

    private readonly ContexteDonnees contexte;

    public ServiceCatalogue(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    // ---------------------------------------------------------------- Marques

    public async Task<Marque> AjouterMarquePersonnaliseeAsync(string nom, int identifiantPays, CancellationToken annulation = default)
    {
        nom = ValiderNom(nom);

        if (!await contexte.Pays.AnyAsync(p => p.Identifiant == identifiantPays, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.PaysIntrouvable);
        }

        var noms = await contexte.Marques.AsNoTracking().Select(m => m.Nom).ToListAsync(annulation);
        if (noms.Any(existant => MemeNom(existant, nom)))
        {
            throw new ErreurMetierException(CodeErreurMetier.MarqueDejaExistante);
        }

        var marque = new Marque
        {
            Identifiant = await ProchainIdentifiantMarqueAsync(annulation),
            Nom = nom,
            IdentifiantPays = identifiantPays,
            EstPersonnalisee = true
        };

        contexte.Marques.Add(marque);
        await contexte.SaveChangesAsync(annulation);
        return marque;
    }

    /// <summary>Supprime une marque personnalisée, à condition qu'elle n'ait plus aucune voiture.</summary>
    public async Task SupprimerMarquePersonnaliseeAsync(int identifiantMarque, CancellationToken annulation = default)
    {
        var marque = await contexte.Marques.AsNoTracking().FirstOrDefaultAsync(m => m.Identifiant == identifiantMarque, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.MarqueIntrouvable);

        if (!marque.EstPersonnalisee)
        {
            throw new ErreurMetierException(CodeErreurMetier.MarqueOfficielleNonModifiable);
        }

        if (await contexte.Voitures.AnyAsync(v => v.IdentifiantMarque == identifiantMarque, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.MarqueNonVide);
        }

        await contexte.Marques.Where(m => m.Identifiant == identifiantMarque).ExecuteDeleteAsync(annulation);
    }

    // ---------------------------------------------------------------- Voitures

    public async Task<Voiture> AjouterVoiturePersonnaliseeAsync(
        int identifiantMarque,
        string nom,
        int annee,
        Division division,
        int valeur = 0,
        bool contenuTelechargeable = false,
        CancellationToken annulation = default)
    {
        nom = ValiderNom(nom);
        ValiderAnnee(annee);
        await VerifierMarqueAsync(identifiantMarque, annulation);
        await VerifierVoitureDisponibleAsync(identifiantMarque, nom, annee, identifiantAIgnorer: null, annulation);

        var voiture = new Voiture
        {
            Identifiant = await ProchainIdentifiantVoitureAsync(annulation),
            IdentifiantMarque = identifiantMarque,
            Nom = nom,
            Annee = annee,
            Division = division,
            Valeur = valeur,
            Achetable = true,
            ContenuTelechargeable = contenuTelechargeable,
            EstPersonnalisee = true
        };

        contexte.Voitures.Add(voiture);
        await contexte.SaveChangesAsync(annulation);
        return voiture;
    }

    /// <summary>Modifie une voiture personnalisée (y compris sa marque). Ses réglages et chronos ne sont pas touchés.</summary>
    public async Task<Voiture> ModifierVoiturePersonnaliseeAsync(
        int identifiantVoiture,
        int identifiantMarque,
        string nom,
        int annee,
        Division division,
        int valeur = 0,
        bool contenuTelechargeable = false,
        CancellationToken annulation = default)
    {
        nom = ValiderNom(nom);
        ValiderAnnee(annee);

        var voiture = await contexte.Voitures.FirstOrDefaultAsync(v => v.Identifiant == identifiantVoiture, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);

        if (!voiture.EstPersonnalisee)
        {
            throw new ErreurMetierException(CodeErreurMetier.VoitureOfficielleNonModifiable);
        }

        await VerifierMarqueAsync(identifiantMarque, annulation);
        await VerifierVoitureDisponibleAsync(identifiantMarque, nom, annee, identifiantVoiture, annulation);

        voiture.IdentifiantMarque = identifiantMarque;
        voiture.Nom = nom;
        voiture.Annee = annee;
        voiture.Division = division;
        voiture.Valeur = valeur;
        voiture.ContenuTelechargeable = contenuTelechargeable;

        await contexte.SaveChangesAsync(annulation);
        return voiture;
    }

    /// <summary>
    /// Supprime une voiture personnalisée. Si elle a des réglages, ils disparaissent avec leurs chronos :
    /// tant que <paramref name="confirmerSuppressionDesReglages"/> est faux, rien n'est supprimé et le résultat
    /// est <see cref="StatutSuppression.ConfirmationRequise"/> avec les nombres concernés.
    /// </summary>
    public async Task<ResultatSuppressionVoiture> SupprimerVoiturePersonnaliseeAsync(
        int identifiantVoiture,
        bool confirmerSuppressionDesReglages = false,
        CancellationToken annulation = default)
    {
        var voiture = await contexte.Voitures.AsNoTracking().FirstOrDefaultAsync(v => v.Identifiant == identifiantVoiture, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);

        if (!voiture.EstPersonnalisee)
        {
            throw new ErreurMetierException(CodeErreurMetier.VoitureOfficielleNonModifiable);
        }

        var nombreDeReglages = await contexte.Reglages.CountAsync(r => r.IdentifiantVoiture == identifiantVoiture, annulation);
        var nombreDeChronos = await contexte.Chronos.CountAsync(c => c.Reglage!.IdentifiantVoiture == identifiantVoiture, annulation);

        if (nombreDeReglages > 0 && !confirmerSuppressionDesReglages)
        {
            return new ResultatSuppressionVoiture(StatutSuppression.ConfirmationRequise, nombreDeReglages, nombreDeChronos);
        }

        // La base supprime en cascade les réglages, leurs rapports et leurs chronos.
        await contexte.Voitures.Where(v => v.Identifiant == identifiantVoiture).ExecuteDeleteAsync(annulation);
        return new ResultatSuppressionVoiture(StatutSuppression.Supprime, nombreDeReglages, nombreDeChronos);
    }

    // ---------------------------------------------------------------- Fusion avec le catalogue officiel

    /// <summary>
    /// Cherche les marques personnalisées dont le nom est celui d'une marque officielle, et les voitures personnalisées
    /// dont la marque, le nom et l'année sont ceux d'une voiture officielle (casse, accents et ponctuation ignorés).
    /// </summary>
    public async Task<FusionsPossibles> DetecterFusionsAsync(CancellationToken annulation = default)
    {
        var marques = await contexte.Marques.AsNoTracking().ToListAsync(annulation);
        var voitures = await contexte.Voitures.AsNoTracking().Include(v => v.Marque).ToListAsync(annulation);

        var marquesOfficielles = marques.Where(m => !m.EstPersonnalisee).ToList();
        var voituresOfficielles = voitures.Where(v => !v.EstPersonnalisee).ToList();

        var correspondancesMarques = marques
            .Where(m => m.EstPersonnalisee)
            .Select(m => (Perso: m, Officielle: marquesOfficielles.FirstOrDefault(o => MemeNom(o.Nom, m.Nom))))
            .Where(c => c.Officielle is not null)
            .Select(c => new CorrespondanceMarque(c.Perso, c.Officielle!))
            .ToList();

        var correspondancesVoitures = voitures
            .Where(v => v.EstPersonnalisee)
            .Select(v => (Perso: v, Officielle: voituresOfficielles.FirstOrDefault(o =>
                MemeNom(o.Marque!.Nom, v.Marque!.Nom) && MemeNom(o.Nom, v.Nom) && o.Annee == v.Annee)))
            .Where(c => c.Officielle is not null)
            .Select(c => new CorrespondanceVoiture(c.Perso, c.Officielle!))
            .ToList();

        return new FusionsPossibles(correspondancesMarques, correspondancesVoitures);
    }

    /// <summary>
    /// Fusionne une voiture personnalisée dans une voiture officielle : les réglages (avec leurs chronos) passent à la voiture
    /// officielle, puis la voiture personnalisée disparaît, ainsi que sa marque si elle est personnalisée et devenue vide.
    /// Un réglage dont le nom existe déjà sur la voiture officielle est renommé « nom (2) », « nom (3) »…
    /// </summary>
    public async Task<ResultatFusion> FusionnerVoitureAsync(
        int identifiantVoiturePersonnalisee,
        int identifiantVoitureOfficielle,
        CancellationToken annulation = default)
    {
        if (identifiantVoiturePersonnalisee == identifiantVoitureOfficielle)
        {
            throw new ErreurMetierException(CodeErreurMetier.FusionInvalide);
        }

        var personnalisee = await ChargerVoitureAsync(identifiantVoiturePersonnalisee, annulation);
        var officielle = await ChargerVoitureAsync(identifiantVoitureOfficielle, annulation);

        if (!personnalisee.EstPersonnalisee || officielle.EstPersonnalisee)
        {
            throw new ErreurMetierException(CodeErreurMetier.FusionInvalide);
        }

        await using var transaction = await contexte.Database.BeginTransactionAsync(annulation);

        var (deplaces, renommes) = await FusionnerVoitureInterneAsync(personnalisee, officielle, annulation);
        var marqueSupprimee = await SupprimerMarquePersonnaliseeSiVideAsync(personnalisee.IdentifiantMarque, annulation);

        await transaction.CommitAsync(annulation);
        return new ResultatFusion(1, 0, deplaces, renommes, marqueSupprimee);
    }

    /// <summary>
    /// Fusionne une marque personnalisée dans une marque officielle : chaque voiture personnalisée qui existe déjà chez la
    /// marque officielle est fusionnée avec elle (voir <see cref="FusionnerVoitureAsync"/>), les autres passent à la marque
    /// officielle. La marque personnalisée disparaît.
    /// </summary>
    public async Task<ResultatFusion> FusionnerMarqueAsync(
        int identifiantMarquePersonnalisee,
        int identifiantMarqueOfficielle,
        CancellationToken annulation = default)
    {
        if (identifiantMarquePersonnalisee == identifiantMarqueOfficielle)
        {
            throw new ErreurMetierException(CodeErreurMetier.FusionInvalide);
        }

        var personnalisee = await contexte.Marques.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Identifiant == identifiantMarquePersonnalisee, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.MarqueIntrouvable);
        var officielle = await contexte.Marques.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Identifiant == identifiantMarqueOfficielle, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.MarqueIntrouvable);

        if (!personnalisee.EstPersonnalisee || officielle.EstPersonnalisee)
        {
            throw new ErreurMetierException(CodeErreurMetier.FusionInvalide);
        }

        await using var transaction = await contexte.Database.BeginTransactionAsync(annulation);

        var voituresPersonnalisees = await contexte.Voitures.AsNoTracking()
            .Where(v => v.IdentifiantMarque == personnalisee.Identifiant)
            .ToListAsync(annulation);
        var voituresDeLaMarqueOfficielle = await contexte.Voitures.AsNoTracking()
            .Where(v => v.IdentifiantMarque == officielle.Identifiant)
            .ToListAsync(annulation);

        int fusionnees = 0, deplacees = 0, reglagesDeplaces = 0, reglagesRenommes = 0;

        foreach (var voiture in voituresPersonnalisees)
        {
            var equivalente = voituresDeLaMarqueOfficielle.FirstOrDefault(
                autre => MemeNom(autre.Nom, voiture.Nom) && autre.Annee == voiture.Annee);

            if (equivalente is not null)
            {
                var (deplaces, renommes) = await FusionnerVoitureInterneAsync(voiture, equivalente, annulation);
                fusionnees++;
                reglagesDeplaces += deplaces;
                reglagesRenommes += renommes;
            }
            else
            {
                await contexte.Voitures
                    .Where(v => v.Identifiant == voiture.Identifiant)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdentifiantMarque, officielle.Identifiant), annulation);
                deplacees++;
            }
        }

        await contexte.Marques.Where(m => m.Identifiant == personnalisee.Identifiant).ExecuteDeleteAsync(annulation);
        await transaction.CommitAsync(annulation);

        return new ResultatFusion(fusionnees, deplacees, reglagesDeplaces, reglagesRenommes, MarqueSupprimee: true);
    }

    /// <summary>Déplace les réglages d'une voiture vers une autre (en renommant les doublons), puis supprime la première.</summary>
    private async Task<(int Deplaces, int Renommes)> FusionnerVoitureInterneAsync(
        Voiture source,
        Voiture destination,
        CancellationToken annulation)
    {
        var nomsUtilises = await contexte.Reglages.AsNoTracking()
            .Where(r => r.IdentifiantVoiture == destination.Identifiant)
            .Select(r => r.Nom)
            .ToListAsync(annulation);

        var reglagesADeplacer = await contexte.Reglages.AsNoTracking()
            .Where(r => r.IdentifiantVoiture == source.Identifiant)
            .OrderBy(r => r.DateCreation)
            .Select(r => new { r.Identifiant, r.Nom })
            .ToListAsync(annulation);

        var renommes = 0;
        foreach (var reglage in reglagesADeplacer)
        {
            var nom = reglage.Nom;
            var suffixe = 2;
            while (nomsUtilises.Any(existant => string.Equals(existant, nom, StringComparison.CurrentCultureIgnoreCase)))
            {
                nom = $"{reglage.Nom} ({suffixe++})";
            }

            if (nom != reglage.Nom)
            {
                renommes++;
            }

            nomsUtilises.Add(nom);

            var identifiant = reglage.Identifiant;
            var destinationId = destination.Identifiant;
            var nomFinal = nom;
            await contexte.Reglages
                .Where(r => r.Identifiant == identifiant)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(r => r.IdentifiantVoiture, destinationId).SetProperty(r => r.Nom, nomFinal),
                    annulation);
        }

        // Plus aucun réglage : la suppression n'entraîne rien d'autre que la voiture.
        await contexte.Voitures.Where(v => v.Identifiant == source.Identifiant).ExecuteDeleteAsync(annulation);
        return (reglagesADeplacer.Count, renommes);
    }

    private async Task<bool> SupprimerMarquePersonnaliseeSiVideAsync(int identifiantMarque, CancellationToken annulation)
    {
        var marque = await contexte.Marques.AsNoTracking().FirstOrDefaultAsync(m => m.Identifiant == identifiantMarque, annulation);
        if (marque is null || !marque.EstPersonnalisee)
        {
            return false;
        }

        if (await contexte.Voitures.AnyAsync(v => v.IdentifiantMarque == identifiantMarque, annulation))
        {
            return false;
        }

        await contexte.Marques.Where(m => m.Identifiant == identifiantMarque).ExecuteDeleteAsync(annulation);
        return true;
    }

    // ---------------------------------------------------------------- Outils

    private async Task<Voiture> ChargerVoitureAsync(int identifiant, CancellationToken annulation) =>
        await contexte.Voitures.AsNoTracking().FirstOrDefaultAsync(v => v.Identifiant == identifiant, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);

    private async Task<int> ProchainIdentifiantMarqueAsync(CancellationToken annulation)
    {
        var maximum = await contexte.Marques.MaxAsync(m => (int?)m.Identifiant, annulation) ?? 0;
        return Math.Max(ConstantesReferentiel.PremierIdentifiantPersonnalise, maximum + 1);
    }

    private async Task<int> ProchainIdentifiantVoitureAsync(CancellationToken annulation)
    {
        var maximum = await contexte.Voitures.MaxAsync(v => (int?)v.Identifiant, annulation) ?? 0;
        return Math.Max(ConstantesReferentiel.PremierIdentifiantPersonnalise, maximum + 1);
    }

    private async Task VerifierMarqueAsync(int identifiantMarque, CancellationToken annulation)
    {
        if (!await contexte.Marques.AnyAsync(m => m.Identifiant == identifiantMarque, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.MarqueIntrouvable);
        }
    }

    /// <summary>Une marque ne peut pas avoir deux voitures de même nom et de même année.</summary>
    private async Task VerifierVoitureDisponibleAsync(int identifiantMarque, string nom, int annee, int? identifiantAIgnorer, CancellationToken annulation)
    {
        var aIgnorer = identifiantAIgnorer ?? -1;
        var existantes = await contexte.Voitures.AsNoTracking()
            .Where(v => v.IdentifiantMarque == identifiantMarque && v.Annee == annee && v.Identifiant != aIgnorer)
            .Select(v => v.Nom)
            .ToListAsync(annulation);

        if (existantes.Any(existant => MemeNom(existant, nom)))
        {
            throw new ErreurMetierException(CodeErreurMetier.VoitureDejaExistante);
        }
    }

    private static string ValiderNom(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ErreurMetierException(CodeErreurMetier.NomVide);
        }

        return nom.Trim();
    }

    private static void ValiderAnnee(int annee)
    {
        if (annee is < 1900 or > 2100)
        {
            throw new ErreurMetierException(CodeErreurMetier.AnneeInvalide);
        }
    }

    private static bool MemeNom(string premier, string second) =>
        CultureInfo.CurrentCulture.CompareInfo.Compare(premier, second, OptionsDeComparaison) == 0;
}
