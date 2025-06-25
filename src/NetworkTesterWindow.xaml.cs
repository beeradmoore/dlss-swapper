using Microsoft.UI.Xaml;

namespace DLSS_Swapper;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class NetworkTesterWindow : Window
{
    public NetworkTesterWindowModel ViewModel { get; private set; }

    public NetworkTesterWindow()
    {
        ViewModel = new NetworkTesterWindowModel(this);
        this.InitializeComponent();
        Closed += OnCurrentWindowClosed;
    }

    private void OnCurrentWindowClosed(object sender, WindowEventArgs args)
    {
        ViewModel.TranslationProperties.Dispose();
    }
}
