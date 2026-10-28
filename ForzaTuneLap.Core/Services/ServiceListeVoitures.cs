using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Critère de classement de la liste des voitures. Les critères chiffrés (indice, puissance, vitesse maximale, accélération)
/// utilisent les valeurs du réglage d'origine de la voiture ; une voiture qui n'en a pas (ou dont la valeur n'est pas
/// renseignée) est toujours classée en dernier, quel que soit le sens du tri.
/// </summary>
public enum CritereDeTriVoitures
{
    MarqueEtNom,
    Annee,
    IndiceDePerformance,
    Puissance,
    VitesseMaximale,

    /// <summary>Temps de 0 à 97 km/h.</summary>
    Acceleration,

    NombreDeReglages
}

/// <summary>
/// Filtres de la liste des voitures. Toutes les conditions renseignées doivent être vraies en même temps ;
/// dans une liste (marques, divisions…), il suffit qu'une valeur corresponde.
/// </summary>
public sealed class FiltreVoitures
{
    /// <summary>Texte cherché dans la marque, le nom et l'année (casse, accents et ponctuation ignorés).</summary>
    public string? Texte { get; set; }

    public HashSet<int> IdentifiantsMarques { get; set; } = new();

    /// <summary>Pays de la marque.</summary>
    public HashSet<int> IdentifiantsPays { get; set; } = new();

    public HashSet<Division> Divisions { get; set; } = new();

    public int? AnneeMinimum { get; set; }
    public int? AnneeMaximum { get; set; }

    /// <summary>Uniquement les voitures pour lesquelles l'utilisateur a créé au moins un réglage.</summary>
    public bool SeulementAvecReglagesCrees { get; set; }

    public bool SeulementAvecChronos { get; set; }

    /// <summary>Uniquement les voitures ajoutées par l'utilisateur.</summary>
    public bool SeulementPersonnalisees { get; set; }
}

/// <summary>
/// Une voiture dans la liste. Les valeurs « d'origine » viennent du réglage d'origine ; elles sont vides si la voiture
/// n'en a pas ou si la valeur n'est pas renseignée. Unités canoniques : kW, km/h, secondes.
/// </summary>
public sealed record LigneVoiture(
    Voiture Voiture,
    string NomMarque,
    int NombreDeReglagesCrees,
    int NombreDeChronos,
    bool ADesReglagesOrigine,
    int? IndiceDePerformanceOrigine,
    double? PuissanceOrigine,
    double? VitesseMaximaleOrigine,
    double? AccelerationOrigine)
{
    public int Identifiant => Voiture.Identifiant;

    public ClasseDePerformance? ClasseOrigine =>
        IndiceDePerformanceOrigine is { } indice ? IndicesDePerformance.ObtenirClasse(indice) : null;
}

public sealed record OptionMarque(int Identifiant, string Nom, int NombreDeVoitures);

/// <summary>Valeurs proposées par les filtres : seulement celles qui existent dans les données.</summary>
public sealed record OptionsFiltresVoitures(IReadOnlyList<OptionMarque> Marques, IReadOnlyList<Division> Divisions);

/// <summary>Une ligne de la liste des réglages d'une voiture. Les valeurs sont en unité canonique.</summary>
public sealed record LigneReglage(
    Guid Identifiant,
    string Nom,
    bool EstOrigine,
    DateTime DateModification,
    int NombreDeChronos,
    int IndiceDePerformance,
    double Puissance,
    Transmission Transmission);

public sealed record DetailVoiture(Voiture Voiture, string NomMarque, IReadOnlyList<LigneReglage> Reglages);

/// <summary>Consultation du catalogue : liste filtrée et triée des voitures, options des filtres, détail d'une voiture.</summary>
public sealed class ServiceListeVoitures
{
    private readonly ContexteDonnees contexte;

    public ServiceListeVoitures(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    /// <summary>
    /// Sens de tri le plus utile pour un critère : le meilleur d'abord (le plus grand indice, la plus grande puissance,
    /// le plus petit temps d'accélération), l'ordre naturel pour le nom et l'année.
    /// </summary>
    public static SensDuTri SensParDefaut(CritereDeTriVoitures critere) => critere switch
    {
        CritereDeTriVoitures.IndiceDePerformance => SensDuTri.Decroissant,
        CritereDeTriVoitures.Puissance => SensDuTri.Decroissant,
        CritereDeTriVoitures.VitesseMaximale => SensDuTri.Decroissant,
        CritereDeTriVoitures.NombreDeReglages => SensDuTri.Decroissant,
        _ => SensDuTri.Croissant
    };

    /// <param name="sens">Sens du tri ; par défaut celui de <see cref="SensParDefaut"/>.</param>
    public async Task<IReadOnlyList<LigneVoiture>> ListerAsync(
        FiltreVoitures? filtre = null,
        CritereDeTriVoitures critere = CritereDeTriVoitures.MarqueEtNom,
        SensDuTri? sens = null,
        CancellationToken annulation = default)
    {
        var voitures = await contexte.Voitures.AsNoTracking().Include(v => v.Marque).ToListAsync(annulation);

        var reglagesCrees = (await contexte.Reglages.AsNoTracking()
                .Where(r => !r.EstOrigine)
                .GroupBy(r => r.IdentifiantVoiture)
                .Select(g => new { Identifiant = g.Key, Nombre = g.Count() })
                .ToListAsync(annulation))
            .ToDictionary(x => x.Identifiant, x => x.Nombre);

        var origines = (await contexte.Reglages.AsNoTracking()
                .Where(r => r.EstOrigine)
                .Select(r => new
                {
                    Identifiant = r.IdentifiantVoiture,
                    r.Generaux.IndiceDePerformance,
                    r.Generaux.Puissance,
                    r.Performances.VitesseMaximale,
                    r.Performances.AccelerationDeZeroA97
                })
                .ToListAsync(annulation))
            .GroupBy(x => x.Identifiant)
            .ToDictionary(g => g.Key, g => g.First());

        var chronos = (await contexte.Chronos.AsNoTracking()
                .GroupBy(c => c.Reglage!.IdentifiantVoiture)
                .Select(g => new { Identifiant = g.Key, Nombre = g.Count() })
                .ToListAsync(annulation))
            .ToDictionary(x => x.Identifiant, x => x.Nombre);

        var lignes = voitures
            .Select(v =>
            {
                var aUneOrigine = origines.TryGetValue(v.Identifiant, out var origine);

                // Une valeur à zéro est une valeur non renseignée.
                return new LigneVoiture(
                    v,
                    v.Marque!.Nom,
                    reglagesCrees.GetValueOrDefault(v.Identifiant),
                    chronos.GetValueOrDefault(v.Identifiant),
                    aUneOrigine,
                    aUneOrigine && origine!.IndiceDePerformance > 0 ? origine.IndiceDePerformance : null,
                    aUneOrigine && origine!.Puissance > 0 ? origine.Puissance : null,
                    aUneOrigine && origine!.VitesseMaximale > 0 ? origine.VitesseMaximale : null,
                    aUneOrigine && origine!.AccelerationDeZeroA97 > 0 ? origine.AccelerationDeZeroA97 : null);
            })
            .Where(ligne => filtre is null || Correspond(ligne, filtre))
            .ToList();

        return Trier(lignes, critere, sens ?? SensParDefaut(critere));
    }

    /// <summary>Marques (avec leur nombre de voitures) et divisions présentes dans le catalogue, triées par nom affiché.</summary>
    public async Task<OptionsFiltresVoitures> ObtenirOptionsAsync(CancellationToken annulation = default)
    {
        var voitures = await contexte.Voitures.AsNoTracking().Include(v => v.Marque).ToListAsync(annulation);

        // On regroupe par identifiant : une requête sans suivi crée un objet Marque distinct pour chaque voiture,
        // donc un regroupement sur l'objet lui-même listerait la même marque autant de fois que de voitures.
        var marques = voitures
            .GroupBy(v => v.IdentifiantMarque)
            .Select(g => new OptionMarque(g.Key, g.First().Marque!.Nom, g.Count()))
            .OrderBy(m => m.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var divisions = voitures
            .Select(v => v.Division)
            .Distinct()
            .OrderBy(d => ServiceLibelles.Obtenir(d), StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new OptionsFiltresVoitures(marques, divisions);
    }

    /// <summary>Une voiture et tous ses réglages : celui d'origine d'abord, puis ceux de l'utilisateur par nom.</summary>
    public async Task<DetailVoiture> ObtenirDetailAsync(int identifiantVoiture, CancellationToken annulation = default)
    {
        var voiture = await contexte.Voitures.AsNoTracking()
            .Include(v => v.Marque)
            .FirstOrDefaultAsync(v => v.Identifiant == identifiantVoiture, annulation)
            ?? throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);

        var chronosParReglage = (await contexte.Chronos.AsNoTracking()
                .Where(c => c.Reglage!.IdentifiantVoiture == identifiantVoiture)
                .GroupBy(c => c.IdentifiantReglage)
                .Select(g => new { Identifiant = g.Key, Nombre = g.Count() })
                .ToListAsync(annulation))
            .ToDictionary(x => x.Identifiant, x => x.Nombre);

        var reglages = await contexte.Reglages.AsNoTracking()
            .Where(r => r.IdentifiantVoiture == identifiantVoiture)
            .Select(r => new
            {
                r.Identifiant,
                r.Nom,
                r.EstOrigine,
                r.DateModification,
                r.Generaux.IndiceDePerformance,
                r.Generaux.Puissance,
                r.Generaux.Transmission
            })
            .ToListAsync(annulation);

        var lignes = reglages
            .OrderByDescending(r => r.EstOrigine)
            .ThenBy(r => r.Nom, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new LigneReglage(
                r.Identifiant,
                r.Nom,
                r.EstOrigine,
                r.DateModification,
                chronosParReglage.GetValueOrDefault(r.Identifiant),
                r.IndiceDePerformance,
                r.Puissance,
                r.Transmission))
            .ToList();

        return new DetailVoiture(voiture, voiture.Marque!.Nom, lignes);
    }

    private static bool Correspond(LigneVoiture ligne, FiltreVoitures filtre)
    {
        var voiture = ligne.Voiture;
        var marque = voiture.Marque!;

        if (filtre.IdentifiantsMarques.Count > 0 && !filtre.IdentifiantsMarques.Contains(marque.Identifiant))
        {
            return false;
        }

        if (filtre.IdentifiantsPays.Count > 0 && !filtre.IdentifiantsPays.Contains(marque.IdentifiantPays))
        {
            return false;
        }

        if (filtre.Divisions.Count > 0 && !filtre.Divisions.Contains(voiture.Division))
        {
            return false;
        }

        if (filtre.AnneeMinimum is { } anneeMinimum && voiture.Annee < anneeMinimum)
        {
            return false;
        }

        if (filtre.AnneeMaximum is { } anneeMaximum && voiture.Annee > anneeMaximum)
        {
            return false;
        }

        if (filtre.SeulementAvecReglagesCrees && ligne.NombreDeReglagesCrees == 0)
        {
            return false;
        }

        if (filtre.SeulementAvecChronos && ligne.NombreDeChronos == 0)
        {
            return false;
        }

        if (filtre.SeulementPersonnalisees && !voiture.EstPersonnalisee)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filtre.Texte)
            && !ComparaisonDeNoms.Contient($"{marque.Nom} {voiture.Nom} {voiture.Annee}", filtre.Texte))
        {
            return false;
        }

        return true;
    }

    private static List<LigneVoiture> Trier(List<LigneVoiture> lignes, CritereDeTriVoitures critere, SensDuTri sens)
    {
        var comparateur = StringComparer.CurrentCultureIgnoreCase;
        var croissant = sens == SensDuTri.Croissant;

        if (critere == CritereDeTriVoitures.MarqueEtNom)
        {
            return (croissant
                    ? lignes.OrderBy(l => l.NomMarque, comparateur).ThenBy(l => l.Voiture.Nom, comparateur).ThenBy(l => l.Voiture.Annee)
                    : lignes.OrderByDescending(l => l.NomMarque, comparateur).ThenByDescending(l => l.Voiture.Nom, comparateur).ThenByDescending(l => l.Voiture.Annee))
                .ToList();
        }

        Func<LigneVoiture, double?> valeur = critere switch
        {
            CritereDeTriVoitures.Annee => l => l.Voiture.Annee,
            CritereDeTriVoitures.NombreDeReglages => l => l.NombreDeReglagesCrees,
            CritereDeTriVoitures.IndiceDePerformance => l => l.IndiceDePerformanceOrigine,
            CritereDeTriVoitures.Puissance => l => l.PuissanceOrigine,
            CritereDeTriVoitures.VitesseMaximale => l => l.VitesseMaximaleOrigine,
            CritereDeTriVoitures.Acceleration => l => l.AccelerationOrigine,
            _ => throw new ArgumentOutOfRangeException(nameof(critere), critere, null)
        };

        var avecValeur = lignes.Where(l => valeur(l) is not null);
        var sansValeur = lignes.Where(l => valeur(l) is null);

        var classees = (croissant
                ? avecValeur.OrderBy(l => valeur(l)!.Value)
                : avecValeur.OrderByDescending(l => valeur(l)!.Value))
            .ThenBy(l => l.NomMarque, comparateur)
            .ThenBy(l => l.Voiture.Nom, comparateur);

        // Sans valeur : toujours à la fin, dans l'ordre marque puis nom, quel que soit le sens.
        var absentes = sansValeur
            .OrderBy(l => l.NomMarque, comparateur)
            .ThenBy(l => l.Voiture.Nom, comparateur);

        return classees.Concat(absentes).ToList();
    }
}
