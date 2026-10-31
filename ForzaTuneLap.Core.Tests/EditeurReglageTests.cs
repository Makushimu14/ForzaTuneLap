using System.Globalization;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Unites;

namespace ForzaTuneLap.Core.Tests;

public class EditeurReglageTests
{
    /// <summary>Un réglage avec des pneus et deux rapports, mais ni aérodynamisme ni amortisseurs.</summary>
    private static Reglage ReglageDeTest(bool estOrigine = false) => new()
    {
        Nom = "Test",
        EstOrigine = estOrigine,
        Generaux = new ReglagesGeneraux
        {
            Code = "A",
            Moteur = "V8",
            Transmission = Transmission.Propulsion,
            IndiceDePerformance = 650,
            Puissance = 300,
            Poids = 1360.77711
        },
        Pneus = new ReglagePneus { PressionAvant = 2.0, PressionArriere = 2.1 },
        RapportFinal = 3.5,
        RapportsDeBoite =
        {
            new RapportDeBoite { Numero = 1, Valeur = 3.2 },
            new RapportDeBoite { Numero = 2, Valeur = 2.1 }
        }
    };

    private static EditeurReglage Creer(Reglage reglage, PreferencesUnites? unites = null, string culture = "fr-FR") =>
        new(reglage, unites ?? PreferencesUnites.Metriques(), new CultureInfo(culture));

    private static SectionEditable Section(EditeurReglage editeur, CategorieDeReglage categorie) =>
        editeur.Sections.Single(s => s.Categorie == categorie);

    private static ChampReglage Champ(SectionEditable section, string cle) => section.Champs.Single(c => c.Cle == cle);

    // ------------------------------------------------------------ Affichage et unités

    [Fact]
    public void Les_champs_affichent_les_valeurs_dans_l_unite_choisie()
    {
        var metrique = Creer(ReglageDeTest());
        var imperial = Creer(ReglageDeTest(), PreferencesUnites.Imperiales());

        Assert.Equal("300", Champ(metrique.Generaux, "Generaux.Puissance").Texte);
        Assert.Equal("kW", Champ(metrique.Generaux, "Generaux.Puissance").Symbole);
        Assert.Equal("3000", Champ(imperial.Generaux, "Generaux.Poids").Texte);
        Assert.Equal("lb", Champ(imperial.Generaux, "Generaux.Poids").Symbole);
        Assert.Equal("2,1", Champ(Section(metrique, CategorieDeReglage.Pneus), "Pneus.PressionArriere").Texte);
    }

    [Fact]
    public void Les_libelles_des_champs_viennent_du_catalogue()
    {
        var editeur = Creer(ReglageDeTest());

        Assert.False(string.IsNullOrWhiteSpace(Champ(editeur.Generaux, "Generaux.Puissance").Libelle));
        Assert.DoesNotContain("Critere_", Champ(editeur.Generaux, "Generaux.Puissance").Libelle);
    }

    [Fact]
    public void Saisir_une_valeur_l_enregistre_en_unite_canonique()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage, PreferencesUnites.Imperiales());

        Champ(editeur.Generaux, "Generaux.Poids").Texte = "3100";

        Assert.Equal(3100 * 0.45359237, reglage.Generaux.Poids, 6);
    }

    [Fact]
    public void Un_champ_que_l_utilisateur_ne_touche_pas_garde_exactement_sa_valeur()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage, PreferencesUnites.Imperiales());

        // On modifie un autre champ : le poids, affiché arrondi en livres, ne doit pas être réécrit.
        Champ(editeur.Generaux, "Generaux.Puissance").Texte = "310";

        Assert.Equal(1360.77711, reglage.Generaux.Poids);
    }

    [Fact]
    public void La_virgule_et_le_point_sont_acceptes_pour_les_decimales()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var champ = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");

        champ.Texte = "2,25";
        Assert.Equal(2.25, reglage.Pneus!.PressionAvant);

        champ.Texte = "2.35";
        Assert.Equal(2.35, reglage.Pneus.PressionAvant);
    }

    // ------------------------------------------------------------ Validité

    [Fact]
    public void Une_valeur_invalide_est_signalee_ne_modifie_rien_et_bloque_l_enregistrement()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var champ = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");
        Assert.True(editeur.EstValide);

        champ.Texte = "abc";

        Assert.True(champ.EstInvalide);
        Assert.False(editeur.EstValide);
        Assert.Equal(2.0, reglage.Pneus!.PressionAvant);

        champ.Texte = "2,2";

        Assert.True(champ.EstValide);
        Assert.True(editeur.EstValide);
        Assert.Equal(2.2, reglage.Pneus.PressionAvant);
    }

    [Fact]
    public void Un_champ_obligatoire_vide_est_invalide_et_un_champ_facultatif_vide_prend_la_valeur_nulle()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);

        var obligatoire = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");
        obligatoire.Texte = "";
        Assert.True(obligatoire.EstInvalide);
        Assert.False(editeur.EstValide);
        obligatoire.Texte = "2";
        Assert.True(editeur.EstValide);

        var rapportFinal = editeur.Rapports.Champs.Single(c => c.Cle == "Rapports.RapportFinal");
        Assert.True(rapportFinal.EstFacultatif);
        rapportFinal.Texte = "";
        Assert.True(rapportFinal.EstValide);
        Assert.Null(reglage.RapportFinal);
        Assert.True(editeur.EstValide);
    }

    [Fact]
    public void Une_valeur_hors_des_bornes_indicatives_est_un_avertissement_pas_une_erreur()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var amortisseurs = Section(editeur, CategorieDeReglage.Amortisseurs);
        amortisseurs.EstPresente = true;
        var champ = Champ(amortisseurs, "Amortisseurs.RigiditeDetenteAvant");

        champ.Texte = "20";
        Assert.True(champ.EstInhabituel);
        Assert.True(champ.EstValide);
        Assert.True(editeur.EstValide);
        Assert.Equal(20, reglage.Amortisseurs!.RigiditeDetenteAvant);

        champ.Texte = "7";
        Assert.False(champ.EstInhabituel);
    }

    [Fact]
    public void Un_nom_vide_bloque_l_enregistrement()
    {
        var editeur = Creer(ReglageDeTest());

        editeur.Nom = "  ";
        Assert.False(editeur.EstValide);

        editeur.Nom = "Course";
        Assert.True(editeur.EstValide);
        Assert.Equal("Course", editeur.Reglage.Nom);
    }

    // ------------------------------------------------------------ Sections facultatives

    [Fact]
    public void Une_section_absente_a_des_champs_inactifs_et_peut_etre_ajoutee_puis_retiree()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var aerodynamisme = Section(editeur, CategorieDeReglage.Aerodynamisme);
        var champ = Champ(aerodynamisme, "Aerodynamisme.PortanceNegativeAvant");

        Assert.False(aerodynamisme.EstPresente);
        Assert.True(aerodynamisme.PeutBasculer);
        Assert.False(champ.PeutEtreSaisi);
        Assert.Equal(string.Empty, champ.Texte);
        Assert.Null(reglage.Aerodynamisme);

        aerodynamisme.EstPresente = true;

        Assert.NotNull(reglage.Aerodynamisme);
        Assert.True(champ.PeutEtreSaisi);
        Assert.Equal("0", champ.Texte);

        champ.Texte = "50";
        Assert.Equal(50, reglage.Aerodynamisme!.PortanceNegativeAvant);

        aerodynamisme.EstPresente = false;

        Assert.Null(reglage.Aerodynamisme);
        Assert.False(champ.PeutEtreSaisi);
    }

    [Fact]
    public void Les_generaux_et_les_performances_ne_peuvent_pas_etre_retires()
    {
        var editeur = Creer(ReglageDeTest());

        Assert.False(editeur.Generaux.PeutBasculer);
        Assert.False(Section(editeur, CategorieDeReglage.Performances).PeutBasculer);

        Section(editeur, CategorieDeReglage.Performances).EstPresente = false;
        Assert.NotNull(editeur.Reglage.Performances);
    }

    [Fact]
    public void Chaque_section_facultative_peut_etre_ajoutee_et_retiree()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);

        foreach (var section in editeur.Sections.Where(s => s.EstFacultative))
        {
            section.EstPresente = true;
            Assert.True(section.EstPresente);
            Assert.All(section.Champs, champ => Assert.True(champ.PeutEtreSaisi));
            section.EstPresente = false;
            Assert.False(section.EstPresente);
        }

        Assert.Null(reglage.Pneus);
        Assert.Null(reglage.Volant);
    }

    [Fact]
    public void Le_differentiel_ajoute_garde_son_equilibre_central_vide()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var differentiel = Section(editeur, CategorieDeReglage.Differentiel);

        differentiel.EstPresente = true;

        Assert.Equal(string.Empty, Champ(differentiel, "Differentiel.EquilibreCentral").Texte);
        Assert.Null(reglage.Differentiel!.EquilibreCentral);
        Assert.True(editeur.EstValide);
    }

    // ------------------------------------------------------------ Généraux

    [Fact]
    public void Le_code_le_moteur_et_la_transmission_se_modifient()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);

        editeur.Code = "S";
        editeur.Moteur = "Flat-6";
        editeur.IndexTransmission = (int)Transmission.TransmissionIntegrale;

        Assert.Equal("S", reglage.Generaux.Code);
        Assert.Equal("Flat-6", reglage.Generaux.Moteur);
        Assert.Equal(Transmission.TransmissionIntegrale, reglage.Generaux.Transmission);

        editeur.IndexTransmission = 99; // valeur invalide : ignorée
        Assert.Equal(Transmission.TransmissionIntegrale, reglage.Generaux.Transmission);
    }

    [Fact]
    public void La_classe_suit_l_indice_de_performance_saisi()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var indice = Champ(editeur.Generaux, "Generaux.IndiceDePerformance");
        Assert.Equal("A 650", editeur.ClasseAffichee);

        indice.Texte = "750";
        Assert.Equal(750, reglage.Generaux.IndiceDePerformance);
        Assert.Equal("S 750", editeur.ClasseAffichee);

        indice.Texte = "650,6"; // l'indice est un entier : arrondi
        Assert.Equal(651, reglage.Generaux.IndiceDePerformance);

        indice.Texte = "0";
        Assert.Equal(string.Empty, editeur.ClasseAffichee);
    }

    // ------------------------------------------------------------ Rapports de boîte

    [Fact]
    public void Les_rapports_s_ajoutent_se_modifient_et_se_retirent()
    {
        var reglage = ReglageDeTest();
        var editeur = Creer(reglage);
        var rapports = editeur.Rapports;
        Assert.Equal(2, rapports.Boite.Count);
        Assert.Equal("3,2", rapports.Boite[0].Texte);

        rapports.Ajouter();

        Assert.Equal(3, reglage.RapportsDeBoite.Count);
        Assert.Equal(3, rapports.Boite[2].Numero);
        Assert.Equal(0, reglage.RapportsDeBoite[2].Valeur);
        Assert.True(rapports.Boite[2].EstInhabituel);

        rapports.Boite[2].Texte = "1,5";
        Assert.Equal(1.5, reglage.RapportsDeBoite[2].Valeur);
        Assert.False(rapports.Boite[2].EstInhabituel);

        rapports.RetirerDernier();
        Assert.Equal(2, reglage.RapportsDeBoite.Count);
        Assert.Equal(2, rapports.Boite.Count);
    }

    [Fact]
    public void Les_rapports_sont_limites_a_dix_et_un_rapport_invalide_bloque_l_enregistrement()
    {
        var editeur = Creer(ReglageDeTest());
        var rapports = editeur.Rapports;

        while (rapports.PeutAjouter)
        {
            rapports.Ajouter();
        }

        Assert.Equal(10, rapports.Boite.Count);
        rapports.Ajouter();
        Assert.Equal(10, rapports.Boite.Count);

        rapports.Boite[0].Texte = "abc";
        Assert.True(rapports.Boite[0].EstInvalide);
        Assert.False(editeur.EstValide);

        rapports.Boite[0].Texte = "3";
        Assert.True(editeur.EstValide);

        while (rapports.PeutRetirer)
        {
            rapports.RetirerDernier();
        }

        Assert.Empty(editeur.Reglage.RapportsDeBoite);
        rapports.RetirerDernier(); // sans effet
    }

    // ------------------------------------------------------------ Clavier des valeurs négatives

    [Fact]
    public void Seuls_les_champs_dont_la_valeur_peut_etre_negative_le_signalent()
    {
        var editeur = Creer(ReglageDeTest());

        Assert.False(Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant").AccepteLesNegatifs);
        Assert.True(Champ(Section(editeur, CategorieDeReglage.Alignement), "Alignement.CarrossageAvant").AccepteLesNegatifs);
        Assert.True(Champ(Section(editeur, CategorieDeReglage.GeometrieDeSuspension), "GeometrieDeSuspension.PourcentageAntigeometrieAvant").AccepteLesNegatifs);
    }

    // ------------------------------------------------------------ Modifications non enregistrées

    [Fact]
    public void Un_editeur_qui_vient_d_etre_charge_n_a_pas_de_modifications()
    {
        Assert.False(Creer(ReglageDeTest()).AModifications);
    }

    [Fact]
    public void Modifier_une_valeur_puis_revenir_a_la_valeur_de_depart_redonne_aucune_modification()
    {
        var editeur = Creer(ReglageDeTest());
        var champ = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");

        champ.Texte = "2,4";
        Assert.True(editeur.AModifications);

        champ.Texte = "2";
        Assert.False(editeur.AModifications);
    }

    [Fact]
    public void Changer_le_nom_une_section_ou_les_rapports_est_une_modification()
    {
        var editeur = Creer(ReglageDeTest());
        editeur.Nom = "Autre nom";
        Assert.True(editeur.AModifications);
        editeur.Nom = "Test";
        Assert.False(editeur.AModifications);

        editeur.Rapports.Ajouter();
        Assert.True(editeur.AModifications);
        editeur.Rapports.RetirerDernier();
        Assert.False(editeur.AModifications);

        Section(editeur, CategorieDeReglage.Aerodynamisme).EstPresente = true;
        Assert.True(editeur.AModifications);
    }

    [Fact]
    public void Une_valeur_invalide_est_comptee_comme_une_modification()
    {
        var editeur = Creer(ReglageDeTest());
        var champ = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");

        champ.Texte = "abc"; // la valeur du réglage ne change pas, mais le texte affiché n'est plus celui du réglage

        Assert.False(editeur.EstValide);
        Assert.True(editeur.AModifications);
    }

    [Fact]
    public void Apres_l_enregistrement_l_etat_actuel_devient_la_reference()
    {
        var editeur = Creer(ReglageDeTest());
        Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant").Texte = "2,4";
        Assert.True(editeur.AModifications);

        editeur.MarquerCommeEnregistre();

        Assert.False(editeur.AModifications);
    }

    [Fact]
    public void Un_reglage_d_origine_n_a_jamais_de_modifications()
    {
        var editeur = Creer(ReglageDeTest(estOrigine: true));
        editeur.Nom = "Piraté";

        Assert.False(editeur.AModifications);
    }

    // ------------------------------------------------------------ Réglage d'origine

    [Fact]
    public void Un_reglage_d_origine_est_en_lecture_seule()
    {
        var reglage = ReglageDeTest(estOrigine: true);
        var editeur = Creer(reglage);
        var champ = Champ(Section(editeur, CategorieDeReglage.Pneus), "Pneus.PressionAvant");
        var aerodynamisme = Section(editeur, CategorieDeReglage.Aerodynamisme);

        Assert.False(editeur.EstModifiable);
        Assert.False(champ.PeutEtreSaisi);
        Assert.False(aerodynamisme.PeutBasculer);
        Assert.False(editeur.Rapports.PeutAjouter);
        Assert.False(editeur.Rapports.PeutRetirer);

        champ.Texte = "9";
        aerodynamisme.EstPresente = true;
        editeur.Nom = "Piraté";
        editeur.Code = "Z";
        editeur.IndexTransmission = (int)Transmission.Traction;
        editeur.Rapports.Ajouter();
        editeur.Rapports.Boite[0].Texte = "9";

        Assert.Equal(2.0, reglage.Pneus!.PressionAvant);
        Assert.Null(reglage.Aerodynamisme);
        Assert.Equal("Test", reglage.Nom);
        Assert.Equal("A", reglage.Generaux.Code);
        Assert.Equal(Transmission.Propulsion, reglage.Generaux.Transmission);
        Assert.Equal(2, reglage.RapportsDeBoite.Count);
        Assert.Equal(3.2, reglage.RapportsDeBoite[0].Valeur);
    }

    // ------------------------------------------------------------ Rapports de boîte : ils vont en décroissant

    private static Reglage ReglageAvecSixRapports()
    {
        var reglage = ReglageDeTest();
        reglage.RapportsDeBoite.Clear();

        var valeurs = new[] { 3.2, 2.1, 1.5, 1.2, 1.0, 0.9 };
        for (var i = 0; i < valeurs.Length; i++)
        {
            reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = i + 1, Valeur = valeurs[i] });
        }

        return reglage;
    }

    private static double[] Valeurs(Reglage reglage) =>
        reglage.RapportsDeBoite.OrderBy(r => r.Numero).Select(r => r.Valeur).ToArray();

    [Fact]
    public void Saisir_un_rapport_ramene_les_suivants_a_sa_valeur_s_ils_la_depassent()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "0,8";

        Assert.Equal(new[] { 3.2, 2.1, 0.8, 0.8, 0.8, 0.8 }, Valeurs(reglage));
        Assert.All(rapports.Boite.Skip(2), rapport => Assert.Equal("0,8", rapport.Texte));
        Assert.Equal("3,2", rapports.Boite[0].Texte);
        Assert.Equal("2,1", rapports.Boite[1].Texte);
    }

    [Fact]
    public void Saisir_un_rapport_caractere_par_caractere_remonte_les_suivants_a_la_valeur_finale()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "0";
        Assert.Equal(new[] { 3.2, 2.1, 0, 0, 0, 0 }, Valeurs(reglage));

        rapports.Boite[2].Texte = "0,";
        rapports.Boite[2].Texte = "0,8";

        Assert.Equal(new[] { 3.2, 2.1, 0.8, 0.8, 0.8, 0.8 }, Valeurs(reglage));
        Assert.All(rapports.Boite.Skip(2), rapport => Assert.Equal("0,8", rapport.Texte));
    }

    [Fact]
    public void Les_rapports_ramenes_automatiquement_sont_signales_jusqu_a_leur_propre_modification()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "0,8";

        Assert.False(rapports.Boite[2].EstAjusteAutomatiquement);
        Assert.False(rapports.Boite[1].EstAjusteAutomatiquement);
        Assert.All(rapports.Boite.Skip(3), rapport => Assert.True(rapport.EstAjusteAutomatiquement));

        rapports.Boite[3].Texte = "0,7";
        Assert.False(rapports.Boite[3].EstAjusteAutomatiquement);
        Assert.True(rapports.Boite[4].EstAjusteAutomatiquement);
    }

    [Fact]
    public void Un_rapport_retabli_a_sa_valeur_d_origine_n_est_plus_signale()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "0,8";
        rapports.Boite[2].Texte = "1,5";

        Assert.Equal(new[] { 3.2, 2.1, 1.5, 1.2, 1.0, 0.9 }, Valeurs(reglage));
        Assert.All(rapports.Boite, rapport => Assert.False(rapport.EstAjusteAutomatiquement));
    }

    [Fact]
    public void Saisir_un_rapport_caractere_par_caractere_ne_releve_pas_un_suivant_deja_plus_petit()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "1";
        rapports.Boite[2].Texte = "1,1";

        Assert.Equal(new[] { 3.2, 2.1, 1.1, 1.1, 1.0, 0.9 }, Valeurs(reglage));
    }

    [Fact]
    public void Seuls_les_rapports_qui_depassent_la_valeur_saisie_sont_modifies()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "1,1";

        Assert.Equal(new[] { 3.2, 2.1, 1.1, 1.1, 1.0, 0.9 }, Valeurs(reglage));
    }

    [Fact]
    public void La_mise_en_coherence_laisse_l_editeur_valide_et_efface_un_texte_invalide_des_rapports_suivants()
    {
        var reglage = ReglageAvecSixRapports();
        var editeur = Creer(reglage);
        var rapports = editeur.Rapports;

        rapports.Boite[4].Texte = "abc";
        Assert.False(editeur.EstValide);

        rapports.Boite[2].Texte = "0,8"; // le rapport 5 est ramené à 0,8 : son texte invalide est remplacé

        Assert.True(rapports.Boite[4].EstValide);
        Assert.Equal("0,8", rapports.Boite[4].Texte);
        Assert.True(editeur.EstValide);
    }

    [Fact]
    public void Un_rapport_plus_grand_que_le_precedent_est_signale_sans_modifier_les_autres_rapports()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[3].Texte = "1,6"; // plus grand que le rapport 3 (1,5)

        Assert.Equal(new[] { 3.2, 2.1, 1.5, 1.6, 1.0, 0.9 }, Valeurs(reglage));
        Assert.True(rapports.Boite[3].EstSuperieurAuPrecedent);
        Assert.False(rapports.Boite[2].EstSuperieurAuPrecedent);
        Assert.False(rapports.Boite[4].EstSuperieurAuPrecedent);
        Assert.True(rapports.Boite[3].EstValide);

        rapports.Boite[3].Texte = "1,4";

        Assert.False(rapports.Boite[3].EstSuperieurAuPrecedent);
    }

    [Fact]
    public void Un_texte_invalide_ne_modifie_pas_les_rapports_suivants()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "abc";

        Assert.Equal(new[] { 3.2, 2.1, 1.5, 1.2, 1.0, 0.9 }, Valeurs(reglage));
    }

    [Fact]
    public void Au_chargement_les_rapports_incoherents_sont_signales_sans_etre_modifies()
    {
        var reglage = ReglageDeTest();
        reglage.RapportsDeBoite.Clear();
        reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = 1, Valeur = 2.0 });
        reglage.RapportsDeBoite.Add(new RapportDeBoite { Numero = 2, Valeur = 3.0 });

        var rapports = Creer(reglage).Rapports;

        Assert.False(rapports.Boite[0].EstSuperieurAuPrecedent);
        Assert.True(rapports.Boite[1].EstSuperieurAuPrecedent);
        Assert.Equal(new[] { 2.0, 3.0 }, Valeurs(reglage));
    }

    [Fact]
    public void Un_rapport_ajoute_n_est_pas_signale_et_retirer_le_dernier_met_les_avertissements_a_jour()
    {
        var reglage = ReglageAvecSixRapports();
        var rapports = Creer(reglage).Rapports;

        rapports.Ajouter();
        Assert.False(rapports.Boite[6].EstSuperieurAuPrecedent);

        rapports.Boite[6].Texte = "2"; // plus grand que le rapport 6 (0,9)
        Assert.True(rapports.Boite[6].EstSuperieurAuPrecedent);

        rapports.RetirerDernier();
        Assert.All(rapports.Boite, rapport => Assert.False(rapport.EstSuperieurAuPrecedent));
    }

    [Fact]
    public void Dans_un_reglage_d_origine_la_mise_en_coherence_ne_modifie_rien()
    {
        var reglage = ReglageAvecSixRapports();
        reglage.EstOrigine = true;
        var rapports = Creer(reglage).Rapports;

        rapports.Boite[2].Texte = "0,8";

        Assert.Equal(new[] { 3.2, 2.1, 1.5, 1.2, 1.0, 0.9 }, Valeurs(reglage));
    }
}
