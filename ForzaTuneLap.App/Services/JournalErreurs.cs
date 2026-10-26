using System.Diagnostics;

namespace ForzaTuneLap.App.Services;

/// <summary>
/// Enregistre les erreurs inattendues : dans la fenêtre Sortie du débogueur et dans un fichier texte du dossier temporaire
/// (forzatunelap-erreurs.log). Sert surtout au diagnostic quand l'application s'arrête sans message.
/// </summary>
public static class JournalErreurs
{
    public static string CheminDuFichier { get; } = Path.Combine(Path.GetTempPath(), "forzatunelap-erreurs.log");

    public static void Ecrire(Exception? exception, string? contexte = null)
    {
        var texte = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {contexte}{Environment.NewLine}{exception?.ToString() ?? "(exception inconnue)"}{Environment.NewLine}{Environment.NewLine}";

        Debug.WriteLine(texte);

        try
        {
            File.AppendAllText(CheminDuFichier, texte);
        }
        catch
        {
            // Le journal ne doit jamais provoquer lui-même une erreur.
        }
    }
}
