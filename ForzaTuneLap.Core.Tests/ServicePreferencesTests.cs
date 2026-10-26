using System.Globalization;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Tests;

public class ServicePreferencesTests
{
    private sealed class StockageEnMemoire : IStockageDePreferences
    {
        public Dictionary<string, string> Valeurs { get; } = new();

        public string? Lire(string cle) => Valeurs.GetValueOrDefault(cle);

        public void Ecrire(string cle, string valeur) => Valeurs[cle] = valeur;

        public void Supprimer(string cle) => Valeurs.Remove(cle);
    }

    private static ServicePreferences Creer(
        StockageEnMemoire stockage,
        string cultureDuSysteme = "fr-FR",
        string langueDuSysteme = "fr-FR") =>
        new(stockage, new CultureInfo(cultureDuSysteme), new CultureInfo(langueDuSysteme));

    [Fact]
    public void Par_defaut_la_langue_suit_le_systeme_et_les_unites_sont_metriques()
    {
        var preferences = Creer(new StockageEnMemoire());

        Assert.Equal(ChoixDeLangue.Systeme, preferences.Langue);
        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            Assert.Equal(CatalogueUnites.ObtenirCanonique(grandeur), preferences.Unites.ObtenirUnite(grandeur));
        }
    }

    [Fact]
    public void La_langue_choisie_est_conservee_d_un_lancement_a_l_autre()
    {
        var stockage = new StockageEnMemoire();
        Creer(stockage).DefinirLangue(ChoixDeLangue.Anglais);

        var relu = Creer(stockage);

        Assert.Equal(ChoixDeLangue.Anglais, relu.Langue);
    }

    [Fact]
    public void Une_unite_choisie_est_conservee_d_un_lancement_a_l_autre()
    {
        var stockage = new StockageEnMemoire();
        var livre = CatalogueUnites.Obtenir(TypeDeGrandeur.Masse, "Livre");
        Creer(stockage).DefinirUnite(TypeDeGrandeur.Masse, livre);

        var relu = Creer(stockage);

        Assert.Equal(livre, relu.Unites.ObtenirUnite(TypeDeGrandeur.Masse));
        Assert.Equal("kW", relu.Unites.ObtenirUnite(TypeDeGrandeur.Puissance).Symbole);
    }

    [Fact]
    public void Le_systeme_imperial_puis_metrique_remplace_toutes_les_unites_et_met_a_jour_le_stockage()
    {
        var stockage = new StockageEnMemoire();
        var preferences = Creer(stockage);

        preferences.UtiliserSystemeImperial();
        Assert.Equal("lb", preferences.Unites.ObtenirUnite(TypeDeGrandeur.Masse).Symbole);
        Assert.Equal("mph", preferences.Unites.ObtenirUnite(TypeDeGrandeur.Vitesse).Symbole);
        Assert.Equal("psi", Creer(stockage).Unites.ObtenirUnite(TypeDeGrandeur.Pression).Symbole);

        preferences.UtiliserSystemeMetrique();
        Assert.Equal("kg", preferences.Unites.ObtenirUnite(TypeDeGrandeur.Masse).Symbole);
        Assert.Equal("bar", Creer(stockage).Unites.ObtenirUnite(TypeDeGrandeur.Pression).Symbole);
        Assert.DoesNotContain(stockage.Valeurs.Keys, cle => cle.StartsWith("unite."));
    }

    [Fact]
    public void Une_valeur_enregistree_invalide_est_ignoree()
    {
        var stockage = new StockageEnMemoire();
        stockage.Valeurs["langue"] = "Klingon";
        stockage.Valeurs["unite.Masse"] = "UniteQuiNExistePas";
        stockage.Valeurs["unite.Vitesse"] = "MilleParHeure";

        var preferences = Creer(stockage);

        Assert.Equal(ChoixDeLangue.Systeme, preferences.Langue);
        Assert.Equal("kg", preferences.Unites.ObtenirUnite(TypeDeGrandeur.Masse).Symbole);
        Assert.Equal("mph", preferences.Unites.ObtenirUnite(TypeDeGrandeur.Vitesse).Symbole);
    }

    [Fact]
    public void Chaque_changement_leve_l_evenement()
    {
        var preferences = Creer(new StockageEnMemoire());
        var notifications = 0;
        preferences.Modifiees += (_, _) => notifications++;

        preferences.DefinirLangue(ChoixDeLangue.Francais);
        preferences.DefinirUnite(TypeDeGrandeur.Masse, CatalogueUnites.Obtenir(TypeDeGrandeur.Masse, "Livre"));
        preferences.UtiliserSystemeImperial();
        preferences.UtiliserSystemeMetrique();

        Assert.Equal(4, notifications);
    }

    [Theory]
    [InlineData(ChoixDeLangue.Francais, "en-US", "fr")]
    [InlineData(ChoixDeLangue.Anglais, "fr-FR", "en")]
    [InlineData(ChoixDeLangue.Systeme, "fr-CA", "fr")]
    [InlineData(ChoixDeLangue.Systeme, "en-GB", "en")]
    [InlineData(ChoixDeLangue.Systeme, "de-DE", "en")] // langue non prise en charge : anglais
    public void La_culture_de_l_interface_depend_du_choix_et_de_la_langue_de_l_appareil(
        ChoixDeLangue choix, string langueDeLAppareil, string attendue)
    {
        var preferences = Creer(new StockageEnMemoire(), langueDuSysteme: langueDeLAppareil);
        preferences.DefinirLangue(choix);

        Assert.Equal(attendue, preferences.CultureInterface.TwoLetterISOLanguageName);
    }

    [Fact]
    public void Le_format_des_nombres_suit_l_appareil_en_mode_systeme_et_la_langue_sinon()
    {
        var preferences = Creer(new StockageEnMemoire(), cultureDuSysteme: "de-DE");

        Assert.Equal("de-DE", preferences.CultureDeFormat.Name);

        preferences.DefinirLangue(ChoixDeLangue.Anglais);
        Assert.Equal("en", preferences.CultureDeFormat.TwoLetterISOLanguageName);
    }
}
