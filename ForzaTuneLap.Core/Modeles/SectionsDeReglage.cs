using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Modeles;

// Toutes les valeurs sont en unité canonique : voir TypeDeGrandeur et CatalogueUnites.
// Ces classes servent à la fois de types détenus EF Core (stockés dans la table Reglages)
// et de contenu du fichier de données voitures.json.

public class ReglagesGeneraux
{
    public string Code { get; set; } = string.Empty;
    public string Moteur { get; set; } = string.Empty;
    public Transmission Transmission { get; set; }
    public int IndiceDePerformance { get; set; }

    /// <summary>Puissance en kilowatts.</summary>
    public double Puissance { get; set; }

    /// <summary>Couple en newtons-mètres.</summary>
    public double Couple { get; set; }

    /// <summary>Poids en kilogrammes.</summary>
    public double Poids { get; set; }

    /// <summary>Pourcentage du poids reposant sur l'avant.</summary>
    public double RepartitionPoidsAvant { get; set; }

    // Notes de 0 à 10 affichées par le jeu.
    public double NoteAcceleration { get; set; }
    public double NoteFreinage { get; set; }
    public double NoteTenueDeRoute { get; set; }
    public double NoteVitesse { get; set; }
}

public class PerformancesMesurees
{
    /// <summary>Secondes.</summary>
    public double AccelerationDeZeroA97 { get; set; }
    public double AccelerationDeZeroA161 { get; set; }
    public double Temps400Metres { get; set; }

    /// <summary>Mètres.</summary>
    public double DistanceDeFreinageDe97AZero { get; set; }
    public double DistanceDeFreinageDe161AZero { get; set; }

    /// <summary>Accélération latérale en g.</summary>
    public double AccelerationLateraleA97 { get; set; }
    public double AccelerationLateraleA193 { get; set; }

    /// <summary>Kilomètres par heure.</summary>
    public double VitesseMaximale { get; set; }
}

public class ReglagePneus
{
    /// <summary>Bar.</summary>
    public double PressionAvant { get; set; }
    public double PressionArriere { get; set; }
}

public class ReglageAlignement
{
    /// <summary>Degrés.</summary>
    public double CarrossageAvant { get; set; }
    public double CarrossageArriere { get; set; }
    public double ParallelismeAvant { get; set; }
    public double ParallelismeArriere { get; set; }
    public double AngleDeChasseAvant { get; set; }
}

public class ReglageRessorts
{
    /// <summary>Newtons par millimètre.</summary>
    public double RessortsAvant { get; set; }
    public double RessortsArriere { get; set; }

    /// <summary>Centimètres.</summary>
    public double GardeAuSolAvant { get; set; }
    public double GardeAuSolArriere { get; set; }
}

public class ReglageBarresAntiroulis
{
    public double Avant { get; set; }
    public double Arriere { get; set; }
}

public class ReglageAmortisseurs
{
    public double RigiditeAmortissementAvant { get; set; }
    public double RigiditeAmortissementArriere { get; set; }
    public double RigiditeDetenteAvant { get; set; }
    public double RigiditeDetenteArriere { get; set; }
}

public class ReglageGeometrieDeSuspension
{
    /// <summary>Centimètres.</summary>
    public double HauteurCentreRoulisAvant { get; set; }
    public double HauteurCentreRoulisArriere { get; set; }

    /// <summary>Pourcentage.</summary>
    public double PourcentageAntigeometrieAvant { get; set; }
    public double PourcentageAntigeometrieArriere { get; set; }
}

public class ReglageDifferentiel
{
    /// <summary>Pourcentage.</summary>
    public double Acceleration { get; set; }
    public double Deceleration { get; set; }

    /// <summary>
    /// Répartition centrale pour les transmissions intégrales. À CONFIRMER : à supprimer
    /// si le jeu ne propose pas ce réglage.
    /// </summary>
    public double? EquilibreCentral { get; set; }
}

public class ReglageFreins
{
    /// <summary>Pourcentage.</summary>
    public double PuissanceFreinageEquilibre { get; set; }
    public double PuissanceFreinagePression { get; set; }
}

public class ReglageAerodynamisme
{
    /// <summary>Appui aérodynamique, en kilogrammes-force.</summary>
    public double PortanceNegativeAvant { get; set; }
    public double PortanceNegativeArriere { get; set; }
}

public class ReglageVolant
{
    /// <summary>Degrés.</summary>
    public double AngleBraquage { get; set; }
    public double IntensiteRetourDeForce { get; set; }
}
