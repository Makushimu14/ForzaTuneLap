using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class BaseDeDonneesTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    [Fact]
    public async Task L_initialisation_charge_le_referentiel_des_circuits()
    {
        await baseDeTest.InitialiserAsync();

        await using var contexte = baseDeTest.CreerContexte();
        Assert.Equal(19, await contexte.Pays.CountAsync());
        Assert.Equal(29, await contexte.Circuits.CountAsync());
        Assert.Equal(73, await contexte.Parcours.CountAsync());
        Assert.True(await contexte.Pays.AnyAsync(p => p.NomFrancais == "Afrique du Sud"));

        var nurburgring = await contexte.Parcours.SingleAsync(p => p.Identifiant == 58);
        Assert.Equal("Nürburgring - Complet", nurburgring.NomFrancais);
        Assert.Equal(25.38, nurburgring.LongueurKilometres, 5);
    }

    [Fact]
    public async Task L_initialisation_est_repetable()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));

        await using var contexte = baseDeTest.CreerContexte();
        Assert.Equal(19, await contexte.Pays.CountAsync());
        Assert.Equal(73, await contexte.Parcours.CountAsync());
        Assert.Equal(1, await contexte.Voitures.CountAsync());
        Assert.Equal(1, await contexte.Reglages.CountAsync());
    }

    [Fact]
    public async Task Les_sections_facultatives_absentes_restent_absentes_apres_relecture()
    {
        await baseDeTest.InitialiserAsync();
        Guid identifiant;

        await using (var contexte = baseDeTest.CreerContexte())
        {
            var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
            var reglage = BaseDeDonneesDeTest.CreerReglage(voiture.Identifiant);
            reglage.Pneus = new ReglagePneus { PressionAvant = 2.1, PressionArriere = 2.2 };
            reglage.Differentiel = new ReglageDifferentiel { Acceleration = 40, Deceleration = 10 };
            reglage.Freins = new ReglageFreins(); // toutes les valeurs à zéro : la section doit exister quand même
            reglage.RapportFinal = 3.5;
            reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = 1, Valeur = 3.0 });
            reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = 2, Valeur = 2.0 });
            contexte.Reglages.Add(reglage);
            identifiant = reglage.Identifiant;
            await contexte.SaveChangesAsync();
        }

        await using (var contexte = baseDeTest.CreerContexte())
        {
            var reglage = await contexte.Reglages.SingleAsync(r => r.Identifiant == identifiant);

            Assert.NotNull(reglage.Pneus);
            Assert.Equal(2.2, reglage.Pneus!.PressionArriere);
            Assert.NotNull(reglage.Differentiel);
            Assert.Null(reglage.Differentiel!.EquilibreCentral);
            Assert.NotNull(reglage.Freins);
            Assert.Null(reglage.Aerodynamisme);
            Assert.Null(reglage.Volant);
            Assert.Null(reglage.Alignement);
            Assert.Equal(Transmission.Propulsion, reglage.Generaux.Transmission);
            Assert.Equal(3.5, reglage.RapportFinal);
            Assert.Equal(new[] { 3.0, 2.0 }, reglage.RapportsDeBoite.OrderBy(r => r.Numero).Select(r => r.Valeur));
        }
    }

    [Fact]
    public async Task La_division_est_enregistree_sous_forme_de_texte()
    {
        await baseDeTest.InitialiserAsync();

        await using var contexte = baseDeTest.CreerContexte();
        BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
        await contexte.SaveChangesAsync();

        var texte = await contexte.Database.SqlQuery<string>($"SELECT Division AS Value FROM Voitures").SingleAsync();
        Assert.Equal("ForzaGT3", texte);
    }

    [Fact]
    public async Task Supprimer_un_reglage_supprime_ses_chronos_et_ses_rapports()
    {
        await baseDeTest.InitialiserAsync();
        Guid identifiant;

        await using (var contexte = baseDeTest.CreerContexte())
        {
            var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
            var reglage = BaseDeDonneesDeTest.CreerReglage(voiture.Identifiant);
            reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = 1, Valeur = 3.0 });
            contexte.Reglages.Add(reglage);
            contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant));
            identifiant = reglage.Identifiant;
            await contexte.SaveChangesAsync();
        }

        await using (var contexte = baseDeTest.CreerContexte())
        {
            // Suppression directe en base : on vérifie la cascade faite par la base elle-même.
            await contexte.Reglages.Where(r => r.Identifiant == identifiant).ExecuteDeleteAsync();

            Assert.Equal(0, await contexte.Chronos.CountAsync());
            var rapports = await contexte.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM RapportsDeBoite").SingleAsync();
            Assert.Equal(0, rapports);
        }
    }

    [Fact]
    public async Task Un_seul_chrono_est_possible_par_reglage_et_par_parcours()
    {
        await baseDeTest.InitialiserAsync();

        await using var contexte = baseDeTest.CreerContexte();
        var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
        var reglage = BaseDeDonneesDeTest.CreerReglage(voiture.Identifiant);
        contexte.Reglages.Add(reglage);
        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant, identifiantParcours: 1));
        await contexte.SaveChangesAsync();

        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant, identifiantParcours: 1));
        await Assert.ThrowsAsync<DbUpdateException>(() => contexte.SaveChangesAsync());
    }

    [Fact]
    public async Task Le_meme_reglage_peut_avoir_un_chrono_sur_chaque_parcours()
    {
        await baseDeTest.InitialiserAsync();

        await using var contexte = baseDeTest.CreerContexte();
        var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
        var reglage = BaseDeDonneesDeTest.CreerReglage(voiture.Identifiant);
        contexte.Reglages.Add(reglage);
        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant, identifiantParcours: 1));
        contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant, identifiantParcours: 2));
        await contexte.SaveChangesAsync();

        Assert.Equal(2, await contexte.Chronos.CountAsync());
    }

    [Fact]
    public void La_duree_d_un_chrono_est_enregistree_en_millisecondes()
    {
        var chrono = new Chrono { Duree = new TimeSpan(0, 0, 1, 23, 456) };

        Assert.Equal(83_456, chrono.DureeMillisecondes);
        Assert.Equal(new TimeSpan(0, 0, 1, 23, 456), chrono.Duree);
    }

    [Fact]
    public async Task Un_reglage_d_origine_inchange_est_conserve_et_un_reglage_corrige_est_remplace()
    {
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));

        await using (var contexte = baseDeTest.CreerContexte())
        {
            var origine = await contexte.Reglages.SingleAsync(r => r.Identifiant == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
            Assert.True(origine.EstOrigine);
            Assert.Equal(300, origine.Generaux.Puissance);
            Assert.Equal(2, origine.RapportsDeBoite.Count);

            // Un chrono sur le réglage d'origine, et des données utilisateur sans rapport avec le fichier.
            contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(origine.Identifiant));
            var voiture = BaseDeDonneesDeTest.AjouterVoiturePersonnalisee(contexte);
            var reglage = BaseDeDonneesDeTest.CreerReglage(voiture.Identifiant);
            contexte.Reglages.Add(reglage);
            contexte.Chronos.Add(BaseDeDonneesDeTest.CreerChrono(reglage.Identifiant));
            await contexte.SaveChangesAsync();
        }

        // Même fichier : rien ne change, les deux chronos restent.
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(300));
        await using (var contexte = baseDeTest.CreerContexte())
        {
            Assert.Equal(2, await contexte.Chronos.CountAsync());
        }

        // Fichier corrigé : le réglage d'origine est remplacé, son chrono disparaît, celui de l'utilisateur reste.
        await baseDeTest.InitialiserAsync(BaseDeDonneesDeTest.FichierAvecUneVoiture(350));
        await using (var contexte = baseDeTest.CreerContexte())
        {
            var origine = await contexte.Reglages.SingleAsync(r => r.Identifiant == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
            Assert.Equal(350, origine.Generaux.Puissance);
            Assert.Equal(2, origine.RapportsDeBoite.Count);

            Assert.Equal(1, await contexte.Chronos.CountAsync());
            Assert.Equal(2, await contexte.Reglages.CountAsync());
            Assert.True(await contexte.Voitures.AnyAsync(v => v.EstPersonnalisee));
        }
    }
}
