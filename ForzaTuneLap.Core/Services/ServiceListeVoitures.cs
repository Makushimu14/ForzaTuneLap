using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

public enum TriVoitures
{
    MarqueEtNom,
    AnneeCroissante,
    AnneeDecroissante,
    NombreDeReglagesDecroissant
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

public sealed record LigneVoiture(
    Voiture Voiture,
    string NomMarque,
    int NombreDeReglagesCrees,
    int NombreDeChronos,
    bool ADesReglagesOrigine)
{
    public int Identifiant => Voiture.Identifiant;
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

    public async Task<IReadOnlyList<LigneVoiture>> ListerAsync(
        FiltreVoitures? filtre = null,
        TriVoitures tri = TriVoitures.MarqueEtNom,
        CancellationToken annulation = default)
    {
        var voitures = await contexte.Voitures.AsNoTracking().Include(v => v.Marque).ToListAsync(annulation);

        var reglagesCrees = (await contexte.Reglages.AsNoTracking()
                .Where(r => !r.EstOrigine)
                .GroupBy(r => r.IdentifiantVoiture)
                .Select(g => new { Identifiant = g.Key, Nombre = g.Count() })
                .ToListAsync(annulation))
            .ToDictionary(x => x.Identifiant, x => x.Nombre);

        var voituresAvecOrigine = (await contexte.Reglages.AsNoTracking()
                .Where(r => r.EstOrigine)
                .Select(r => r.IdentifiantVoiture)
                .Distinct()
                .ToListAsync(annulation))
            .ToHashSet();

        var chronos = (await contexte.Chronos.AsNoTracking()
                .GroupBy(c => c.Reglage!.IdentifiantVoiture)
                .Select(g => new { Identifiant = g.Key, Nombre = g.Count() })
                .ToListAsync(annulation))
            .ToDictionary(x => x.Identifiant, x => x.Nombre);

        var lignes = voitures
            .Select(v => new LigneVoiture(
                v,
                v.Marque!.Nom,
                reglagesCrees.GetValueOrDefault(v.Identifiant),
                chronos.GetValueOrDefault(v.Identifiant),
                voituresAvecOrigine.Contains(v.Identifiant)))
            .Where(ligne => filtre is null || Correspond(ligne, filtre))
            .ToList();

        return Trier(lignes, tri);
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

    private static List<LigneVoiture> Trier(List<LigneVoiture> lignes, TriVoitures tri)
    {
        var comparateur = StringComparer.CurrentCultureIgnoreCase;

        return tri switch
        {
            TriVoitures.AnneeCroissante => lignes
                .OrderBy(l => l.Voiture.Annee)
                .ThenBy(l => l.NomMarque, comparateur)
                .ThenBy(l => l.Voiture.Nom, comparateur)
                .ToList(),
            TriVoitures.AnneeDecroissante => lignes
                .OrderByDescending(l => l.Voiture.Annee)
                .ThenBy(l => l.NomMarque, comparateur)
                .ThenBy(l => l.Voiture.Nom, comparateur)
                .ToList(),
            TriVoitures.NombreDeReglagesDecroissant => lignes
                .OrderByDescending(l => l.NombreDeReglagesCrees)
                .ThenBy(l => l.NomMarque, comparateur)
                .ThenBy(l => l.Voiture.Nom, comparateur)
                .ToList(),
            _ => lignes
                .OrderBy(l => l.NomMarque, comparateur)
                .ThenBy(l => l.Voiture.Nom, comparateur)
                .ThenBy(l => l.Voiture.Annee)
                .ToList()
        };
    }
}
