using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.App.Traductions;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Écran de démarrage : prépare la base de données, puis prévient la page que l'application peut s'ouvrir.</summary>
public partial class DemarrageViewModel : ObservableObject
{
    private readonly ServiceDemarrage demarrage;

    public DemarrageViewModel(ServiceDemarrage demarrage)
    {
        this.demarrage = demarrage;
        message = Texte.Obtenir("Demarrage_Chargement");
    }

    /// <summary>Levé quand la base est prête.</summary>
    public event EventHandler? Termine;

    [ObservableProperty]
    private bool enCours = true;

    [ObservableProperty]
    private bool enErreur;

    [ObservableProperty]
    private string message;

    [RelayCommand]
    private async Task DemarrerAsync()
    {
        EnCours = true;
        EnErreur = false;
        Message = Texte.Obtenir("Demarrage_Chargement");

        try
        {
            // Hors du fil de l'interface : la première exécution crée la base et peut durer un instant.
            await Task.Run(() => demarrage.DemarrerAsync());
            Termine?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            EnErreur = true;
            EnCours = false;
            Message = $"{Texte.Obtenir("Demarrage_Erreur")}{Environment.NewLine}{exception.Message}";
        }
    }
}
