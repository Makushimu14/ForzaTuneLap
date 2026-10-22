using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.Core.Tests;

public class ServiceComparaisonChronosTests : IDisposable
{
    private const int ParcoursCible = 1;

    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    // Identifiant fixe du réglage d'origine de chaque voiture du jeu d'essai (même numéro que la voiture).
    private static Guid Origine(int numero) => new(numero, 0, 0, new byte[8]);

    private static VoitureFichier Voiture(
        int identifiant, string nom, int annee, Division division, Transmission transmission, int indice, double puissance) => new()
    {
        Identifiant = identifiant,
        Nom = nom,
        Annee = annee,
        Division = division,
        Valeur = 1,
        ReglageOrigine = new ReglageOrigineFichier
        {
            Identifiant = Origine(identifiant),
            Generaux = new ReglagesGeneraux { Transmission = transmission, IndiceDePerformance = indice, Puissance = puissance }
        }
    };

    /// <summary>
    /// Alpha (France) : Éclair 2010 GT3 propulsion IP 700 / 300 kW, Foudre 2020 hypercar intégrale IP 900 / 600 kW.
    /// Beta (Allemagne) : Tonnerre 1995 compacte traction IP 500 / 200 kW, avec des pneus renseignés (les autres non).
    /// </summary>
    private static FichierVoitures Fichier()
    {
        var fichier = new FichierVoitures
        {
            Marques =
            {
                new MarqueFichier
                {
                    Identifiant = 1, Nom = "Alpha", IdentifiantPays = 11,
                    Voitures =
                    {
                        Voiture(1, "Éclair", 2010, Division.ForzaGT3, Transmission.Propulsion, 700, 300),
                        Voiture(2, "Foudre", 2020, Division.HypercarsModernes, Transmission.TransmissionIntegrale, 900, 600)
                    }
                },
                new MarqueFichier
                {
                    Identifiant = 2, Nom = "Beta", IdentifiantPays = 2,
                    Voitures = { Voiture(3, "Tonnerre", 1995, Division.CompactesModernes, Transmission.Traction, 500, 200) }
                }
            }
        };
        fichier.Marques[1].Voitures[0].ReglageOrigine!.Pneus = new ReglagePneus { PressionAvant = 2.0, PressionArriere = 2.1 };
        return fichier;
    }

    /// <summary>
    /// Chronos sur le parcours 1 : Foudre 85 s, réglage créé d'Éclair 88 s, Éclair d'origine 90 s, Tonnerre 95 s.
    /// Plus un chrono de Foudre sur le parcours 2, qui ne doit jamais apparaître.
    /// </summary>
    private async Task<ServiceComparaisonChronos> PreparerAsync()
    {
        await baseDeTest.InitialiserAsync(Fichier());
        var contexte = baseDeTest.CreerContexte();

        var reglages = new ServiceReglages(contexte);
        var brouillon = await reglages.CreerBrouillonAsync(1);
        brouillon.Nom = "Mon réglage";
        await reglages.EnregistrerNouveauAsync(brouillon);

        var chronos = new ServiceChronos(contexte);
        await chronos.AjouterAsync(Origine(2), ParcoursCible, TimeSpan.FromSeconds(85));
        await chronos.AjouterAsync(brouillon.Identifiant, ParcoursCible, TimeSpan.FromSeconds(88));
        await chronos.AjouterAsync(Origine(1), ParcoursCible, TimeSpan.FromSeconds(90));
        await chronos.AjouterAsync(Origine(3), ParcoursCible, TimeSpan.FromSeconds(95));
        await chronos.AjouterAsync(Origine(2), 2, TimeSpan.FromSeconds(10));

        contexte.ChangeTracker.Clear();
        return new ServiceComparaisonChronos(contexte);
    }

    private static string[] Voitures(ResultatComparaisonChronos resultat) =>
        resultat.Lignes.Select(l => l.Voiture.Nom).ToArray();

    [Fact]
    public async Task Les_chronos_sont_classes_du_plus_rapide_au_plus_lent_avec_leur_ecart()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible);

        Assert.Equal(new[] { "Foudre", "Éclair", "Éclair", "Tonnerre" }, Voitures(resultat));
        Assert.Equal(new[] { 1, 2, 3, 4 }, resultat.Lignes.Select(l => l.Rang));
        Assert.Equal(new long[] { 0, 3_000, 5_000, 10_000 }, resultat.Lignes.Select(l => l.EcartMillisecondes));
        Assert.Equal(0, resultat.Lignes[0].EcartPourcentage);
        Assert.Equal(5000 * 100.0 / 85_000, resultat.Lignes[2].EcartPourcentage, 6);
        Assert.Equal(ParcoursCible, resultat.Parcours.Identifiant);
        Assert.NotNull(resultat.Parcours.Circuit);
    }

    [Fact]
    public async Task Chaque_ligne_donne_acces_au_reglage_et_a_la_marque()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible);

        var premiere = resultat.Lignes[0];
        Assert.Equal(Origine(2), premiere.Reglage.Identifiant);
        Assert.Equal("Alpha", premiere.Voiture.Marque!.Nom);
        Assert.Equal(85_000, premiere.Chrono.DureeMillisecondes);
        Assert.Equal(600, premiere.Reglage.Generaux.Puissance);
    }

    [Fact]
    public async Task Deux_temps_identiques_partagent_le_meme_rang()
    {
        await baseDeTest.InitialiserAsync(Fichier());
        await using var contexte = baseDeTest.CreerContexte();
        var chronos = new ServiceChronos(contexte);
        await chronos.AjouterAsync(Origine(1), ParcoursCible, TimeSpan.FromSeconds(90));
        await chronos.AjouterAsync(Origine(2), ParcoursCible, TimeSpan.FromSeconds(90));
        await chronos.AjouterAsync(Origine(3), ParcoursCible, TimeSpan.FromSeconds(95));

        var resultat = await new ServiceComparaisonChronos(contexte).ComparerAsync(ParcoursCible);

        Assert.Equal(new[] { 1, 1, 3 }, resultat.Lignes.Select(l => l.Rang));
    }

    [Fact]
    public async Task Un_parcours_sans_chrono_donne_une_liste_vide()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(3);

        Assert.Empty(resultat.Lignes);
    }

    [Fact]
    public async Task Un_parcours_inconnu_est_refuse()
    {
        var service = await PreparerAsync();

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => service.ComparerAsync(9999));

        Assert.Equal(CodeErreurMetier.ParcoursIntrouvable, erreur.Code);
    }

    [Fact]
    public async Task Le_filtre_par_marque_ne_garde_que_cette_marque()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible, new FiltreChronos { IdentifiantsMarques = { 2 } });

        Assert.Equal(new[] { "Tonnerre" }, Voitures(resultat));
        Assert.Equal(1, resultat.Lignes[0].Rang);
        Assert.Equal(0, resultat.Lignes[0].EcartMillisecondes);
    }

    [Fact]
    public async Task Le_filtre_par_pays_de_la_marque_fonctionne()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible, new FiltreChronos { IdentifiantsPays = { 11 } });

        Assert.Equal(new[] { "Foudre", "Éclair", "Éclair" }, Voitures(resultat));
    }

    [Fact]
    public async Task Le_filtre_par_division_accepte_plusieurs_valeurs()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { Divisions = { Division.HypercarsModernes, Division.CompactesModernes } });

        Assert.Equal(new[] { "Foudre", "Tonnerre" }, Voitures(resultat));
    }

    [Fact]
    public async Task Le_filtre_par_transmission_fonctionne()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible, new FiltreChronos { Transmissions = { Transmission.Propulsion } });

        Assert.Equal(new[] { "Éclair", "Éclair" }, Voitures(resultat));
    }

    [Fact]
    public async Task Les_filtres_d_annee_et_d_indice_de_performance_ont_des_bornes_inclusives()
    {
        var service = await PreparerAsync();

        var parAnnee = await service.ComparerAsync(ParcoursCible, new FiltreChronos { AnneeMinimum = 2010, AnneeMaximum = 2010 });
        var parIndice = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { IndiceDePerformanceMinimum = 500, IndiceDePerformanceMaximum = 700 });

        Assert.Equal(new[] { "Éclair", "Éclair" }, Voitures(parAnnee));
        Assert.Equal(new[] { "Éclair", "Éclair", "Tonnerre" }, Voitures(parIndice));
    }

    [Fact]
    public async Task Le_filtre_d_origine_separe_reglages_d_origine_et_de_creation()
    {
        var service = await PreparerAsync();

        var origine = await service.ComparerAsync(ParcoursCible, new FiltreChronos { Origine = OrigineDuReglage.Origine });
        var creation = await service.ComparerAsync(ParcoursCible, new FiltreChronos { Origine = OrigineDuReglage.Creation });

        Assert.Equal(3, origine.Lignes.Count);
        Assert.All(origine.Lignes, l => Assert.True(l.Reglage.EstOrigine));
        Assert.Single(creation.Lignes);
        Assert.Equal("Mon réglage", creation.Lignes[0].Reglage.Nom);
    }

    [Theory]
    [InlineData("tonnerre", "Tonnerre")]
    [InlineData("eclair", "Éclair")] // sans accent
    [InlineData("BETA", "Tonnerre")] // marque, casse ignorée
    [InlineData("mon règlage", "Éclair")] // nom du réglage (avec une faute d'accent)
    public async Task Le_filtre_texte_cherche_dans_la_marque_la_voiture_et_le_reglage(string texte, string voitureAttendue)
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible, new FiltreChronos { Texte = texte });

        Assert.NotEmpty(resultat.Lignes);
        Assert.All(resultat.Lignes, l => Assert.Equal(voitureAttendue, l.Voiture.Nom));
    }

    [Fact]
    public async Task Un_filtre_numerique_porte_sur_n_importe_quel_critere_du_catalogue()
    {
        var service = await PreparerAsync();

        var puissants = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { Criteres = { new FiltreNumerique("Generaux.Puissance", Minimum: 250) } });
        var moyens = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { Criteres = { new FiltreNumerique("Generaux.Puissance", Minimum: 250, Maximum: 400) } });

        Assert.Equal(new[] { "Foudre", "Éclair", "Éclair" }, Voitures(puissants));
        Assert.Equal(new[] { "Éclair", "Éclair" }, Voitures(moyens));
    }

    [Fact]
    public async Task Un_filtre_numerique_ecarte_les_reglages_dont_la_section_est_absente()
    {
        var service = await PreparerAsync();

        // Seul Tonnerre a des pneus renseignés.
        var resultat = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { Criteres = { new FiltreNumerique("Pneus.PressionAvant", Minimum: 1.0) } });

        Assert.Equal(new[] { "Tonnerre" }, Voitures(resultat));
    }

    [Fact]
    public async Task Un_critere_inconnu_est_refuse()
    {
        var service = await PreparerAsync();

        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos { Criteres = { new FiltreNumerique("Critere.Inexistant", Minimum: 1) } }));

        Assert.Equal(CodeErreurMetier.CritereInconnu, erreur.Code);
    }

    [Fact]
    public async Task Les_filtres_se_combinent()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(
            ParcoursCible,
            new FiltreChronos
            {
                IdentifiantsMarques = { 1 },
                Origine = OrigineDuReglage.Origine,
                IndiceDePerformanceMinimum = 800
            });

        Assert.Equal(new[] { "Foudre" }, Voitures(resultat));
    }

    [Fact]
    public async Task Un_filtre_vide_ne_retire_rien()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(ParcoursCible, new FiltreChronos());

        Assert.Equal(4, resultat.Lignes.Count);
    }
}
