using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

public class ServiceSauvegardeTests : IDisposable
{
    private readonly List<Appareil> appareils = new();

    public void Dispose()
    {
        foreach (var appareil in appareils)
        {
            appareil.Dispose();
        }
    }

    /// <summary>Un appareil : sa propre base en mémoire et ses services, à partir d'un catalogue officiel.</summary>
    private sealed class Appareil : IDisposable
    {
        public BaseDeDonneesDeTest Base { get; } = new();
        public ContexteDonnees Contexte { get; private set; } = null!;
        public ServiceCatalogue Catalogue { get; private set; } = null!;
        public ServiceReglages Reglages { get; private set; } = null!;
        public ServiceChronos Chronos { get; private set; } = null!;
        public ServiceSauvegarde Sauvegarde { get; private set; } = null!;

        public async Task InitialiserAsync(FichierVoitures catalogue)
        {
            await Base.InitialiserAsync(catalogue);
            Contexte = Base.CreerContexte();
            Catalogue = new ServiceCatalogue(Contexte);
            Reglages = new ServiceReglages(Contexte);
            Chronos = new ServiceChronos(Contexte);
            Sauvegarde = new ServiceSauvegarde(Contexte);
        }

        public async Task<Guid> AjouterReglageAsync(int identifiantVoiture, string nom, int? parcours = null, double secondes = 90)
        {
            var brouillon = await Reglages.CreerBrouillonAsync(identifiantVoiture);
            brouillon.Nom = nom;
            await Reglages.EnregistrerNouveauAsync(brouillon);

            if (parcours is { } identifiantParcours)
            {
                await Chronos.AjouterAsync(brouillon.Identifiant, identifiantParcours, TimeSpan.FromSeconds(secondes));
            }

            Contexte.ChangeTracker.Clear();
            return brouillon.Identifiant;
        }

        public async Task ModifierReglageAsync(Guid identifiant, Action<Reglage> modification)
        {
            var edition = await Reglages.ObtenirPourEditionAsync(identifiant);
            modification(edition);
            await Reglages.ModifierAsync(edition, confirmerSuppressionDesChronos: true);
            Contexte.ChangeTracker.Clear();
        }

        public async Task ModifierChronoAsync(Guid identifiantReglage, int parcours, double secondes)
        {
            var chrono = await Chronos.RechercherAsync(identifiantReglage, parcours);
            await Chronos.ModifierAsync(chrono!.Identifiant, TimeSpan.FromSeconds(secondes), chrono.DateRealisation);
            Contexte.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            Contexte?.Dispose();
            Base.Dispose();
        }
    }

    private static FichierVoitures Depart() => BaseDeDonneesDeTest.FichierAvecUneVoiture(300);

    /// <summary>Catalogue où la voiture « Nouvelle » 2015 (id 2) est officielle, chez la marque officielle 1.</summary>
    private static FichierVoitures AvecNouvelleVoiture()
    {
        var fichier = Depart();
        fichier.Marques[0].Voitures.Add(new VoitureFichier
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
        });
        return fichier;
    }

    private async Task<Appareil> CreerAppareilAsync(FichierVoitures? catalogue = null)
    {
        var appareil = new Appareil();
        appareils.Add(appareil);
        await appareil.InitialiserAsync(catalogue ?? Depart());
        return appareil;
    }

    private static async Task<CodeErreurMetier> CodeAsync(Func<Task> action) =>
        (await Assert.ThrowsAsync<ErreurMetierException>(action)).Code;

    // ------------------------------------------------------------ Export

    [Fact]
    public async Task L_export_contient_les_donnees_de_l_utilisateur_et_pas_le_catalogue_officiel()
    {
        var a = await CreerAppareilAsync();
        var marque = await a.Catalogue.AjouterMarquePersonnaliseeAsync("Ma marque", 11);
        var perso = await a.Catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Nouvelle", 2015, Division.ForzaGT3, 1234, true);
        var reglageOfficielle = await a.AjouterReglageAsync(1, "Course", 1);
        var reglagePerso = await a.AjouterReglageAsync(perso.Identifiant, "Perso", 1);
        await a.Chronos.AjouterAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine, 2, TimeSpan.FromSeconds(100));

        var fichier = await a.Sauvegarde.ConstruireSauvegardeAsync();

        Assert.Equal(FichierSauvegarde.VersionActuelle, fichier.Version);
        Assert.Equal(DateTimeKind.Utc, fichier.DateExport.Kind);
        Assert.Single(fichier.MarquesPersonnalisees);
        Assert.Single(fichier.VoituresPersonnalisees);
        Assert.Equal(2, fichier.Reglages.Count);
        Assert.DoesNotContain(fichier.Reglages, r => r.Contenu.Identifiant == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        Assert.Equal(3, fichier.Chronos.Count);
        Assert.Contains(fichier.Chronos, c => c.IdentifiantReglage == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        Assert.All(fichier.Chronos, c => Assert.Equal(DateTimeKind.Utc, c.DateRealisation.Kind));

        var voitureOfficielle = fichier.Reglages.Single(r => r.Contenu.Identifiant == reglageOfficielle).Voiture;
        Assert.Equal(1, voitureOfficielle.IdentifiantOfficiel);

        var voiturePersonnalisee = fichier.Reglages.Single(r => r.Contenu.Identifiant == reglagePerso).Voiture;
        Assert.Null(voiturePersonnalisee.IdentifiantOfficiel);
        Assert.Equal("Ma marque", voiturePersonnalisee.NomMarque);
        Assert.Equal("Nouvelle", voiturePersonnalisee.Nom);
        Assert.Equal(2015, voiturePersonnalisee.Annee);
    }

    [Fact]
    public async Task L_export_en_texte_est_un_json_versionne()
    {
        var a = await CreerAppareilAsync();

        var texte = await a.Sauvegarde.ExporterEnTexteAsync();

        Assert.Contains("\"version\": 1", texte);
        Assert.Contains("\"reglages\"", texte);
    }

    [Fact]
    public async Task L_export_vers_un_flux_peut_etre_reimporte()
    {
        var a = await CreerAppareilAsync();
        var reglage = await a.AjouterReglageAsync(1, "Course", 1);
        using var flux = new MemoryStream();
        await a.Sauvegarde.ExporterAsync(flux);
        flux.Position = 0;

        var b = await CreerAppareilAsync();
        var resultat = await b.Sauvegarde.ImporterAsync(flux);

        Assert.Equal(1, resultat.ReglagesAjoutes);
        await using var contexte = b.Base.CreerContexte();
        Assert.True(await contexte.Reglages.AnyAsync(r => r.Identifiant == reglage));
    }

    // ------------------------------------------------------------ Import dans un appareil vide

    [Fact]
    public async Task Un_import_recree_les_voitures_personnalisees_les_reglages_et_les_chronos()
    {
        var a = await CreerAppareilAsync();
        var marque = await a.Catalogue.AjouterMarquePersonnaliseeAsync("Ma marque", 11);
        var perso = await a.Catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Nouvelle", 2015, Division.Nascar, 1234, true);
        var reglageOfficielle = await a.AjouterReglageAsync(1, "Course", 1, 90);
        var reglagePerso = await a.AjouterReglageAsync(perso.Identifiant, "Perso", 1, 95);
        await a.Chronos.AjouterAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine, 2, TimeSpan.FromSeconds(100));
        var texte = await a.Sauvegarde.ExporterEnTexteAsync();

        // L'appareil B a déjà sa propre marque personnalisée : les identifiants personnalisés diffèrent d'un appareil à l'autre.
        var b = await CreerAppareilAsync();
        await b.Catalogue.AjouterMarquePersonnaliseeAsync("Autre marque", 2);

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(texte);

        Assert.Equal(1, resultat.MarquesAjoutees);
        Assert.Equal(1, resultat.VoituresAjoutees);
        Assert.Equal(2, resultat.ReglagesAjoutes);
        Assert.Equal(3, resultat.ChronosAjoutes);
        Assert.Empty(resultat.Avertissements);

        await using var contexte = b.Base.CreerContexte();
        var marqueImportee = await contexte.Marques.SingleAsync(m => m.Nom == "Ma marque");
        Assert.True(marqueImportee.EstPersonnalisee);
        Assert.Equal(ConstantesReferentiel.PremierIdentifiantPersonnalise + 1, marqueImportee.Identifiant);

        var voitureImportee = await contexte.Voitures.SingleAsync(v => v.Nom == "Nouvelle");
        Assert.Equal(marqueImportee.Identifiant, voitureImportee.IdentifiantMarque);
        Assert.Equal(1234, voitureImportee.Valeur);
        Assert.True(voitureImportee.ContenuTelechargeable);
        Assert.Equal(Division.Nascar, voitureImportee.Division);

        Assert.Equal(1, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglageOfficielle)).IdentifiantVoiture);
        Assert.Equal(voitureImportee.Identifiant, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglagePerso)).IdentifiantVoiture);
        Assert.Equal(300, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglageOfficielle)).Generaux.Puissance);
        Assert.Equal(3, await contexte.Chronos.CountAsync());
        Assert.Equal(95_000, (await contexte.Chronos.SingleAsync(c => c.IdentifiantReglage == reglagePerso)).DureeMillisecondes);
    }

    [Fact]
    public async Task Importer_deux_fois_le_meme_fichier_ne_change_rien()
    {
        var a = await CreerAppareilAsync();
        await a.AjouterReglageAsync(1, "Course", 1);
        await a.AjouterReglageAsync(1, "Pluie", 2);
        var texte = await a.Sauvegarde.ExporterEnTexteAsync();

        var b = await CreerAppareilAsync();
        await b.Sauvegarde.ImporterDepuisTexteAsync(texte);
        var second = await b.Sauvegarde.ImporterDepuisTexteAsync(texte);

        Assert.Equal(0, second.ReglagesAjoutes);
        Assert.Equal(0, second.ReglagesMisAJour);
        Assert.Equal(2, second.ReglagesIgnores);
        Assert.Equal(0, second.ChronosAjoutes);
        Assert.Equal(0, second.ChronosMisAJour);
        Assert.Equal(2, second.ChronosIgnores);
        await using var contexte = b.Base.CreerContexte();
        Assert.Equal(2, await contexte.Reglages.CountAsync(r => !r.EstOrigine));
        Assert.Equal(2, await contexte.Chronos.CountAsync());
    }

    [Fact]
    public async Task Une_voiture_personnalisee_devenue_officielle_sur_l_appareil_recepteur_recoit_les_reglages()
    {
        var a = await CreerAppareilAsync();
        var perso = await a.Catalogue.AjouterVoiturePersonnaliseeAsync(1, "Nouvelle", 2015, Division.ForzaGT3);
        var reglage = await a.AjouterReglageAsync(perso.Identifiant, "Course", 1);
        var texte = await a.Sauvegarde.ExporterEnTexteAsync();

        var b = await CreerAppareilAsync(AvecNouvelleVoiture());
        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(texte);

        Assert.Equal(0, resultat.VoituresAjoutees);
        await using var contexte = b.Base.CreerContexte();
        Assert.False(await contexte.Voitures.AnyAsync(v => v.EstPersonnalisee));
        Assert.Equal(2, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglage)).IdentifiantVoiture);
    }

    // ------------------------------------------------------------ Fusion : le plus récent gagne

    [Fact]
    public async Task Un_reglage_plus_recent_remplace_le_reglage_local_et_ses_chronos()
    {
        var a = await CreerAppareilAsync();
        var b = await CreerAppareilAsync();
        var reglage = await a.AjouterReglageAsync(1, "Course", 1, 90);
        await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        await Task.Delay(20);
        await a.ModifierReglageAsync(reglage, r => r.Generaux.Puissance = 500); // supprime son chrono
        await a.Chronos.AjouterAsync(reglage, 2, TimeSpan.FromSeconds(100));

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        Assert.Equal(1, resultat.ReglagesMisAJour);
        Assert.Equal(1, resultat.ChronosSupprimes);
        Assert.Equal(1, resultat.ChronosAjoutes);
        await using var contexte = b.Base.CreerContexte();
        Assert.Equal(500, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglage)).Generaux.Puissance);
        var chrono = await contexte.Chronos.SingleAsync();
        Assert.Equal(2, chrono.IdentifiantParcours);
        Assert.Equal(100_000, chrono.DureeMillisecondes);
    }

    [Fact]
    public async Task Un_reglage_local_plus_recent_est_conserve_et_les_chronos_perimes_du_fichier_sont_ignores()
    {
        var a = await CreerAppareilAsync();
        var b = await CreerAppareilAsync();
        var reglage = await a.AjouterReglageAsync(1, "Course", 1, 90);
        var ancienFichier = await a.Sauvegarde.ExporterEnTexteAsync();
        await b.Sauvegarde.ImporterDepuisTexteAsync(ancienFichier);

        await Task.Delay(20);
        await b.ModifierReglageAsync(reglage, r => r.Generaux.Puissance = 400); // supprime son chrono
        await b.Chronos.AjouterAsync(reglage, 2, TimeSpan.FromSeconds(80));

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(ancienFichier);

        Assert.Equal(1, resultat.ReglagesIgnores);
        Assert.Equal(0, resultat.ReglagesMisAJour);
        Assert.Equal(1, resultat.ChronosIgnores);
        Assert.Equal(0, resultat.ChronosAjoutes);
        await using var contexte = b.Base.CreerContexte();
        Assert.Equal(400, (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglage)).Generaux.Puissance);
        var chrono = await contexte.Chronos.SingleAsync();
        Assert.Equal(2, chrono.IdentifiantParcours);
    }

    [Fact]
    public async Task Renommer_un_reglage_plus_recemment_met_a_jour_le_nom_sans_toucher_aux_chronos()
    {
        var a = await CreerAppareilAsync();
        var b = await CreerAppareilAsync();
        var reglage = await a.AjouterReglageAsync(1, "Course", 1);
        await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        await Task.Delay(20);
        await a.ModifierReglageAsync(reglage, r => r.Nom = "Course sèche");

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        Assert.Equal(1, resultat.ReglagesMisAJour);
        Assert.Equal(0, resultat.ChronosSupprimes);
        await using var contexte = b.Base.CreerContexte();
        Assert.Equal("Course sèche", (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglage)).Nom);
        Assert.Equal(1, await contexte.Chronos.CountAsync());
    }

    [Fact]
    public async Task Un_chrono_plus_recent_remplace_le_chrono_local_et_un_chrono_plus_ancien_est_ignore()
    {
        var a = await CreerAppareilAsync();
        var b = await CreerAppareilAsync();
        var reglage = await a.AjouterReglageAsync(1, "Course", 1, 90);
        await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        await Task.Delay(20);
        await a.ModifierChronoAsync(reglage, 1, 85);
        var fichierDeA = await a.Sauvegarde.ExporterEnTexteAsync();

        var premier = await b.Sauvegarde.ImporterDepuisTexteAsync(fichierDeA);
        Assert.Equal(1, premier.ChronosMisAJour);
        Assert.Equal(85_000, (await b.Chronos.RechercherAsync(reglage, 1))!.DureeMillisecondes);

        await Task.Delay(20);
        await b.ModifierChronoAsync(reglage, 1, 80); // plus récent que le fichier de A

        var second = await b.Sauvegarde.ImporterDepuisTexteAsync(fichierDeA);
        Assert.Equal(0, second.ChronosMisAJour);
        Assert.Equal(1, second.ChronosIgnores);
        Assert.Equal(80_000, (await b.Chronos.RechercherAsync(reglage, 1))!.DureeMillisecondes);
    }

    [Fact]
    public async Task Un_reglage_importe_dont_le_nom_est_deja_pris_est_renomme()
    {
        var a = await CreerAppareilAsync();
        var b = await CreerAppareilAsync();
        var reglageDeA = await a.AjouterReglageAsync(1, "Course");
        await b.AjouterReglageAsync(1, "Course"); // autre réglage, autre identifiant, même nom

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(await a.Sauvegarde.ExporterEnTexteAsync());

        Assert.Equal(1, resultat.ReglagesAjoutes);
        Assert.Equal(1, resultat.ReglagesRenommes);
        await using var contexte = b.Base.CreerContexte();
        Assert.Equal("Course (2)", (await contexte.Reglages.SingleAsync(r => r.Identifiant == reglageDeA)).Nom);
        Assert.Equal(2, await contexte.Reglages.CountAsync(r => !r.EstOrigine));
    }

    // ------------------------------------------------------------ Tout remplacer

    [Fact]
    public async Task Le_mode_tout_remplacer_efface_les_donnees_locales_avant_de_charger_le_fichier()
    {
        var a = await CreerAppareilAsync();
        var reglageDeA = await a.AjouterReglageAsync(1, "Course", 1);
        var texte = await a.Sauvegarde.ExporterEnTexteAsync();

        var b = await CreerAppareilAsync();
        var marque = await b.Catalogue.AjouterMarquePersonnaliseeAsync("Locale", 11);
        var voiture = await b.Catalogue.AjouterVoiturePersonnaliseeAsync(marque.Identifiant, "Locale", 2020, Division.ForzaGT3);
        await b.AjouterReglageAsync(voiture.Identifiant, "Locale", 1);
        await b.AjouterReglageAsync(1, "Autre", 1);
        await b.Chronos.AjouterAsync(BaseDeDonneesDeTest.IdentifiantReglageOrigine, 3, TimeSpan.FromSeconds(100));

        var resultat = await b.Sauvegarde.ImporterDepuisTexteAsync(texte, ModeImport.TousRemplacer);

        Assert.Equal(3, resultat.ChronosSupprimes);
        Assert.Equal(1, resultat.ReglagesAjoutes);
        Assert.Equal(1, resultat.ChronosAjoutes);
        await using var contexte = b.Base.CreerContexte();
        Assert.False(await contexte.Marques.AnyAsync(m => m.EstPersonnalisee));
        Assert.False(await contexte.Voitures.AnyAsync(v => v.EstPersonnalisee));
        var reglages = await contexte.Reglages.Where(r => !r.EstOrigine).ToListAsync();
        Assert.Equal(reglageDeA, Assert.Single(reglages).Identifiant);
        Assert.Equal(1, await contexte.Chronos.CountAsync());
        Assert.True(await contexte.Reglages.AnyAsync(r => r.EstOrigine)); // le catalogue officiel reste intact
    }

    // ------------------------------------------------------------ Fichiers invalides ou incomplets

    [Theory]
    [InlineData("ceci n'est pas du json", CodeErreurMetier.SauvegardeInvalide)]
    [InlineData("null", CodeErreurMetier.SauvegardeInvalide)]
    [InlineData("{ \"version\": 0 }", CodeErreurMetier.SauvegardeInvalide)]
    [InlineData("{ \"version\": 99 }", CodeErreurMetier.SauvegardeVersionNonSupportee)]
    public async Task Un_fichier_invalide_ou_trop_recent_est_refuse(string contenu, CodeErreurMetier attendu)
    {
        var b = await CreerAppareilAsync();

        Assert.Equal(attendu, await CodeAsync(() => b.Sauvegarde.ImporterDepuisTexteAsync(contenu)));
    }

    [Fact]
    public async Task Les_elements_inutilisables_sont_signales_sans_bloquer_le_reste_de_l_import()
    {
        var b = await CreerAppareilAsync();
        var maintenant = DateTime.UtcNow;
        var origine = BaseDeDonneesDeTest.IdentifiantReglageOrigine;

        ChronoSauvegarde Chrono(Guid reglage, int parcours, long millisecondes) => new()
        {
            Identifiant = Guid.NewGuid(),
            IdentifiantReglage = reglage,
            IdentifiantParcours = parcours,
            DureeMillisecondes = millisecondes,
            DateRealisation = maintenant,
            DateModification = maintenant
        };

        var fichier = new FichierSauvegarde
        {
            MarquesPersonnalisees = { new MarqueSauvegarde { Nom = "Sans pays", IdentifiantPays = 9999 } },
            VoituresPersonnalisees = { new VoitureSauvegarde { NomMarque = "Introuvable", Nom = "V", Annee = 2000, Division = Division.ForzaGT3 } },
            Reglages =
            {
                new ReglageSauvegarde
                {
                    Voiture = new ReferenceVoiture { IdentifiantOfficiel = 9999, NomMarque = "X", Nom = "Y", Annee = 2000 },
                    DateCreation = maintenant,
                    DateModification = maintenant,
                    Contenu = new ReglageOrigineFichier { Identifiant = Guid.NewGuid(), Nom = "Orphelin" }
                }
            },
            Chronos =
            {
                Chrono(origine, 9999, 90_000),
                Chrono(origine, 1, 0),
                Chrono(Guid.NewGuid(), 1, 90_000),
                Chrono(origine, 1, 91_000) // le seul valide
            }
        };

        var resultat = await b.Sauvegarde.ImporterAsync(fichier);

        var codes = resultat.Avertissements.Select(a => a.Code).ToHashSet();
        Assert.Contains(CodeAvertissementImport.PaysIntrouvable, codes);
        Assert.Contains(CodeAvertissementImport.MarqueIntrouvable, codes);
        Assert.Contains(CodeAvertissementImport.VoitureIntrouvable, codes);
        Assert.Contains(CodeAvertissementImport.ParcoursIntrouvable, codes);
        Assert.Contains(CodeAvertissementImport.DureeInvalide, codes);
        Assert.Contains(CodeAvertissementImport.ReglageIntrouvable, codes);
        Assert.Equal(1, resultat.ChronosAjoutes);
        Assert.Equal(0, resultat.ReglagesAjoutes);
    }

    [Fact]
    public async Task Un_reglage_d_origine_present_dans_le_fichier_n_est_jamais_modifie()
    {
        var b = await CreerAppareilAsync();
        var maintenant = DateTime.UtcNow.AddHours(1);
        var fichier = new FichierSauvegarde
        {
            Reglages =
            {
                new ReglageSauvegarde
                {
                    Voiture = new ReferenceVoiture { IdentifiantOfficiel = 1, NomMarque = "Marque d'origine", Nom = "Voiture d'origine", Annee = 2000 },
                    DateCreation = maintenant,
                    DateModification = maintenant,
                    Contenu = new ReglageOrigineFichier
                    {
                        Identifiant = BaseDeDonneesDeTest.IdentifiantReglageOrigine,
                        Nom = "Piraté",
                        Generaux = new ReglagesGeneraux { Puissance = 9999 }
                    }
                }
            }
        };

        var resultat = await b.Sauvegarde.ImporterAsync(fichier);

        Assert.Equal(1, resultat.ReglagesIgnores);
        await using var contexte = b.Base.CreerContexte();
        var origine = await contexte.Reglages.SingleAsync(r => r.Identifiant == BaseDeDonneesDeTest.IdentifiantReglageOrigine);
        Assert.True(origine.EstOrigine);
        Assert.Equal(300, origine.Generaux.Puissance);
        Assert.NotEqual("Piraté", origine.Nom);
    }
}
