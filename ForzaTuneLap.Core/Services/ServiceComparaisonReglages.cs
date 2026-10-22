using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Services;

public enum SensDuTri
{
    Croissant,
    Decroissant
}

/// <summary>Une ligne du tableau comparatif : un réglage, sa voiture et ses valeurs.</summary>
/// <param name="Rang">
/// Rang selon le critère de tri (1 pour la première ligne, rang « à la compétition » en cas d'égalité).
/// Vide si le réglage n'a pas de valeur pour ce critère : il est alors classé en dernier.
/// </param>
/// <param name="Valeurs">Valeur canonique de chaque critère affiché, ou vide si la section n'existe pas pour ce réglage.</param>
/// <param name="Ecart">Différence de la valeur de tri avec celle de la première ligne (négatif en tri décroissant).</param>
/// <param name="EcartPourcentage">Même différence, en pourcentage de la valeur de la première ligne (vide si elle vaut zéro).</param>
public sealed record LigneComparaisonReglage(
    int? Rang,
    Reglage Reglage,
    Voiture Voiture,
    IReadOnlyDictionary<string, double?> Valeurs,
    double? Ecart,
    double? EcartPourcentage);

/// <summary>Résumé d'un critère sur l'ensemble des réglages retenus.</summary>
public sealed record StatistiquesCritere(
    string CleCritere,
    int NombreDeValeurs,
    double? Minimum,
    double? Maximum,
    double? Moyenne,
    double? Mediane);

public sealed record ResultatComparaisonReglages(
    DescriptionCritere CritereDeTri,
    SensDuTri Sens,
    IReadOnlyList<DescriptionCritere> CriteresAffiches,
    IReadOnlyList<LigneComparaisonReglage> Lignes,
    IReadOnlyList<StatistiquesCritere> Statistiques);

/// <summary>
/// Compare tous les réglages qui respectent les filtres choisis (réglages d'origine et de création, toutes voitures),
/// sur n'importe quel critère du catalogue. Les valeurs sont en unité canonique : l'interface les convertit
/// avec PreferencesUnites. Les filtres sont appliqués en mémoire, ce qui permet de filtrer sur tous les critères.
/// </summary>
public sealed class ServiceComparaisonReglages
{
    private readonly ContexteDonnees contexte;

    public ServiceComparaisonReglages(ContexteDonnees contexte)
    {
        this.contexte = contexte;
    }

    /// <param name="cleCritereDeTri">Critère servant au classement (voir CatalogueCriteres).</param>
    /// <param name="filtre">Filtres sur les voitures et les réglages. Vide ou nul : tous les réglages.</param>
    /// <param name="clesAffichees">Critères dont on veut les valeurs. Nul : tous les critères du catalogue.</param>
    /// <param name="sens">
    /// Sens du tri. Par défaut, le meilleur d'abord quand le critère a un sens (le plus petit temps, la plus grande vitesse),
    /// et le plus grand d'abord sinon.
    /// </param>
    public async Task<ResultatComparaisonReglages> ComparerAsync(
        string cleCritereDeTri,
        FiltreReglages? filtre = null,
        IReadOnlyCollection<string>? clesAffichees = null,
        SensDuTri? sens = null,
        CancellationToken annulation = default)
    {
        var critereDeTri = CatalogueCriteres.Rechercher(cleCritereDeTri)
            ?? throw new ErreurMetierException(CodeErreurMetier.CritereInconnu);
        var criteresAffiches = ResoudreCriteresAffiches(critereDeTri, clesAffichees);
        var sensDuTri = sens ?? (critereDeTri.Sens == SensPrefere.PlusPetitEstMeilleur ? SensDuTri.Croissant : SensDuTri.Decroissant);
        var criteresDuFiltre = ApplicationFiltre.ResoudreCriteres(filtre);

        var reglages = await contexte.Reglages.AsNoTracking()
            .Include(r => r.Voiture).ThenInclude(v => v!.Marque)
            .ToListAsync(annulation);

        var lignesBrutes = reglages
            .Where(r => filtre is null || ApplicationFiltre.Correspond(r, filtre, criteresDuFiltre))
            .Select(r => (Reglage: r, Valeurs: LireValeurs(r, criteresAffiches), ValeurDeTri: critereDeTri.Lire(r)))
            .ToList();

        var avecValeur = lignesBrutes.Where(l => l.ValeurDeTri is not null);
        var sansValeur = lignesBrutes.Where(l => l.ValeurDeTri is null);

        var classes = (sensDuTri == SensDuTri.Croissant
                ? avecValeur.OrderBy(l => l.ValeurDeTri!.Value)
                : avecValeur.OrderByDescending(l => l.ValeurDeTri!.Value))
            .ThenBy(l => l.Reglage.Voiture!.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(l => l.Reglage.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var absents = sansValeur
            .OrderBy(l => l.Reglage.Voiture!.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(l => l.Reglage.Nom, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var valeurDeReference = classes.Count > 0 ? classes[0].ValeurDeTri!.Value : 0;
        var lignes = new List<LigneComparaisonReglage>(lignesBrutes.Count);

        foreach (var ligne in classes)
        {
            var valeur = ligne.ValeurDeTri!.Value;

            // Rang « à la compétition » : on compte les lignes strictement meilleures selon le sens du tri.
            var rang = 1 + classes.Count(autre => sensDuTri == SensDuTri.Croissant
                ? autre.ValeurDeTri!.Value < valeur
                : autre.ValeurDeTri!.Value > valeur);

            var ecart = valeur - valeurDeReference;
            double? pourcentage = valeurDeReference != 0 ? ecart * 100.0 / Math.Abs(valeurDeReference) : null;

            lignes.Add(new LigneComparaisonReglage(rang, ligne.Reglage, ligne.Reglage.Voiture!, ligne.Valeurs, ecart, pourcentage));
        }

        foreach (var ligne in absents)
        {
            lignes.Add(new LigneComparaisonReglage(null, ligne.Reglage, ligne.Reglage.Voiture!, ligne.Valeurs, null, null));
        }

        var statistiques = criteresAffiches
            .Select(critere => CalculerStatistiques(critere, lignesBrutes.Select(l => l.Valeurs[critere.Cle])))
            .ToList();

        return new ResultatComparaisonReglages(critereDeTri, sensDuTri, criteresAffiches, lignes, statistiques);
    }

    /// <summary>Le critère de tri est toujours affiché en premier, sans doublon.</summary>
    private static List<DescriptionCritere> ResoudreCriteresAffiches(
        DescriptionCritere critereDeTri,
        IReadOnlyCollection<string>? clesAffichees)
    {
        if (clesAffichees is null)
        {
            return CatalogueCriteres.Tous.ToList();
        }

        var criteres = new List<DescriptionCritere> { critereDeTri };
        foreach (var cle in clesAffichees)
        {
            var critere = CatalogueCriteres.Rechercher(cle)
                ?? throw new ErreurMetierException(CodeErreurMetier.CritereInconnu);

            if (criteres.All(existant => existant.Cle != critere.Cle))
            {
                criteres.Add(critere);
            }
        }

        return criteres;
    }

    private static Dictionary<string, double?> LireValeurs(Reglage reglage, List<DescriptionCritere> criteres)
    {
        var valeurs = new Dictionary<string, double?>(criteres.Count);
        foreach (var critere in criteres)
        {
            valeurs[critere.Cle] = critere.Lire(reglage);
        }

        return valeurs;
    }

    private static StatistiquesCritere CalculerStatistiques(DescriptionCritere critere, IEnumerable<double?> valeurs)
    {
        var triees = valeurs.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).ToList();
        if (triees.Count == 0)
        {
            return new StatistiquesCritere(critere.Cle, 0, null, null, null, null);
        }

        var milieu = triees.Count / 2;
        var mediane = triees.Count % 2 == 1 ? triees[milieu] : (triees[milieu - 1] + triees[milieu]) / 2;

        return new StatistiquesCritere(critere.Cle, triees.Count, triees[0], triees[^1], triees.Average(), mediane);
    }
}
