namespace ForzaTuneLap.App;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    /// <summary>Affiche l'onglet demandé (0 = voitures, 1 = chronos, 2 = comparaison, 3 = paramètres).</summary>
    public void SelectionnerOnglet(int index)
    {
        if (Items.Count == 0)
        {
            return;
        }

        var barre = Items[0];
        if (index >= 0 && index < barre.Items.Count)
        {
            barre.CurrentItem = barre.Items[index];
        }
    }
}
