using System.Globalization;
using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Ressources;

namespace ForzaTuneLap.Core.Tests;

public class CatalogueCriteresTests
{
    private static readonly CultureInfo Francais = new("fr");

    [Fact]
    public void Les_cles_de_criteres_sont_uniques()
    {
        Assert.Equal(CatalogueCriteres.Tous.Count, CatalogueCriteres.Tous.Select(c => c.Cle).Distinct().Count());
    }

    [Fact]
    public void Chaque_critere_a_un_libelle_francais()
    {
        foreach (var critere in CatalogueCriteres.Tous)
        {
            Assert.NotEqual(critere.CleLibelle, ServiceLibelles.Obtenir(critere, Francais));
        }
    }

    [Fact]
    public void Chaque_division_transmission_et_categorie_a_un_libelle()
    {
        foreach (var division in Enum.GetValues<Division>())
        {
            Assert.NotEqual($"Division_{division}", ServiceLibelles.Obtenir(division, Francais));
        }

        foreach (var transmission in Enum.GetValues<Transmission>())
        {
            Assert.NotEqual($"Transmission_{transmission}", ServiceLibelles.Obtenir(transmission, Francais));
        }

        foreach (var categorie in Enum.GetValues<CategorieDeReglage>())
        {
            Assert.NotEqual($"Categorie_{categorie}", ServiceLibelles.Obtenir(categorie, Francais));
        }
    }

    [Fact]
    public void Les_libelles_de_divisions_sont_ceux_du_jeu()
    {
        Assert.Equal("Forza T/A", ServiceLibelles.Obtenir(Division.ForzaTA, Francais));
        Assert.Equal("Forza GT2", ServiceLibelles.Obtenir(Division.ForzaGT2, Francais));
        Assert.Equal("488 Challenge", ServiceLibelles.Obtenir(Division.Challenge488, Francais));
    }

    [Fact]
    public void Les_sections_facultatives_absentes_donnent_une_valeur_nulle()
    {
        var reglage = new Reglage();
        var categoriesToujoursPresentes = new[] { CategorieDeReglage.Generaux, CategorieDeReglage.Performances };

        foreach (var critere in CatalogueCriteres.Tous.Where(c => !categoriesToujoursPresentes.Contains(c.Categorie)))
        {
            Assert.Null(critere.Lire(reglage));
        }
    }

    [Fact]
    public void Les_rapports_de_boite_sont_lus_par_numero()
    {
        var reglage = new Reglage
        {
            RapportsDeBoite =
            {
                new RapportDeBoite { Numero = 1, Valeur = 3.2 },
                new RapportDeBoite { Numero = 2, Valeur = 2.1 },
                new RapportDeBoite { Numero = 3, Valeur = 1.5 }
            }
        };

        Assert.Equal(3.0, CatalogueCriteres.Rechercher("Rapports.NombreDeRapports")!.Lire(reglage));
        Assert.Equal(2.1, CatalogueCriteres.Rechercher("Rapports.Rapport2")!.Lire(reglage));
        Assert.Null(CatalogueCriteres.Rechercher("Rapports.Rapport4")!.Lire(reglage));
    }

    [Fact]
    public void Une_valeur_de_section_presente_est_lue()
    {
        var reglage = new Reglage { Pneus = new ReglagePneus { PressionAvant = 2.1, PressionArriere = 2.3 } };

        Assert.Equal(2.3, CatalogueCriteres.Rechercher("Pneus.PressionArriere")!.Lire(reglage));
    }
}
