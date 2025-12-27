using Microsoft.UI.Xaml;


namespace DLSS_Swapper;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FailToLaunchWindow : Window
{
    public FailToLaunchWindowModel ViewModel { get; private set; }

    public FailToLaunchWindow()
    {
        this.InitializeComponent();
        ViewModel = new FailToLaunchWindowModel();
        Closed += OnCurrentWindowClosed;
    }

    private void OnCurrentWindowClosed(object sender, WindowEventArgs args)
    {
        ViewModel.TranslationProperties.Dispose();
    }
}
