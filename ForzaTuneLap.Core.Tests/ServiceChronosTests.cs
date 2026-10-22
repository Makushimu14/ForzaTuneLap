using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class ServiceChronosTests : IDisposable
{
    private static readonly DateTime DateDeTest = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private async Task<ContexteDonnees> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        return baseDeTest.CreerContexte();
    }

    /// <summary>Crée un réglage de l'utilisateur (la voiture 1 vient du fichier de données).</summary>
    private static async Task<Guid> CreerReglageAsync(ContexteDonnees contexte, string nom)
    {
        var service = new ServiceReglages(contexte);
        var brouillon = await service.CreerBrouillonAsync(1);
        brouillon.Nom = nom;
        await service.EnregistrerNouveauAsync(brouillon);
        return brouillon.Identifiant;
    }

    private static async Task<ErreurMetierException> ErreurAsync(Func<Task> action) =>
        await Assert.ThrowsAsync<ErreurMetierException>(action);

    [Fact]
    public async Task Ajouter_un_chrono_l_enregistre_en_millisecondes()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");

        var chrono = await new ServiceChronos(contexte).AjouterAsync(
            reglage, 1, new TimeSpan(0, 0, 1, 23, 456), DateDeTest, "  Pluie  ");

        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Chronos.SingleAsync(c => c.Identifiant == chrono.Identifiant);
        Assert.Equal(83_456, relu.DureeMillisecondes);
        Assert.Equal(DateDeTest, relu.DateRealisation);
        Assert.Equal("Pluie", relu.Commentaire);
        Assert.Equal(reglage, relu.IdentifiantReglage);
        Assert.Equal(1, relu.IdentifiantParcours);
    }

    [Fact]
    public async Task Un_commentaire_vide_est_enregistre_comme_absent()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");

        var chrono = await new ServiceChronos(contexte).AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90), commentaire: "   ");

        Assert.Null(chrono.Commentaire);
    }

    [Fact]
    public async Task Ajouter_un_deuxieme_chrono_pour_le_meme_reglage_et_parcours_est_refuse()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));

        // Même un temps plus rapide est refusé : il faut passer par la modification.
        var erreur = await ErreurAsync(() => service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(80)));

        Assert.Equal(CodeErreurMetier.ChronoDejaExistant, erreur.Code);
        await using var autre = baseDeTest.CreerContexte();
        var chrono = await autre.Chronos.SingleAsync();
        Assert.Equal(90_000, chrono.DureeMillisecondes);
    }

    [Fact]
    public async Task Un_meme_reglage_peut_avoir_un_chrono_par_parcours_et_un_meme_parcours_un_chrono_par_reglage()
    {
        await using var contexte = await PreparerAsync();
        var premier = await CreerReglageAsync(contexte, "Course");
        var second = await CreerReglageAsync(contexte, "Qualification");
        var service = new ServiceChronos(contexte);

        await service.AjouterAsync(premier, 1, TimeSpan.FromSeconds(90));
        await service.AjouterAsync(premier, 2, TimeSpan.FromSeconds(95));
        await service.AjouterAsync(second, 1, TimeSpan.FromSeconds(91));

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(3, await autre.Chronos.CountAsync());
    }

    [Fact]
    public async Task Un_chrono_peut_etre_rattache_au_reglage_d_origine()
    {
        await using var contexte = await PreparerAsync();

        var chrono = await new ServiceChronos(contexte).AjouterAsync(
            BaseDeDonneesDeTest.IdentifiantReglageOrigine, 1, TimeSpan.FromSeconds(90));

        Assert.Equal(BaseDeDonneesDeTest.IdentifiantReglageOrigine, chrono.IdentifiantReglage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Une_duree_nulle_ou_negative_est_refusee(int secondes)
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");

        var erreur = await ErreurAsync(() => new ServiceChronos(contexte).AjouterAsync(reglage, 1, TimeSpan.FromSeconds(secondes)));

        Assert.Equal(CodeErreurMetier.DureeInvalide, erreur.Code);
    }

    [Fact]
    public async Task Un_reglage_ou_un_parcours_inconnu_est_refuse()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);

        var reglageInconnu = await ErreurAsync(() => service.AjouterAsync(Guid.NewGuid(), 1, TimeSpan.FromSeconds(90)));
        var parcoursInconnu = await ErreurAsync(() => service.AjouterAsync(reglage, 9999, TimeSpan.FromSeconds(90)));

        Assert.Equal(CodeErreurMetier.ReglageIntrouvable, reglageInconnu.Code);
        Assert.Equal(CodeErreurMetier.ParcoursIntrouvable, parcoursInconnu.Code);
    }

    [Fact]
    public async Task Modifier_un_chrono_est_toujours_possible_meme_vers_un_temps_plus_lent()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        var chrono = await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90), DateDeTest, "Avant");

        var nouvelleDate = DateDeTest.AddDays(1);
        await service.ModifierAsync(chrono.Identifiant, TimeSpan.FromSeconds(100), nouvelleDate, "Après");

        await using var autre = baseDeTest.CreerContexte();
        var relu = await autre.Chronos.SingleAsync();
        Assert.Equal(100_000, relu.DureeMillisecondes);
        Assert.Equal(nouvelleDate, relu.DateRealisation);
        Assert.Equal("Après", relu.Commentaire);
        Assert.Equal(reglage, relu.IdentifiantReglage);
        Assert.Equal(1, relu.IdentifiantParcours);
    }

    [Fact]
    public async Task Apres_une_modification_l_ajout_pour_le_meme_couple_reste_refuse()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        var chrono = await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));
        await service.ModifierAsync(chrono.Identifiant, TimeSpan.FromSeconds(85), DateDeTest);

        var erreur = await ErreurAsync(() => service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(80)));

        Assert.Equal(CodeErreurMetier.ChronoDejaExistant, erreur.Code);
    }

    [Fact]
    public async Task Modifier_un_chrono_inconnu_ou_avec_une_duree_invalide_est_refuse()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        var chrono = await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));

        var inconnu = await ErreurAsync(() => service.ModifierAsync(Guid.NewGuid(), TimeSpan.FromSeconds(90), DateDeTest));
        var invalide = await ErreurAsync(() => service.ModifierAsync(chrono.Identifiant, TimeSpan.Zero, DateDeTest));

        Assert.Equal(CodeErreurMetier.ChronoIntrouvable, inconnu.Code);
        Assert.Equal(CodeErreurMetier.DureeInvalide, invalide.Code);
    }

    [Fact]
    public async Task Apres_une_suppression_un_nouveau_chrono_peut_etre_ajoute()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        var chrono = await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));

        await service.SupprimerAsync(chrono.Identifiant);
        await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(95));

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(95_000, (await autre.Chronos.SingleAsync()).DureeMillisecondes);
    }

    [Fact]
    public async Task Supprimer_un_chrono_inconnu_est_refuse()
    {
        await using var contexte = await PreparerAsync();

        var erreur = await ErreurAsync(() => new ServiceChronos(contexte).SupprimerAsync(Guid.NewGuid()));

        Assert.Equal(CodeErreurMetier.ChronoIntrouvable, erreur.Code);
    }

    [Fact]
    public async Task Rechercher_renvoie_le_chrono_du_couple_ou_rien()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var service = new ServiceChronos(contexte);
        await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));

        var present = await service.RechercherAsync(reglage, 1);
        var absent = await service.RechercherAsync(reglage, 2);

        Assert.NotNull(present);
        Assert.Equal(90_000, present!.DureeMillisecondes);
        Assert.Null(absent);
    }

    [Fact]
    public async Task Lister_renvoie_les_chronos_du_reglage_avec_leur_parcours()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var autreReglage = await CreerReglageAsync(contexte, "Qualification");
        var service = new ServiceChronos(contexte);
        await service.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));
        await service.AjouterAsync(reglage, 2, TimeSpan.FromSeconds(95));
        await service.AjouterAsync(autreReglage, 1, TimeSpan.FromSeconds(91));

        var chronos = await service.ListerDuReglageAsync(reglage);

        Assert.Equal(2, chronos.Count);
        Assert.All(chronos, c => Assert.NotNull(c.Parcours?.Circuit));
        Assert.All(chronos, c => Assert.Equal(reglage, c.IdentifiantReglage));
    }

    [Fact]
    public async Task Modifier_le_contenu_d_un_reglage_apres_confirmation_libere_ses_parcours()
    {
        await using var contexte = await PreparerAsync();
        var reglage = await CreerReglageAsync(contexte, "Course");
        var chronos = new ServiceChronos(contexte);
        await chronos.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(90));
        contexte.ChangeTracker.Clear();

        var reglages = new ServiceReglages(contexte);
        var edition = await reglages.ObtenirPourEditionAsync(reglage);
        edition.Generaux.Puissance = 999;
        await reglages.ModifierAsync(edition, confirmerSuppressionDesChronos: true);

        // Le chrono a été supprimé avec l'ancien contenu : on peut en saisir un nouveau pour ce parcours.
        await chronos.AjouterAsync(reglage, 1, TimeSpan.FromSeconds(88));

        await using var autre = baseDeTest.CreerContexte();
        Assert.Equal(88_000, (await autre.Chronos.SingleAsync()).DureeMillisecondes);
    }
}
