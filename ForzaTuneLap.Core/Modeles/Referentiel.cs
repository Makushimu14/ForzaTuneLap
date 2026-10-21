using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Modeles;

/// <summary>Constantes liées aux identifiants du référentiel.</summary>
public static class ConstantesReferentiel
{
    /// <summary>
    /// Les éléments du fichier de données ont un identifiant inférieur à cette valeur.
    /// Les voitures et marques ajoutées par l'utilisateur reçoivent un identifiant supérieur ou égal,
    /// pour ne jamais entrer en collision avec une entrée ajoutée plus tard au fichier.
    /// </summary>
    public const int PremierIdentifiantPersonnalise = 100_000;
}

public class Pays
{
    public int Identifiant { get; set; }
    public string NomFrancais { get; set; } = string.Empty;
    public string? NomAnglais { get; set; }

    public List<Circuit> Circuits { get; set; } = new();
    public List<Marque> Marques { get; set; } = new();
}

public class Circuit
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;

    public int IdentifiantPays { get; set; }
    public Pays? Pays { get; set; }

    public List<Parcours> Parcours { get; set; } = new();
}

/// <summary>Variante d'un circuit (ce que le jeu appelle un tracé). Les chronos sont rattachés à un parcours.</summary>
public class Parcours
{
    public int Identifiant { get; set; }
    public string NomFrancais { get; set; } = string.Empty;
    public string? NomAnglais { get; set; }

    /// <summary>Longueur en kilomètres (unité canonique de la grandeur DistanceLongue).</summary>
    public double LongueurKilometres { get; set; }

    public int IdentifiantCircuit { get; set; }
    public Circuit? Circuit { get; set; }
}

public class Marque
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;

    public int IdentifiantPays { get; set; }
    public Pays? Pays { get; set; }

    public bool EstPersonnalisee { get; set; }

    public List<Voiture> Voitures { get; set; } = new();
}

public class Voiture
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Annee { get; set; }
    public Division Division { get; set; }

    /// <summary>Prix en crédits du jeu.</summary>
    public int Valeur { get; set; }

    public bool Achetable { get; set; } = true;
    public bool ContenuTelechargeable { get; set; }

    /// <summary>Vrai si la voiture a été ajoutée par l'utilisateur et ne vient pas du fichier de données.</summary>
    public bool EstPersonnalisee { get; set; }

    public int IdentifiantMarque { get; set; }
    public Marque? Marque { get; set; }

    public List<Reglage> Reglages { get; set; } = new();

    /// <summary>Calculé, non enregistré en base.</summary>
    public string NomComplet => $"{Annee} {Marque?.Nom} {Nom}".Replace("  ", " ");
}
