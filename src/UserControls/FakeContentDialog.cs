using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DLSS_Swapper.UserControls;

public class FakeContentDialog : Control
{
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content),
        typeof(object),
        typeof(FakeContentDialog),
        new PropertyMetadata(null));

    public object Content
    {
        get => (object)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }


    public static readonly DependencyProperty ContentTemplateProperty = DependencyProperty.Register(
       nameof(ContentTemplate),
       typeof(DataTemplate),
       typeof(FakeContentDialog),
       new PropertyMetadata(null));

    public DataTemplate ContentTemplate
    {
        get => (DataTemplate)GetValue(ContentTemplateProperty);
        set => SetValue(ContentTemplateProperty, value);
    }


    public static readonly DependencyProperty CloseButtonCommandParameterProperty = DependencyProperty.Register(
       nameof(CloseButtonCommandParameter),
       typeof(object),
       typeof(FakeContentDialog),
       new PropertyMetadata(null));

    public object CloseButtonCommandParameter
    {
        get => (object)GetValue(CloseButtonCommandParameterProperty);
        set => SetValue(CloseButtonCommandParameterProperty, value);
    }


    public static readonly DependencyProperty CloseButtonCommandProperty = DependencyProperty.Register(
               nameof(CloseButtonCommand),
               typeof(ICommand),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public ICommand CloseButtonCommand
    {
        get => (ICommand)GetValue(CloseButtonCommandProperty);
        set => SetValue(CloseButtonCommandProperty, value);
    }


    public static readonly DependencyProperty CloseButtonStyleProperty = DependencyProperty.Register(
               nameof(CloseButtonStyle),
               typeof(Style),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public Style CloseButtonStyle
    {
        get => (Style)GetValue(CloseButtonStyleProperty);
        set => SetValue(CloseButtonStyleProperty, value);
    }


    public static readonly DependencyProperty CloseButtonTextProperty = DependencyProperty.Register(
               nameof(CloseButtonText),
               typeof(string),
               typeof(FakeContentDialog),
               new PropertyMetadata(string.Empty));

    public string CloseButtonText
    {
        get => (string)GetValue(CloseButtonTextProperty);
        set => SetValue(CloseButtonTextProperty, value);
    }


    public static readonly DependencyProperty DefaultButtonProperty = DependencyProperty.Register(
               nameof(DefaultButton),
               typeof(ContentDialogButton),
               typeof(FakeContentDialog),
               new PropertyMetadata(ContentDialogButton.None));

    public ContentDialogButton DefaultButton
    {
        get => (ContentDialogButton)GetValue(DefaultButtonProperty);
        set => SetValue(DefaultButtonProperty, value);
    }


    public static readonly DependencyProperty FullSizeDesiredProperty = DependencyProperty.Register(
               nameof(FullSizeDesired),
               typeof(bool),
               typeof(FakeContentDialog),
               new PropertyMetadata(false));

    public bool FullSizeDesired
    {
        get => (bool)GetValue(FullSizeDesiredProperty);
        set => SetValue(FullSizeDesiredProperty, value);
    }


    public static readonly DependencyProperty IsPrimaryButtonEnabledProperty = DependencyProperty.Register(
               nameof(IsPrimaryButtonEnabled),
               typeof(bool),
               typeof(FakeContentDialog),
               new PropertyMetadata(true));

    public bool IsPrimaryButtonEnabled
    {
        get => (bool)GetValue(IsPrimaryButtonEnabledProperty);
        set => SetValue(IsPrimaryButtonEnabledProperty, value);
    }


    public static readonly DependencyProperty IsSecondaryButtonEnabledProperty = DependencyProperty.Register(
               nameof(IsSecondaryButtonEnabled),
               typeof(bool),
               typeof(FakeContentDialog),
               new PropertyMetadata(true));

    public bool IsSecondaryButtonEnabled
    {
        get => (bool)GetValue(IsSecondaryButtonEnabledProperty);
        set => SetValue(IsSecondaryButtonEnabledProperty, value);
    }


    public static readonly DependencyProperty PrimaryButtonCommandParameterProperty = DependencyProperty.Register(
               nameof(PrimaryButtonCommandParameter),
               typeof(object),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public object PrimaryButtonCommandParameter
    {
        get => (object)GetValue(PrimaryButtonCommandParameterProperty);
        set => SetValue(PrimaryButtonCommandParameterProperty, value);
    }


    public static readonly DependencyProperty PrimaryButtonCommandProperty = DependencyProperty.Register(
               nameof(PrimaryButtonCommand),
               typeof(ICommand),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public ICommand PrimaryButtonCommand
    {
        get => (ICommand)GetValue(PrimaryButtonCommandProperty);
        set => SetValue(PrimaryButtonCommandProperty, value);
    }


    public static readonly DependencyProperty PrimaryButtonStyleProperty = DependencyProperty.Register(
               nameof(PrimaryButtonStyle),
               typeof(Style),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public Style PrimaryButtonStyle
    {
        get => (Style)GetValue(PrimaryButtonStyleProperty);
        set => SetValue(PrimaryButtonStyleProperty, value);
    }


    public static readonly DependencyProperty PrimaryButtonTextProperty = DependencyProperty.Register(
               nameof(PrimaryButtonText),
               typeof(string),
               typeof(FakeContentDialog),
               new PropertyMetadata(string.Empty));

    public string PrimaryButtonText
    {
        get => (string)GetValue(PrimaryButtonTextProperty);
        set => SetValue(PrimaryButtonTextProperty, value);
    }


    public static readonly DependencyProperty SecondaryButtonCommandParameterProperty = DependencyProperty.Register(
               nameof(SecondaryButtonCommandParameter),
               typeof(object),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public object SecondaryButtonCommandParameter
    {
        get => (object)GetValue(SecondaryButtonCommandParameterProperty);
        set => SetValue(SecondaryButtonCommandParameterProperty, value);
    }


    public static readonly DependencyProperty SecondaryButtonCommandProperty = DependencyProperty.Register(
               nameof(SecondaryButtonCommand),
               typeof(ICommand),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public ICommand SecondaryButtonCommand
    {
        get => (ICommand)GetValue(SecondaryButtonCommandProperty);
        set => SetValue(SecondaryButtonCommandProperty, value);
    }


    public static readonly DependencyProperty SecondaryButtonStyleProperty = DependencyProperty.Register(
               nameof(SecondaryButtonStyle),
               typeof(Style),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public Style SecondaryButtonStyle
    {
        get => (Style)GetValue(SecondaryButtonStyleProperty);
        set => SetValue(SecondaryButtonStyleProperty, value);
    }


    public static readonly DependencyProperty SecondaryButtonTextProperty = DependencyProperty.Register(
               nameof(SecondaryButtonText),
               typeof(string),
               typeof(FakeContentDialog),
               new PropertyMetadata(string.Empty));

    public string SecondaryButtonText
    {
        get => (string)GetValue(SecondaryButtonTextProperty);
        set => SetValue(SecondaryButtonTextProperty, value);
    }


    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
               nameof(Title),
               typeof(object),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public object Title
    {
        get => (object)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public static readonly DependencyProperty TitleTemplateProperty = DependencyProperty.Register(
               nameof(TitleTemplate),
               typeof(DataTemplate),
               typeof(FakeContentDialog),
               new PropertyMetadata(null));

    public DataTemplate TitleTemplate
    {
        get => (DataTemplate)GetValue(TitleTemplateProperty);
        set => SetValue(TitleTemplateProperty, value);
    }



    TaskCompletionSource<ContentDialogResult> taskCompletionSource = new();

    public FakeContentDialog()
    {
        DefaultStyleKey = typeof(FakeContentDialog);
        Style = App.Current.Resources["DefaultFakeContentDialogStyle"] as Style;
    }


    public Task<ContentDialogResult> ShowAsync()
    {
        if (((App)App.Current).MainWindow.Content is Grid rootGrid)
        {
            /*
            var mainNavigationView = rootGrid.FindChild<NavigationView>(x => x.Name == "MainNavigationView");
            var appTitleBar = rootGrid.FindChild<Grid>(x => x.Name == "AppTitleBar");
            */

            Grid.SetColumnSpan(this, rootGrid.ColumnDefinitions.Count);
            Grid.SetRowSpan(this, rootGrid.RowDefinitions.Count);
           
            XamlRoot = rootGrid.XamlRoot;
            
            rootGrid.Children.Add(this);
            Focus(FocusState.Programmatic);
        }

        return taskCompletionSource.Task;
    }

    public void Hide()
    {
        HideImplementation(ContentDialogResult.None);
    }

    void HideImplementation(ContentDialogResult contentDialogResult)
    {
        if (VisualStateManager.GoToState(this, "DialogHidden", true) == false)
        {

        }

        if (taskCompletionSource.TrySetResult(contentDialogResult) == false)
        {

        }
    }

    bool hasSetTemplate = false;
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (hasSetTemplate)
        {
            return;
        }
        hasSetTemplate = true;

        /*
        var container = GetTemplateChild("Container") as Border;
        var groups = VisualStateManager.GetVisualStateGroups(container);
        var dialogShowingStates = groups.FirstOrDefault(x => x.Name == "DialogShowingStates");
        var dialogShowing = dialogShowingStates.States.FirstOrDefault(x => x.Name == "DialogShowing");
        */

        var primaryButton = GetTemplateChild("PrimaryButton") as Button;
        var secondaryButton = GetTemplateChild("SecondaryButton") as Button;
        var closeButton = GetTemplateChild("CloseButton") as Button;

        if (primaryButton is not null)
        {
            primaryButton.Command = new RelayCommand(() =>
            {
                HideImplementation(ContentDialogResult.Primary);
                PrimaryButtonCommand?.Execute(PrimaryButtonCommandParameter);
            });
        }

        if (secondaryButton is not null)
        {
            secondaryButton.Command = new RelayCommand(() =>
            {
                HideImplementation(ContentDialogResult.Secondary);
                SecondaryButtonCommand?.Execute(SecondaryButtonCommandParameter);
            });
        }

        if (closeButton is not null)
        {
            closeButton.Command = new RelayCommand(() =>
            {
                HideImplementation(ContentDialogResult.None);
                CloseButtonCommand?.Execute(CloseButtonCommandParameter);
            });
        }

        if (Title is string title && TitleTemplate is null)
        {
            // For some reason this doens't just work. Removing the ContentControl.Template xaml works but I
            // can't replicate it here. Nor can I set a blank ControlTemplate to override it.

            /*
            var titleControl = GetTemplateChild("Title") as ContentControl;
            //titleControl.Template = null
            //titleControl.Template = new ControlTemplate();
            */

            // This is apparently the only way to load a DataTemplate
            /*
            string xaml = @"<DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                    <Grid>
                        <TextBlock Text=""test123"" />
                    </Grid>
                </DataTemplate>";
            var dataTemplate = (DataTemplate)XamlReader.Load(xaml);            
            TitleTemplate = dataTemplate;
            */


            // But this works, just don't try get the Title after displaying the FakeContentDialog I guess 
            var titleTextBlock = new TextBlock() { Text = title };
            Title = titleTextBlock;
        }

        /*
        // Visual states to set.
        */

        /*
        DialogShowingStates
            - DialogHidden
            - DialogShowing
            - DialogShowingWithoutSmokeLayer
        */

        /*
        DialogSizingStates
            - DefaultDialogSizing
            - FullDialogSizing
        */
        if (VisualStateManager.GoToState(this, "DefaultDialogSizing", true) == false)
        {

        }

        /*
        ButtonsVisibilityStates
            - AllVisible
            - NoneVisible
            - PrimaryVisible
            - SecondaryVisible
            - CloseVisible
            - PrimaryAndSecondaryVisible
            - PrimaryAndCloseVisible
            - SecondaryAndCloseVisible
        */
        var hasPrimary = string.IsNullOrEmpty(PrimaryButtonText) == false;
        var hasSecondary = string.IsNullOrEmpty(SecondaryButtonText) == false;
        var hasClose = string.IsNullOrEmpty(CloseButtonText) == false;

        if (hasPrimary && hasSecondary && hasClose)
        {
            if (VisualStateManager.GoToState(this, "AllVisible", true) == false)
            {
            }
        }
        else if (hasPrimary == false && hasSecondary == false && hasClose == false)
        {
            if (VisualStateManager.GoToState(this, "NoneVisible", true) == false)
            {
            }
        }
        else if (hasPrimary && hasSecondary == false && hasClose == false)
        {
            if (VisualStateManager.GoToState(this, "PrimaryVisible", true) == false)
            {
            }
        }
        else if (hasPrimary == false && hasSecondary && hasClose == false)
        {
            if (VisualStateManager.GoToState(this, "SecondaryVisible", true) == false)
            {
            }
        }
        else if (hasPrimary == false && hasSecondary == false && hasClose)
        {
            if (VisualStateManager.GoToState(this, "CloseVisible", true) == false)
            {
            }
        }
        else if (hasPrimary && hasSecondary && hasClose == false)
        {
            if (VisualStateManager.GoToState(this, "PrimaryAndSecondaryVisible", true) == false)
            {
            }
        }
        else if (hasPrimary && hasSecondary == false && hasClose)
        {
            if (VisualStateManager.GoToState(this, "PrimaryAndCloseVisible", true) == false)
            {
            }
        }
        else if (hasPrimary == false && hasSecondary && hasClose)
        {
            if (VisualStateManager.GoToState(this, "SecondaryAndCloseVisible", true) == false)
            {
            }
        }

        /*
        DefaultButtonStates
            - NoDefaultButton
            - PrimaryAsDefaultButton
            - SecondaryAsDefaultButton
            - CloseAsDefaultButton
        */
        var defaultButtonVisualState = DefaultButton switch
        {
            ContentDialogButton.Primary => "PrimaryAsDefaultButton",
            ContentDialogButton.Secondary => "SecondaryAsDefaultButton",
            ContentDialogButton.Close => "CloseAsDefaultButton",
            _ => "NoDefaultButton",
        };
        if (VisualStateManager.GoToState(this, defaultButtonVisualState, true) == false)
        {

        }


        /*
        DialogBorderStates
            - NoBorder
            - AccentColorBorder
        */
        if (VisualStateManager.GoToState(this, "NoBorder", true) == false)
        {

        }

        // Finally show the dialog.
        if (VisualStateManager.GoToState(this, "DialogShowing", true) == false)
        {

        }
    }
}
