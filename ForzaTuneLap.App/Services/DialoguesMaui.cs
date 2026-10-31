using ForzaTuneLap.App.Traductions;

namespace ForzaTuneLap.App.Services;

public sealed class DialoguesMaui : IDialogues
{
    // La page affichée à l'écran : celle de l'onglet courant dans la coquille.
    private static Page? PageCourante =>
        Application.Current?.Windows.FirstOrDefault()?.Page is Shell coquille
            ? coquille.CurrentPage
            : Application.Current?.Windows.FirstOrDefault()?.Page;

    public async Task<string?> DemanderTexteAsync(string titre, string message, string valeurInitiale = "")
    {
        var page = PageCourante;
        if (page is null)
        {
            return null;
        }

        return await page.DisplayPromptAsync(
            titre,
            message,
            accept: Texte.Obtenir("Dialogue_Valider"),
            cancel: Texte.Obtenir("Dialogue_Annuler"),
            initialValue: valeurInitiale);
    }

    public Task<bool> ConfirmerAsync(string titre, string message) =>
        ConfirmerAsync(titre, message, Texte.Obtenir("Dialogue_Oui"), Texte.Obtenir("Dialogue_Non"));

    public async Task<bool> ConfirmerAsync(string titre, string message, string accepter, string refuser)
    {
        var page = PageCourante;
        return page is not null && await page.DisplayAlertAsync(titre, message, accepter, refuser);
    }

    public async Task<int?> ChoisirAsync(string titre, string annuler, params string[] choix)
    {
        var page = PageCourante;
        if (page is null)
        {
            return null;
        }

        var retenu = await page.DisplayActionSheetAsync(titre, annuler, null, choix);
        var position = Array.IndexOf(choix, retenu);
        return position >= 0 ? position : null;
    }

    public async Task AfficherMessageAsync(string titre, string message)
    {
        var page = PageCourante;
        if (page is not null)
        {
            await page.DisplayAlertAsync(titre, message, Texte.Obtenir("Dialogue_Ok"));
        }
    }
}
