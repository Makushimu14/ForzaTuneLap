using System.Collections;
using System.Globalization;
using System.Resources;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Ressources;

namespace ForzaTuneLap.Core.Tests;

public class LibellesTests
{
    private static readonly CultureInfo Anglais = new("en");

    private static HashSet<string> Cles(ResourceSet jeu) =>
        jeu.Cast<DictionaryEntry>().Select(entree => (string)entree.Key).ToHashSet();

    [Fact]
    public void La_traduction_anglaise_contient_exactement_les_memes_cles_que_le_francais()
    {
        var gestionnaire = new ResourceManager("ForzaTuneLap.Core.Ressources.Libelles", typeof(ServiceLibelles).Assembly);

        var francais = gestionnaire.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
        var anglais = gestionnaire.GetResourceSet(Anglais, createIfNotExists: true, tryParents: false);

        Assert.NotNull(francais);
        Assert.NotNull(anglais);
        Assert.Equal(Cles(francais!).Order(), Cles(anglais!).Order());
    }

    [Fact]
    public void Tous_les_libelles_existent_en_anglais()
    {
        foreach (var critere in CatalogueCriteres.Tous)
        {
            Assert.NotEqual(critere.CleLibelle, ServiceLibelles.Obtenir(critere, Anglais));
        }

        foreach (var division in Enum.GetValues<Division>())
        {
            Assert.NotEqual($"Division_{division}", ServiceLibelles.Obtenir(division, Anglais));
        }

        foreach (var transmission in Enum.GetValues<Transmission>())
        {
            Assert.NotEqual($"Transmission_{transmission}", ServiceLibelles.Obtenir(transmission, Anglais));
        }

        foreach (var categorie in Enum.GetValues<CategorieDeReglage>())
        {
            Assert.NotEqual($"Categorie_{categorie}", ServiceLibelles.Obtenir(categorie, Anglais));
        }
    }

    [Fact]
    public void Les_libelles_changent_avec_la_langue()
    {
        Assert.Equal("Power", ServiceLibelles.Obtenir(CatalogueCriteres.Rechercher("Generaux.Puissance")!, Anglais));
        Assert.Equal("Puissance", ServiceLibelles.Obtenir(CatalogueCriteres.Rechercher("Generaux.Puissance")!, new CultureInfo("fr")));
        Assert.Equal("Rear-wheel drive", ServiceLibelles.Obtenir(Transmission.Propulsion, Anglais));
    }
}
