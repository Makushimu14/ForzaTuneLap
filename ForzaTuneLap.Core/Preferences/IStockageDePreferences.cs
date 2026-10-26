namespace ForzaTuneLap.Core.Preferences;

/// <summary>
/// Stockage clé-valeur des préférences. Implémenté par l'application avec le mécanisme de chaque plateforme
/// (Preferences de .NET MAUI), et en mémoire dans les tests.
/// </summary>
public interface IStockageDePreferences
{
    string? Lire(string cle);

    void Ecrire(string cle, string valeur);

    void Supprimer(string cle);
}
