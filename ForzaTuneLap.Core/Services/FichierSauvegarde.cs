using System.Text.Json;
using System.Text.Json.Serialization;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Contenu du fichier d'export/import. Il ne contient que les données de l'utilisateur : le catalogue officiel
/// (voitures, circuits, réglages d'origine) vient de l'application elle-même.
/// Les identifiants des voitures et marques personnalisées sont propres à chaque appareil : elles sont donc désignées
/// par leur marque, leur nom et leur année. Les réglages et chronos ont des identifiants uniques (GUID) valables partout.
/// </summary>
public sealed class FichierSauvegarde
{
    /// <summary>Version du format. Un fichier d'une version plus récente que celle de l'application est refusé.</summary>
    public const int VersionActuelle = 1;

    public int Version { get; set; } = VersionActuelle;

    public DateTime DateExport { get; set; }

    public List<MarqueSauvegarde> MarquesPersonnalisees { get; set; } = new();
    public List<VoitureSauvegarde> VoituresPersonnalisees { get; set; } = new();

    /// <summary>Réglages créés par l'utilisateur (les réglages d'origine ne sont pas exportés).</summary>
    public List<ReglageSauvegarde> Reglages { get; set; } = new();

    /// <summary>Tous les chronos, y compris ceux des réglages d'origine.</summary>
    public List<ChronoSauvegarde> Chronos { get; set; } = new();
}

public sealed class MarqueSauvegarde
{
    public string Nom { get; set; } = string.Empty;
    public int IdentifiantPays { get; set; }
}

public sealed class VoitureSauvegarde
{
    public string NomMarque { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public int Annee { get; set; }
    public Division Division { get; set; }
    public int Valeur { get; set; }
    public bool ContenuTelechargeable { get; set; }
}

/// <summary>Désigne une voiture de façon valable d'un appareil à l'autre.</summary>
public sealed class ReferenceVoiture
{
    /// <summary>Identifiant d'une voiture officielle (stable, fourni par le catalogue). Vide pour une voiture personnalisée.</summary>
    public int? IdentifiantOfficiel { get; set; }

    public string NomMarque { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public int Annee { get; set; }
}

public sealed class ReglageSauvegarde
{
    public ReferenceVoiture Voiture { get; set; } = new();
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }

    /// <summary>Identifiant, nom et valeurs du réglage.</summary>
    public ReglageOrigineFichier Contenu { get; set; } = new();
}

public sealed class ChronoSauvegarde
{
    public Guid Identifiant { get; set; }
    public Guid IdentifiantReglage { get; set; }
    public int IdentifiantParcours { get; set; }
    public long DureeMillisecondes { get; set; }
    public DateTime DateRealisation { get; set; }
    public DateTime DateModification { get; set; }
    public string? Commentaire { get; set; }
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    WriteIndented = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(FichierSauvegarde))]
internal partial class ContexteJsonSauvegarde : JsonSerializerContext
{
}
