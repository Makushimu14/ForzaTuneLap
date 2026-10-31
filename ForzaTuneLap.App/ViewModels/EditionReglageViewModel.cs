using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>
/// Édition d'un réglage. Toute la logique (unités, validation, sections facultatives, rapports) est dans
/// <see cref="EditeurReglage"/> du Core : ce modèle de vue le charge, l'enregistre et gère les dialogues.
/// </summary>
public partial class EditionReglageViewModel : ObservableObject, IQueryAttributable
{
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;
    private Guid identifiantReglage;

    public EditionReglageViewModel(FabriqueUniteDeTravail fabrique, ServicePreferences preferences, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.preferences = preferences;
        this.dialogues = dialogues;

        // Même ordre que l'énumération Transmission : l'index du sélecteur est la valeur de l'énumération.
        Transmissions = Enum.GetValues<Transmission>().Select(transmission => ServiceLibelles.Obtenir(transmission)).ToList();

        titre = string.Empty;
        messageChronos = string.Empty;
    }

    public IReadOnlyList<string> Transmissions { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstEnLectureSeule))]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    [NotifyPropertyChangedFor(nameof(ChampsInvalides))]
    private EditeurReglage? editeur;

    [ObservableProperty]
    private string titre;

    [ObservableProperty]
    private string messageChronos;

    [ObservableProperty]
    private bool aUnMessageChronos;

    public bool EstEnLectureSeule => Editeur is { EstModifiable: false };

    public bool PeutEnregistrer => Editeur is { EstModifiable: true };

    /// <summary>Vrai tant qu'une valeur est invalide : l'enregistrement est alors refusé.</summary>
    public bool ChampsInvalides => Editeur is { EstValide: false };

    /// <summary>Reçoit l'identifiant du réglage depuis la navigation (« reglage?id=... »).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valeur)
            && Guid.TryParse(Convert.ToString(valeur, CultureInfo.InvariantCulture), out var identifiant))
        {
            identifiantReglage = identifiant;
        }
    }

    /// <summary>Charge le réglage. Appelée à l'affichage de la page : sans effet si c'est déjà fait, pour ne pas perdre une saisie.</summary>
    public async Task ChargerAsync()
    {
        if (Editeur is not null)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();
            var reglage = await travail.Reglages.ObtenirPourEditionAsync(identifiantReglage);
            var nombreDeChronos = await travail.Reglages.CompterChronosAsync(identifiantReglage);

            Titre = reglage.Nom;
            Editeur = new EditeurReglage(reglage, preferences.Unites, preferences.CultureDeFormat);

            AUnMessageChronos = nombreDeChronos > 0 && Editeur.EstModifiable;
            MessageChronos = string.Format(Texte.Obtenir("Edition_MessageChronos"), nombreDeChronos);
        });
    }

    partial void OnEditeurChanged(EditeurReglage? oldValue, EditeurReglage? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= SurChangementDeLEditeur;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += SurChangementDeLEditeur;
        }
    }

    private void SurChangementDeLEditeur(object? expediteur, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditeurReglage.EstValide))
        {
            OnPropertyChanged(nameof(ChampsInvalides));
        }
    }

    [RelayCommand]
    private void AjouterRapport() => Editeur?.Rapports.Ajouter();

    [RelayCommand]
    private void RetirerRapport() => Editeur?.Rapports.RetirerDernier();

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (Editeur is not { EstModifiable: true } editeur)
        {
            return;
        }

        if (!editeur.EstValide)
        {
            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), Texte.Obtenir("Edition_ChampsInvalides"));
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();

            var resultat = await travail.Reglages.ModifierAsync(editeur.Reglage);

            // Modifier les valeurs supprime les chronos du réglage : on demande confirmation avant.
            if (resultat.Statut == StatutModification.ConfirmationRequise)
            {
                var message = string.Format(Texte.Obtenir("Edition_ConfirmerChronos"), resultat.NombreDeChronos);
                if (!await dialogues.ConfirmerAsync(Texte.Obtenir("Edition_ConfirmerTitre"), message))
                {
                    return;
                }

                await travail.Reglages.ModifierAsync(editeur.Reglage, confirmerSuppressionDesChronos: true);
            }

            await Shell.Current.GoToAsync("..");
        });
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
                JournalErreurs.Ecrire(exception, "Édition d'un réglage");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
