using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Donnees;

// ---- Contenu de Referentiel/circuits.json ----

public sealed class FichierCircuits
{
    public List<PaysFichier> Pays { get; set; } = new();
}

public sealed class PaysFichier
{
    public int Identifiant { get; set; }
    public string NomFrancais { get; set; } = string.Empty;
    public string? NomAnglais { get; set; }
    public List<CircuitFichier> Circuits { get; set; } = new();
}

public sealed class CircuitFichier
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public List<ParcoursFichier> Parcours { get; set; } = new();
}

public sealed class ParcoursFichier
{
    public int Identifiant { get; set; }
    public string NomFrancais { get; set; } = string.Empty;
    public string? NomAnglais { get; set; }
    public double LongueurKilometres { get; set; }
}

// ---- Contenu de Referentiel/voitures.json ----

public sealed class FichierVoitures
{
    public List<MarqueFichier> Marques { get; set; } = new();
}

public sealed class MarqueFichier
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;

    /// <summary>Identifiant d'un pays de circuits.json.</summary>
    public int IdentifiantPays { get; set; }

    public List<VoitureFichier> Voitures { get; set; } = new();
}

public sealed class VoitureFichier
{
    public int Identifiant { get; set; }
    public string Nom { get; set; } = string.Empty;
    public int Annee { get; set; }
    public Division Division { get; set; }
    public int Valeur { get; set; }
    public bool Achetable { get; set; } = true;
    public bool ContenuTelechargeable { get; set; }

    /// <summary>Réglage d'origine. Facultatif tant que la saisie n'est pas faite.</summary>
    public ReglageOrigineFichier? ReglageOrigine { get; set; }
}

public sealed class ReglageOrigineFichier
{
    /// <summary>Identifiant fixe, propre à ce réglage. Ne jamais le modifier une fois attribué.</summary>
    public Guid Identifiant { get; set; }

    public string Nom { get; set; } = "Origine";

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

    public double? RapportFinal { get; set; }

    /// <summary>Valeurs des rapports de boîte, du premier au dernier.</summary>
    public List<double> RapportsDeBoite { get; set; } = new();

    public Reglage VersReglage(int identifiantVoiture, string empreinte, DateTime maintenant) => new()
    {
        Identifiant = Identifiant,
        IdentifiantVoiture = identifiantVoiture,
        Nom = Nom,
        EstOrigine = true,
        EmpreinteOrigine = empreinte,
        DateCreation = maintenant,
        DateModification = maintenant,
        Generaux = Generaux,
        Performances = Performances,
        Pneus = Pneus,
        Alignement = Alignement,
        Ressorts = Ressorts,
        BarresAntiroulis = BarresAntiroulis,
        Amortisseurs = Amortisseurs,
        GeometrieDeSuspension = GeometrieDeSuspension,
        Differentiel = Differentiel,
        Freins = Freins,
        Aerodynamisme = Aerodynamisme,
        Volant = Volant,
        RapportFinal = RapportFinal,
        RapportsDeBoite = RapportsDeBoite
            .Select((valeur, index) => new RapportDeBoite { Numero = index + 1, Valeur = valeur })
            .ToList()
    };
}

/// <summary>Sérialisation JSON générée à la compilation (pas de réflexion : compatible iOS et Mac Catalyst).</summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(FichierCircuits))]
[JsonSerializable(typeof(FichierVoitures))]
[JsonSerializable(typeof(ReglageOrigineFichier))]
internal partial class ContexteJsonReferentiel : JsonSerializerContext
{
}

/// <summary>Lecture des fichiers de données embarqués dans l'assembly.</summary>
public static class ChargeurReferentiel
{
    private const string NomRessourceCircuits = "ForzaTuneLap.Core.Referentiel.circuits.json";
    private const string NomRessourceVoitures = "ForzaTuneLap.Core.Referentiel.voitures.json";

    public static FichierCircuits LireCircuits() =>
        Lire(NomRessourceCircuits, ContexteJsonReferentiel.Default.FichierCircuits);

    public static FichierVoitures LireVoitures() =>
        Lire(NomRessourceVoitures, ContexteJsonReferentiel.Default.FichierVoitures);

    /// <summary>Empreinte stable du contenu d'un réglage d'origine, pour détecter une correction du fichier.</summary>
    internal static string CalculerEmpreinte(ReglageOrigineFichier reglage)
    {
        var json = JsonSerializer.Serialize(reglage, ContexteJsonReferentiel.Default.ReglageOrigineFichier);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    private static T Lire<T>(string nomRessource, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        var assembly = typeof(ChargeurReferentiel).Assembly;
        using var flux = assembly.GetManifestResourceStream(nomRessource)
            ?? throw new InvalidOperationException($"Ressource embarquée introuvable : {nomRessource}");

        return JsonSerializer.Deserialize(flux, typeInfo)
            ?? throw new InvalidOperationException($"Fichier vide ou invalide : {nomRessource}");
    }
}
