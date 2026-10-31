using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Modeles;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.App.ViewModels;

public sealed record OptionParcours(int Identifiant, string Nom);

public sealed record OptionCircuit(int Identifiant, string Nom, IReadOnlyList<OptionParcours> Parcours);

public sealed record OptionVoiture(int Identifiant, string Nom);

public sealed record OptionReglage(Guid Identifiant, string Nom, bool ADejaUnChrono)
{
    public string Libelle => ADejaUnChrono ? $"{Nom} ({Texte.Obtenir("Chrono_DejaEnregistre")})" : Nom;
}

/// <summary>
/// Ajout ou correction d'un chrono. Navigation :
/// « chrono?id=... » corrige un chrono ; « chrono?reglage=... » ajoute un chrono à ce réglage ;
/// « chrono?voiture=12 » ajoute un chrono à l'un des réglages de cette voiture ; « chrono » seul laisse tout choisir.
/// La saisie de la durée est dans <see cref="EditeurChrono"/> du Core.
/// </summary>
public partial class ChronoViewModel : ObservableObject, IQueryAttributable
{
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly ServicePreferences preferences;
    private readonly IDialogues dialogues;

    private Guid? identifiantChronoDemande;
    private Guid? identifiantReglageDemande;
    private int? identifiantVoitureDemandee;
    private bool charge;
    private bool chargementEnCours;

    public ChronoViewModel(FabriqueUniteDeTravail fabrique, ServicePreferences preferences, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.preferences = preferences;
        this.dialogues = dialogues;

        titre = string.Empty;
        resume = string.Empty;
    }

    [ObservableProperty]
    private string titre;

    /// <summary>En correction : le parcours, la voiture et le réglage, qui ne se changent pas.</summary>
    [ObservableProperty]
    private string resume;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    [NotifyPropertyChangedFor(nameof(DureeInvalide))]
    private EditeurChrono? editeur;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChoixDuParcoursVisible))]
    [NotifyPropertyChangedFor(nameof(ChoixDeLaVoitureVisible))]
    [NotifyPropertyChangedFor(nameof(ChoixDuReglageVisible))]
    [NotifyPropertyChangedFor(nameof(PeutSupprimer))]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    [NotifyPropertyChangedFor(nameof(ReglageDejaChronometre))]
    private bool estModification;

    [ObservableProperty]
    private bool voitureImposee;

    [ObservableProperty]
    private bool reglageImpose;

    [ObservableProperty]
    private IReadOnlyList<OptionCircuit> circuits = Array.Empty<OptionCircuit>();

    [ObservableProperty]
    private OptionCircuit? circuitSelectionne;

    [ObservableProperty]
    private IReadOnlyList<OptionParcours> parcours = Array.Empty<OptionParcours>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private OptionParcours? parcoursSelectionne;

    [ObservableProperty]
    private IReadOnlyList<OptionVoiture> voitures = Array.Empty<OptionVoiture>();

    [ObservableProperty]
    private OptionVoiture? voitureSelectionnee;

    [ObservableProperty]
    private IReadOnlyList<OptionReglage> reglages = Array.Empty<OptionReglage>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    [NotifyPropertyChangedFor(nameof(ReglageDejaChronometre))]
    private OptionReglage? reglageSelectionne;

    public bool ChoixDuParcoursVisible => !EstModification;

    public bool ChoixDeLaVoitureVisible => !EstModification && !VoitureImposee && !ReglageImpose;

    public bool ChoixDuReglageVisible => !EstModification && !ReglageImpose;

    public bool PeutSupprimer => EstModification;

    /// <summary>Le réglage choisi a déjà un chrono sur ce parcours : le deuxième serait refusé, il faut modifier l'existant.</summary>
    public bool ReglageDejaChronometre => !EstModification && ReglageSelectionne is { ADejaUnChrono: true };

    public bool DureeInvalide => Editeur is { EstInvalide: true };

    public bool PeutEnregistrer =>
        Editeur is { PeutEnregistrer: true }
        && (EstModification || (ParcoursSelectionne is not null && ReglageSelectionne is { ADejaUnChrono: false }));

    partial void OnVoitureImposeeChanged(bool value) => OnPropertyChanged(nameof(ChoixDeLaVoitureVisible));

    partial void OnReglageImposeChanged(bool value)
    {
        OnPropertyChanged(nameof(ChoixDeLaVoitureVisible));
        OnPropertyChanged(nameof(ChoixDuReglageVisible));
    }

    /// <summary>Reçoit ce que la navigation demande (voir la description de la classe).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var chrono) && Guid.TryParse(Convert.ToString(chrono, CultureInfo.InvariantCulture), out var identifiantChrono))
        {
            identifiantChronoDemande = identifiantChrono;
        }

        if (query.TryGetValue("reglage", out var reglage) && Guid.TryParse(Convert.ToString(reglage, CultureInfo.InvariantCulture), out var identifiantReglage))
        {
            identifiantReglageDemande = identifiantReglage;
        }

        if (query.TryGetValue("voiture", out var voiture)
            && int.TryParse(Convert.ToString(voiture, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var identifiantVoiture))
        {
            identifiantVoitureDemandee = identifiantVoiture;
        }
    }

    /// <summary>Charge l'écran. Sans effet si c'est déjà fait, pour ne pas perdre une saisie.</summary>
    public async Task ChargerAsync()
    {
        if (charge)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            chargementEnCours = true;
            try
            {
                using var travail = fabrique.Creer();

                if (identifiantChronoDemande is { } identifiantChrono)
                {
                    await ChargerCorrectionAsync(travail, identifiantChrono);
                }
                else
                {
                    await ChargerAjoutAsync(travail);
                }
            }
            finally
            {
                chargementEnCours = false;
            }

            charge = true;
        });
    }

    private async Task ChargerCorrectionAsync(UniteDeTravail travail, Guid identifiantChrono)
    {
        var chrono = await travail.Contexte.Chronos.AsNoTracking()
            .Include(c => c.Parcours).ThenInclude(p => p!.Circuit)
            .Include(c => c.Reglage).ThenInclude(r => r!.Voiture).ThenInclude(v => v!.Marque)
            .FirstOrDefaultAsync(c => c.Identifiant == identifiantChrono)
            ?? throw new ErreurMetierException(CodeErreurMetier.ChronoIntrouvable);

        EstModification = true;
        Titre = Texte.Obtenir("Chrono_TitreModification");
        Resume = string.Join(
            Environment.NewLine,
            $"{chrono.Parcours!.Circuit!.Nom} · {NomsAffiches.Parcours(chrono.Parcours)}",
            $"{chrono.Reglage!.Voiture!.Marque!.Nom} {chrono.Reglage.Voiture.Nom} · {chrono.Reglage.Nom}");

        Editeur = new EditeurChrono(chrono, preferences.CultureDeFormat);
    }

    private async Task ChargerAjoutAsync(UniteDeTravail travail)
    {
        EstModification = false;
        Titre = Texte.Obtenir("Chrono_TitreAjout");
        Editeur = new EditeurChrono(preferences.CultureDeFormat);

        Circuits = await OptionsDeParcours.ChargerAsync(travail);

        int? identifiantVoiture = identifiantVoitureDemandee;
        if (identifiantReglageDemande is { } identifiantReglage)
        {
            var reglage = await travail.Contexte.Reglages.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Identifiant == identifiantReglage)
                ?? throw new ErreurMetierException(CodeErreurMetier.ReglageIntrouvable);
            identifiantVoiture = reglage.IdentifiantVoiture;
            ReglageImpose = true;
        }

        if (identifiantVoiture is { } voitureConnue)
        {
            VoitureImposee = true;
            var detail = await travail.ListeVoitures.ObtenirDetailAsync(voitureConnue);
            Voitures = new[] { new OptionVoiture(detail.Voiture.Identifiant, NomVoiture(detail.NomMarque, detail.Voiture)) };
            VoitureSelectionnee = Voitures[0];
        }
        else
        {
            var lignes = await travail.ListeVoitures.ListerAsync();
            Voitures = lignes
                .Select(ligne => new OptionVoiture(ligne.Identifiant, NomVoiture(ligne.NomMarque, ligne.Voiture)))
                .ToList();
        }

        // La voiture choisie ci-dessus a déjà déclenché le chargement de ses réglages (en tâche de fond) : on le refait ici
        // en attendant, pour pouvoir sélectionner le réglage demandé avant l'affichage.
        if (VoitureSelectionnee is not null)
        {
            await ActualiserReglagesAsync(travail, identifiantReglageDemande);
        }
    }

    partial void OnEditeurChanged(EditeurChrono? oldValue, EditeurChrono? newValue)
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
        if (e.PropertyName is nameof(EditeurChrono.PeutEnregistrer) or nameof(EditeurChrono.EstInvalide))
        {
            OnPropertyChanged(nameof(PeutEnregistrer));
            OnPropertyChanged(nameof(DureeInvalide));
        }
    }

    partial void OnCircuitSelectionneChanged(OptionCircuit? value)
    {
        Parcours = value?.Parcours ?? Array.Empty<OptionParcours>();
        ParcoursSelectionne = Parcours.Count == 1 ? Parcours[0] : null;
    }

    partial void OnParcoursSelectionneChanged(OptionParcours? value) => ActualiserEnTacheDeFond();

    partial void OnVoitureSelectionneeChanged(OptionVoiture? value) => ActualiserEnTacheDeFond();

    private void ActualiserEnTacheDeFond()
    {
        if (chargementEnCours || EstModification)
        {
            return;
        }

        _ = ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();
            await ActualiserReglagesAsync(travail, ReglageSelectionne?.Identifiant);
        });
    }

    /// <summary>
    /// Recharge les réglages de la voiture choisie en signalant ceux qui ont déjà un chrono sur le parcours choisi,
    /// puis rétablit la sélection du réglage <paramref name="aSelectionner"/> s'il existe encore.
    /// </summary>
    private async Task ActualiserReglagesAsync(UniteDeTravail travail, Guid? aSelectionner)
    {
        if (VoitureSelectionnee is not { } voiture)
        {
            Reglages = Array.Empty<OptionReglage>();
            ReglageSelectionne = null;
            return;
        }

        var detail = await travail.ListeVoitures.ObtenirDetailAsync(voiture.Identifiant);
        IReadOnlySet<Guid> dejaChronometres = ParcoursSelectionne is { } choisi
            ? await travail.Chronos.ReglagesAvecChronoAsync(voiture.Identifiant, choisi.Identifiant)
            : new HashSet<Guid>();

        Reglages = detail.Reglages
            .OrderByDescending(r => r.EstOrigine)
            .ThenBy(r => r.Nom, StringComparer.CurrentCultureIgnoreCase)
            .Select(r => new OptionReglage(r.Identifiant, r.Nom, dejaChronometres.Contains(r.Identifiant)))
            .ToList();

        ReglageSelectionne = aSelectionner is { } identifiant
            ? Reglages.FirstOrDefault(r => r.Identifiant == identifiant)
            : null;
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (Editeur is not { Duree: { } duree } editeur || !PeutEnregistrer)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();
            var date = DateTime.SpecifyKind(editeur.DateRealisation.Date, DateTimeKind.Utc);

            if (editeur.IdentifiantChrono is { } identifiantChrono)
            {
                await travail.Chronos.ModifierAsync(identifiantChrono, duree, date, editeur.Commentaire);
            }
            else
            {
                await travail.Chronos.AjouterAsync(
                    ReglageSelectionne!.Identifiant,
                    ParcoursSelectionne!.Identifiant,
                    duree,
                    date,
                    editeur.Commentaire);
            }

            await Shell.Current.GoToAsync("..");
        });
    }

    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (Editeur is not { IdentifiantChrono: { } identifiantChrono } editeur)
        {
            return;
        }

        var message = string.Format(
            Texte.Obtenir("Chrono_ConfirmerSuppression"),
            DureesDeTour.Formater(editeur.Duree ?? TimeSpan.Zero, preferences.CultureDeFormat));
        if (!await dialogues.ConfirmerAsync(Texte.Obtenir("Chrono_SupprimerTitre"), message))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();
            await travail.Chronos.SupprimerAsync(identifiantChrono);
            await Shell.Current.GoToAsync("..");
        });
    }

    private static string NomVoiture(string nomMarque, Voiture voiture) =>
        $"{voiture.Annee.ToString(CultureInfo.InvariantCulture)} {nomMarque} {voiture.Nom}";

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
                JournalErreurs.Ecrire(exception, "Écran d'un chrono");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
