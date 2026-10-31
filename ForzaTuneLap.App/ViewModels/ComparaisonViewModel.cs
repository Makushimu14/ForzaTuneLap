using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>L'onglet Comparer : une bascule entre la comparaison des chronos et celle des réglages.</summary>
public partial class ComparaisonViewModel : ObservableObject
{
    public ComparaisonViewModel(ComparaisonChronosViewModel chronos)
    {
        Chronos = chronos;
    }

    public ComparaisonChronosViewModel Chronos { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReglagesAffiches))]
    private bool chronosAffiches = true;

    public bool ReglagesAffiches => !ChronosAffiches;

    [RelayCommand]
    private void AfficherChronos() => ChronosAffiches = true;

    [RelayCommand]
    private void AfficherReglages() => ChronosAffiches = false;

    /// <summary>Appelée à chaque affichage de l'onglet.</summary>
    public async Task ChargerAsync()
    {
        if (ChronosAffiches)
        {
            await Chronos.ChargerAsync();
        }
    }

    partial void OnChronosAffichesChanged(bool value)
    {
        if (value)
        {
            _ = Chronos.ChargerAsync();
        }
    }
}
