using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Unites;

/// <summary>
/// Unités disponibles pour chaque grandeur. La première unité de chaque liste est l'unité canonique,
/// celle dans laquelle les valeurs sont enregistrées en base.
/// </summary>
public static class CatalogueUnites
{
    private static readonly Dictionary<TypeDeGrandeur, IReadOnlyList<Unite>> Unites = new()
    {
        [TypeDeGrandeur.SansUnite] = new[] { new Unite("SansUnite", "", 1) },
        [TypeDeGrandeur.Pourcentage] = new[] { new Unite("Pourcentage", "%", 1) },
        [TypeDeGrandeur.Masse] = new[]
        {
            new Unite("Kilogramme", "kg", 1),
            new Unite("Livre", "lb", 0.45359237)
        },
        [TypeDeGrandeur.Puissance] = new[]
        {
            new Unite("Kilowatt", "kW", 1),
            new Unite("ChevalVapeur", "ch", 0.73549875),
            new Unite("ChevalMecanique", "hp", 0.745699872)
        },
        [TypeDeGrandeur.Couple] = new[]
        {
            new Unite("NewtonMetre", "N·m", 1),
            new Unite("LivrePied", "lb·ft", 1.3558179483314004),
            new Unite("KilogrammeForceMetre", "kgf·m", 9.80665)
        },
        [TypeDeGrandeur.Vitesse] = new[]
        {
            new Unite("KilometreParHeure", "km/h", 1),
            new Unite("MilleParHeure", "mph", 1.609344)
        },
        [TypeDeGrandeur.Distance] = new[]
        {
            new Unite("Metre", "m", 1),
            new Unite("Pied", "ft", 0.3048)
        },
        [TypeDeGrandeur.DistanceLongue] = new[]
        {
            new Unite("Kilometre", "km", 1),
            new Unite("Mille", "mi", 1.609344)
        },
        [TypeDeGrandeur.Hauteur] = new[]
        {
            new Unite("Centimetre", "cm", 1),
            new Unite("Pouce", "in", 2.54),
            new Unite("Millimetre", "mm", 0.1)
        },
        [TypeDeGrandeur.Pression] = new[]
        {
            new Unite("Bar", "bar", 1),
            new Unite("LivreParPouceCarre", "psi", 0.0689475729),
            new Unite("Kilopascal", "kPa", 0.01)
        },
        [TypeDeGrandeur.RaideurDeRessort] = new[]
        {
            new Unite("NewtonParMillimetre", "N/mm", 1),
            new Unite("KilogrammeForceParMillimetre", "kgf/mm", 9.80665),
            new Unite("LivreParPouce", "lb/in", 0.175126835)
        },
        [TypeDeGrandeur.Force] = new[]
        {
            new Unite("KilogrammeForce", "kgf", 1),
            new Unite("LivreForce", "lbf", 0.45359237),
            new Unite("Newton", "N", 0.101971621)
        },
        [TypeDeGrandeur.Angle] = new[] { new Unite("Degre", "°", 1) },
        [TypeDeGrandeur.Duree] = new[] { new Unite("Seconde", "s", 1) },
        [TypeDeGrandeur.AccelerationLaterale] = new[] { new Unite("ForceG", "g", 1) }
    };

    public static IReadOnlyList<Unite> ObtenirUnites(TypeDeGrandeur grandeur) => Unites[grandeur];

    public static Unite ObtenirCanonique(TypeDeGrandeur grandeur) => Unites[grandeur][0];

    public static Unite Obtenir(TypeDeGrandeur grandeur, string cleUnite) =>
        Unites[grandeur].FirstOrDefault(unite => unite.Cle == cleUnite) ?? ObtenirCanonique(grandeur);
}
