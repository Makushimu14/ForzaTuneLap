namespace ForzaTuneLap.App.Services;

/// <summary>Le partage et le choix de fichiers du système. Une interface isole les spécificités de chaque plateforme.</summary>
public interface IFichiers
{
    /// <summary>Dossier où écrire le fichier à partager (propre à l'application, effacé par le système au besoin).</summary>
    string DossierTemporaire { get; }

    /// <summary>Ouvre le panneau de partage du système pour ce fichier (enregistrer dans Fichiers, un cloud, un message…).</summary>
    Task PartagerAsync(string titre, string cheminFichier);

    /// <summary>Demande à l'utilisateur de choisir un fichier JSON. Renvoie le nom et le flux, ou null s'il annule.</summary>
    Task<(string Nom, Stream Contenu)?> ChoisirFichierJsonAsync(string titre);
}
