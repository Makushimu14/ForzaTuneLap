using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Paramètres de l'application : langue et unités de mesure.</summary>
public partial class ParametresViewModel : ObservableObject
{
    /// <summary>Grandeurs pour lesquelles plusieurs unités existent.</summary>
    private static readonly TypeDeGrandeur[] GrandeursConfigurables =
    {
        TypeDeGrandeur.Masse,
        TypeDeGrandeur.Puissance,
        TypeDeGrandeur.Couple,
        TypeDeGrandeur.Vitesse,
        TypeDeGrandeur.Distance,
        TypeDeGrandeur.DistanceLongue,
        TypeDeGrandeur.Hauteur,
        TypeDeGrandeur.Pression,
        TypeDeGrandeur.RaideurDeRessort,
        TypeDeGrandeur.Force
    };

    private readonly ServicePreferences preferences;
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly IDialogues dialogues;
    private readonly IFichiers fichiers;
    private int indexLangue;

    public ParametresViewModel(
        ServicePreferences preferences,
        FabriqueUniteDeTravail fabrique,
        IDialogues dialogues,
        IFichiers fichiers)
    {
        this.preferences = preferences;
        this.fabrique = fabrique;
        this.dialogues = dialogues;
        this.fichiers = fichiers;

        // Même ordre que l'énumération ChoixDeLangue : l'index du sélecteur est la valeur de l'énumération.
        Langues = new[]
        {
            Texte.Obtenir("Langue_Systeme"),
            Texte.Obtenir("Langue_Francais"),
            Texte.Obtenir("Langue_Anglais")
        };
        indexLangue = (int)preferences.Langue;

        Unites = GrandeursConfigurables.Select(grandeur => new ChoixUniteViewModel(grandeur, preferences)).ToList();
    }

    /// <summary>Levé après un changement de langue : la page demande alors à l'application de reconstruire son interface.</summary>
    public event EventHandler? LangueModifiee;

    public IReadOnlyList<string> Langues { get; }

    public IReadOnlyList<ChoixUniteViewModel> Unites { get; }

    public int IndexLangue
    {
        get => indexLangue;
        set
        {
            if (value < 0 || value >= Langues.Count)
            {
                OnPropertyChanged();
                return;
            }

            if (value == indexLangue)
            {
                return;
            }

            indexLangue = value;
            preferences.DefinirLangue((ChoixDeLangue)value);
            preferences.AppliquerLangue();
            OnPropertyChanged();
            LangueModifiee?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void UtiliserMetrique()
    {
        preferences.UtiliserSystemeMetrique();
        ActualiserUnites();
    }

    [RelayCommand]
    private void UtiliserImperial()
    {
        preferences.UtiliserSystemeImperial();
        ActualiserUnites();
    }

    /// <summary>Écrit toutes les données de l'utilisateur dans un fichier JSON, puis ouvre le panneau de partage du système.</summary>
    [RelayCommand]
    private async Task ExporterAsync()
    {
        await ExecuterAsync(async () =>
        {
            // Les anciens exports (qui contiennent les données de l'utilisateur) ne restent pas dans le dossier temporaire.
            foreach (var ancien in Directory.EnumerateFiles(fichiers.DossierTemporaire, "forzatunelap-*.json"))
            {
                File.Delete(ancien);
            }

            var chemin = Path.Combine(
                fichiers.DossierTemporaire,
                $"forzatunelap-{DateTime.Now.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture)}.json");

            using (var travail = fabrique.Creer())
            await using (var flux = File.Create(chemin))
            {
                await travail.Sauvegarde.ExporterAsync(flux);
            }

            await fichiers.PartagerAsync(Texte.Obtenir("Donnees_ExporterTitre"), chemin);
        });
    }

    /// <summary>Choisit un fichier, demande le mode (fusionner ou tout remplacer), importe, puis résume le résultat.</summary>
    [RelayCommand]
    private async Task ImporterAsync()
    {
        await ExecuterAsync(async () =>
        {
            var choisi = await fichiers.ChoisirFichierJsonAsync(Texte.Obtenir("Donnees_ImporterTitre"));
            if (choisi is null)
            {
                return;
            }

            await using var contenu = choisi.Value.Contenu;

            var choix = await dialogues.ChoisirAsync(
                string.Format(Texte.Obtenir("Donnees_ChoixMode"), choisi.Value.Nom),
                Texte.Obtenir("Dialogue_Annuler"),
                Texte.Obtenir("Donnees_Fusionner"),
                Texte.Obtenir("Donnees_ToutRemplacer"));
            if (choix is null)
            {
                return;
            }

            var mode = choix == 0 ? ModeImport.Fusionner : ModeImport.TousRemplacer;

            // Tout remplacer efface les données de l'utilisateur : une confirmation explicite est demandée.
            if (mode == ModeImport.TousRemplacer
                && !await dialogues.ConfirmerAsync(
                    Texte.Obtenir("Donnees_ToutRemplacer"),
                    Texte.Obtenir("Donnees_ConfirmerToutRemplacer"),
                    Texte.Obtenir("Donnees_ToutRemplacer"),
                    Texte.Obtenir("Dialogue_Annuler")))
            {
                return;
            }

            ResultatImport resultat;
            using (var travail = fabrique.Creer())
            {
                resultat = await travail.Sauvegarde.ImporterAsync(contenu, mode);
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Donnees_ImportTermine"), Resumer(resultat));
        });
    }

    private static string Resumer(ResultatImport resultat)
    {
        var lignes = new List<string>
        {
            string.Format(
                Texte.Obtenir("Donnees_ResumeReglages"),
                resultat.ReglagesAjoutes,
                resultat.ReglagesMisAJour,
                resultat.ReglagesIgnores,
                resultat.ReglagesRenommes),
            string.Format(
                Texte.Obtenir("Donnees_ResumeChronos"),
                resultat.ChronosAjoutes,
                resultat.ChronosMisAJour,
                resultat.ChronosIgnores,
                resultat.ChronosSupprimes),
            string.Format(Texte.Obtenir("Donnees_ResumeVoitures"), resultat.MarquesAjoutees, resultat.VoituresAjoutees)
        };

        if (resultat.Avertissements.Count > 0)
        {
            lignes.Add(string.Format(Texte.Obtenir("Donnees_ResumeAvertissements"), resultat.Avertissements.Count));
        }

        return string.Join(Environment.NewLine, lignes);
    }

    /// <summary>Exécute une action en transformant toute erreur en message, au lieu d'arrêter l'application.</summary>
    private async Task ExecuterAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            if (exception is not ErreurMetierException)
            {
                JournalErreurs.Ecrire(exception, "Import ou export des données");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }

    private void ActualiserUnites()
    {
        foreach (var choix in Unites)
        {
            choix.Actualiser();
        }
    }
}
