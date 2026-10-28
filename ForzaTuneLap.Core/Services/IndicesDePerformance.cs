using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Services;

/// <summary>
/// Correspondance entre l'indice de performance et sa classe :
/// E 0 à 300, D 301 à 400, C 401 à 500, B 501 à 600, A 601 à 700, S 701 à 800, R 801 à 900, P 901 à 998, X 999.
/// </summary>
public static class IndicesDePerformance
{
    public const int Maximum = 999;

    public static ClasseDePerformance ObtenirClasse(int indice) => indice switch
    {
        <= 300 => ClasseDePerformance.E,
        <= 400 => ClasseDePerformance.D,
        <= 500 => ClasseDePerformance.C,
        <= 600 => ClasseDePerformance.B,
        <= 700 => ClasseDePerformance.A,
        <= 800 => ClasseDePerformance.S,
        <= 900 => ClasseDePerformance.R,
        <= 998 => ClasseDePerformance.P,
        _ => ClasseDePerformance.X
    };

    /// <summary>Texte affiché, par exemple « S 750 ».</summary>
    public static string Formater(int indice) => $"{ObtenirClasse(indice)} {indice}";
}
