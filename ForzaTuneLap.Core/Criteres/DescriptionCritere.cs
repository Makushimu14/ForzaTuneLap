using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Criteres;

/// <summary>
/// Décrit un critère numérique d'un réglage, utilisable pour comparer, trier et filtrer.
/// Le libellé affiché est dans les ressources, clé « Critere_ » + Cle (points remplacés par des « _ »).
/// </summary>
/// <param name="Cle">Identifiant stable, par exemple « Pneus.PressionAvant ».</param>
/// <param name="Lire">Retourne la valeur canonique, ou null si la section n'existe pas pour ce réglage.</param>
/// <param name="Minimum">Borne indicative (aide à la saisie), en unité canonique. Jamais imposée.</param>
public sealed record DescriptionCritere(
    string Cle,
    CategorieDeReglage Categorie,
    TypeDeGrandeur Grandeur,
    SensPrefere Sens,
    Func<Reglage, double?> Lire,
    double? Minimum = null,
    double? Maximum = null)
{
    public string CleLibelle => "Critere_" + Cle.Replace('.', '_');
}
