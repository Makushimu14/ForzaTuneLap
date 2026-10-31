using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>L'onglet Comparer : une bascule entre la comparaison des chronos et celle des réglages.</summary>
public partial class ComparaisonViewModel : ObservableObject
{
    public ComparaisonViewModel(ComparaisonChronosViewModel chronos, ComparaisonReglagesViewModel reglages)
    {
        Chronos = chronos;
        Reglages = reglages;
    }

    public ComparaisonChronosViewModel Chronos { get; }

    public ComparaisonReglagesViewModel Reglages { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReglagesAffiches))]
    private bool chronosAffiches = true;

    public bool ReglagesAffiches => !ChronosAffiches;

    [RelayCommand]
    private void AfficherChronos() => ChronosAffiches = true;

    [RelayCommand]
    private void AfficherReglages() => ChronosAffiches = false;

    /// <summary>Appelée à chaque affichage de l'onglet : seule la comparaison visible est calculée.</summary>
    public Task ChargerAsync() => ChronosAffiches ? Chronos.ChargerAsync() : Reglages.ChargerAsync();

    partial void OnChronosAffichesChanged(bool value) => _ = ChargerAsync();
}
