using System.Globalization;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Preferences;

public enum ChoixDeLangue
{
    /// <summary>Suit la langue de l'appareil (français ou anglais ; l'anglais pour toute autre langue).</summary>
    Systeme,
    Francais,
    Anglais
}

/// <summary>
/// Préférences de l'utilisateur : langue de l'interface et unité de mesure de chaque grandeur.
/// Les valeurs sont enregistrées immédiatement dans le stockage fourni. Aucune valeur de la base n'est concernée :
/// changer d'unité ne modifie jamais les données.
/// </summary>
public sealed class ServicePreferences
{
    private const string CleLangue = "langue";
    private const string PrefixeCleUnite = "unite.";

    private readonly IStockageDePreferences stockage;
    private readonly CultureInfo cultureDuSysteme;
    private readonly CultureInfo cultureInterfaceDuSysteme;

    /// <param name="cultureDuSysteme">Culture de format de l'appareil (nombres, dates). Celle du thread courant par défaut.</param>
    /// <param name="cultureInterfaceDuSysteme">Langue de l'appareil. Celle du thread courant par défaut.</param>
    /// <remarks>À construire au démarrage, avant tout changement de culture, pour mémoriser celles de l'appareil.</remarks>
    public ServicePreferences(
        IStockageDePreferences stockage,
        CultureInfo? cultureDuSysteme = null,
        CultureInfo? cultureInterfaceDuSysteme = null)
    {
        this.stockage = stockage;
        this.cultureDuSysteme = cultureDuSysteme ?? CultureInfo.CurrentCulture;
        this.cultureInterfaceDuSysteme = cultureInterfaceDuSysteme ?? CultureInfo.CurrentUICulture;

        Langue = LireLangue();
        Unites = LireUnites();
    }

    /// <summary>Levé après chaque changement de préférence.</summary>
    public event EventHandler? Modifiees;

    public ChoixDeLangue Langue { get; private set; }

    /// <summary>Unités choisies. À utiliser pour convertir les valeurs à l'affichage et à la saisie.</summary>
    public PreferencesUnites Unites { get; private set; }

    /// <summary>Langue effective de l'interface : français ou anglais.</summary>
    public CultureInfo CultureInterface => Langue switch
    {
        ChoixDeLangue.Francais => new CultureInfo("fr"),
        ChoixDeLangue.Anglais => new CultureInfo("en"),
        _ => EstFrancaisOuAnglais(cultureInterfaceDuSysteme) ? cultureInterfaceDuSysteme : new CultureInfo("en")
    };

    /// <summary>Culture utilisée pour afficher les nombres et les dates (séparateur décimal, format de date…).</summary>
    public CultureInfo CultureDeFormat => Langue switch
    {
        ChoixDeLangue.Francais => new CultureInfo("fr"),
        ChoixDeLangue.Anglais => new CultureInfo("en"),
        _ => cultureDuSysteme
    };

    public void DefinirLangue(ChoixDeLangue langue)
    {
        Langue = langue;
        stockage.Ecrire(CleLangue, langue.ToString());
        Modifiees?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Applique la langue choisie à l'application (textes traduits, formats des nombres et des dates).
    /// À appeler au démarrage, puis après chaque changement de langue.
    /// </summary>
    public void AppliquerLangue()
    {
        var interfaceUtilisateur = CultureInterface;
        var format = CultureDeFormat;

        CultureInfo.DefaultThreadCurrentUICulture = interfaceUtilisateur;
        CultureInfo.DefaultThreadCurrentCulture = format;
        CultureInfo.CurrentUICulture = interfaceUtilisateur;
        CultureInfo.CurrentCulture = format;
    }

    public void DefinirUnite(TypeDeGrandeur grandeur, Unite unite)
    {
        Unites.Choix[grandeur] = unite.Cle;
        stockage.Ecrire(PrefixeCleUnite + grandeur, unite.Cle);
        Modifiees?.Invoke(this, EventArgs.Empty);
    }

    public void UtiliserSystemeMetrique() => RemplacerUnites(PreferencesUnites.Metriques());

    public void UtiliserSystemeImperial() => RemplacerUnites(PreferencesUnites.Imperiales());

    private void RemplacerUnites(PreferencesUnites preferences)
    {
        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            if (preferences.Choix.TryGetValue(grandeur, out var cle))
            {
                stockage.Ecrire(PrefixeCleUnite + grandeur, cle);
            }
            else
            {
                stockage.Supprimer(PrefixeCleUnite + grandeur);
            }
        }

        Unites = preferences;
        Modifiees?.Invoke(this, EventArgs.Empty);
    }

    private ChoixDeLangue LireLangue() =>
        Enum.TryParse<ChoixDeLangue>(stockage.Lire(CleLangue), out var langue) && Enum.IsDefined(langue)
            ? langue
            : ChoixDeLangue.Systeme;

    private PreferencesUnites LireUnites()
    {
        var preferences = PreferencesUnites.Metriques();

        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            var cle = stockage.Lire(PrefixeCleUnite + grandeur);

            // Une clé inconnue (donnée corrompue, unité retirée dans une version ultérieure) est ignorée.
            if (cle is not null && CatalogueUnites.ObtenirUnites(grandeur).Any(unite => unite.Cle == cle))
            {
                preferences.Choix[grandeur] = cle;
            }
        }

        return preferences;
    }

    private static bool EstFrancaisOuAnglais(CultureInfo culture) =>
        culture.TwoLetterISOLanguageName is "fr" or "en";
}
