using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Services;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Tests;

public class EditeurFiltreReglagesTests
{
    private static DescriptionCritere Puissance =>
        CatalogueCriteres.Tous.First(c => c.Grandeur == TypeDeGrandeur.Puissance);

    [Fact]
    public void Un_editeur_vide_construit_un_filtre_qui_ne_retire_rien()
    {
        var filtre = new EditeurFiltreReglages(PreferencesUnites.Metriques()).ConstruireFiltreChronos();

        Assert.Null(filtre.Texte);
        Assert.Empty(filtre.IdentifiantsMarques);
        Assert.Empty(filtre.Divisions);
        Assert.Empty(filtre.Transmissions);
        Assert.Equal(OrigineDuReglage.Tous, filtre.Origine);
        Assert.Null(filtre.AnneeMinimum);
        Assert.Null(filtre.IndiceDePerformanceMaximum);
        Assert.Empty(filtre.Criteres);
    }

    [Fact]
    public void Les_filtres_simples_sont_reportes_dans_le_filtre()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques())
        {
            Texte = "  gt  ",
            IdentifiantMarque = 12,
            Division = Division.ForzaGT3,
            Transmission = Transmission.Propulsion,
            Origine = OrigineDuReglage.Creation,
            AnneeMinimumTexte = "1990",
            AnneeMaximumTexte = "2005",
            IndiceMinimumTexte = "500",
            IndiceMaximumTexte = "650"
        };

        var filtre = editeur.ConstruireFiltreReglages();

        Assert.Equal("  gt  ", filtre.Texte);
        Assert.Equal(new[] { 12 }, filtre.IdentifiantsMarques);
        Assert.Equal(new[] { Division.ForzaGT3 }, filtre.Divisions);
        Assert.Equal(new[] { Transmission.Propulsion }, filtre.Transmissions);
        Assert.Equal(OrigineDuReglage.Creation, filtre.Origine);
        Assert.Equal(1990, filtre.AnneeMinimum);
        Assert.Equal(2005, filtre.AnneeMaximum);
        Assert.Equal(500, filtre.IndiceDePerformanceMinimum);
        Assert.Equal(650, filtre.IndiceDePerformanceMaximum);
    }

    [Fact]
    public void Les_bornes_d_un_critere_sont_converties_de_l_unite_choisie_en_unite_canonique()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Imperiales());
        var critere = editeur.AjouterCritere(Puissance);
        critere.MinimumTexte = "400";
        critere.MaximumTexte = "";

        var filtre = editeur.ConstruireFiltreChronos();

        var numerique = Assert.Single(filtre.Criteres);
        Assert.Equal(Puissance.Cle, numerique.CleCritere);
        var attendu = PreferencesUnites.Imperiales().VersCanonique(TypeDeGrandeur.Puissance, 400);
        Assert.Equal(attendu, numerique.Minimum!.Value, 6);
        Assert.Null(numerique.Maximum);
    }

    [Fact]
    public void Un_critere_sans_borne_est_conserve_pour_l_affichage()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques());
        editeur.AjouterCritere(Puissance);

        var numerique = Assert.Single(editeur.ConstruireFiltreChronos().Criteres);
        Assert.Null(numerique.Minimum);
        Assert.Null(numerique.Maximum);
    }

    [Fact]
    public void Ajouter_deux_fois_le_meme_critere_ne_le_duplique_pas_et_on_peut_le_retirer()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques());

        var premier = editeur.AjouterCritere(Puissance);
        var second = editeur.AjouterCritere(Puissance);

        Assert.Same(premier, second);
        Assert.Single(editeur.Criteres);

        editeur.RetirerCritere(premier);
        Assert.Empty(editeur.Criteres);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12,5")]
    public void Une_borne_d_annee_ou_d_indice_invalide_rend_l_editeur_invalide(string texte)
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques());
        Assert.True(editeur.EstValide);

        editeur.AnneeMinimumTexte = texte;
        Assert.False(editeur.EstValide);

        editeur.AnneeMinimumTexte = "";
        Assert.True(editeur.EstValide);

        editeur.IndiceMaximumTexte = texte;
        Assert.False(editeur.EstValide);
    }

    [Fact]
    public void Une_borne_de_critere_invalide_rend_l_editeur_invalide_et_est_ignoree_a_la_construction()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques());
        var critere = editeur.AjouterCritere(Puissance);

        critere.MinimumTexte = "beaucoup";

        Assert.True(critere.EstInvalide);
        Assert.False(editeur.EstValide);
        Assert.Empty(editeur.ConstruireFiltreChronos().Criteres);

        critere.MinimumTexte = "300,5";
        Assert.True(editeur.EstValide);
        Assert.Single(editeur.ConstruireFiltreChronos().Criteres);
    }

    [Fact]
    public void Chaque_changement_leve_l_evenement_et_la_reinitialisation_le_leve_une_seule_fois()
    {
        var editeur = new EditeurFiltreReglages(PreferencesUnites.Metriques());
        var nombre = 0;
        editeur.Modifie += (_, _) => nombre++;

        editeur.Texte = "a";
        editeur.Texte = "a"; // inchangé
        editeur.IdentifiantMarque = 3;
        var critere = editeur.AjouterCritere(Puissance);
        critere.MaximumTexte = "500";
        Assert.Equal(4, nombre);

        nombre = 0;
        editeur.Reinitialiser();

        Assert.Equal(1, nombre);
        Assert.Equal(string.Empty, editeur.Texte);
        Assert.Null(editeur.IdentifiantMarque);
        Assert.Empty(editeur.Criteres);
        Assert.True(editeur.EstValide);
    }
}
