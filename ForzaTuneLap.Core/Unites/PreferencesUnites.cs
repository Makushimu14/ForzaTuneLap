using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Unites;

/// <summary>
/// Unité choisie par l'utilisateur pour chaque grandeur. Une grandeur absente utilise l'unité canonique.
/// La persistance (stockage des préférences de l'application) est du ressort de l'interface :
/// Choix est un simple dictionnaire de chaînes, facile à enregistrer.
/// </summary>
public sealed class PreferencesUnites
{
    /// <summary>Grandeur -> clé de l'unité choisie.</summary>
    public Dictionary<TypeDeGrandeur, string> Choix { get; set; } = new();

    public static PreferencesUnites Metriques() => new();

    public static PreferencesUnites Imperiales() => new()
    {
        Choix =
        {
            [TypeDeGrandeur.Masse] = "Livre",
            [TypeDeGrandeur.Puissance] = "ChevalMecanique",
            [TypeDeGrandeur.Couple] = "LivrePied",
            [TypeDeGrandeur.Vitesse] = "MilleParHeure",
            [TypeDeGrandeur.Distance] = "Pied",
            [TypeDeGrandeur.DistanceLongue] = "Mille",
            [TypeDeGrandeur.Hauteur] = "Pouce",
            [TypeDeGrandeur.Pression] = "LivreParPouceCarre",
            [TypeDeGrandeur.RaideurDeRessort] = "LivreParPouce",
            [TypeDeGrandeur.Force] = "LivreForce"
        }
    };

    public Unite ObtenirUnite(TypeDeGrandeur grandeur) =>
        Choix.TryGetValue(grandeur, out var cle)
            ? CatalogueUnites.Obtenir(grandeur, cle)
            : CatalogueUnites.ObtenirCanonique(grandeur);

    /// <summary>Convertit une valeur canonique vers l'unité choisie, pour l'affichage.</summary>
    public double VersAffichage(TypeDeGrandeur grandeur, double valeurCanonique) =>
        ObtenirUnite(grandeur).DepuisCanonique(valeurCanonique);

    /// <summary>Convertit une valeur saisie dans l'unité choisie vers l'unité canonique, pour l'enregistrement.</summary>
    public double VersCanonique(TypeDeGrandeur grandeur, double valeurSaisie) =>
        ObtenirUnite(grandeur).VersCanonique(valeurSaisie);
}
