using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.Services;

/// <summary>Traduit une exception en message affichable. Les services du Core ne renvoient que des codes, jamais de texte.</summary>
public static class MessagesErreurs
{
    public static string Obtenir(Exception exception) =>
        exception is ErreurMetierException erreur
            ? Texte.Obtenir($"Erreur_{erreur.Code}")
            : Texte.Obtenir("Erreur_Inconnue");
}
