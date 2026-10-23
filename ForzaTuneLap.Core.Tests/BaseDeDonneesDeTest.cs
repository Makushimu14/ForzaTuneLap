using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Modeles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Tests;

/// <summary>Base SQLite en mémoire, avec le schéma des vraies migrations. Une instance par test.</summary>
public sealed class BaseDeDonneesDeTest : IDisposable
{
    public static readonly Guid IdentifiantReglageOrigine = new("00000000-0000-0000-0000-0000000000a1");

    private readonly SqliteConnection connexion;

    public BaseDeDonneesDeTest()
    {
        // La base en mémoire disparaît à la fermeture de la connexion : on la garde ouverte pendant tout le test.
        connexion = new SqliteConnection("DataSource=:memory:");
        connexion.Open();
    }

    public ContexteDonnees CreerContexte() =>
        new(new DbContextOptionsBuilder<ContexteDonnees>().UseSqlite(connexion).Options);

    public async Task InitialiserAsync(FichierVoitures? voitures = null)
    {
        await using var contexte = CreerContexte();
        await InitialiseurBaseDeDonnees.InitialiserAsync(
            contexte,
            ChargeurReferentiel.LireCircuits(),
            voitures ?? new FichierVoitures());
    }

    /// <summary>Ajoute une marque et une voiture personnalisées. Les pays existent après InitialiserAsync.</summary>
    public static Voiture AjouterVoiturePersonnalisee(ContexteDonnees contexte)
    {
        var marque = new Marque
        {
            Identifiant = ConstantesReferentiel.PremierIdentifiantPersonnalise,
            Nom = "Marque de test",
            IdentifiantPays = 1,
            EstPersonnalisee = true
        };
        var voiture = new Voiture
        {
            Identifiant = ConstantesReferentiel.PremierIdentifiantPersonnalise,
            Nom = "Voiture de test",
            Annee = 2020,
            Division = Division.ForzaGT3,
            Valeur = 1,
            IdentifiantMarque = marque.Identifiant,
            EstPersonnalisee = true
        };

        contexte.Marques.Add(marque);
        contexte.Voitures.Add(voiture);
        return voiture;
    }

    public static Reglage CreerReglage(int identifiantVoiture, string nom = "Test") => new()
    {
        IdentifiantVoiture = identifiantVoiture,
        Nom = nom,
        DateCreation = DateTime.UtcNow,
        DateModification = DateTime.UtcNow,
        Generaux = new ReglagesGeneraux { Code = "A", Moteur = "V8", Transmission = Transmission.Propulsion, IndiceDePerformance = 700 }
    };

    public static Chrono CreerChrono(Guid identifiantReglage, int identifiantParcours = 1) => new()
    {
        IdentifiantReglage = identifiantReglage,
        IdentifiantParcours = identifiantParcours,
        Duree = TimeSpan.FromSeconds(90.123),
        DateRealisation = DateTime.UtcNow,
        DateModification = DateTime.UtcNow
    };

    /// <summary>Fichier de données contenant une seule voiture dont la puissance d'origine est paramétrable.</summary>
    public static FichierVoitures FichierAvecUneVoiture(double puissance) => new()
    {
        Marques =
        {
            new MarqueFichier
            {
                Identifiant = 1,
                Nom = "Marque d'origine",
                IdentifiantPays = 11,
                Voitures =
                {
                    new VoitureFichier
                    {
                        Identifiant = 1,
                        Nom = "Voiture d'origine",
                        Annee = 2000,
                        Division = Division.ForzaGT3,
                        Valeur = 1,
                        ReglageOrigine = new ReglageOrigineFichier
                        {
                            Identifiant = IdentifiantReglageOrigine,
                            Generaux = new ReglagesGeneraux { Puissance = puissance, IndiceDePerformance = 700 },
                            RapportsDeBoite = { 3.0, 2.0 }
                        }
                    }
                }
            }
        }
    };

    public void Dispose() => connexion.Dispose();
}
