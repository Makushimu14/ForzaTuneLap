using System.Globalization;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Tests;

public class EditeurChronoTests
{
    private static readonly CultureInfo Francais = new("fr-FR");
    private static readonly CultureInfo Anglais = new("en-US");

    [Theory]
    [InlineData("1:32,450", 92_450)]
    [InlineData("1:32.450", 92_450)]
    [InlineData("1:32.45", 92_450)]
    [InlineData("1:32", 92_000)]
    [InlineData("0:59,9", 59_900)]
    [InlineData("92,450", 92_450)]
    [InlineData("  2:05,001  ", 125_001)]
    [InlineData("59:59,999", 3_599_999)]
    public void Les_durees_valides_sont_lues(string texte, long millisecondes)
    {
        Assert.True(DureesDeTour.TryParse(texte, out var duree));
        Assert.Equal(millisecondes, (long)duree.TotalMilliseconds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("0:00,000")]
    [InlineData("-5")]
    [InlineData("1:60")]
    [InlineData("1:75,2")]
    [InlineData("60:00")]
    [InlineData("1:2:3")]
    [InlineData(":30")]
    [InlineData("a:30")]
    [InlineData("1:-5")]
    public void Les_durees_invalides_sont_refusees(string texte)
    {
        Assert.False(DureesDeTour.TryParse(texte, out _));
    }

    [Fact]
    public void La_duree_est_affichee_au_format_du_jeu_avec_le_separateur_de_la_langue()
    {
        Assert.Equal("1:32,450", DureesDeTour.Formater(92_450, Francais));
        Assert.Equal("1:32.450", DureesDeTour.Formater(92_450, Anglais));
        Assert.Equal("0:59,900", DureesDeTour.Formater(59_900, Francais));
        Assert.Equal("12:05,007", DureesDeTour.Formater(725_007, Francais));
    }

    [Fact]
    public void Un_nouvel_editeur_est_vide_sans_erreur_et_ne_peut_pas_enregistrer()
    {
        var editeur = new EditeurChrono(Francais);

        Assert.False(editeur.EstModification);
        Assert.Null(editeur.IdentifiantChrono);
        Assert.False(editeur.EstValide);
        Assert.False(editeur.EstInvalide);
        Assert.False(editeur.PeutEnregistrer);
        Assert.Equal("1:32,450", editeur.Exemple);
    }

    [Fact]
    public void La_saisie_valide_donne_la_duree_et_une_saisie_invalide_est_signalee()
    {
        var editeur = new EditeurChrono(Francais);

        editeur.DureeTexte = "1:32,450";
        Assert.True(editeur.PeutEnregistrer);
        Assert.Equal(TimeSpan.FromMilliseconds(92_450), editeur.Duree);
        Assert.False(editeur.EstInvalide);

        editeur.DureeTexte = "1:9x";
        Assert.False(editeur.PeutEnregistrer);
        Assert.Null(editeur.Duree);
        Assert.True(editeur.EstInvalide);

        editeur.DureeTexte = "";
        Assert.False(editeur.EstInvalide);
    }

    [Fact]
    public void Modifier_un_chrono_existant_reprend_ses_valeurs()
    {
        var date = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var chrono = new Chrono { DureeMillisecondes = 83_456, DateRealisation = date, Commentaire = "Pluie" };

        var editeur = new EditeurChrono(chrono, Francais);

        Assert.True(editeur.EstModification);
        Assert.Equal(chrono.Identifiant, editeur.IdentifiantChrono);
        Assert.Equal("1:23,456", editeur.DureeTexte);
        Assert.Equal(TimeSpan.FromMilliseconds(83_456), editeur.Duree);
        Assert.Equal(date, editeur.DateRealisation);
        Assert.Equal("Pluie", editeur.Commentaire);
        Assert.True(editeur.PeutEnregistrer);
    }

    [Fact]
    public void Les_changements_sont_notifies()
    {
        var editeur = new EditeurChrono(Francais);
        var notifies = new List<string?>();
        editeur.PropertyChanged += (_, e) => notifies.Add(e.PropertyName);

        editeur.DureeTexte = "1:30";

        Assert.Contains(nameof(EditeurChrono.PeutEnregistrer), notifies);
        Assert.Contains(nameof(EditeurChrono.EstInvalide), notifies);
    }
}
