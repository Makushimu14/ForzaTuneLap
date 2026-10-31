using ForzaTuneLap.App.Pages;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.Core.Preferences;

namespace ForzaTuneLap.App;

public partial class App : Application
{
    private readonly IServiceProvider services;
    private Window? fenetre;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        this.services = services;

        // Page de détail, atteinte par navigation (« voiture?id=12 »). À enregistrer une seule fois.
        Routing.RegisterRoute("voiture", typeof(PageVoiture));
        Routing.RegisterRoute("reglage", typeof(PageEditionReglage));
        Routing.RegisterRoute("chrono", typeof(PageChrono));

        AppDomain.CurrentDomain.UnhandledException += (_, e) => JournalErreurs.Ecrire(e.ExceptionObject as Exception, "AppDomain");
        TaskScheduler.UnobservedTaskException += (_, e) => JournalErreurs.Ecrire(e.Exception, "Tâche");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // La langue doit être appliquée avant la création de la première page, pour que les textes soient traduits.
        services.GetRequiredService<ServicePreferences>().AppliquerLangue();

        fenetre = new Window(services.GetRequiredService<PageDemarrage>()) { Title = "ForzaTuneLap" };
        return fenetre;
    }

    /// <summary>Remplace l'écran de démarrage par l'application, une fois la base prête.</summary>
    public void OuvrirApplication() => AfficherCoquille(ongletInitial: 0);

    /// <summary>Reconstruit toute l'interface (après un changement de langue) en revenant sur l'onglet indiqué.</summary>
    public void RecreerApplication(int ongletInitial) => AfficherCoquille(ongletInitial);

    private void AfficherCoquille(int ongletInitial)
    {
        if (fenetre is null)
        {
            return;
        }

        // Le remplacement se fait APRÈS la fin du gestionnaire d'événement qui l'a demandé (par exemple le changement
        // de la liste déroulante de langue) : on ne détruit pas un contrôle pendant qu'il est en train de s'en servir.
        Dispatcher.Dispatch(() =>
        {
            try
            {
                var coquille = services.GetRequiredService<AppShell>();

                // L'onglet est choisi AVANT d'attacher la coquille à la fenêtre. Le changer après, pendant que la coquille
                // effectue sa navigation initiale, laisse cette navigation « en attente » : le clic suivant sur un onglet
                // est alors refusé (« Pending Navigations still processing »).
                coquille.SelectionnerOnglet(ongletInitial);
                fenetre.Page = coquille;
            }
            catch (Exception exception)
            {
                JournalErreurs.Ecrire(exception, "Affichage de la coquille");
                throw;
            }
        });
    }
}
