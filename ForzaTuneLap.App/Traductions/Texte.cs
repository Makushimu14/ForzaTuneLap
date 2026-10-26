using System.Globalization;
using System.Resources;

namespace ForzaTuneLap.App.Traductions;

/// <summary>
/// Textes de l'interface. Textes.resx contient le français (langue par défaut) et Textes.en.resx l'anglais.
/// Les libellés des données (divisions, critères…) sont dans le Core : voir ServiceLibelles.
/// </summary>
public static class Texte
{
    private static readonly ResourceManager Gestionnaire =
        new("ForzaTuneLap.App.Traductions.Textes", typeof(Texte).Assembly);

    /// <summary>Texte dans la langue courante. Une clé absente renvoie la clé elle-même, ce qui rend l'oubli visible.</summary>
    public static string Obtenir(string cle) =>
        Gestionnaire.GetString(cle, CultureInfo.CurrentUICulture) ?? cle;
}
