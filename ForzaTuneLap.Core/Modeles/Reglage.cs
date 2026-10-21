namespace ForzaTuneLap.Core.Modeles;

/// <summary>
/// Un réglage de voiture. Toutes les valeurs numériques sont stockées dans l'unité canonique de leur grandeur
/// (voir ForzaTuneLap.Core.Unites).
/// Les sections de réglage sont facultatives : une voiture sans aérodynamisme réglable n'a simplement pas
/// de section Aerodynamisme.
/// </summary>
public class Reglage
{
    public Guid Identifiant { get; set; } = Guid.NewGuid();

    public int IdentifiantVoiture { get; set; }
    public Voiture? Voiture { get; set; }

    public string Nom { get; set; } = string.Empty;

    /// <summary>Réglage d'origine fourni avec l'application : lecture seule.</summary>
    public bool EstOrigine { get; set; }

    /// <summary>
    /// Empreinte du contenu du fichier de données ayant produit ce réglage d'origine.
    /// Sert à détecter une correction du fichier. Vide pour les réglages créés par l'utilisateur.
    /// </summary>
    public string? EmpreinteOrigine { get; set; }

    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }

    public ReglagesGeneraux Generaux { get; set; } = new();
    public PerformancesMesurees Performances { get; set; } = new();

    public ReglagePneus? Pneus { get; set; }
    public ReglageAlignement? Alignement { get; set; }
    public ReglageRessorts? Ressorts { get; set; }
    public ReglageBarresAntiroulis? BarresAntiroulis { get; set; }
    public ReglageAmortisseurs? Amortisseurs { get; set; }
    public ReglageGeometrieDeSuspension? GeometrieDeSuspension { get; set; }
    public ReglageDifferentiel? Differentiel { get; set; }
    public ReglageFreins? Freins { get; set; }
    public ReglageAerodynamisme? Aerodynamisme { get; set; }
    public ReglageVolant? Volant { get; set; }

    /// <summary>Rapport de transmission final (couple final). Vide si la voiture ne le permet pas.</summary>
    public double? RapportFinal { get; set; }

    /// <summary>Rapports de boîte de vitesses, numérotés à partir de 1.</summary>
    public List<RapportDeBoite> RapportsDeBoite { get; set; } = new();

    public List<Chrono> Chronos { get; set; } = new();
}

public class RapportDeBoite
{
    /// <summary>Numéro du rapport, à partir de 1.</summary>
    public int Numero { get; set; }
    public double Valeur { get; set; }
}
