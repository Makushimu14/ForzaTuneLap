namespace ForzaTuneLap.Core.Enumerations;

/// <summary>
/// Nature physique d'une valeur. Détermine les unités proposées à l'utilisateur.
/// En base, chaque valeur est stockée dans l'unité canonique de sa grandeur (la première de CatalogueUnites).
/// </summary>
public enum TypeDeGrandeur
{
    SansUnite,
    Pourcentage,
    Masse,
    Puissance,
    Couple,
    Vitesse,
    Distance,
    DistanceLongue,
    Hauteur,
    Pression,
    RaideurDeRessort,
    Force,
    Angle,
    Duree,
    AccelerationLaterale
}
