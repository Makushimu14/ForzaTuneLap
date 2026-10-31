using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Noms des données du référentiel dans la langue de l'interface.</summary>
public static class NomsAffiches
{
    /// <summary>Le nom anglais du parcours quand l'interface est en anglais et qu'il existe, sinon le nom français.</summary>
    public static string Parcours(Parcours parcours) =>
        System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en"
        && !string.IsNullOrWhiteSpace(parcours.NomAnglais)
            ? parcours.NomAnglais
            : parcours.NomFrancais;
}
