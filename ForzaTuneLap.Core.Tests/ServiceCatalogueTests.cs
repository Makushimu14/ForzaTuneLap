using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class ServiceCatalogueTests : IDisposable
{
    private const int MarqueOfficielle = 1;
    private const int VoitureOfficielle = 1;

    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private static VoitureFichier Nouvelle() => new()
    {
        Identifiant = 2,
        Nom = "Nouvelle",
        Annee = 2015,
        Division = Division.ForzaGT3,
        Valeur = 1,
        ReglageOrigine = new ReglageOrigineFichier
        {
            Identifiant = new Guid(2, 0, 0, new byte[8]),
            Generaux = new ReglagesGeneraux { IndiceDePerformance = 600 }
        }
    };

    /// <summary>Catalogue de départ : une marque officielle (id 1) et sa voiture (id 1).</summary>
    private static FichierVoitures Depart() => BaseDeDonneesDeTest.FichierAvecUneVoiture(300);

    /// <summary>Catalogue après mise à jour : la voiture « Nouvelle » (id 2) rejoint la marque officielle 1.</summary>
    private static FichierVoitures AvecNouvelleVoiture()
    {
        var fichier = Depart();
        fichier.Marques[0].Voitures.Add(Nouvelle());
        return fichier;
    }

    /// <summary>Catalogue après mise à jour : une marque « marque NEUVE » (id 2, Allemagne) avec la voiture « Nouvelle ».</summary>
    private static FichierVoitures AvecMarqueNeuve()
    {
        var fichier = Depart();
        fichier.Marques.Add(new MarqueFichier
        {
            Identifiant = 2,
            Nom = "marque NEUVE",
            IdentifiantPays = 2,
            Voitures = { Nouvelle() }
        });
        return fichier;
    }

    private async Task<(ContexteDonnees Contexte, ServiceCatalogue Catalogue)> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(Depart());
        var contexte = baseDeTest.CreerContexte();
        return (contexte, new ServiceCatalogue(contexte));
    }

    private static async Task<Guid> AjouterReglageAsync(ContexteDonnees contexte, int identifiantVoiture, string nom, bool avecChrono = false)
    {
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(identifiantVoiture);
        brouillon.Nom = nom;
        await service.EnregistrerNouveauAsync(brouillon);

        if (avecChrono)
        {
            await new ServiceChronos(contexte).AjouterAsync(brouillon.Identifiant, 1, TimeSpan.FromSeconds(90));
        }

        contexte.ChangeTracker.Clear();
        return brouillon.Identifiant;
    }

    private static async Task<CodeErreurMetier> CodeAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<ErreurMetierException>(action)).Code;

    // ------------------------------------------------------------ Marques

    [Fact]
    public async Task Une_marque_personnalisee_recoit_un_identifiant_a_partir_de_100000()
    {
        var (_, catalogue) = await PreparerAsync();

        var premiere = await catalogue.AjouterMarquePersonnaliseeAsync("  Ma marque  ", 11);
        var seconde = await catalogue.AjouterMarquePersonnaliseeAsync("Autre marque", 2);

        Assert.Equal(ConstantesReferentiel.PremierIdentifiantPersonnalise, premiere.Identifiant);
        Assert.Equal(ConstantesReferentiel.PremierIdentifiantPersonnalise + 1, seconde.Identifiant);
        Assert.True(premiere.EstPersonnalisee);
        Assert.Equal("Ma marque", premiere.Nom);
    }

    [Fact]
    public async Task Une_marque_existante_inconnue_ou_sans_nom_est_refusee()
    {
        var (_, catalogue) = await PreparerAsync();

        Assert.Equal(CodeErreurMetier.MarqueDejaExistante, await CodeAsync(() => catalogue.AjouterMarquePersonnaliseeAsync("MARQUE D'ORIGINE", 11)));
        Assert.Equal(CodeErreurMetier.PaysIntrouvable, await CodeAsync(() => catalogue.AjouterMarquePersonnaliseeAsync("Nouvelle marque", 9999)));
        Assert.Equal(CodeErreurMetier.NomVide, await CodeAsync(() => catalogue.AjouterMarquePersonnaliseeAsync("   ", 11)));
    }

    [Fact]
    public async Task Seule_une_marque_personnalisee_vide_peut_etre_supprimee()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var vide = await catalogue.AjouterMarquePersonnaliseeAsync("Vide", 11);
        var occupee = await catalogue.AjouterMarquePersonnaliseeAsync("Occupée", 11);
        await catalogue.AjouterVoiturePersonnaliseeAsync(occupee.Identifiant, "Modèle", 2020, Division.ForzaGT3);

        Assert.Equal(CodeErreurMetier.MarqueNonVide, await CodeAsync(() => catalogue.SupprimerMarquePersonnaliseeAsync(occupee.Identifiant)));
        Assert.Equal(CodeErreurMetier.MarqueOfficielleNonModifiable, await CodeAsync(() => catalogue.SupprimerMarquePersonnaliseeAsync(MarqueOfficielle)));

        await catalogue.SupprimerMarquePersonnaliseeAsync(vide.Identifiant);

        Assert.False(await contexte.Marques.AnyAsync(m => m.Identifiant == vide.Identifiant));
    }

    // ------------------------------------------------------------ Voitures

    [Fact]
    public async Task Une_voiture_personnalisee_peut_rejoindre_une_marque_officielle_ou_personnalisee()
    {
        var (_, catalogue) = await PreparerAsync();
        var marque = await catalogue.AjouterMarquePersonnaliseeAsync("Ma marque", 11);

        var chezOfficielle = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3, valeur: 5000, contenuTelechargeable: true);
        var chezPersonnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Autre", 2016, Division.Nascar);

        Assert.Equal(ConstantesReferentiel.PremierIdentifiantPersonnalise, chezOfficielle.Identifiant);
        Assert.Equal(ConstantesReferentiel.PremierIdentifiantPersonnalise + 1, chezPersonnalisee.Identifiant);
        Assert.True(chezOfficielle.EstPersonnalisee);
        Assert.Equal(5000, chezOfficielle.Valeur);
        Assert.True(chezOfficielle.ContenuTelechargeable);
        Assert.Equal(MarqueOfficielle, chezOfficielle.IdentifiantMarque);
    }

    [Fact]
    public async Task Une_voiture_en_double_ou_invalide_est_refusee()
    {
        var (_, catalogue) = await PreparerAsync();

        // Même marque, même nom (casse et accents ignorés), même année qu'une voiture officielle.
        Assert.Equal(
            CodeErreurMetier.VoitureDejaExistante,
            await CodeAsync(() => catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "VOITURE D'ORIGINE", 2000, Division.ForzaGT3)));
        Assert.Equal(CodeErreurMetier.AnneeInvalide, await CodeAsync(() => catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Ancienne", 1800, Division.ForzaGT3)));
        Assert.Equal(CodeErreurMetier.MarqueIntrouvable, await CodeAsync(() => catalogue.AjouterVoiturePersonnaliseeAsync(9999, "Orpheline", 2020, Division.ForzaGT3)));
        Assert.Equal(CodeErreurMetier.NomVide, await CodeAsync(() => catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, " ", 2020, Division.ForzaGT3)));

        // Une autre année est une autre voiture.
        var autreAnnee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Voiture d'origine", 2001, Division.ForzaGT3);
        Assert.True(autreAnnee.EstPersonnalisee);
    }

    [Fact]
    public async Task Une_voiture_personnalisee_se_modifie_mais_pas_une_officielle()
    {
        var (_, catalogue) = await PreparerAsync();
        var marque = await catalogue.AjouterMarquePersonnaliseeAsync("Ma marque", 11);
        var voiture = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Brouillon", 2015, Division.ForzaGT3);

        var modifiee = await catalogue.ModifierVoiturePersonnaliseeAsync(
            voiture.Identifiant, marque.Identifiant, "Définitive", 2017, Division.HypercarsModernes, valeur: 100);

        Assert.Equal(marque.Identifiant, modifiee.IdentifiantMarque);
        Assert.Equal("Définitive", modifiee.Nom);
        Assert.Equal(2017, modifiee.Annee);
        Assert.Equal(Division.HypercarsModernes, modifiee.Division);
        Assert.Equal(CodeErreurMetier.VoitureOfficielleNonModifiable, await CodeAsync(() =>
            catalogue.ModifierVoiturePersonnaliseeAsync(VoitureOfficielle, MarqueOfficielle, "Autre", 2000, Division.ForzaGT3)));
    }

    [Fact]
    public async Task Modifier_une_voiture_sans_changer_son_nom_ne_declenche_pas_le_refus_de_doublon()
    {
        var (_, catalogue) = await PreparerAsync();
        var voiture = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);

        var modifiee = await catalogue.ModifierVoiturePersonnaliseeAsync(
            voiture.Identifiant, MarqueOfficielle, "Nouvelle", 2015, Division.Nascar);

        Assert.Equal(Division.Nascar, modifiee.Division);
    }

    [Fact]
    public async Task Supprimer_une_voiture_avec_reglages_demande_confirmation_puis_supprime_tout()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var voiture = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);
        var reglage = await AjouterReglageAsync(contexte, voiture.Identifiant, "Course", avecChrono: true);

        var demande = await catalogue.SupprimerVoiturePersonnaliseeAsync(voiture.Identifiant);

        Assert.Equal(StatutSuppression.ConfirmationRequise, demande.Statut);
        Assert.Equal(1, demande.NombreDeReglages);
        Assert.Equal(1, demande.NombreDeChronos);
        Assert.True(await contexte.Voitures.AnyAsync(v => v.Identifiant == voiture.Identifiant));

        var effectue = await catalogue.SupprimerVoiturePersonnaliseeAsync(voiture.Identifiant, confirmerSuppressionDesReglages: true);

        Assert.Equal(StatutSuppression.Supprime, effectue.Statut);
        Assert.Equal(1, effectue.NombreDeReglages);
        Assert.False(await contexte.Voitures.AnyAsync(v => v.Identifiant == voiture.Identifiant));
        Assert.False(await contexte.Reglages.AnyAsync(r => r.Identifiant == reglage));
        Assert.Equal(0, await contexte.Chronos.CountAsync());
    }

    [Fact]
    public async Task Une_voiture_sans_reglage_est_supprimee_sans_confirmation_et_une_officielle_jamais()
    {
        var (_, catalogue) = await PreparerAsync();
        var voiture = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);

        var resultat = await catalogue.SupprimerVoiturePersonnaliseeAsync(voiture.Identifiant);

        Assert.Equal(StatutSuppression.Supprime, resultat.Statut);
        Assert.Equal(CodeErreurMetier.VoitureOfficielleNonModifiable, await CodeAsync(() => catalogue.SupprimerVoiturePersonnaliseeAsync(VoitureOfficielle)));
    }

    [Fact]
    public async Task Une_voiture_personnalisee_recoit_un_brouillon_de_reglage_a_zero()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var voiture = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);

        var brouillon = await new ServiceReglages(contexte).CreerBrouillonAsync(voiture.Identifiant);

        Assert.NotNull(brouillon.Pneus);
        Assert.NotNull(brouillon.Aerodynamisme);
    }

    // ------------------------------------------------------------ Fusions

    [Fact]
    public async Task Sans_correspondance_aucune_fusion_n_est_proposee()
    {
        var (_, catalogue) = await PreparerAsync();
        await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Inconnue", 2015, Division.ForzaGT3);

        var fusions = await catalogue.DetecterFusionsAsync();

        Assert.True(fusions.EstVide);
    }

    [Fact]
    public async Task Une_voiture_personnalisee_devenue_officielle_est_detectee_puis_fusionnee()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var personnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "nouvelle", 2015, Division.ForzaGT3);
        var reglage = await AjouterReglageAsync(contexte, personnalisee.Identifiant, "Course", avecChrono: true);

        // Le catalogue officiel est mis à jour avec la même voiture.
        await baseDeTest.InitialiserAsync(AvecNouvelleVoiture());

        var fusions = await catalogue.DetecterFusionsAsync();
        Assert.Empty(fusions.Marques);
        var correspondance = Assert.Single(fusions.Voitures);
        Assert.Equal(personnalisee.Identifiant, correspondance.Personnalisee.Identifiant);
        Assert.Equal(2, correspondance.Officielle.Identifiant);

        var resultat = await catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, 2);

        Assert.Equal(new ResultatFusion(1, 0, 1, 0, false), resultat);
        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Reglages.SingleAsync(r => r.Identifiant == reglage);
        Assert.Equal(2, relu.IdentifiantVoiture);
        Assert.Equal("Course", relu.Nom);
        Assert.Equal(1, await autre.Chronos.CountAsync(c => c.IdentifiantReglage == reglage));
        Assert.False(await autre.Voitures.AnyAsync(v => v.Identifiant == personnalisee.Identifiant));
        Assert.True((await new ServiceCatalogue(autre).DetecterFusionsAsync()).EstVide);
    }

    [Fact]
    public async Task Un_reglage_dont_le_nom_existe_deja_est_renomme_pendant_la_fusion()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var personnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);
        var reglage = await AjouterReglageAsync(contexte, personnalisee.Identifiant, "Course");

        await baseDeTest.InitialiserAsync(AvecNouvelleVoiture());
        await AjouterReglageAsync(contexte, 2, "Course"); // déjà un réglage « Course » sur la voiture officielle

        var resultat = await catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, 2);

        Assert.Equal(1, resultat.NombreDeReglagesDeplaces);
        Assert.Equal(1, resultat.NombreDeReglagesRenommes);
        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal("Course (2)", (await autre.Reglages.SingleAsync(r => r.Identifiant == reglage)).Nom);
        Assert.Equal(2, await autre.Reglages.CountAsync(r => r.IdentifiantVoiture == 2 && !r.EstOrigine));
    }

    [Fact]
    public async Task Fusionner_une_voiture_supprime_sa_marque_personnalisee_devenue_vide()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var marque = await catalogue.AjouterMarquePersonnaliseeAsync("Marque Neuve", 2);
        var personnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Nouvelle", 2015, Division.ForzaGT3);
        await AjouterReglageAsync(contexte, personnalisee.Identifiant, "Course");

        await baseDeTest.InitialiserAsync(AvecMarqueNeuve());

        var resultat = await catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, 2);

        Assert.True(resultat.MarqueSupprimee);
        await using var autre = baseDeTest.CreerContexte();
        Assert.False(await autre.Marques.AnyAsync(m => m.Identifiant == marque.Identifiant));
    }

    [Fact]
    public async Task Fusionner_une_marque_fusionne_les_voitures_equivalentes_et_deplace_les_autres()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var marque = await catalogue.AjouterMarquePersonnaliseeAsync("Marque Neuve", 2);
        var nouvelle = await catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Nouvelle", 2015, Division.ForzaGT3);
        var autre = await catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Autre", 2016, Division.ForzaGT3);
        var reglageNouvelle = await AjouterReglageAsync(contexte, nouvelle.Identifiant, "Course", avecChrono: true);
        var reglageAutre = await AjouterReglageAsync(contexte, autre.Identifiant, "Course");

        await baseDeTest.InitialiserAsync(AvecMarqueNeuve());

        var fusions = await catalogue.DetecterFusionsAsync();
        var correspondance = Assert.Single(fusions.Marques);
        Assert.Equal(marque.Identifiant, correspondance.Personnalisee.Identifiant);
        Assert.Equal(2, correspondance.Officielle.Identifiant);

        var resultat = await catalogue.FusionnerMarqueAsync(marque.Identifiant, 2);

        Assert.Equal(new ResultatFusion(1, 1, 1, 0, true), resultat);
        await using var relecture = baseDeTest.CreerContexte();
        Assert.False(await relecture.Marques.AnyAsync(m => m.Identifiant == marque.Identifiant));
        Assert.Equal(2, (await relecture.Voitures.SingleAsync(v => v.Identifiant == autre.Identifiant)).IdentifiantMarque);
        Assert.False(await relecture.Voitures.AnyAsync(v => v.Identifiant == nouvelle.Identifiant));
        Assert.Equal(2, (await relecture.Reglages.SingleAsync(r => r.Identifiant == reglageNouvelle)).IdentifiantVoiture);
        Assert.Equal(autre.Identifiant, (await relecture.Reglages.SingleAsync(r => r.Identifiant == reglageAutre)).IdentifiantVoiture);
        Assert.Equal(1, await relecture.Chronos.CountAsync(c => c.IdentifiantReglage == reglageNouvelle));
    }

    [Fact]
    public async Task Les_fusions_invalides_sont_refusees()
    {
        var (_, catalogue) = await PreparerAsync();
        var personnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);
        var autrePersonnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Autre", 2016, Division.ForzaGT3);
        var marque = await catalogue.AjouterMarquePersonnaliseeAsync("Ma marque", 11);

        Assert.Equal(CodeErreurMetier.FusionInvalide, await CodeAsync(() => catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, personnalisee.Identifiant)));
        Assert.Equal(CodeErreurMetier.FusionInvalide, await CodeAsync(() => catalogue.FusionnerVoitureAsync(VoitureOfficielle, personnalisee.Identifiant)));
        Assert.Equal(CodeErreurMetier.FusionInvalide, await CodeAsync(() => catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, autrePersonnalisee.Identifiant)));
        Assert.Equal(CodeErreurMetier.VoitureIntrouvable, await CodeAsync(() => catalogue.FusionnerVoitureAsync(personnalisee.Identifiant, 9999)));
        Assert.Equal(CodeErreurMetier.FusionInvalide, await CodeAsync(() => catalogue.FusionnerMarqueAsync(marque.Identifiant, marque.Identifiant)));
        Assert.Equal(CodeErreurMetier.FusionInvalide, await CodeAsync(() => catalogue.FusionnerMarqueAsync(MarqueOfficielle, marque.Identifiant)));
        Assert.Equal(CodeErreurMetier.MarqueIntrouvable, await CodeAsync(() => catalogue.FusionnerMarqueAsync(marque.Identifiant, 9999)));
    }

    [Fact]
    public async Task L_initialisation_du_catalogue_ne_touche_pas_aux_voitures_personnalisees()
    {
        var (contexte, catalogue) = await PreparerAsync();
        var personnalisee = await catalogue.AjouterVoiturePersonnaliseeAsync(MarqueOfficielle, "Nouvelle", 2015, Division.ForzaGT3);
        var reglage = await AjouterReglageAsync(contexte, personnalisee.Identifiant, "Course", avecChrono: true);

        await baseDeTest.InitialiserAsync(AvecNouvelleVoiture());

        await using var autre = baseDeTest.CreerContexte();
        Assert.True((await autre.Voitures.SingleAsync(v => v.Identifiant == personnalisee.Identifiant)).EstPersonnalisee);
        Assert.True(await autre.Reglages.AnyAsync(r => r.Identifiant == reglage));
        Assert.Equal(1, await autre.Chronos.CountAsync());
    }
}
