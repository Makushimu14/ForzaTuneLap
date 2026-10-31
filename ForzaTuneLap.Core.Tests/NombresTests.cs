using System.Globalization;
using ForzaTuneLap.Core.Edition;

namespace ForzaTuneLap.Core.Tests;

public class NombresTests
{
    [Theory]
    [InlineData("2,5", 2.5)]
    [InlineData("2.5", 2.5)]
    [InlineData("  2,5  ", 2.5)]
    [InlineData("1 234,5", 1234.5)]
    [InlineData("1\u00A0234,5", 1234.5)]
    [InlineData("1,234.5", 1234.5)]
    [InlineData("1.234,5", 1234.5)]
    [InlineData("-1,5", -1.5)]
    [InlineData("+3", 3)]
    [InlineData("0", 0)]
    [InlineData("1,234", 1.234)] // un séparateur seul est toujours décimal
    public void Les_nombres_sont_lus_avec_la_virgule_comme_avec_le_point(string texte, double attendu)
    {
        Assert.True(Nombres.TryParse(texte, out var valeur));
        Assert.Equal(attendu, valeur, 9);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("-")]
    [InlineData("1e")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("12 kg")]
    public void Les_textes_qui_ne_sont_pas_des_nombres_sont_refuses(string texte)
    {
        Assert.False(Nombres.TryParse(texte, out _));
    }

    [Fact]
    public void L_affichage_garde_jusqu_a_quatre_decimales_sans_zeros_inutiles()
    {
        Assert.Equal("3000", Nombres.Formater(3000, new CultureInfo("fr-FR")));
        Assert.Equal("3,727", Nombres.Formater(3.727, new CultureInfo("fr-FR")));
        Assert.Equal("3.727", Nombres.Formater(3.727, new CultureInfo("en-US")));
        Assert.Equal("2,0757", Nombres.Formater(2.07574, new CultureInfo("fr-FR")));
        Assert.Equal("0", Nombres.Formater(-0.0, new CultureInfo("fr-FR")));
    }
}
