namespace ForzaTuneLap.App.Services;

/// <summary>Les claviers à choisir selon la plateforme.</summary>
public static class Claviers
{
    /// <summary>
    /// Clavier pour un champ numérique qui peut être négatif. Le clavier numérique d'iOS et de macOS n'a pas de signe
    /// « moins » : on y utilise le clavier normal. Ailleurs, le clavier numérique a le signe.
    /// </summary>
    public static Keyboard PourValeursNegatives =>
        DeviceInfo.Platform == DevicePlatform.iOS || DeviceInfo.Platform == DevicePlatform.MacCatalyst
            ? Keyboard.Default
            : Keyboard.Numeric;
}
