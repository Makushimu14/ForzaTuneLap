using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class ServiceReglagesTests : IDisposable
{
    private const int IdentifiantVoiture = 1;

    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    /// <summary>Fichier d'une voiture dont l'origine a des pneus et des freins, mais pas d'aérodynamisme.</summary>
    private static FichierVoitures FichierOrigineRiche()
    {
        var fichier = BaseDeDonneesDeTest.FichierAvecUneVoiture(300);
        var origine = fichier.Marques[0].Voitures[0].ReglageOrigine!;
        origine.Pneus = new ReglagePneus { PressionAvant = 2.0, PressionArriere = 2.1 };
        origine.Freins = new ReglageFreins { PuissanceFreinageEquilibre = 50, PuissanceFreinagePression = 100 };
        return fichier;
    }

    private async Task<ContexteDonnees> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(FichierOrigineRiche());
        return baseDeTest.CreerContexte();
    }

    /// <summary>Enregistre un réglage nommé, créé à partir de l'origine, puis repart d'un contexte « propre ».</summary>
    private static async Task<Guid> EnregistrerReglageAsync(ContexteDonnees contexte, string nom = "Course")
    {
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(IdentifiantVoiture);
        brouillon.Nom = nom;
        await service.EnregistrerNouveauAsync(brouillon);
        contexte.ChangeTracker.Clear();
        return brouillon.Identifiant;
    }

    private static async Task AjouterChronoAsync(ContexteDonnees contexte, Guid identifiantReglage)
    {
        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(identifiantReglage));
        await contexte.SaveChangesAsync();
        contexte.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Le_brouillon_reprend_l_origine_sans_la_partager()
    {
        await using var contexte = await PreparerAsync();
        var service = new ServiceReglages(contexte);

        var brouillon = await service.CreerBrouillonAsync(IdentifiantVoiture);

        Assert.False(brouillon.EstOrigine);
        Assert.Null(brouillon.EmpreinteOrigine);
        Assert.NotEqual(BaseDeDonneesDeTest.IdentifiantReglageOrigine, brouillon.Identifiant);
        Assert.Equal(IdentifiantVoiture, brouillon.IdentifiantVoiture);
        Assert.Equal(string.Empty, brouillon.Nom);
        Assert.Equal(300, brouillon.Generaux.Puissance);
        Assert.Equal(2.1, brouillon.Pneus!.PressionArriere);
        Assert.NotNull(brouillon.Freins);
        Assert.Null(brouillon.Aerodynamisme);
        Assert.Equal(new[] { 3.0, 2.0 }, brouillon.RapportsDeBoite.Select(r => r.Valeur));

        brouillon.Pneus.PressionAvant = 9;
        var origine = await contexte.Reglages.AsNoTracking().SingleAsync(r => r.Identifiant == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        Assert.Equal(2.0, origine.Pneus!.PressionAvant);
    }

    [Fact]
    public async Task Sans_origine_le_brouillon_contient_toutes_les_sections()
    {
        await baseDeTest.InitialiserAsync();
        await using var contexte = baseDeTest.CreerContexte();
        var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
        await contexte.SaveChangesAsync();

        var brouillon = await new ServiceReglages(contexte).CreerBrouillonAsync(voiture.Identifiant);

        Assert.NotNull(brouillon.Pneus);
        Assert.NotNull(brouillon.Alignement);
        Assert.NotNull(brouillon.Ressorts);
        Assert.NotNull(brouillon.BarresAntiroulis);
        Assert.NotNull(brouillon.Amortisseurs);
        Assert.NotNull(brouillon.GeometrieDeSuspension);
        Assert.NotNull(brouillon.Differentiel);
        Assert.NotNull(brouillon.Freins);
        Assert.NotNull(brouillon.Aerodynamisme);
        Assert.NotNull(brouillon.Volant);
    }

    [Fact]
    public async Task Un_brouillon_pour_une_voiture_inconnue_est_refuse()
    {
        await using var contexte = await PreparerAsync();

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => new ServiceReglages(contexte).CreerBrouillonAsync(424242));

        Assert.Equal(CodeErreurMetier.VoitureIntrouvable, erreur.Code);
    }

    [Fact]
    public async Task Un_nouveau_reglage_est_enregistre_comme_reglage_de_creation()
    {
        await using var contexte = await PreparerAsync();
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(IdentifiantVoiture);
        brouillon.Nom = "  Mon réglage  ";
        brouillon.Generaux.Puissance = 320;
        brouillon.EstOrigine = true; // doit être ignoré

        await service.EnregistrerNouveauAsync(brouillon);

        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Reglages.SingleAsync(r => r.Identifiant == brouillon.Identifiant);
        Assert.Equal("Mon réglage", relu.Nom);
        Assert.False(relu.EstOrigine);
        Assert.Equal(320, relu.Generaux.Puissance);
        Assert.Equal(2, relu.RapportsDeBoite.Count);
    }

    [Fact]
    public async Task Un_nom_vide_est_refuse()
    {
        await using var contexte = await PreparerAsync();
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(IdentifiantVoiture);

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => service.EnregistrerNouveauAsync(brouillon));

        Assert.Equal(CodeErreurMetier.NomVide, erreur.Code);
    }

    [Fact]
    public async Task Un_nom_deja_utilise_pour_la_meme_voiture_est_refuse_sans_tenir_compte_de_la_casse()
    {
        await using var contexte = await PreparerAsync();
        await EnregistrerReglageAsync(contexte, "Course");
        var service = new ServiceReglages(contexte);
        var autre = await service.CreerBrouillonAsync(IdentifiantVoiture);
        autre.Nom = "COURSE";

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => service.EnregistrerNouveauAsync(autre));

        Assert.Equal(CodeErreurMetier.NomDejaUtilise, erreur.Code);
    }

    [Fact]
    public async Task Modifier_sans_chrono_applique_ajouts_retraits_et_remplacements()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(identifiant);
        edition.Generaux.Puissance = 400;
        edition.Pneus = null; // section retirée
        edition.Aerodynamisme = new ReglageAerodynamisme { PortanceNegativeAvant = 50, PortanceNegativeArriere = 80 }; // section ajoutée
        edition.Freins!.PuissanceFreinagePression = 120; // section modifiée
        edition.RapportsDeBoite = new List<RapportDeBoite> { new() { Numero = 1, Valeur = 4.0 } };

        var resultat = await service.ModifierAsync(edition);

        Assert.Equal(StatutModification.Modifie, resultat.Statut);
        Assert.Equal(0, resultat.NombreDeChronos);

        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Reglages.SingleAsync(r => r.Identifiant == identifiant);
        Assert.Equal(400, relu.Generaux.Puissance);
        Assert.Null(relu.Pneus);
        Assert.Equal(80, relu.Aerodynamisme!.PortanceNegativeArriere);
        Assert.Equal(120, relu.Freins!.PuissanceFreinagePression);
        Assert.Equal(50, relu.Freins.PuissanceFreinageEquilibre);
        Assert.Equal(new[] { 4.0 }, relu.RapportsDeBoite.Select(r => r.Valeur));
    }

    [Fact]
    public async Task Modifier_un_reglage_avec_chronos_demande_confirmation_et_ne_change_rien()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(identifiant);
        edition.Generaux.Puissance = 400;
        var resultat = await service.ModifierAsync(edition);

        Assert.Equal(StatutModification.ConfirmationRequise, resultat.Statut);
        Assert.Equal(1, resultat.NombreDeChronos);

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(300, (await autre.Reglages.SingleAsync(r => r.Identifiant == identifiant)).Generaux.Puissance);
        Assert.Equal(1, await autre.Chronos.CountAsync());
    }

    [Fact]
    public async Task Modifier_un_reglage_avec_chronos_apres_confirmation_supprime_les_chronos()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(identifiant);
        edition.Generaux.Puissance = 400;
        var resultat = await service.ModifierAsync(edition, confirmerSuppressionDesChronos: true);

        Assert.Equal(StatutModification.Modifie, resultat.Statut);
        Assert.Equal(1, resultat.NombreDeChronos);

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(400, (await autre.Reglages.SingleAsync(r => r.Identifiant == identifiant)).Generaux.Puissance);
        Assert.Equal(0, await autre.Chronos.CountAsync());
    }

    [Fact]
    public async Task Renommer_un_reglage_conserve_ses_chronos_sans_confirmation()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(identifiant);
        edition.Nom = "Autre nom";
        var resultat = await service.ModifierAsync(edition);

        Assert.Equal(StatutModification.Modifie, resultat.Statut);
        Assert.Equal(0, resultat.NombreDeChronos);

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal("Autre nom", (await autre.Reglages.SingleAsync(r => r.Identifiant == identifiant)).Nom);
        Assert.Equal(1, await autre.Chronos.CountAsync());
    }

    [Fact]
    public async Task Enregistrer_sans_rien_changer_conserve_les_chronos()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(identifiant);
        var resultat = await service.ModifierAsync(edition);

        Assert.Equal(StatutModification.Modifie, resultat.Statut);
        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(1, await autre.Chronos.CountAsync());
    }

    [Fact]
    public async Task Un_reglage_d_origine_n_est_ni_modifiable_ni_supprimable()
    {
        await using var contexte = await PreparerAsync();
        var service = new ServiceReglages(contexte);

        var edition = await service.ObtenirPourEditionAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        edition.Generaux.Puissance = 1;

        var erreurModification = await Assert.ThrowsAsync<ErreurMetierException>(() => service.ModifierAsync(edition));
        var erreurSuppression = await Assert.ThrowsAsync<ErreurMetierException>(() => service.SupprimerAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine));

        Assert.Equal(CodeErreurMetier.ReglageOrigineNonModifiable, erreurModification.Code);
        Assert.Equal(CodeErreurMetier.ReglageOrigineNonModifiable, erreurSuppression.Code);
    }

    [Fact]
    public async Task Dupliquer_cree_un_reglage_independant_sans_les_chronos()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);
        var service = new ServiceReglages(contexte);

        var copie = await service.DupliquerAsync(identifiant, "Copie");

        Assert.NotEqual(identifiant, copie.Identifiant);

        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Reglages.SingleAsync(r => r.Identifiant == copie.Identifiant);
        Assert.Equal("Copie", relu.Nom);
        Assert.Equal(300, relu.Generaux.Puissance);
        Assert.Equal(2, relu.RapportsDeBoite.Count);
        Assert.Equal(1, await autre.Chronos.CountAsync());
        Assert.Equal(0, await autre.Chronos.CountAsync(c => c.IdentifiantReglage == copie.Identifiant));
    }

    [Fact]
    public async Task Supprimer_retire_le_reglage_et_renvoie_le_nombre_de_chronos_supprimes()
    {
        await using var contexte = await PreparerAsync();
        var identifiant = await EnregistrerReglageAsync(contexte);
        await AjouterChronoAsync(contexte, identifiant);

        var nombre = await new ServiceReglages(contexte).SupprimerAsync(identifiant);

        Assert.Equal(1, nombre);
        await using var autre = baseDeTest.CreerContexte();
        Assert.False(await autre.Reglages.AnyAsync(r => r.Identifiant == identifiant));
        Assert.Equal(0, await autre.Chronos.CountAsync());
    }
}
