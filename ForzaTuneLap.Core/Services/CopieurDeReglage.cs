using System.Text.Json;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Copie et comparaison de réglages, en passant par le contenu de fichier déjà décrit pour le JSON
/// (sérialisation générée à la compilation, sans réflexion).
/// </summary>
internal static class CopieurDeReglage
{
    /// <summary>
    /// Copie indépendante d'un réglage : nouvel identifiant, jamais « d'origine », sans chrono.
    /// Les sections absentes de la source restent absentes.
    /// </summary>
    public static Reglage CopierProfondement(Reglage source)
    {
        var json = JsonSerializer.Serialize(
            ReglageOrigineFichier.DepuisReglage(source),
            ContexteJsonReferentiel.Default.ReglageOrigineFichier);

        var contenu = JsonSerializer.Deserialize(json, ContexteJsonReferentiel.Default.ReglageOrigineFichier)
            ?? throw new InvalidOperationException("Copie du réglage impossible.");

        var copie = contenu.VersReglage(source.IdentifiantVoiture, string.Empty, DateTime.UtcNow);
        copie.Identifiant = Guid.NewGuid();
        copie.Nom = source.Nom;
        copie.EstOrigine = false;
        copie.EmpreinteOrigine = null;
        return copie;
    }

    /// <summary>
    /// Représentation du contenu d'un réglage sans son identité (identifiant, nom, dates) :
    /// deux réglages ont le même contenu si ces textes sont égaux.
    /// </summary>
    public static string CalculerContenu(Reglage reglage)
    {
        var contenu = ReglageOrigineFichier.DepuisReglage(reglage);
        contenu.Identifiant = Guid.Empty;
        contenu.Nom = string.Empty;
        return JsonSerializer.Serialize(contenu, ContexteJsonReferentiel.Default.ReglageOrigineFichier);
    }
}
