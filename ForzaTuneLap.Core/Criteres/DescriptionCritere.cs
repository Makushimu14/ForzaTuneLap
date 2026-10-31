using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Criteres;

/// <summary>
/// Décrit un critère numérique d'un réglage, utilisable pour comparer, trier, filtrer et éditer.
/// Le libellé affiché est dans les ressources, clé « Critere_ » + Cle (points remplacés par des « _ »).
/// </summary>
/// <param name="Cle">Identifiant stable, par exemple « Pneus.PressionAvant ».</param>
/// <param name="Lire">Retourne la valeur canonique, ou null si la section n'existe pas pour ce réglage.</param>
/// <param name="Minimum">Borne indicative (aide à la saisie), en unité canonique. Jamais imposée.</param>
/// <param name="Ecrire">
/// Enregistre une valeur canonique. La section doit exister. Vide pour les critères qui ne s'éditent pas
/// un par un (les rapports de boîte numérotés, qui forment une liste de longueur variable).
/// </param>
/// <param name="EstFacultatif">Vrai si le champ peut rester vide (valeur nulle).</param>
public sealed record DescriptionCritere(
    string Cle,
    CategorieDeReglage Categorie,
    TypeDeGrandeur Grandeur,
    SensPrefere Sens,
    Func<Reglage, double?> Lire,
    double? Minimum = null,
    double? Maximum = null,
    Action<Reglage, double?>? Ecrire = null,
    bool EstFacultatif = false)
{
    public string CleLibelle => "Critere_" + Cle.Replace('.', '_');
}
