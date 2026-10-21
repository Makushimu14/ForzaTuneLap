namespace ForzaTuneLap.Core.Unites;

/// <summary>
/// Unité de mesure. FacteurVersCanonique est le nombre d'unités canoniques contenues dans une unité
/// (ex. 1 livre = 0,45359237 kilogramme). Toutes les conversions de ce projet sont linéaires.
/// </summary>
public sealed record Unite(string Cle, string Symbole, double FacteurVersCanonique)
{
    public double VersCanonique(double valeur) => valeur * FacteurVersCanonique;

    public double DepuisCanonique(double valeurCanonique) => valeurCanonique / FacteurVersCanonique;
}
