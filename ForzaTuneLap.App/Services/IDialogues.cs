namespace ForzaTuneLap.App.Services;

/// <summary>Boîtes de dialogue simples. Une interface permet aux modèles de vue de ne pas dépendre des pages.</summary>
public interface IDialogues
{
    /// <summary>Demande un texte. Renvoie null si l'utilisateur annule.</summary>
    Task<string?> DemanderTexteAsync(string titre, string message, string valeurInitiale = "");

    Task<bool> ConfirmerAsync(string titre, string message);

    Task AfficherMessageAsync(string titre, string message);
}
