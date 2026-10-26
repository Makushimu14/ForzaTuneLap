using ForzaTuneLap.Core.Preferences;

namespace ForzaTuneLap.App.Services;

/// <summary>Préférences enregistrées avec le mécanisme de chaque plateforme (même code partout).</summary>
public sealed class StockageDePreferencesMaui : IStockageDePreferences
{
    public string? Lire(string cle) =>
        Preferences.Default.ContainsKey(cle) ? Preferences.Default.Get<string?>(cle, null) : null;

    public void Ecrire(string cle, string valeur) => Preferences.Default.Set(cle, valeur);

    public void Supprimer(string cle) => Preferences.Default.Remove(cle);
}
