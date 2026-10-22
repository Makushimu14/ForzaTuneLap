using ForzaTuneLap.Core.Criteres;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.Core.Tests;

public class ServiceComparaisonReglagesTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

    private static VoitureFichier Voiture(
        int identifiant, string nom, int annee, Division division, Transmission transmission,
        int indice, double puissance, double acceleration) => new()
    {
        Identifiant = identifiant,
        Nom = nom,
        Annee = annee,
        Division = division,
        Valeur = 1,
        ReglageOrigine = new ReglageOrigineFichier
        {
            Identifiant = new Guid(identifiant, 0, 0, new byte[8]),
            Generaux = new ReglagesGeneraux { Transmission = transmission, IndiceDePerformance = indice, Puissance = puissance },
            Performances = new PerformancesMesurees { AccelerationDeZeroA97 = acceleration }
        }
    };

    /// <summary>
    /// Alpha (France) : Éclair 2010 GT3 propulsion IP 700, 300 kW, 0-97 en 4,5 s ; Foudre 2020 hypercar intégrale IP 900, 600 kW, 2,8 s.
    /// Beta (Allemagne) : Tonnerre 1995 compacte traction IP 500, 200 kW, 6,0 s, seule voiture avec des pneus renseignés.
    /// Plus un réglage créé par l'utilisateur à partir d'Éclair, à 320 kW.
    /// </summary>
    private async Task<ServiceComparaisonReglages> PreparerAsync()
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
                        Voiture(1, "Éclair", 2010, Division.ForzaGT3, Transmission.Propulsion, 700, 300, 4.5),
                        Voiture(2, "Foudre", 2020, Division.HypercarsModernes, Transmission.TransmissionIntegrale, 900, 600, 2.8)
                    }
                },
                new MarqueFichier
                {
                    Identifiant = 2, Nom = "Beta", IdentifiantPays = 2,
                    Voitures = { Voiture(3, "Tonnerre", 1995, Division.CompactesModernes, Transmission.Traction, 500, 200, 6.0) }
                }
            }
        };
        fichier.Marques[1].Voitures[0].ReglageOrigine!.Pneus = new ReglagePneus { PressionAvant = 2.0, PressionArriere = 2.1 };

        await baseDeTest.InitialiserAsync(fichier);
        var contexte = baseDeTest.CreerContexte();

        var reglages = new ServiceReglages(contexte);
        var brouillon = await reglages.CreerBrouillonAsync(1);
        brouillon.Nom = "Mon réglage";
        brouillon.Generaux.Puissance = 320;
        await reglages.EnregistrerNouveauAsync(brouillon);

        contexte.ChangeTracker.Clear();
        return new ServiceComparaisonReglages(contexte);
    }

    private static string[] Noms(ResultatComparaisonReglages resultat) =>
        resultat.Lignes.Select(l => $"{l.Voiture.Nom}/{l.Reglage.Nom}").ToArray();

    [Fact]
    public async Task Sans_filtre_tous_les_reglages_sont_compares_origine_et_creation()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance");

        Assert.Equal(4, resultat.Lignes.Count);
        Assert.Equal(2, resultat.Lignes.Count(l => l.Voiture.Nom == "Éclair"));
        Assert.Contains(resultat.Lignes, l => l.Reglage.EstOrigine);
        Assert.Contains(resultat.Lignes, l => !l.Reglage.EstOrigine);
    }

    [Fact]
    public async Task Un_critere_sans_sens_prefere_est_classe_du_plus_grand_au_plus_petit()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance");

        Assert.Equal(SensDuTri.Decroissant, resultat.Sens);
        Assert.Equal(new[] { "Foudre/Origine", "Éclair/Mon réglage", "Éclair/Origine", "Tonnerre/Origine" }, Noms(resultat));
        Assert.Equal(new int?[] { 1, 2, 3, 4 }, resultat.Lignes.Select(l => l.Rang));
        Assert.Equal(new double?[] { 0, -280, -300, -400 }, resultat.Lignes.Select(l => l.Ecart));
        Assert.Equal(-280 * 100.0 / 600, resultat.Lignes[1].EcartPourcentage!.Value, 6);
    }

    [Fact]
    public async Task Un_critere_ou_le_plus_petit_est_meilleur_est_classe_du_plus_petit_au_plus_grand()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Performances.AccelerationDeZeroA97");

        Assert.Equal(SensDuTri.Croissant, resultat.Sens);
        Assert.Equal(new[] { "Foudre/Origine", "Éclair/Mon réglage", "Éclair/Origine", "Tonnerre/Origine" }, Noms(resultat));

        // Les deux réglages d'Éclair ont le même temps : même rang, puis le suivant saute une place.
        Assert.Equal(new int?[] { 1, 2, 2, 4 }, resultat.Lignes.Select(l => l.Rang));
        Assert.Equal((4.5 - 2.8) * 100 / 2.8, resultat.Lignes[1].EcartPourcentage!.Value, 6);
    }

    [Fact]
    public async Task Le_sens_du_tri_peut_etre_impose()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance", sens: SensDuTri.Croissant);

        Assert.Equal(new[] { "Tonnerre/Origine", "Éclair/Origine", "Éclair/Mon réglage", "Foudre/Origine" }, Noms(resultat));
        Assert.Equal(new int?[] { 1, 2, 3, 4 }, resultat.Lignes.Select(l => l.Rang));
    }

    [Fact]
    public async Task Les_reglages_sans_valeur_pour_le_critere_sont_classes_en_dernier_sans_rang()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Pneus.PressionAvant");

        Assert.Equal("Tonnerre", resultat.Lignes[0].Voiture.Nom);
        Assert.Equal(1, resultat.Lignes[0].Rang);
        Assert.All(resultat.Lignes.Skip(1), l =>
        {
            Assert.Null(l.Rang);
            Assert.Null(l.Ecart);
            Assert.Null(l.EcartPourcentage);
            Assert.Null(l.Valeurs["Pneus.PressionAvant"]);
        });
    }

    [Fact]
    public async Task Par_defaut_toutes_les_valeurs_du_catalogue_sont_fournies()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance");

        Assert.Equal(CatalogueCriteres.Tous.Count, resultat.CriteresAffiches.Count);
        Assert.Equal(CatalogueCriteres.Tous.Count, resultat.Statistiques.Count);
        Assert.Equal(CatalogueCriteres.Tous.Count, resultat.Lignes[0].Valeurs.Count);
    }

    [Fact]
    public async Task Les_criteres_affiches_commencent_par_le_critere_de_tri_sans_doublon()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(
            "Generaux.Puissance",
            clesAffichees: new[] { "Generaux.Poids", "Generaux.Puissance", "Generaux.Poids" });

        Assert.Equal(new[] { "Generaux.Puissance", "Generaux.Poids" }, resultat.CriteresAffiches.Select(c => c.Cle));
        Assert.Equal(2, resultat.Lignes[0].Valeurs.Count);
    }

    [Fact]
    public async Task Les_statistiques_resument_les_reglages_retenus()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance");

        var puissance = resultat.Statistiques.Single(s => s.CleCritere == "Generaux.Puissance");
        Assert.Equal(4, puissance.NombreDeValeurs);
        Assert.Equal(200, puissance.Minimum);
        Assert.Equal(600, puissance.Maximum);
        Assert.Equal(355, puissance.Moyenne);
        Assert.Equal(310, puissance.Mediane); // moyenne des deux valeurs du milieu (300 et 320)

        var pneus = resultat.Statistiques.Single(s => s.CleCritere == "Pneus.PressionAvant");
        Assert.Equal(1, pneus.NombreDeValeurs);
        Assert.Equal(2.0, pneus.Mediane);

        var aerodynamisme = resultat.Statistiques.Single(s => s.CleCritere == "Aerodynamisme.PortanceNegativeAvant");
        Assert.Equal(0, aerodynamisme.NombreDeValeurs);
        Assert.Null(aerodynamisme.Minimum);
        Assert.Null(aerodynamisme.Moyenne);
    }

    [Fact]
    public async Task Les_filtres_limitent_les_reglages_compares()
    {
        var service = await PreparerAsync();

        var parMarque = await service.ComparerAsync("Generaux.Puissance", new FiltreReglages { IdentifiantsMarques = { 1 } });
        var parOrigine = await service.ComparerAsync("Generaux.Puissance", new FiltreReglages { Origine = OrigineDuReglage.Creation });
        var parCritere = await service.ComparerAsync(
            "Generaux.Puissance",
            new FiltreReglages { Criteres = { new FiltreNumerique("Generaux.Puissance", Minimum: 250, Maximum: 400) } });

        Assert.Equal(new[] { "Foudre/Origine", "Éclair/Mon réglage", "Éclair/Origine" }, Noms(parMarque));
        Assert.Equal(new[] { "Éclair/Mon réglage" }, Noms(parOrigine));
        Assert.Equal(new[] { "Éclair/Mon réglage", "Éclair/Origine" }, Noms(parCritere));
    }

    [Fact]
    public async Task Le_rang_et_l_ecart_sont_calcules_sur_les_reglages_retenus_seulement()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync(
            "Generaux.Puissance",
            new FiltreReglages { IdentifiantsMarques = { 2 } });

        Assert.Single(resultat.Lignes);
        Assert.Equal(1, resultat.Lignes[0].Rang);
        Assert.Equal(0, resultat.Lignes[0].Ecart);
    }

    [Fact]
    public async Task Les_filtres_des_chronos_sont_des_filtres_de_reglages()
    {
        var service = await PreparerAsync();
        FiltreReglages filtre = new FiltreChronos { Divisions = { Division.HypercarsModernes } };

        var resultat = await service.ComparerAsync("Generaux.Puissance", filtre);

        Assert.Equal(new[] { "Foudre/Origine" }, Noms(resultat));
    }

    [Fact]
    public async Task Aucun_reglage_ne_correspond_donne_un_resultat_vide_sans_erreur()
    {
        var service = await PreparerAsync();

        var resultat = await service.ComparerAsync("Generaux.Puissance", new FiltreReglages { AnneeMinimum = 2100 });

        Assert.Empty(resultat.Lignes);
        Assert.All(resultat.Statistiques, s => Assert.Equal(0, s.NombreDeValeurs));
    }

    [Fact]
    public async Task Un_critere_inconnu_est_refuse_pour_le_tri_comme_pour_l_affichage()
    {
        var service = await PreparerAsync();

        var erreurTri = await Assert.ThrowsAsync<ErreurMetierException>(() => service.ComparerAsync("Critere.Inexistant"));
        var erreurAffichage = await Assert.ThrowsAsync<ErreurMetierException>(() => service.ComparerAsync(
            "Generaux.Puissance",
            clesAffichees: new[] { "Critere.Inexistant" }));

        Assert.Equal(CodeErreurMetier.CritereInconnu, erreurTri.Code);
        Assert.Equal(CodeErreurMetier.CritereInconnu, erreurAffichage.Code);
    }
}
