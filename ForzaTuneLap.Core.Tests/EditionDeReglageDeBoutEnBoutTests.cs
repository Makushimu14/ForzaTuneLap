using System.Globalization;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;
using ForzaTuneLap.Core.Unites;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

/// <summary>L'éditeur, le service des réglages et la base de données ensemble.</summary>
public class EditionDeReglageDeBoutEnBoutTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private FabriqueUniteDeTravail CreerFabrique() => new(new FabriqueDeContextesDeTest(baseDeTest));

    /// <summary>Un réglage « Course » créé à partir de l'origine de la voiture 1 (300 kW, deux rapports), avec un chrono.</summary>
    private async Task<Guid> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));

        using var travail = CreerFabrique().Creer();
        var brouillon = await travail.Reglages.CreerBrouillonAsync(1);
        brouillon.Nom = "Course";
        await travail.Reglages.EnregistrerNouveauAsync(brouillon);
        await travail.Chronos.AjouterAsync(brouillon.Identifiant, 1, TimeSpan.FromSeconds(90));
        return brouillon.Identifiant;
    }

    private static EditeurReglage OuvrirEditeur(Reglage copie, PreferencesUnites? unites = null) =>
        new(copie, unites ?? PreferencesUnites.Metriques(), new CultureInfo("fr-FR"));

    [Fact]
    public async Task Ouvrir_l_editeur_sans_rien_modifier_ne_change_pas_le_contenu_et_garde_les_chronos()
    {
        var identifiant = await PreparerAsync();
        using var travail = CreerFabrique().Creer();

        var copie = await travail.Reglages.ObtenirPourEditionAsync(identifiant);
        var editeur = OuvrirEditeur(copie, PreferencesUnites.Imperiales());
        var resultat = await travail.Reglages.ModifierAsync(editeur.Reglage);

        // Les valeurs affichées sont arrondies mais jamais réécrites si on n'y touche pas : le contenu est inchangé.
        Assert.Equal(StatutModification.Modifie, resultat.Statut);
        Assert.Equal(0, resultat.NombreDeChronos);
        Assert.Equal(1, await travail.Contexte.Chronos.CountAsync());
    }

    [Fact]
    public async Task Modifier_une_valeur_demande_confirmation_puis_supprime_les_chronos()
    {
        var identifiant = await PreparerAsync();
        using var travail = CreerFabrique().Creer();
        var copie = await travail.Reglages.ObtenirPourEditionAsync(identifiant);
        var editeur = OuvrirEditeur(copie);

        editeur.Generaux.Champs.Single(c => c.Cle == "Generaux.Puissance").Texte = "320";

        var demande = await travail.Reglages.ModifierAsync(editeur.Reglage);
        Assert.Equal(StatutModification.ConfirmationRequise, demande.Statut);
        Assert.Equal(1, demande.NombreDeChronos);

        var effectue = await travail.Reglages.ModifierAsync(editeur.Reglage, confirmerSuppressionDesChronos: true);
        Assert.Equal(StatutModification.Modifie, effectue.Statut);

        await using var contexte = baseDeTest.CreerContexte();
        Assert.Equal(320, (await contexte.Reglages.SingleAsync(r => r.Identifiant == identifiant)).Generaux.Puissance);
        Assert.Equal(0, await contexte.Chronos.CountAsync());
    }

    [Fact]
    public async Task Une_valeur_saisie_dans_une_unite_imperiale_est_enregistree_en_unite_canonique()
    {
        var identifiant = await PreparerAsync();
        using var travail = CreerFabrique().Creer();
        var copie = await travail.Reglages.ObtenirPourEditionAsync(identifiant);
        var editeur = OuvrirEditeur(copie, PreferencesUnites.Imperiales());

        editeur.Generaux.Champs.Single(c => c.Cle == "Generaux.Poids").Texte = "3000";
        await travail.Reglages.ModifierAsync(editeur.Reglage, confirmerSuppressionDesChronos: true);

        await using var contexte = baseDeTest.CreerContexte();
        var relu = await contexte.Reglages.SingleAsync(r => r.Identifiant == identifiant);
        Assert.Equal(3000 * 0.45359237, relu.Generaux.Poids, 6);
    }

    [Fact]
    public async Task Une_section_ajoutee_un_rapport_ajoute_et_un_renommage_sont_enregistres()
    {
        var identifiant = await PreparerAsync();
        using var travail = CreerFabrique().Creer();
        var copie = await travail.Reglages.ObtenirPourEditionAsync(identifiant);
        var editeur = OuvrirEditeur(copie);

        var pneus = editeur.Sections.Single(s => s.Categorie == CategorieDeReglage.Pneus);
        pneus.EstPresente = true;
        pneus.Champs.Single(c => c.Cle == "Pneus.PressionAvant").Texte = "2,1";
        editeur.Rapports.Ajouter();
        editeur.Rapports.Boite[2].Texte = "1,5";
        editeur.Nom = "Course sèche";
        Assert.True(editeur.EstValide);

        await travail.Reglages.ModifierAsync(editeur.Reglage, confirmerSuppressionDesChronos: true);

        await using var contexte = baseDeTest.CreerContexte();
        var relu = await contexte.Reglages.SingleAsync(r => r.Identifiant == identifiant);
        Assert.Equal("Course sèche", relu.Nom);
        Assert.Equal(2.1, relu.Pneus!.PressionAvant);
        Assert.Equal(new[] { 3.0, 2.0, 1.5 }, relu.RapportsDeBoite.OrderBy(r => r.Numero).Select(r => r.Valeur));
    }

    [Fact]
    public async Task Retirer_une_section_et_un_rapport_est_enregistre()
    {
        var identifiant = await PreparerAsync();
        using var travail = CreerFabrique().Creer();

        // D'abord on ajoute une section, puis on la retire dans une seconde édition.
        var premiere = OuvrirEditeur(await travail.Reglages.ObtenirPourEditionAsync(identifiant));
        premiere.Sections.Single(s => s.Categorie == CategorieDeReglage.Freins).EstPresente = true;
        await travail.Reglages.ModifierAsync(premiere.Reglage, confirmerSuppressionDesChronos: true);

        var seconde = OuvrirEditeur(await travail.Reglages.ObtenirPourEditionAsync(identifiant));
        Assert.True(seconde.Sections.Single(s => s.Categorie == CategorieDeReglage.Freins).EstPresente);
        seconde.Sections.Single(s => s.Categorie == CategorieDeReglage.Freins).EstPresente = false;
        seconde.Rapports.RetirerDernier();
        await travail.Reglages.ModifierAsync(seconde.Reglage, confirmerSuppressionDesChronos: true);

        await using var contexte = baseDeTest.CreerContexte();
        var relu = await contexte.Reglages.SingleAsync(r => r.Identifiant == identifiant);
        Assert.Null(relu.Freins);
        Assert.Single(relu.RapportsDeBoite);
    }

    [Fact]
    public async Task Le_reglage_d_origine_s_ouvre_en_lecture_seule_et_le_service_refuse_de_l_enregistrer()
    {
        await PreparerAsync();
        using var travail = CreerFabrique().Creer();

        var copie = await travail.Reglages.ObtenirPourEditionAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        var editeur = OuvrirEditeur(copie);

        Assert.False(editeur.EstModifiable);
        Assert.False(editeur.Generaux.Champs.Any(c => c.PeutEtreSaisi));

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => travail.Reglages.ModifierAsync(editeur.Reglage));
        Assert.Equal(CodeErreurMetier.ReglageOrigineNonModifiable, erreur.Code);
    }
}
