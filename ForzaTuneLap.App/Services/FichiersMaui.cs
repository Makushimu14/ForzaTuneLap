namespace ForzaTuneLap.App.Services;

public sealed class FichiersMaui : IFichiers
{
    // Types de fichiers acceptés par plateforme. Écrits à la main, sans réflexion (compatibilité iOS).
    private static readonly FilePickerFileType TypesJson = new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".json" } },
        { DevicePlatform.Android, new[] { "application/json", "text/plain" } },
        { DevicePlatform.iOS, new[] { "public.json", "public.text" } },
        { DevicePlatform.MacCatalyst, new[] { "public.json", "public.text" } }
    });

    public string DossierTemporaire => FileSystem.CacheDirectory;

    public async Task PartagerAsync(string titre, string cheminFichier)
    {
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = titre,
            File = new ShareFile(cheminFichier, "application/json")
        });
    }

    public async Task<(string Nom, Stream Contenu)?> ChoisirFichierJsonAsync(string titre)
    {
        var fichier = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = titre,
            FileTypes = TypesJson
        });

        if (fichier is null)
        {
            return null;
        }

        return (fichier.FileName, await fichier.OpenReadAsync());
    }
}
