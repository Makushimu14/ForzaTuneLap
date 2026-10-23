using System.Text.Json;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

public enum ModeImport
{
    /// <summary>
    /// Ajoute ce qui manque et, pour ce qui existe des deux côtés, garde la version la plus récente
    /// (date de modification ; en cas d'égalité, la version locale).
    /// </summary>
    Fusionner,

    /// <summary>Efface toutes les données de l'utilisateur, puis charge le fichier (restauration d'une sauvegarde).</summary>
    TousRemplacer
}

public enum CodeAvertissementImport
{
    PaysIntrouvable,
    MarqueIntrouvable,
    VoitureIntrouvable,
    ReglageIntrouvable,
    ParcoursIntrouvable,
    DureeInvalide
}

/// <summary>Élément du fichier qui n'a pas pu être importé. Le détail sert au diagnostic, pas à l'affichage.</summary>
public sealed record AvertissementImport(CodeAvertissementImport Code, string Detail);

public sealed record ResultatImport(
    int MarquesAjoutees,
    int VoituresAjoutees,
    int ReglagesAjoutes,
    int ReglagesMisAJour,
    int ReglagesIgnores,
    int ReglagesRenommes,
    int ChronosAjoutes,
    int ChronosMisAJour,
    int ChronosIgnores,
    int ChronosSupprimes,
    IReadOnlyList<AvertissementImport> Avertissements);

/// <summary>
/// Export et import des données de l'utilisateur en JSON (voir <see cref="FichierSauvegarde"/>).
/// L'import est atomique : en cas d'erreur, rien n'est modifié. Il remet à zéro le suivi du contexte :
/// on l'appelle donc sur un contexte qui ne contient pas de modifications en attente.
///
/// Règles de fusion (mode Fusionner) :
/// - Réglage : identifié par son GUID. Le plus récent gagne. Si le contenu change, les chronos locaux de ce réglage sont
///   supprimés (ils ont été réalisés avec l'ancien contenu) et ceux du fichier les remplacent. Si le réglage local est plus récent
///   avec un contenu différent, les chronos du fichier pour ce réglage sont ignorés.
/// - Chrono : identifié par son couple (réglage, parcours). Le plus récent gagne.
/// - Marques et voitures personnalisées : retrouvées par leur nom et leur année ; créées si elles manquent. Si la voiture est
///   devenue officielle sur cet appareil, les réglages lui sont rattachés.
/// - Deux réglages d'une même voiture ne peuvent pas porter le même nom : le réglage importé est renommé « nom (2) ».
/// </summary>
public sealed class ServiceSauvegarde
{
    private readonly ContexteDonnees contexte;

    public ServiceSauvegarde(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    // ---------------------------------------------------------------- Export

    public async Task<FichierSauvegarde> ConstruireSauvegardeAsync(CancellationToken annulation = default)
    {
        var marques = await contexte.Marques.AsNoTracking()
            .Where(m => m.EstPersonnalisee)
            .OrderBy(m => m.Nom)
            .ToListAsync(annulation);

        var voitures = await contexte.Voitures.AsNoTracking()
            .Include(v => v.Marque)
            .Where(v => v.EstPersonnalisee)
            .OrderBy(v => v.Nom)
            .ToListAsync(annulation);

        var reglages = await contexte.Reglages.AsNoTracking()
            .Include(r => r.Voiture).ThenInclude(v => v!.Marque)
            .Where(r => !r.EstOrigine)
            .OrderBy(r => r.Nom)
            .ToListAsync(annulation);

        var chronos = await contexte.Chronos.AsNoTracking()
            .OrderBy(c => c.DateRealisation)
            .ToListAsync(annulation);

        return new FichierSauvegarde
        {
            DateExport = DateTime.UtcNow,
            MarquesPersonnalisees = marques
                .Select(m => new MarqueSauvegarde { Nom = m.Nom, IdentifiantPays = m.IdentifiantPays })
                .ToList(),
            VoituresPersonnalisees = voitures
                .Select(v => new VoitureSauvegarde
                {
                    NomMarque = v.Marque!.Nom,
                    Nom = v.Nom,
                    Annee = v.Annee,
                    Division = v.Division,
                    Valeur = v.Valeur,
                    ContenuTelechargeable = v.ContenuTelechargeable
                })
                .ToList(),
            Reglages = reglages
                .Select(r => new ReglageSauvegarde
                {
                    Voiture = ReferencerVoiture(r.Voiture!),
                    DateCreation = EnUtc(r.DateCreation),
                    DateModification = EnUtc(r.DateModification),
                    Contenu = ReglageOrigineFichier.DepuisReglage(r)
                })
                .ToList(),
            Chronos = chronos
                .Select(c => new ChronoSauvegarde
                {
                    Identifiant = c.Identifiant,
                    IdentifiantReglage = c.IdentifiantReglage,
                    IdentifiantParcours = c.IdentifiantParcours,
                    DureeMillisecondes = c.DureeMillisecondes,
                    DateRealisation = EnUtc(c.DateRealisation),
                    DateModification = EnUtc(c.DateModification),
                    Commentaire = c.Commentaire
                })
                .ToList()
        };
    }

    public async Task ExporterAsync(Stream destination, CancellationToken annulation = default)
    {
        var fichier = await ConstruireSauvegardeAsync(annulation);
        await JsonSerializer.SerializeAsync(destination, fichier, ContexteJsonSauvegarde.Default.FichierSauvegarde, annulation);
    }

    public async Task<string> ExporterEnTexteAsync(CancellationToken annulation = default)
    {
        var fichier = await ConstruireSauvegardeAsync(annulation);
        return JsonSerializer.Serialize(fichier, ContexteJsonSauvegarde.Default.FichierSauvegarde);
    }

    // ---------------------------------------------------------------- Import

    public async Task<ResultatImport> ImporterAsync(Stream source, ModeImport mode = ModeImport.Fusionner, CancellationToken annulation = default)
    {
        FichierSauvegarde? fichier;
        try
        {
            fichier = await JsonSerializer.DeserializeAsync(source, ContexteJsonSauvegarde.Default.FichierSauvegarde, annulation);
        }
        catch (JsonException)
        {
            throw new ErreurMetierException(CodeErreurMetier.SauvegardeInvalide);
        }

        return await ImporterAsync(fichier ?? throw new ErreurMetierException(CodeErreurMetier.SauvegardeInvalide), mode, annulation);
    }

    public async Task<ResultatImport> ImporterDepuisTexteAsync(string json, ModeImport mode = ModeImport.Fusionner, CancellationToken annulation = default)
    {
        FichierSauvegarde? fichier;
        try
        {
            fichier = JsonSerializer.Deserialize(json, ContexteJsonSauvegarde.Default.FichierSauvegarde);
        }
        catch (JsonException)
        {
            throw new ErreurMetierException(CodeErreurMetier.SauvegardeInvalide);
        }

        return await ImporterAsync(fichier ?? throw new ErreurMetierException(CodeErreurMetier.SauvegardeInvalide), mode, annulation);
    }

    public async Task<ResultatImport> ImporterAsync(FichierSauvegarde fichier, ModeImport mode = ModeImport.Fusionner, CancellationToken annulation = default)
    {
        if (fichier.Version > FichierSauvegarde.VersionActuelle)
        {
            throw new ErreurMetierException(CodeErreurMetier.SauvegardeVersionNonSupportee);
        }

        if (fichier.Version < 1)
        {
            throw new ErreurMetierException(CodeErreurMetier.SauvegardeInvalide);
        }

        contexte.ChangeTracker.Clear();

        var compteurs = new Compteurs();
        var avertissements = new List<AvertissementImport>();

        await using var transaction = await contexte.Database.BeginTransactionAsync(annulation);

        if (mode == ModeImport.TousRemplacer)
        {
            await SupprimerToutesLesDonneesAsync(compteurs, annulation);
        }

        await ImporterCatalogueAsync(fichier, compteurs, avertissements, annulation);
        var reglagesAcceptes = await ImporterReglagesAsync(fichier, compteurs, avertissements, annulation);
        await ImporterChronosAsync(fichier, reglagesAcceptes, compteurs, avertissements, annulation);

        await transaction.CommitAsync(annulation);
        contexte.ChangeTracker.Clear();

        return new ResultatImport(
            compteurs.MarquesAjoutees,
            compteurs.VoituresAjoutees,
            compteurs.ReglagesAjoutes,
            compteurs.ReglagesMisAJour,
            compteurs.ReglagesIgnores,
            compteurs.ReglagesRenommes,
            compteurs.ChronosAjoutes,
            compteurs.ChronosMisAJour,
            compteurs.ChronosIgnores,
            compteurs.ChronosSupprimes,
            avertissements);
    }

    private async Task SupprimerToutesLesDonneesAsync(Compteurs compteurs, CancellationToken annulation)
    {
        compteurs.ChronosSupprimes += await contexte.Chronos.ExecuteDeleteAsync(annulation);
        await contexte.Reglages.Where(r => !r.EstOrigine).ExecuteDeleteAsync(annulation);
        await contexte.Voitures.Where(v => v.EstPersonnalisee).ExecuteDeleteAsync(annulation);
        await contexte.Marques.Where(m => m.EstPersonnalisee).ExecuteDeleteAsync(annulation);
    }

    /// <summary>Crée les marques et voitures personnalisées du fichier qui n'existent pas encore.</summary>
    private async Task ImporterCatalogueAsync(
        FichierSauvegarde fichier,
        Compteurs compteurs,
        List<AvertissementImport> avertissements,
        CancellationToken annulation)
    {
        var marques = await contexte.Marques.ToListAsync(annulation);
        var voitures = await contexte.Voitures.ToListAsync(annulation);
        var identifiantsPays = (await contexte.Pays.Select(p => p.Identifiant).ToListAsync(annulation)).ToHashSet();

        var prochaineMarque = Math.Max(
            ConstantesReferentiel.PremierIdentifiantPersonnalise,
            (marques.Count > 0 ? marques.Max(m => m.Identifiant) : 0) + 1);
        var prochaineVoiture = Math.Max(
            ConstantesReferentiel.PremierIdentifiantPersonnalise,
            (voitures.Count > 0 ? voitures.Max(v => v.Identifiant) : 0) + 1);

        foreach (var marqueFichier in fichier.MarquesPersonnalisees)
        {
            if (string.IsNullOrWhiteSpace(marqueFichier.Nom)
                || marques.Any(m => ComparaisonDeNoms.Identiques(m.Nom, marqueFichier.Nom)))
            {
                continue;
            }

            if (!identifiantsPays.Contains(marqueFichier.IdentifiantPays))
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.PaysIntrouvable,
                    $"Marque « {marqueFichier.Nom} » : pays {marqueFichier.IdentifiantPays}."));
                continue;
            }

            var marque = new Marque
            {
                Identifiant = prochaineMarque++,
                Nom = marqueFichier.Nom.Trim(),
                IdentifiantPays = marqueFichier.IdentifiantPays,
                EstPersonnalisee = true
            };
            contexte.Marques.Add(marque);
            marques.Add(marque);
            compteurs.MarquesAjoutees++;
        }

        await contexte.SaveChangesAsync(annulation);

        foreach (var voitureFichier in fichier.VoituresPersonnalisees)
        {
            if (string.IsNullOrWhiteSpace(voitureFichier.Nom))
            {
                continue;
            }

            var marque = marques.FirstOrDefault(m => ComparaisonDeNoms.Identiques(m.Nom, voitureFichier.NomMarque));
            if (marque is null)
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.MarqueIntrouvable,
                    $"Voiture « {voitureFichier.Nom} » : marque « {voitureFichier.NomMarque} »."));
                continue;
            }

            var existe = voitures.Any(v =>
                v.IdentifiantMarque == marque.Identifiant
                && v.Annee == voitureFichier.Annee
                && ComparaisonDeNoms.Identiques(v.Nom, voitureFichier.Nom));
            if (existe)
            {
                continue;
            }

            var voiture = new Voiture
            {
                Identifiant = prochaineVoiture++,
                IdentifiantMarque = marque.Identifiant,
                Nom = voitureFichier.Nom.Trim(),
                Annee = voitureFichier.Annee,
                Division = voitureFichier.Division,
                Valeur = voitureFichier.Valeur,
                Achetable = true,
                ContenuTelechargeable = voitureFichier.ContenuTelechargeable,
                EstPersonnalisee = true
            };
            contexte.Voitures.Add(voiture);
            voitures.Add(voiture);
            compteurs.VoituresAjoutees++;
        }

        await contexte.SaveChangesAsync(annulation);
        contexte.ChangeTracker.Clear();
    }

    /// <summary>
    /// Importe les réglages du fichier. Renvoie les identifiants des réglages dont les chronos du fichier sont cohérents
    /// avec le contenu local (ajoutés, remplacés ou de contenu identique).
    /// </summary>
    private async Task<HashSet<Guid>> ImporterReglagesAsync(
        FichierSauvegarde fichier,
        Compteurs compteurs,
        List<AvertissementImport> avertissements,
        CancellationToken annulation)
    {
        var voitures = await contexte.Voitures.AsNoTracking().Include(v => v.Marque).ToListAsync(annulation);
        var acceptes = new HashSet<Guid>();

        foreach (var reglageFichier in fichier.Reglages)
        {
            var identifiant = reglageFichier.Contenu.Identifiant;
            var voiture = ResoudreVoiture(reglageFichier.Voiture, voitures);
            if (voiture is null)
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.VoitureIntrouvable,
                    $"Réglage « {reglageFichier.Contenu.Nom} » : voiture « {reglageFichier.Voiture.NomMarque} {reglageFichier.Voiture.Nom} {reglageFichier.Voiture.Annee} »."));
                continue;
            }

            var local = await contexte.Reglages.AsNoTracking().FirstOrDefaultAsync(r => r.Identifiant == identifiant, annulation);
            var importe = ConstruireReglage(reglageFichier, voiture.Identifiant);

            if (local is null)
            {
                await EcrireNomUniqueAsync(importe, compteurs, annulation);
                contexte.Reglages.Add(importe);
                await contexte.SaveChangesAsync(annulation);
                contexte.ChangeTracker.Clear();
                acceptes.Add(identifiant);
                compteurs.ReglagesAjoutes++;
                continue;
            }

            if (local.EstOrigine)
            {
                compteurs.ReglagesIgnores++;
                continue;
            }

            var plusRecent = importe.DateModification > local.DateModification;
            var memeContenu = local.IdentifiantVoiture == voiture.Identifiant
                && CopieurDeReglage.CalculerContenu(local) == CopieurDeReglage.CalculerContenu(importe);

            if (memeContenu)
            {
                acceptes.Add(identifiant);

                if (plusRecent && importe.Nom != local.Nom)
                {
                    await EcrireNomUniqueAsync(importe, compteurs, annulation);
                    var nom = importe.Nom;
                    var date = importe.DateModification;
                    await contexte.Reglages
                        .Where(r => r.Identifiant == identifiant)
                        .ExecuteUpdateAsync(s => s.SetProperty(r => r.Nom, nom).SetProperty(r => r.DateModification, date), annulation);
                    compteurs.ReglagesMisAJour++;
                }
                else
                {
                    if (plusRecent)
                    {
                        var date = importe.DateModification;
                        await contexte.Reglages
                            .Where(r => r.Identifiant == identifiant)
                            .ExecuteUpdateAsync(s => s.SetProperty(r => r.DateModification, date), annulation);
                    }

                    compteurs.ReglagesIgnores++;
                }

                continue;
            }

            if (!plusRecent)
            {
                // Le réglage local est plus récent (ou aussi récent) : il gagne, et les chronos du fichier sont périmés.
                compteurs.ReglagesIgnores++;
                continue;
            }

            // Le réglage du fichier est plus récent et son contenu diffère : il remplace le local, chronos compris.
            compteurs.ChronosSupprimes += await contexte.Chronos.CountAsync(c => c.IdentifiantReglage == identifiant, annulation);
            await contexte.Reglages.Where(r => r.Identifiant == identifiant).ExecuteDeleteAsync(annulation);

            await EcrireNomUniqueAsync(importe, compteurs, annulation);
            contexte.Reglages.Add(importe);
            await contexte.SaveChangesAsync(annulation);
            contexte.ChangeTracker.Clear();
            acceptes.Add(identifiant);
            compteurs.ReglagesMisAJour++;
        }

        return acceptes;
    }

    private async Task ImporterChronosAsync(
        FichierSauvegarde fichier,
        HashSet<Guid> reglagesAcceptes,
        Compteurs compteurs,
        List<AvertissementImport> avertissements,
        CancellationToken annulation)
    {
        var parcoursExistants = (await contexte.Parcours.Select(p => p.Identifiant).ToListAsync(annulation)).ToHashSet();
        var reglagesExistants = (await contexte.Reglages.Select(r => r.Identifiant).ToListAsync(annulation)).ToHashSet();
        var reglagesDuFichier = fichier.Reglages.Select(r => r.Contenu.Identifiant).ToHashSet();

        var locaux = await contexte.Chronos.ToListAsync(annulation);
        var parCouple = locaux.ToDictionary(c => (c.IdentifiantReglage, c.IdentifiantParcours));
        var identifiantsUtilises = locaux.Select(c => c.Identifiant).ToHashSet();

        foreach (var chronoFichier in fichier.Chronos)
        {
            if (!reglagesExistants.Contains(chronoFichier.IdentifiantReglage))
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.ReglageIntrouvable,
                    $"Chrono {chronoFichier.Identifiant} : réglage {chronoFichier.IdentifiantReglage}."));
                continue;
            }

            // Réglage du fichier resté tel quel côté local avec un contenu différent : ses chronos sont périmés.
            if (reglagesDuFichier.Contains(chronoFichier.IdentifiantReglage) && !reglagesAcceptes.Contains(chronoFichier.IdentifiantReglage))
            {
                compteurs.ChronosIgnores++;
                continue;
            }

            if (!parcoursExistants.Contains(chronoFichier.IdentifiantParcours))
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.ParcoursIntrouvable,
                    $"Chrono {chronoFichier.Identifiant} : parcours {chronoFichier.IdentifiantParcours}."));
                continue;
            }

            if (chronoFichier.DureeMillisecondes <= 0)
            {
                avertissements.Add(new AvertissementImport(
                    CodeAvertissementImport.DureeInvalide,
                    $"Chrono {chronoFichier.Identifiant} : durée {chronoFichier.DureeMillisecondes} ms."));
                continue;
            }

            var cle = (chronoFichier.IdentifiantReglage, chronoFichier.IdentifiantParcours);
            if (parCouple.TryGetValue(cle, out var local))
            {
                if (chronoFichier.DateModification > local.DateModification)
                {
                    local.DureeMillisecondes = chronoFichier.DureeMillisecondes;
                    local.DateRealisation = chronoFichier.DateRealisation;
                    local.DateModification = chronoFichier.DateModification;
                    local.Commentaire = chronoFichier.Commentaire;
                    compteurs.ChronosMisAJour++;
                }
                else
                {
                    compteurs.ChronosIgnores++;
                }

                continue;
            }

            var chrono = new Chrono
            {
                Identifiant = identifiantsUtilises.Contains(chronoFichier.Identifiant) ? Guid.NewGuid() : chronoFichier.Identifiant,
                IdentifiantReglage = chronoFichier.IdentifiantReglage,
                IdentifiantParcours = chronoFichier.IdentifiantParcours,
                DureeMillisecondes = chronoFichier.DureeMillisecondes,
                DateRealisation = chronoFichier.DateRealisation,
                DateModification = chronoFichier.DateModification,
                Commentaire = chronoFichier.Commentaire
            };
            contexte.Chronos.Add(chrono);
            parCouple[cle] = chrono;
            identifiantsUtilises.Add(chrono.Identifiant);
            compteurs.ChronosAjoutes++;
        }

        await contexte.SaveChangesAsync(annulation);
    }

    // ---------------------------------------------------------------- Outils

    /// <summary>
    /// Retrouve la voiture désignée : par son identifiant si elle est officielle, sinon (ou si l'identifiant est inconnu ici)
    /// par sa marque, son nom et son année. Une voiture officielle est préférée à une personnalisée, ce qui rattache les réglages
    /// à la voiture officielle quand une voiture personnalisée l'a rejointe.
    /// </summary>
    private static Voiture? ResoudreVoiture(ReferenceVoiture reference, List<Voiture> voitures)
    {
        if (reference.IdentifiantOfficiel is { } identifiant)
        {
            var parIdentifiant = voitures.FirstOrDefault(v => v.Identifiant == identifiant && !v.EstPersonnalisee);
            if (parIdentifiant is not null)
            {
                return parIdentifiant;
            }
        }

        return voitures
            .Where(v => v.Annee == reference.Annee
                && ComparaisonDeNoms.Identiques(v.Nom, reference.Nom)
                && ComparaisonDeNoms.Identiques(v.Marque!.Nom, reference.NomMarque))
            .OrderBy(v => v.EstPersonnalisee)
            .FirstOrDefault();
    }

    private static ReferenceVoiture ReferencerVoiture(Voiture voiture) => new()
    {
        IdentifiantOfficiel = voiture.EstPersonnalisee ? null : voiture.Identifiant,
        NomMarque = voiture.Marque!.Nom,
        Nom = voiture.Nom,
        Annee = voiture.Annee
    };

    private static Reglage ConstruireReglage(ReglageSauvegarde reglageFichier, int identifiantVoiture)
    {
        var reglage = reglageFichier.Contenu.VersReglage(identifiantVoiture, string.Empty, reglageFichier.DateCreation);
        reglage.EstOrigine = false;
        reglage.EmpreinteOrigine = null;
        reglage.DateCreation = reglageFichier.DateCreation;
        reglage.DateModification = reglageFichier.DateModification;
        reglage.Nom = string.IsNullOrWhiteSpace(reglageFichier.Contenu.Nom) ? "Import" : reglageFichier.Contenu.Nom.Trim();
        return reglage;
    }

    /// <summary>Renomme le réglage « nom (2) », « nom (3) »… si son nom est déjà pris sur la même voiture.</summary>
    private async Task EcrireNomUniqueAsync(Reglage reglage, Compteurs compteurs, CancellationToken annulation)
    {
        var autresNoms = await contexte.Reglages.AsNoTracking()
            .Where(r => r.IdentifiantVoiture == reglage.IdentifiantVoiture && r.Identifiant != reglage.Identifiant)
            .Select(r => r.Nom)
            .ToListAsync(annulation);

        var nomSouhaite = reglage.Nom;
        var nom = nomSouhaite;
        var suffixe = 2;
        while (autresNoms.Any(autre => string.Equals(autre, nom, StringComparison.CurrentCultureIgnoreCase)))
        {
            nom = $"{nomSouhaite} ({suffixe++})";
        }

        if (nom != nomSouhaite)
        {
            reglage.Nom = nom;
            compteurs.ReglagesRenommes++;
        }
    }

    private static DateTime EnUtc(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc);

    private sealed class Compteurs
    {
        public int MarquesAjoutees;
        public int VoituresAjoutees;
        public int ReglagesAjoutes;
        public int ReglagesMisAJour;
        public int ReglagesIgnores;
        public int ReglagesRenommes;
        public int ChronosAjoutes;
        public int ChronosMisAJour;
        public int ChronosIgnores;
        public int ChronosSupprimes;
    }
}
