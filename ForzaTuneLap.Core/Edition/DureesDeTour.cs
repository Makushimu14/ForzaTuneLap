using System.Globalization;

namespace ForzaTuneLap.Core.Edition;

/// <summary>
/// Lecture et affichage de la durée d'un tour, au format du jeu « m:ss,mmm » (par exemple « 1:32,450 »).
/// </summary>
public static class DureesDeTour
{
    private const int MinutesMaximum = 59;

    /// <summary>
    /// Lit une durée. Formats acceptés : « 1:32,450 », « 1:32.45 », « 1:32 », « 92,450 » (secondes seules).
    /// La virgule et le point sont acceptés comme séparateur décimal. Les secondes valent moins de 60 quand il y a des
    /// minutes. La durée doit être strictement positive et inférieure à une heure.
    /// </summary>
    public static bool TryParse(string? texte, out TimeSpan duree)
    {
        duree = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(texte))
        {
            return false;
        }

        var parties = texte.Trim().Split(':');
        if (parties.Length > 2)
        {
            return false;
        }

        var minutes = 0;
        var texteSecondes = parties[^1];

        if (parties.Length == 2)
        {
            var texteMinutes = parties[0].Trim();
            if (texteMinutes.Length == 0
                || !int.TryParse(texteMinutes, NumberStyles.None, CultureInfo.InvariantCulture, out minutes)
                || minutes > MinutesMaximum)
            {
                return false;
            }
        }

        if (!Nombres.TryParse(texteSecondes, out var secondes) || secondes < 0)
        {
            return false;
        }

        if (parties.Length == 2 && secondes >= 60)
        {
            return false;
        }

        var millisecondes = (long)Math.Round((minutes * 60 + secondes) * 1000);
        if (millisecondes <= 0 || millisecondes >= 3_600_000)
        {
            return false;
        }

        duree = TimeSpan.FromMilliseconds(millisecondes);
        return true;
    }

    /// <summary>Affiche une durée en « m:ss,mmm », avec le séparateur décimal de la culture.</summary>
    public static string Formater(long millisecondes, CultureInfo culture)
    {
        var total = Math.Max(0, millisecondes);
        var minutes = total / 60_000;
        var secondes = total % 60_000 / 1000;
        var reste = total % 1000;
        var separateur = culture.NumberFormat.NumberDecimalSeparator;

        return string.Create(CultureInfo.InvariantCulture, $"{minutes}:{secondes:00}{separateur}{reste:000}");
    }

    /// <summary>
    /// Écart avec le meilleur temps, précédé de « + » : « +1,234 s » sous une minute, « +1:05,300 » au-delà.
    /// Un écart nul ou négatif donne un texte vide (c'est le meilleur temps).
    /// </summary>
    public static string FormaterEcart(long ecartMillisecondes, CultureInfo culture)
    {
        if (ecartMillisecondes <= 0)
        {
            return string.Empty;
        }

        if (ecartMillisecondes >= 60_000)
        {
            return "+" + Formater(ecartMillisecondes, culture);
        }

        var separateur = culture.NumberFormat.NumberDecimalSeparator;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"+{ecartMillisecondes / 1000}{separateur}{ecartMillisecondes % 1000:000} s");
    }

    public static string Formater(TimeSpan duree, CultureInfo culture) =>
        Formater((long)Math.Round(duree.TotalMilliseconds), culture);
}
