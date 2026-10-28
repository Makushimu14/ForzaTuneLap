using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.Core.Tests;

public class IndicesDePerformanceTests
{
    [Theory]
    [InlineData(0, ClasseDePerformance.E)]
    [InlineData(100, ClasseDePerformance.E)]
    [InlineData(300, ClasseDePerformance.E)]
    [InlineData(301, ClasseDePerformance.D)]
    [InlineData(400, ClasseDePerformance.D)]
    [InlineData(401, ClasseDePerformance.C)]
    [InlineData(500, ClasseDePerformance.C)]
    [InlineData(501, ClasseDePerformance.B)]
    [InlineData(600, ClasseDePerformance.B)]
    [InlineData(601, ClasseDePerformance.A)]
    [InlineData(700, ClasseDePerformance.A)]
    [InlineData(701, ClasseDePerformance.S)]
    [InlineData(800, ClasseDePerformance.S)]
    [InlineData(801, ClasseDePerformance.R)]
    [InlineData(900, ClasseDePerformance.R)]
    [InlineData(901, ClasseDePerformance.P)]
    [InlineData(998, ClasseDePerformance.P)]
    [InlineData(999, ClasseDePerformance.X)]
    public void Chaque_indice_donne_sa_classe(int indice, ClasseDePerformance attendue)
    {
        Assert.Equal(attendue, IndicesDePerformance.ObtenirClasse(indice));
    }

    [Theory]
    [InlineData(-5, ClasseDePerformance.E)]
    [InlineData(1500, ClasseDePerformance.X)]
    public void Un_indice_hors_limites_donne_la_classe_la_plus_proche(int indice, ClasseDePerformance attendue)
    {
        Assert.Equal(attendue, IndicesDePerformance.ObtenirClasse(indice));
    }

    [Theory]
    [InlineData(750, "S 750")]
    [InlineData(999, "X 999")]
    [InlineData(301, "D 301")]
    public void Le_texte_affiche_est_la_classe_suivie_de_l_indice(int indice, string attendu)
    {
        Assert.Equal(attendu, IndicesDePerformance.Formater(indice));
    }
}
