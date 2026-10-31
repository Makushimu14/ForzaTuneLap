using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.Core.Tests;

public class EditeurVoiturePersonnaliseeTests
{
    [Fact]
    public void Un_nouvel_editeur_est_vide_sans_erreur_et_ne_peut_pas_enregistrer()
    {
        var editeur = new EditeurVoiturePersonnalisee();

        Assert.False(editeur.EstModification);
        Assert.Null(editeur.IdentifiantVoiture);
        Assert.True(editeur.NomVide);
        Assert.Null(editeur.Annee);
        Assert.False(editeur.AnneeInvalide);
        Assert.Equal(0, editeur.Valeur);
        Assert.False(editeur.ValeurInvalide);
        Assert.False(editeur.PeutEnregistrer);
    }

    [Fact]
    public void Un_nom_et_une_annee_valides_permettent_d_enregistrer()
    {
        var editeur = new EditeurVoiturePersonnalisee { Nom = "  Ma voiture ", AnneeTexte = "1999" };

        Assert.True(editeur.PeutEnregistrer);
        Assert.Equal(1999, editeur.Annee);
    }

    [Theory]
    [InlineData("1899")]
    [InlineData("2101")]
    [InlineData("abc")]
    [InlineData("19,5")]
    public void Une_annee_hors_limites_ou_illisible_est_signalee(string texte)
    {
        var editeur = new EditeurVoiturePersonnalisee { Nom = "Ma voiture", AnneeTexte = texte };

        Assert.True(editeur.AnneeInvalide);
        Assert.Null(editeur.Annee);
        Assert.False(editeur.PeutEnregistrer);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("beaucoup")]
    [InlineData("1,5")]
    public void Un_prix_invalide_bloque_l_enregistrement(string texte)
    {
        var editeur = new EditeurVoiturePersonnalisee { Nom = "Ma voiture", AnneeTexte = "2000", ValeurTexte = texte };

        Assert.True(editeur.ValeurInvalide);
        Assert.False(editeur.PeutEnregistrer);

        editeur.ValeurTexte = "";
        Assert.True(editeur.PeutEnregistrer);

        editeur.ValeurTexte = "250000";
        Assert.Equal(250_000, editeur.Valeur);
    }

    [Fact]
    public void Corriger_une_voiture_reprend_ses_valeurs()
    {
        var voiture = new Voiture
        {
            Identifiant = 100_001,
            Nom = "Ma voiture",
            Annee = 1987,
            Division = Division.ForzaGT3,
            Valeur = 12_000,
            ContenuTelechargeable = true
        };

        var editeur = new EditeurVoiturePersonnalisee(voiture);

        Assert.True(editeur.EstModification);
        Assert.Equal(100_001, editeur.IdentifiantVoiture);
        Assert.Equal("Ma voiture", editeur.Nom);
        Assert.Equal(1987, editeur.Annee);
        Assert.Equal(Division.ForzaGT3, editeur.Division);
        Assert.Equal(12_000, editeur.Valeur);
        Assert.True(editeur.ContenuTelechargeable);
        Assert.True(editeur.PeutEnregistrer);
    }

    [Fact]
    public void Un_prix_nul_est_affiche_comme_un_champ_vide()
    {
        var editeur = new EditeurVoiturePersonnalisee(new Voiture { Nom = "A", Annee = 2000, Valeur = 0 });

        Assert.Equal(string.Empty, editeur.ValeurTexte);
    }

    [Fact]
    public void Les_changements_sont_notifies()
    {
        var editeur = new EditeurVoiturePersonnalisee();
        var notifies = new List<string?>();
        editeur.PropertyChanged += (_, e) => notifies.Add(e.PropertyName);

        editeur.AnneeTexte = "2001";

        Assert.Contains(nameof(EditeurVoiturePersonnalisee.PeutEnregistrer), notifies);
        Assert.Contains(nameof(EditeurVoiturePersonnalisee.AnneeInvalide), notifies);
        Assert.Contains(nameof(EditeurVoiturePersonnalisee.Annee), notifies);
    }
}
