using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Enumerations;
using ForzaTuneLap.Core.Ressources;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Un choix de marque. L'identifiant -1 est l'entrée « Nouvelle marque… ».</summary>
public sealed record ChoixMarque(int Identifiant, string Nom)
{
    public const int NouvelleMarque = -1;

    public bool EstNouvelle => Identifiant == NouvelleMarque;
}

public sealed record OptionPays(int Identifiant, string Nom);

/// <summary>
/// Ajout ou correction d'une voiture personnalisée. Navigation : « editionvoiture » ajoute une voiture,
/// « editionvoiture?id=100001 » corrige une voiture personnalisée. La saisie est dans <see cref="EditeurVoiturePersonnalisee"/> du Core.
/// </summary>
public partial class EditionVoitureViewModel : ObservableObject, IQueryAttributable
{
    private readonly FabriqueUniteDeTravail fabrique;
    private readonly IDialogues dialogues;
    private readonly IReadOnlyList<Division> valeursDivision = Enum.GetValues<Division>();

    private int? identifiantVoitureDemandee;
    private bool charge;

    public EditionVoitureViewModel(FabriqueUniteDeTravail fabrique, IDialogues dialogues)
    {
        this.fabrique = fabrique;
        this.dialogues = dialogues;

        Divisions = valeursDivision.Select(division => ServiceLibelles.Obtenir(division)).ToList();
        titre = string.Empty;
        nomNouvelleMarque = string.Empty;
    }

    public IReadOnlyList<string> Divisions { get; }

    [ObservableProperty]
    private string titre;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private EditeurVoiturePersonnalisee? editeur;

    [ObservableProperty]
    private IReadOnlyList<ChoixMarque> marques = Array.Empty<ChoixMarque>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NouvelleMarqueChoisie))]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private ChoixMarque? marqueSelectionnee;

    [ObservableProperty]
    private IReadOnlyList<OptionPays> pays = Array.Empty<OptionPays>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private OptionPays? paysSelectionne;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutEnregistrer))]
    private string nomNouvelleMarque;

    [ObservableProperty]
    private int indexDivision;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TexteBoutonAvance))]
    private bool avanceVisible;

    public bool NouvelleMarqueChoisie => MarqueSelectionnee is { EstNouvelle: true };

    public string TexteBoutonAvance => Texte.Obtenir(AvanceVisible ? "Voiture_MasquerAvance" : "Voiture_Avance");

    public bool PeutEnregistrer =>
        Editeur is { PeutEnregistrer: true }
        && MarqueSelectionnee is not null
        && (!NouvelleMarqueChoisie || (!string.IsNullOrWhiteSpace(NomNouvelleMarque) && PaysSelectionne is not null));

    partial void OnIndexDivisionChanged(int value)
    {
        if (Editeur is not null && value >= 0 && value < valeursDivision.Count)
        {
            Editeur.Division = valeursDivision[value];
        }
    }

    partial void OnEditeurChanged(EditeurVoiturePersonnalisee? oldValue, EditeurVoiturePersonnalisee? newValue)
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
        if (e.PropertyName == nameof(EditeurVoiturePersonnalisee.PeutEnregistrer))
        {
            OnPropertyChanged(nameof(PeutEnregistrer));
        }
    }

    [RelayCommand]
    private void BasculerAvance() => AvanceVisible = !AvanceVisible;

    /// <summary>Reçoit l'identifiant de la voiture à corriger depuis la navigation.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var valeur)
            && int.TryParse(Convert.ToString(valeur, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var identifiant))
        {
            identifiantVoitureDemandee = identifiant;
        }
    }

    /// <summary>Charge le formulaire. Sans effet si c'est déjà fait, pour ne pas perdre une saisie.</summary>
    public async Task ChargerAsync()
    {
        if (charge)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();

            var marquesCharges = await travail.Contexte.Marques.AsNoTracking().ToListAsync();
            var choix = marquesCharges
                .OrderBy(m => m.Nom, StringComparer.CurrentCultureIgnoreCase)
                .Select(m => new ChoixMarque(m.Identifiant, m.Nom))
                .Append(new ChoixMarque(ChoixMarque.NouvelleMarque, Texte.Obtenir("Voiture_NouvelleMarque")))
                .ToList();

            var nomsDePays = await travail.Contexte.Pays.AsNoTracking().ToListAsync();
            var anglais = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
            Pays = nomsDePays
                .Select(p => new OptionPays(p.Identifiant, anglais && !string.IsNullOrWhiteSpace(p.NomAnglais) ? p.NomAnglais : p.NomFrancais))
                .OrderBy(p => p.Nom, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            Marques = choix;

            if (identifiantVoitureDemandee is { } identifiant)
            {
                var voiture = await travail.Contexte.Voitures.AsNoTracking().FirstOrDefaultAsync(v => v.Identifiant == identifiant)
                    ?? throw new ErreurMetierException(CodeErreurMetier.VoitureIntrouvable);
                if (!voiture.EstPersonnalisee)
                {
                    throw new ErreurMetierException(CodeErreurMetier.VoitureOfficielleNonModifiable);
                }

                Titre = Texte.Obtenir("Voiture_TitreModification");
                Editeur = new EditeurVoiturePersonnalisee(voiture);
                IndexDivision = valeursDivision.ToList().IndexOf(voiture.Division);
                MarqueSelectionnee = choix.First(c => c.Identifiant == voiture.IdentifiantMarque);
            }
            else
            {
                Titre = Texte.Obtenir("Voiture_TitreAjout");
                Editeur = new EditeurVoiturePersonnalisee();
                IndexDivision = valeursDivision.ToList().IndexOf(Editeur.Division);
            }

            charge = true;
        });
    }

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (Editeur is not { Annee: { } annee, Valeur: { } valeur } editeur || MarqueSelectionnee is not { } marque || !PeutEnregistrer)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            using var travail = fabrique.Creer();

            var identifiantMarque = marque.Identifiant;
            var marqueCreee = false;
            if (marque.EstNouvelle)
            {
                var creee = await travail.Catalogue.AjouterMarquePersonnaliseeAsync(NomNouvelleMarque, PaysSelectionne!.Identifiant);
                identifiantMarque = creee.Identifiant;
                marqueCreee = true;
            }

            try
            {
                if (editeur.IdentifiantVoiture is { } identifiantVoiture)
                {
                    await travail.Catalogue.ModifierVoiturePersonnaliseeAsync(
                        identifiantVoiture, identifiantMarque, editeur.Nom, annee, editeur.Division, valeur, editeur.ContenuTelechargeable);
                }
                else
                {
                    await travail.Catalogue.AjouterVoiturePersonnaliseeAsync(
                        identifiantMarque, editeur.Nom, annee, editeur.Division, valeur, editeur.ContenuTelechargeable);
                }
            }
            catch
            {
                // La voiture est refusée (doublon…) : on ne laisse pas la marque qu'on vient de créer, vide et inutile.
                if (marqueCreee)
                {
                    await travail.Catalogue.SupprimerMarquePersonnaliseeAsync(identifiantMarque);
                }

                throw;
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
                JournalErreurs.Ecrire(exception, "Édition d'une voiture personnalisée");
            }

            await dialogues.AfficherMessageAsync(Texte.Obtenir("Erreur_Titre"), MessagesErreurs.Obtenir(exception));
        }
    }
}
