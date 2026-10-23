using System.Globalization;

namespace ForzaTuneLap.Core.Services;

/// <summary>Comparaison de noms de marques ou de voitures : casse, accents et ponctuation ignorés.</summary>
internal static class ComparaisonDeNoms
{
    private const CompareOptions Options =
        CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols;

    public static bool Identiques(string premier, string second) =>
        CultureInfo.CurrentCulture.CompareInfo.Compare(premier, second, Options) == 0;
}
