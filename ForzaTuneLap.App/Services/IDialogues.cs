namespace ForzaTuneLap.App.Services;

/// <summary>Boîtes de dialogue simples. Une interface permet aux modèles de vue de ne pas dépendre des pages.</summary>
public interface IDialogues
{
    /// <summary>Demande un texte. Renvoie null si l'utilisateur annule.</summary>
    Task<string?> DemanderTexteAsync(string titre, string message, string valeurInitiale = "");

    Task<bool> ConfirmerAsync(string titre, string message);

    /// <summary>Même chose avec les libellés des deux boutons (par exemple « Fusionner » et « Plus tard »).</summary>
    Task<bool> ConfirmerAsync(string titre, string message, string accepter, string refuser);

    Task AfficherMessageAsync(string titre, string message);

    /// <summary>Propose une liste de choix. Renvoie la position du choix retenu, ou null si l'utilisateur annule.</summary>
    Task<int?> ChoisirAsync(string titre, string annuler, params string[] choix);
}
