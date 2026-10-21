using System.Globalization;
using System.Resources;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;

namespace ForzaTuneLap.Core.Ressources;

/// <summary>
/// Accès aux libellés traduits. Le fichier Libelles.resx contient le français (langue par défaut) ;
/// une traduction anglaise s'ajoute simplement avec Libelles.en.resx.
/// Une clé absente renvoie la clé elle-même, ce qui rend les oublis visibles à l'écran.
/// </summary>
public static class ServiceLibelles
{
    private static readonly ResourceManager Gestionnaire =
        new("ForzaTuneLap.Core.Ressources.Libelles", typeof(ServiceLibelles).Assembly);

    public static string Obtenir(string cle, CultureInfo? culture = null) =>
        Gestionnaire.GetString(cle, culture ?? CultureInfo.CurrentUICulture) ?? cle;

    public static string Obtenir(Division division, CultureInfo? culture = null) =>
        Obtenir($"Division_{division}", culture);

    public static string Obtenir(Transmission transmission, CultureInfo? culture = null) =>
        Obtenir($"Transmission_{transmission}", culture);

    public static string Obtenir(CategorieDeReglage categorie, CultureInfo? culture = null) =>
        Obtenir($"Categorie_{categorie}", culture);

    public static string Obtenir(DescriptionCritere critere, CultureInfo? culture = null) =>
        Obtenir(critere.CleLibelle, culture);
}
