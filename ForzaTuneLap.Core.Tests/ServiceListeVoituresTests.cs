using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.Core.Tests;

public class ServiceListeVoituresTests : IDisposable
{
    private readonly BaseDeDonneesDeTest baseDeTest = new();

    public void Dispose() => baseDeTest.Dispose();

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
            Identifiant = new Guid(identifiant, 0, 0, new byte[8]),
            Generaux = new ReglagesGeneraux { Transmission = transmission, IndiceDePerformance = indice, Puissance = puissance }
        }
    };

    /// <summary>
    /// Alpha (id 1, France) : Éclair 2010 GT3, Foudre 2020 hypercar. Beta (id 2, Allemagne) : Tonnerre 1995 compacte.
    /// Plus la voiture personnalisée « Zeta » 2024 (Nascar) chez Alpha, et un réglage « Course » avec chrono sur Éclair.
    /// </summary>
    private async Task<(UniteDeTravail Travail, Guid ReglageCourse)> PreparerAsync()
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

        await baseDeTest.InitialiserAsync(fichier);
        var travail = new FabriqueUniteDeTravail(new FabriqueDeContextesDeTest(baseDeTest)).Creer();

        await travail.Catalogue.AjouterVoiturePersonnaliseeAsync(1, "Zeta", 2024, Division.Nascar);

        var brouillon = await travail.Reglages.CreerBrouillonAsync(1);
        brouillon.Nom = "Course";
        await travail.Reglages.EnregistrerNouveauAsync(brouillon);
        await travail.Chronos.AjouterAsync(brouillon.Identifiant, 1, TimeSpan.FromSeconds(90));

        travail.Contexte.ChangeTracker.Clear();
        return (travail, brouillon.Identifiant);
    }

    private static string[] Noms(IReadOnlyList<LigneVoiture> lignes) => lignes.Select(l => l.Voiture.Nom).ToArray();

    [Fact]
    public async Task Sans_filtre_toutes_les_voitures_sont_listees_par_marque_puis_par_nom()
    {
        var (travail, _) = await PreparerAsync();

        var lignes = await travail.ListeVoitures.ListerAsync();

        Assert.Equal(new[] { "Éclair", "Foudre", "Zeta", "Tonnerre" }, Noms(lignes));
        Assert.Equal(new[] { "Alpha", "Alpha", "Alpha", "Beta" }, lignes.Select(l => l.NomMarque));
    }

    [Fact]
    public async Task Chaque_ligne_indique_ses_reglages_ses_chronos_et_son_origine()
    {
        var (travail, _) = await PreparerAsync();

        var lignes = await travail.ListeVoitures.ListerAsync();

        var eclair = lignes.Single(l => l.Voiture.Nom == "Éclair");
        Assert.Equal(1, eclair.NombreDeReglagesCrees);
        Assert.Equal(1, eclair.NombreDeChronos);
        Assert.True(eclair.ADesReglagesOrigine);

        var foudre = lignes.Single(l => l.Voiture.Nom == "Foudre");
        Assert.Equal(0, foudre.NombreDeReglagesCrees);
        Assert.Equal(0, foudre.NombreDeChronos);

        var zeta = lignes.Single(l => l.Voiture.Nom == "Zeta");
        Assert.True(zeta.Voiture.EstPersonnalisee);
        Assert.False(zeta.ADesReglagesOrigine);
    }

    [Theory]
    [InlineData("eclair", "Éclair")]
    [InlineData("ALPHA 2020", "Foudre")]
    [InlineData("beta", "Tonnerre")]
    [InlineData("1995", "Tonnerre")]
    public async Task Le_texte_cherche_dans_la_marque_le_nom_et_l_annee(string texte, string attendue)
    {
        var (travail, _) = await PreparerAsync();

        var lignes = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { Texte = texte });

        Assert.Equal(new[] { attendue }, Noms(lignes));
    }

    [Fact]
    public async Task Les_filtres_de_marque_de_pays_et_de_division_fonctionnent()
    {
        var (travail, _) = await PreparerAsync();

        var parMarque = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { IdentifiantsMarques = { 2 } });
        var parPays = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { IdentifiantsPays = { 11 } });
        var parDivision = await travail.ListeVoitures.ListerAsync(
            new FiltreVoitures { Divisions = { Division.HypercarsModernes, Division.Nascar } });

        Assert.Equal(new[] { "Tonnerre" }, Noms(parMarque));
        Assert.Equal(new[] { "Éclair", "Foudre", "Zeta" }, Noms(parPays));
        Assert.Equal(new[] { "Foudre", "Zeta" }, Noms(parDivision));
    }

    [Fact]
    public async Task Les_bornes_d_annee_sont_inclusives()
    {
        var (travail, _) = await PreparerAsync();

        var lignes = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { AnneeMinimum = 2010, AnneeMaximum = 2020 });

        Assert.Equal(new[] { "Éclair", "Foudre" }, Noms(lignes));
    }

    [Fact]
    public async Task Les_filtres_sur_les_reglages_les_chronos_et_les_voitures_personnalisees_fonctionnent()
    {
        var (travail, _) = await PreparerAsync();

        var avecReglages = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { SeulementAvecReglagesCrees = true });
        var avecChronos = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { SeulementAvecChronos = true });
        var personnalisees = await travail.ListeVoitures.ListerAsync(new FiltreVoitures { SeulementPersonnalisees = true });

        Assert.Equal(new[] { "Éclair" }, Noms(avecReglages));
        Assert.Equal(new[] { "Éclair" }, Noms(avecChronos));
        Assert.Equal(new[] { "Zeta" }, Noms(personnalisees));
    }

    [Fact]
    public async Task Les_filtres_se_combinent_et_un_filtre_vide_ne_retire_rien()
    {
        var (travail, _) = await PreparerAsync();

        var combines = await travail.ListeVoitures.ListerAsync(
            new FiltreVoitures { IdentifiantsMarques = { 1 }, AnneeMinimum = 2015, Texte = "o" });
        var vide = await travail.ListeVoitures.ListerAsync(new FiltreVoitures());

        Assert.Equal(new[] { "Foudre" }, Noms(combines));
        Assert.Equal(4, vide.Count);
    }

    [Fact]
    public async Task Les_tris_fonctionnent()
    {
        var (travail, _) = await PreparerAsync();

        var croissant = await travail.ListeVoitures.ListerAsync(tri: TriVoitures.AnneeCroissante);
        var decroissant = await travail.ListeVoitures.ListerAsync(tri: TriVoitures.AnneeDecroissante);
        var parReglages = await travail.ListeVoitures.ListerAsync(tri: TriVoitures.NombreDeReglagesDecroissant);

        Assert.Equal(new[] { "Tonnerre", "Éclair", "Foudre", "Zeta" }, Noms(croissant));
        Assert.Equal(new[] { "Zeta", "Foudre", "Éclair", "Tonnerre" }, Noms(decroissant));
        Assert.Equal("Éclair", parReglages[0].Voiture.Nom);
    }

    [Fact]
    public async Task Les_options_ne_proposent_que_les_marques_et_divisions_presentes()
    {
        var (travail, _) = await PreparerAsync();

        var options = await travail.ListeVoitures.ObtenirOptionsAsync();

        Assert.Equal(new[] { "Alpha", "Beta" }, options.Marques.Select(m => m.Nom));
        Assert.Equal(new[] { 3, 1 }, options.Marques.Select(m => m.NombreDeVoitures));
        Assert.Equal(4, options.Divisions.Count);
        Assert.Contains(Division.Nascar, options.Divisions);
        Assert.DoesNotContain(Division.ForzaGT2, options.Divisions);
    }

    [Fact]
    public async Task Le_detail_liste_l_origine_d_abord_puis_les_reglages_de_l_utilisateur()
    {
        var (travail, reglageCourse) = await PreparerAsync();

        var detail = await travail.ListeVoitures.ObtenirDetailAsync(1);

        Assert.Equal("Éclair", detail.Voiture.Nom);
        Assert.Equal("Alpha", detail.NomMarque);
        Assert.Equal(2, detail.Reglages.Count);

        var origine = detail.Reglages[0];
        Assert.True(origine.EstOrigine);
        Assert.Equal(700, origine.IndiceDePerformance);
        Assert.Equal(300, origine.Puissance);
        Assert.Equal(Transmission.Propulsion, origine.Transmission);
        Assert.Equal(0, origine.NombreDeChronos);

        var course = detail.Reglages[1];
        Assert.Equal(reglageCourse, course.Identifiant);
        Assert.Equal("Course", course.Nom);
        Assert.False(course.EstOrigine);
        Assert.Equal(1, course.NombreDeChronos);
    }

    [Fact]
    public async Task Une_voiture_sans_reglage_a_un_detail_vide_et_une_voiture_inconnue_est_refusee()
    {
        var (travail, _) = await PreparerAsync();

        var zeta = await travail.ListeVoitures.ObtenirDetailAsync(ConstantesReferentiel.PremierIdentifiantPersonnalise);
        var erreur = await Assert.ThrowsAsync<ErreurMetierException>(() => travail.ListeVoitures.ObtenirDetailAsync(9999));

        Assert.Empty(zeta.Reglages);
        Assert.Equal(CodeErreurMetier.VoitureIntrouvable, erreur.Code);
    }
}
