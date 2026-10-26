using Microsoft.Maui.Controls.Xaml;

namespace ForzaTuneLap.App.Traductions;

/// <summary>Utilisation en XAML : Text="{t:Traduire Onglet_Voitures}" avec xmlns:t="clr-namespace:ForzaTuneLap.App.Traductions".</summary>
[ContentProperty(nameof(Cle))]
public sealed class TraduireExtension : IMarkupExtension<string>
{
    public string Cle { get; set; } = string.Empty;

    public string ProvideValue(IServiceProvider serviceProvider) => Texte.Obtenir(Cle);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
