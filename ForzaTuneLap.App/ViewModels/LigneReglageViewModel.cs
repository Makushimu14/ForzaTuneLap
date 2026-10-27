using CommunityToolkit.Mvvm.Input;
using ForzaTuneLap.Core.Services;

namespace ForzaTuneLap.App.ViewModels;

/// <summary>Un réglage dans la liste des réglages d'une voiture.</summary>
public sealed class LigneReglageViewModel
{
    public LigneReglageViewModel(
        LigneReglage ligne,
        string detail,
        string badge,
        Func<LigneReglageViewModel, Task> ouvrir,
        Func<LigneReglageViewModel, Task> dupliquer,
        Func<LigneReglageViewModel, Task> supprimer)
    {
        Ligne = ligne;
        Detail = detail;
        Badge = badge;

        OuvrirCommand = new AsyncRelayCommand(() => ouvrir(this));
        DupliquerCommand = new AsyncRelayCommand(() => dupliquer(this));
        SupprimerCommand = new AsyncRelayCommand(() => supprimer(this));
    }

    public LigneReglage Ligne { get; }

    public Guid Identifiant => Ligne.Identifiant;

    public string Nom => Ligne.Nom;

    public bool EstOrigine => Ligne.EstOrigine;

    /// <summary>Un réglage d'origine ne se supprime pas.</summary>
    public bool PeutSupprimer => !Ligne.EstOrigine;

    public string Detail { get; }

    public string Badge { get; }

    public IAsyncRelayCommand OuvrirCommand { get; }

    public IAsyncRelayCommand DupliquerCommand { get; }

    public IAsyncRelayCommand SupprimerCommand { get; }
}
