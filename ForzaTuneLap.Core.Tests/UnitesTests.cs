using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Tests;

public class UnitesTests
{
    public static IEnumerable<object[]> ToutesLesUnites()
    {
        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            foreach (var unite in CatalogueUnites.ObtenirUnites(grandeur))
            {
                yield return new object[] { grandeur, unite.Cle };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ToutesLesUnites))]
    public void Conversion_aller_retour_conserve_la_valeur(TypeDeGrandeur grandeur, string cleUnite)
    {
        var unite = CatalogueUnites.Obtenir(grandeur, cleUnite);

        var canonique = unite.VersCanonique(123.456);

        Assert.Equal(123.456, unite.DepuisCanonique(canonique), 9);
    }

    [Fact]
    public void L_unite_canonique_de_chaque_grandeur_a_un_facteur_de_un()
    {
        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            Assert.Equal(1.0, CatalogueUnites.ObtenirCanonique(grandeur).FacteurVersCanonique);
        }
    }

    [Fact]
    public void Les_cles_d_unites_sont_uniques_au_sein_d_une_grandeur()
    {
        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            var unites = CatalogueUnites.ObtenirUnites(grandeur);
            Assert.Equal(unites.Count, unites.Select(u => u.Cle).Distinct().Count());
        }
    }

    [Fact]
    public void Mille_livres_valent_environ_453_kilogrammes()
    {
        var preferences = PreferencesUnites.Imperiales();

        Assert.Equal(453.59237, preferences.VersCanonique(TypeDeGrandeur.Masse, 1000), 5);
    }

    [Fact]
    public void Soixante_milles_par_heure_valent_environ_96_6_kilometres_par_heure()
    {
        var preferences = PreferencesUnites.Imperiales();

        Assert.Equal(96.56064, preferences.VersCanonique(TypeDeGrandeur.Vitesse, 60), 5);
    }

    [Fact]
    public void Les_preferences_metriques_ne_convertissent_rien()
    {
        var preferences = PreferencesUnites.Metriques();

        foreach (var grandeur in Enum.GetValues<TypeDeGrandeur>())
        {
            Assert.Equal(42.0, preferences.VersAffichage(grandeur, 42.0));
        }
    }

    [Fact]
    public void Une_cle_d_unite_inconnue_retombe_sur_l_unite_canonique()
    {
        var unite = CatalogueUnites.Obtenir(TypeDeGrandeur.Masse, "UniteQuiNExistePas");

        Assert.Equal("kg", unite.Symbole);
    }
}
