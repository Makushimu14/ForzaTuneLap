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

    public async Task<bool> ConfirmerAsync(string titre, string message)
    {
        var page = PageCourante;
        return page is not null
            && await page.DisplayAlertAsync(titre, message, Texte.Obtenir("Dialogue_Oui"), Texte.Obtenir("Dialogue_Non"));
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
