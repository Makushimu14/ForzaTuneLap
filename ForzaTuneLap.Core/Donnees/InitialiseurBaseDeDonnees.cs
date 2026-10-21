using ForzaTuneLap.Core.Modeles;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.Core.Donnees;

/// <summary>
/// À appeler une fois au démarrage de l'application : applique les migrations, puis synchronise le
/// référentiel (pays, circuits, parcours, marques, voitures, réglages d'origine) avec les fichiers embarqués.
/// Les données de l'utilisateur (réglages créés, chronos, voitures personnalisées) ne sont jamais touchées,
/// sauf les chronos d'un réglage d'origine dont le contenu a été corrigé dans le fichier.
/// </summary>
public static class InitialiseurBaseDeDonnees
{
    public static Task InitialiserAsync(ContexteDonnees contexte, CancellationToken annulation = default) =>
        InitialiserAsync(contexte, ChargeurReferentiel.LireCircuits(), ChargeurReferentiel.LireVoitures(), annulation);

    /// <summary>Variante recevant le contenu des fichiers de données (utile pour les tests).</summary>
    public static async Task InitialiserAsync(
        ContexteDonnees contexte,
        FichierCircuits circuits,
        FichierVoitures voitures,
        CancellationToken annulation = default)
    {
        await contexte.Database.MigrateAsync(annulation);

        await SynchroniserCircuitsAsync(contexte, circuits, annulation);
        await SynchroniserVoituresAsync(contexte, voitures, annulation);
    }

    private static async Task SynchroniserCircuitsAsync(ContexteDonnees contexte, FichierCircuits fichier, CancellationToken annulation)
    {
        var pays = await contexte.Pays.ToDictionaryAsync(p => p.Identifiant, annulation);
        var circuits = await contexte.Circuits.ToDictionaryAsync(c => c.Identifiant, annulation);
        var parcours = await contexte.Parcours.ToDictionaryAsync(p => p.Identifiant, annulation);

        foreach (var paysFichier in fichier.Pays)
        {
            if (!pays.TryGetValue(paysFichier.Identifiant, out var unPays))
            {
                unPays = new Pays { Identifiant = paysFichier.Identifiant };
                contexte.Pays.Add(unPays);
                pays[unPays.Identifiant] = unPays;
            }

            unPays.NomFrancais = paysFichier.NomFrancais;
            unPays.NomAnglais = paysFichier.NomAnglais;

            foreach (var circuitFichier in paysFichier.Circuits)
            {
                if (!circuits.TryGetValue(circuitFichier.Identifiant, out var unCircuit))
                {
                    unCircuit = new Circuit { Identifiant = circuitFichier.Identifiant };
                    contexte.Circuits.Add(unCircuit);
                    circuits[unCircuit.Identifiant] = unCircuit;
                }

                unCircuit.Nom = circuitFichier.Nom;
                unCircuit.IdentifiantPays = paysFichier.Identifiant;

                foreach (var parcoursFichier in circuitFichier.Parcours)
                {
                    if (!parcours.TryGetValue(parcoursFichier.Identifiant, out var unParcours))
                    {
                        unParcours = new Parcours { Identifiant = parcoursFichier.Identifiant };
                        contexte.Parcours.Add(unParcours);
                        parcours[unParcours.Identifiant] = unParcours;
                    }

                    unParcours.NomFrancais = parcoursFichier.NomFrancais;
                    unParcours.NomAnglais = parcoursFichier.NomAnglais;
                    unParcours.LongueurKilometres = parcoursFichier.LongueurKilometres;
                    unParcours.IdentifiantCircuit = circuitFichier.Identifiant;
                }
            }
        }

        await contexte.SaveChangesAsync(annulation);
    }

    private static async Task SynchroniserVoituresAsync(ContexteDonnees contexte, FichierVoitures fichier, CancellationToken annulation)
    {
        var marques = await contexte.Marques.ToDictionaryAsync(m => m.Identifiant, annulation);
        var voitures = await contexte.Voitures.ToDictionaryAsync(v => v.Identifiant, annulation);

        // Projection légère : on ne charge pas les ~600 réglages complets à chaque démarrage.
        var empreintesExistantes = await contexte.Reglages
            .Where(r => r.EstOrigine)
            .Select(r => new { r.Identifiant, r.EmpreinteOrigine })
            .ToDictionaryAsync(x => x.Identifiant, x => x.EmpreinteOrigine, annulation);

        var reglagesAjoutes = new List<(int IdentifiantVoiture, ReglageOrigineFichier Fichier, string Empreinte)>();
        var reglagesARemplacer = new List<Guid>();

        foreach (var marqueFichier in fichier.Marques)
        {
            if (!marques.TryGetValue(marqueFichier.Identifiant, out var uneMarque))
            {
                uneMarque = new Marque { Identifiant = marqueFichier.Identifiant };
                contexte.Marques.Add(uneMarque);
                marques[uneMarque.Identifiant] = uneMarque;
            }

            uneMarque.Nom = marqueFichier.Nom;
            uneMarque.IdentifiantPays = marqueFichier.IdentifiantPays;
            uneMarque.EstPersonnalisee = false;

            foreach (var voitureFichier in marqueFichier.Voitures)
            {
                if (!voitures.TryGetValue(voitureFichier.Identifiant, out var uneVoiture))
                {
                    uneVoiture = new Voiture { Identifiant = voitureFichier.Identifiant };
                    contexte.Voitures.Add(uneVoiture);
                    voitures[uneVoiture.Identifiant] = uneVoiture;
                }

                uneVoiture.Nom = voitureFichier.Nom;
                uneVoiture.Annee = voitureFichier.Annee;
                uneVoiture.Division = voitureFichier.Division;
                uneVoiture.Valeur = voitureFichier.Valeur;
                uneVoiture.Achetable = voitureFichier.Achetable;
                uneVoiture.ContenuTelechargeable = voitureFichier.ContenuTelechargeable;
                uneVoiture.IdentifiantMarque = marqueFichier.Identifiant;
                uneVoiture.EstPersonnalisee = false;

                var origine = voitureFichier.ReglageOrigine;
                if (origine is null)
                {
                    continue;
                }

                var empreinte = ChargeurReferentiel.CalculerEmpreinte(origine);

                if (empreintesExistantes.TryGetValue(origine.Identifiant, out var empreinteExistante))
                {
                    if (empreinteExistante == empreinte)
                    {
                        continue; // inchangé
                    }

                    // Corrigé dans le fichier : on remplace (les chronos associés sont supprimés, comme pour toute modification).
                    reglagesARemplacer.Add(origine.Identifiant);
                }

                reglagesAjoutes.Add((voitureFichier.Identifiant, origine, empreinte));
            }
        }

        // Étape 1 : marques et voitures (les réglages en dépendent).
        await contexte.SaveChangesAsync(annulation);

        // Étape 2 : suppression des réglages d'origine corrigés (la suppression en cascade est faite par la base).
        if (reglagesARemplacer.Count > 0)
        {
            await contexte.Reglages
                .Where(r => reglagesARemplacer.Contains(r.Identifiant))
                .ExecuteDeleteAsync(annulation);
        }

        // Étape 3 : ajout des réglages d'origine nouveaux ou corrigés.
        var maintenant = DateTime.UtcNow;
        foreach (var (identifiantVoiture, origine, empreinte) in reglagesAjoutes)
        {
            contexte.Reglages.Add(origine.VersReglage(identifiantVoiture, empreinte, maintenant));
        }

        await contexte.SaveChangesAsync(annulation);
    }
}
