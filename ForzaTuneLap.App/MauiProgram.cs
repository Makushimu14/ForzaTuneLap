using ForzaTuneLap.App.Pages;
using ForzaTuneLap.App.Services;
using ForzaTuneLap.App.ViewModels;
using ForzaTuneLap.Core.Donnees;
using ForzaTuneLap.Core.Preferences;
using ForzaTuneLap.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForzaTuneLap.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(polices =>
            {
                polices.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                polices.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Base de données locale : un fichier SQLite dans le dossier privé de l'application, propre à chaque plateforme.
        var cheminBase = Path.Combine(FileSystem.AppDataDirectory, "forzatunelap.db");
        builder.Services.AddDbContextFactory<ContexteDonnees>(options => options.UseSqlite($"Data Source={cheminBase}"));

        // Services du Core. Les écrans demandent une unité de travail par opération (voir FabriqueUniteDeTravail).
        builder.Services.AddSingleton<FabriqueUniteDeTravail>();
        builder.Services.AddSingleton<ServiceDemarrage>();
        builder.Services.AddSingleton<IStockageDePreferences, StockageDePreferencesMaui>();
        builder.Services.AddSingleton<ServicePreferences>();
        builder.Services.AddSingleton<IDialogues, DialoguesMaui>();

        // Pages et modèles de vue. Transitoires : une page neuve à chaque affichage de la coquille.
        builder.Services.AddTransient<AppShell>();

        builder.Services.AddTransient<PageDemarrage>();
        builder.Services.AddTransient<DemarrageViewModel>();

        builder.Services.AddTransient<PageVoitures>();
        builder.Services.AddTransient<VoituresViewModel>();
        builder.Services.AddTransient<PageVoiture>();
        builder.Services.AddTransient<VoitureViewModel>();
        builder.Services.AddTransient<PageEditionReglage>();
        builder.Services.AddTransient<EditionReglageViewModel>();

        builder.Services.AddTransient<PageChrono>();
        builder.Services.AddTransient<ChronoViewModel>();

        builder.Services.AddTransient<PageChronos>();
        builder.Services.AddTransient<ChronosViewModel>();
        builder.Services.AddTransient<PageComparaison>();
        builder.Services.AddTransient<ComparaisonViewModel>();
        builder.Services.AddTransient<ComparaisonChronosViewModel>();
        builder.Services.AddTransient<ComparaisonReglagesViewModel>();
        builder.Services.AddTransient<FiltreReglagesViewModel>();

        builder.Services.AddTransient<PageParametres>();
        builder.Services.AddTransient<ParametresViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
