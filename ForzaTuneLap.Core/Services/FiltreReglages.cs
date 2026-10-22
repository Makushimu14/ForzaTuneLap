using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Services;

public enum OrigineDuReglage
{
    Tous,

    /// <summary>Uniquement les réglages d'origine du jeu.</summary>
    Origine,

    /// <summary>Uniquement les réglages créés par l'utilisateur.</summary>
    Creation
}

/// <summary>
/// Filtre numérique sur un critère du catalogue (voir CatalogueCriteres). Les bornes sont inclusives,
/// exprimées en UNITÉ CANONIQUE : l'interface convertit la saisie de l'utilisateur avec PreferencesUnites.
/// Un réglage dont le critère n'existe pas (section absente) est écarté dès qu'une borne est donnée.
/// </summary>
public sealed record FiltreNumerique(string CleCritere, double? Minimum = null, double? Maximum = null);

/// <summary>
/// Filtres sur les réglages, leur voiture et leur marque : ils servent à la comparaison des réglages
/// et, par héritage, à celle des chronos. Un filtre vide ne retire rien : toutes les conditions renseignées
/// doivent être vraies en même temps. Dans une liste (marques, divisions…), il suffit qu'une valeur corresponde.
/// </summary>
public class FiltreReglages
{
    public HashSet<int> IdentifiantsMarques { get; set; } = new();

    /// <summary>Pays de la marque.</summary>
    public HashSet<int> IdentifiantsPays { get; set; } = new();

    public HashSet<Division> Divisions { get; set; } = new();
    public HashSet<Transmission> Transmissions { get; set; } = new();

    public int? AnneeMinimum { get; set; }
    public int? AnneeMaximum { get; set; }

    public int? IndiceDePerformanceMinimum { get; set; }
    public int? IndiceDePerformanceMaximum { get; set; }

    public OrigineDuReglage Origine { get; set; } = OrigineDuReglage.Tous;

    /// <summary>
    /// Texte recherché dans le nom de la marque, de la voiture ou du réglage
    /// (sans tenir compte de la casse ni des accents, selon la culture courante).
    /// </summary>
    public string? Texte { get; set; }

    /// <summary>Filtres sur n'importe quel critère numérique du catalogue (puissance, poids, pression des pneus…).</summary>
    public List<FiltreNumerique> Criteres { get; set; } = new();
}
