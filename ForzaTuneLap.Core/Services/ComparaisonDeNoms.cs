using System.Globalization;

namespace ForzaTuneLap.Core.Services;

/// <summary>Comparaison de noms de marques ou de voitures : casse, accents et ponctuation ignorés.</summary>
internal static class ComparaisonDeNoms
{
    private const CompareOptions Options =
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols;

    public static bool Identiques(string premier, string second) =>
        CultureInfo.CurrentCulture.CompareInfo.Compare(premier, second, Options) == 0;

    /// <summary>
    /// Vrai si <paramref name="texte"/> contient chacun des mots de <paramref name="recherche"/>, dans n'importe quel ordre
    /// (casse, accents et ponctuation ignorés). « alpha 2020 » trouve donc « Alpha Foudre 2020 ».
    /// </summary>
    public static bool Contient(string texte, string recherche)
    {
        var comparaison = CultureInfo.CurrentCulture.CompareInfo;

        return recherche
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(mot => comparaison.IndexOf(texte, mot, Options) >= 0);
    }
}
