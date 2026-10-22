using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Création, modification, duplication et suppression des réglages.
/// Règle métier : modifier le contenu d'un réglage supprime ses chronos, après confirmation de l'utilisateur.
/// Un réglage d'origine n'est jamais modifiable ni supprimable.
/// </summary>
public sealed class ServiceReglages
{
    private readonly ContexteDonnees contexte;

    public ServiceReglages(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    /// <summary>
    /// Charge un réglage pour l'éditer. Le résultat n'est pas suivi par le contexte :
    /// c'est la copie à modifier puis à passer à <see cref="ModifierAsync"/>.
    /// </summary>
    public async Task<Reglage> ObtenirPourEditionAsync(Guid identifiant, CancellationToken annulation = default) =>
        await contexte.Reglages.AsNoTracking().FirstOrDefaultAsync(r => r.Identifiant == identifiant, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.ReglageIntrouvable);

    public Task<int> CompterChronosAsync(Guid identifiantReglage, CancellationToken annulation = default) =>
        contexte.Chronos.CountAsync(c => c.IdentifiantReglage == identifiantReglage, annulation);

    /// <summary>
    /// Prépare un nouveau réglage, non enregistré et sans nom. Il reprend les valeurs du réglage d'origine de la voiture
    /// (les sections que l'origine n'a pas restent absentes). Sans réglage d'origine, toutes les sections sont présentes
    /// avec des valeurs à zéro ; l'utilisateur retire celles que la voiture n'a pas.
    /// </summary>
    public async Task<Reglage> CreerBrouillonAsync(int identifiantVoiture, CancellationToken annulation = default)
    {
        await VerifierVoitureAsync(identifiantVoiture, annulation);

        var origine = await contexte.Reglages.AsNoTracking()
            .FirstOrDefaultAsync(r => r.IdentifiantVoiture == identifiantVoiture && r.EstOrigine, annulation);

        if (origine is null)
        {
            return CreerReglageVierge(identifiantVoiture);
        }

        var brouillon = CopieurDeReglage.CopierProfondement(origine);
        brouillon.Nom = string.Empty;
        return brouillon;
    }

    public async Task<Reglage> EnregistrerNouveauAsync(Reglage reglage, CancellationToken annulation = default)
    {
        var nom = ValiderNom(reglage.Nom);
        await VerifierVoitureAsync(reglage.IdentifiantVoiture, annulation);
        await VerifierNomDisponibleAsync(reglage.IdentifiantVoiture, nom, reglage.Identifiant, annulation);

        if (await contexte.Reglages.AnyAsync(r => r.Identifiant == reglage.Identifiant, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.ReglageDejaExistant);
        }

        var maintenant = DateTime.UtcNow;
        reglage.Nom = nom;
        reglage.EstOrigine = false;
        reglage.EmpreinteOrigine = null;
        reglage.DateCreation = maintenant;
        reglage.DateModification = maintenant;
        reglage.Chronos.Clear();

        contexte.Reglages.Add(reglage);
        await contexte.SaveChangesAsync(annulation);
        return reglage;
    }

    /// <summary>Crée et enregistre une copie d'un réglage (les chronos ne sont pas copiés).</summary>
    public async Task<Reglage> DupliquerAsync(Guid identifiantSource, string nouveauNom, CancellationToken annulation = default)
    {
        var source = await ObtenirPourEditionAsync(identifiantSource, annulation);
        var copie = CopieurDeReglage.CopierProfondement(source);
        copie.Nom = nouveauNom;
        return await EnregistrerNouveauAsync(copie, annulation);
    }

    /// <summary>
    /// Enregistre les modifications d'un réglage identifié par <c>modifie.Identifiant</c>.
    /// Si le contenu change et que le réglage a des chronos, rien n'est fait tant que
    /// <paramref name="confirmerSuppressionDesChronos"/> est faux : le résultat est alors
    /// <see cref="StatutModification.ConfirmationRequise"/> avec le nombre de chronos concernés.
    /// Renommer un réglage sans changer son contenu ne touche jamais aux chronos.
    /// </summary>
    /// <param name="modifie">Copie éditée, obtenue par <see cref="ObtenirPourEditionAsync"/> (non suivie par le contexte).</param>
    public async Task<ResultatModification> ModifierAsync(
        Reglage modifie,
        bool confirmerSuppressionDesChronos = false,
        CancellationToken annulation = default)
    {
        var nom = ValiderNom(modifie.Nom);

        var existant = await contexte.Reglages.FirstOrDefaultAsync(r => r.Identifiant == modifie.Identifiant, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.ReglageIntrouvable);

        if (ReferenceEquals(existant, modifie))
        {
            throw new ArgumentException("Le réglage à enregistrer doit être une copie obtenue par ObtenirPourEditionAsync.", nameof(modifie));
        }

        if (existant.EstOrigine)
        {
            throw new ErreurMetierException(CodeErreurMetier.ReglageOrigineNonModifiable);
        }

        if (existant.IdentifiantVoiture != modifie.IdentifiantVoiture)
        {
            throw new ErreurMetierException(CodeErreurMetier.ChangementDeVoitureInterdit);
        }

        await VerifierNomDisponibleAsync(existant.IdentifiantVoiture, nom, existant.Identifiant, annulation);

        var contenuModifie = CopieurDeReglage.CalculerContenu(existant) != CopieurDeReglage.CalculerContenu(modifie);
        var nombreDeChronos = contenuModifie
            ? await CompterChronosAsync(existant.Identifiant, annulation)
            : 0;

        if (nombreDeChronos > 0 && !confirmerSuppressionDesChronos)
        {
            return new ResultatModification(StatutModification.ConfirmationRequise, nombreDeChronos);
        }

        await using var transaction = await contexte.Database.BeginTransactionAsync(annulation);

        if (nombreDeChronos > 0)
        {
            await contexte.Chronos
                .Where(c => c.IdentifiantReglage == existant.Identifiant)
                .ExecuteDeleteAsync(annulation);
        }

        if (contenuModifie)
        {
            AppliquerContenu(existant, modifie);
        }

        existant.Nom = nom;
        existant.DateModification = DateTime.UtcNow;

        await contexte.SaveChangesAsync(annulation);
        await transaction.CommitAsync(annulation);

        return new ResultatModification(StatutModification.Modifie, nombreDeChronos);
    }

    /// <summary>Supprime un réglage et ses chronos. Renvoie le nombre de chronos supprimés.</summary>
    public async Task<int> SupprimerAsync(Guid identifiant, CancellationToken annulation = default)
    {
        var reglage = await contexte.Reglages.AsNoTracking()
            .Where(r => r.Identifiant == identifiant)
            .Select(r => new { r.EstOrigine })
            .FirstOrDefaultAsync(annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.ReglageIntrouvable);

        if (reglage.EstOrigine)
        {
            throw new ErreurMetierException(CodeErreurMetier.ReglageOrigineNonModifiable);
        }

        var nombreDeChronos = await CompterChronosAsync(identifiant, annulation);

        // La suppression en cascade des chronos et des rapports est faite par la base.
        await contexte.Reglages.Where(r => r.Identifiant == identifiant).ExecuteDeleteAsync(annulation);
        return nombreDeChronos;
    }

    private static Reglage CreerReglageVierge(int identifiantVoiture) => new()
    {
        IdentifiantVoiture = identifiantVoiture,
        Generaux = new ReglagesGeneraux(),
        Performances = new PerformancesMesurees(),
        Pneus = new ReglagePneus(),
        Alignement = new ReglageAlignement(),
        Ressorts = new ReglageRessorts(),
        BarresAntiroulis = new ReglageBarresAntiroulis(),
        Amortisseurs = new ReglageAmortisseurs(),
        GeometrieDeSuspension = new ReglageGeometrieDeSuspension(),
        Differentiel = new ReglageDifferentiel(),
        Freins = new ReglageFreins(),
        Aerodynamisme = new ReglageAerodynamisme(),
        Volant = new ReglageVolant()
    };

    /// <summary>
    /// Recopie le contenu de <paramref name="source"/> dans le réglage suivi <paramref name="cible"/>.
    /// Les sections présentes des deux côtés sont mises à jour sur place ; une section apparue est ajoutée,
    /// une section retirée est supprimée.
    /// </summary>
    private void AppliquerContenu(Reglage cible, Reglage source)
    {
        // Copie indépendante : fournit les nouvelles instances de sections sans partager celles de « source ».
        var copie = CopieurDeReglage.CopierProfondement(source);

        contexte.Entry(cible.Generaux).CurrentValues.SetValues(source.Generaux);
        contexte.Entry(cible.Performances).CurrentValues.SetValues(source.Performances);

        cible.Pneus = Synchroniser(cible.Pneus, source.Pneus, copie.Pneus);
        cible.Alignement = Synchroniser(cible.Alignement, source.Alignement, copie.Alignement);
        cible.Ressorts = Synchroniser(cible.Ressorts, source.Ressorts, copie.Ressorts);
        cible.BarresAntiroulis = Synchroniser(cible.BarresAntiroulis, source.BarresAntiroulis, copie.BarresAntiroulis);
        cible.Amortisseurs = Synchroniser(cible.Amortisseurs, source.Amortisseurs, copie.Amortisseurs);
        cible.GeometrieDeSuspension = Synchroniser(cible.GeometrieDeSuspension, source.GeometrieDeSuspension, copie.GeometrieDeSuspension);
        cible.Differentiel = Synchroniser(cible.Differentiel, source.Differentiel, copie.Differentiel);
        cible.Freins = Synchroniser(cible.Freins, source.Freins, copie.Freins);
        cible.Aerodynamisme = Synchroniser(cible.Aerodynamisme, source.Aerodynamisme, copie.Aerodynamisme);
        cible.Volant = Synchroniser(cible.Volant, source.Volant, copie.Volant);

        cible.RapportFinal = source.RapportFinal;

        // Les rapports sont renumérotés de 1 à n dans la copie.
        cible.RapportsDeBoite.Clear();
        foreach (var rapport in copie.RapportsDeBoite)
        {
            cible.RapportsDeBoite.Add(rapport);
        }
    }

    private T? Synchroniser<T>(T? actuelle, T? source, T? copie) where T : class
    {
        if (source is null)
        {
            return null;
        }

        if (actuelle is null)
        {
            return copie;
        }

        contexte.Entry(actuelle).CurrentValues.SetValues(source);
        return actuelle;
    }

    private static string ValiderNom(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ErreurMetierException(CodeErreurMetier.NomVide);
        }

        return nom.Trim();
    }

    private async Task VerifierVoitureAsync(int identifiantVoiture, CancellationToken annulation)
    {
        if (!await contexte.Voitures.AnyAsync(v => v.Identifiant == identifiantVoiture, annulation))
        {
            throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);
        }
    }

    /// <summary>Deux réglages d'une même voiture ne peuvent pas porter le même nom (casse ignorée).</summary>
    private async Task VerifierNomDisponibleAsync(int identifiantVoiture, string nom, Guid identifiantAIgnorer, CancellationToken annulation)
    {
        var autresNoms = await contexte.Reglages.AsNoTracking()
            .Where(r => r.IdentifiantVoiture == identifiantVoiture && r.Identifiant != identifiantAIgnorer)
            .Select(r => r.Nom)
            .ToListAsync(annulation);

        if (autresNoms.Any(autre => string.Equals(autre, nom, StringComparison.CurrentCultureIgnoreCase)))
        {
            throw new ErreurMetierException(CodeErreurMetier.NomDejaUtilise);
        }
    }
}
