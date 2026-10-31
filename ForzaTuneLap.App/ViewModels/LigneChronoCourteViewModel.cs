using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.Core.Edition;
using ForzaTuneLap.Core.Modeles;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>
/// Un chrono dans la liste d'une voiture ou d'un réglage : le circuit et le parcours, le temps, le réglage (ou la date)
/// et le commentaire. Le chrono doit être chargé avec son parcours, son circuit et son réglage.
/// </summary>
public sealed class LigneChronoCourteViewModel
{
    private LigneChronoCourteViewModel(
        Guid identifiant,
        string parcours,
        string duree,
        string detail,
        string? commentaire,
        Func<LigneChronoCourteViewModel, Task> ouvrir)
    {
        Identifiant = identifiant;
        Parcours = parcours;
        Duree = duree;
        Detail = detail;
        Commentaire = commentaire;
        OuvrirCommand = new AsyncRelayCommand(() => ouvrir(this));
    }

    public Guid Identifiant { get; }

    public string Parcours { get; }

    public string Duree { get; }

    /// <summary>Le réglage et la date.</summary>
    public string Detail { get; }

    public string? Commentaire { get; }

    public bool ACommentaire => !string.IsNullOrWhiteSpace(Commentaire);

    public IAsyncRelayCommand OuvrirCommand { get; }

    /// <param name="avecReglage">Vrai pour la liste d'une voiture (qui a plusieurs réglages), faux pour celle d'un réglage.</param>
    /// <param name="ouvrir">Appelée quand l'utilisateur touche le chrono.</param>
    public static List<LigneChronoCourteViewModel> Creer(
        IEnumerable<Chrono> chronos,
        CultureInfo culture,
        bool avecReglage,
        Func<LigneChronoCourteViewModel, Task> ouvrir) =>
        chronos
            .Select(chrono =>
            {
                var date = chrono.DateRealisation.ToString("d", culture);
                return new LigneChronoCourteViewModel(
                    chrono.Identifiant,
                    $"{chrono.Parcours!.Circuit!.Nom} · {NomsAffiches.Parcours(chrono.Parcours)}",
                    DureesDeTour.Formater(chrono.DureeMillisecondes, culture),
                    avecReglage ? $"{chrono.Reglage!.Nom} · {date}" : date,
                    chrono.Commentaire,
                    ouvrir);
            })
            .ToList();
}
