using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.Services;

/// <summary>
/// Après le démarrage, propose de fusionner les marques et voitures personnalisées qui existent désormais dans le catalogue
/// officiel. Une correspondance à la fois, et rien n'est fusionné sans l'accord de l'utilisateur ; « Plus tard » laisse
/// la correspondance en place, elle sera reproposée au prochain démarrage.
/// </summary>
public sealed class ProposeurDeFusions
{
    // Garde-fou : chaque tour traite une correspondance, jamais plus de cet ordre de grandeur.
    private const int NombreMaximumDePropositions = 500;

    private readonly FabriqueUniteDeTravail fabrique;
    private readonly IDialogues dialogues;

    public ProposeurDeFusions(FabriqueUniteDeTravail fabrique, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.dialogues = dialogues;
    }

    public async Task ProposerAsync()
    {
        try
        {
            var marquesIgnorees = new HashSet<int>();
            var voituresIgnorees = new HashSet<int>();

            for (var tour = 0; tour < NombreMaximumDePropositions; tour++)
            {
                // Les correspondances sont recalculées à chaque tour : fusionner une marque fusionne aussi ses voitures.
                FusionsPossibles fusions;
                using (var travail = fabrique.Creer())
                {
                    fusions = await travail.Catalogue.DetecterFusionsAsync();
                }

                var marque = fusions.Marques.FirstOrDefault(c => !marquesIgnorees.Contains(c.Personnalisee.Identifiant));
                if (marque is not null)
                {
                    var message = string.Format(Texte.Obtenir("Fusion_Marque"), marque.Personnalisee.Nom, marque.Officielle.Nom);
                    if (await Demander(message))
                    {
                        using var travail = fabrique.Creer();
                        await travail.Catalogue.FusionnerMarqueAsync(marque.Personnalisee.Identifiant, marque.Officielle.Identifiant);
                    }
                    else
                    {
                        marquesIgnorees.Add(marque.Personnalisee.Identifiant);
                    }

                    continue;
                }

                var voiture = fusions.Voitures.FirstOrDefault(c => !voituresIgnorees.Contains(c.Personnalisee.Identifiant));
                if (voiture is not null)
                {
                    var message = string.Format(Texte.Obtenir("Fusion_Voiture"), voiture.Personnalisee.NomComplet, voiture.Officielle.NomComplet);
                    if (await Demander(message))
                    {
                        using var travail = fabrique.Creer();
                        await travail.Catalogue.FusionnerVoitureAsync(voiture.Personnalisee.Identifiant, voiture.Officielle.Identifiant);
                    }
                    else
                    {
                        voituresIgnorees.Add(voiture.Personnalisee.Identifiant);
                    }

                    continue;
                }

                return; // plus rien à proposer
            }
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Fusion des voitures personnalisées");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }

    private Task<bool> Demander(string message) =>
        dialogues.ConfirmerAsync(
            Texte.Obtenir("Fusion_Titre"),
            message,
            Texte.Obtenir("Fusion_Fusionner"),
            Texte.Obtenir("Fusion_PlusTard"));
}
