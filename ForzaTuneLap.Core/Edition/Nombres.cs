using System.Globalization;

namespace ForzaTuneLap.Core.Edition;

/// <summary>Lecture et affichage des nombres saisis par l'utilisateur.</summary>
public static class Nombres
{
    /// <summary>
    /// Lit un nombre en acceptant la virgule comme le point pour les décimales, quelle que soit la langue :
    /// « 2,5 » et « 2.5 » valent 2,5. Les espaces sont ignorés (« 1 234,5 »). Quand les deux séparateurs sont présents,
    /// le dernier est le séparateur décimal (« 1,234.5 » et « 1.234,5 » valent 1234,5). Un séparateur seul est toujours
    /// décimal : « 1,234 » vaut donc 1,234 et non 1234.
    /// </summary>
    public static bool TryParse(string? texte, out double valeur)
    {
        valeur = 0;
        if (string.IsNullOrWhiteSpace(texte))
        {
            return false;
        }

        var normalise = texte.Trim()
            .Replace("\u00A0", string.Empty)
            .Replace("\u202F", string.Empty)
            .Replace(" ", string.Empty);

        var virgule = normalise.LastIndexOf(',');
        var point = normalise.LastIndexOf('.');

        if (virgule >= 0 && point >= 0)
        {
            var decimale = virgule > point ? ',' : '.';
            var milliers = decimale == ',' ? '.' : ',';
            normalise = normalise.Replace(milliers.ToString(), string.Empty).Replace(decimale, '.');
        }
        else if (virgule >= 0)
        {
            normalise = normalise.Replace(',', '.');
        }

        return double.TryParse(normalise, NumberStyles.Float, CultureInfo.InvariantCulture, out valeur)
            && double.IsFinite(valeur);
    }

    /// <summary>Texte affiché pour une valeur : jusqu'à quatre décimales, sans zéros inutiles.</summary>
    public static string Formater(double valeur, CultureInfo culture)
    {
        if (valeur == 0)
        {
            valeur = 0; // évite l'affichage de « -0 »
        }

        return valeur.ToString("0.####", culture);
    }
}
