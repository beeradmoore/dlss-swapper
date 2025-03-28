using System;
using System.Collections.Generic;
using System.Linq;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DLSS_Swapper.UserControls
{
    internal class GameLibrarySelectorControl : UserControl, IDisposable
    {
        public static readonly DependencyProperty SavesWhenToggledProperty = DependencyProperty.Register(
            nameof(SavesWhenToggled),
            typeof(bool),
            typeof(GameLibrarySelectorControl),
            new PropertyMetadata(false));

        public bool SavesWhenToggled
        {
            get { return (bool)GetValue(SavesWhenToggledProperty); }
            set { SetValue(SavesWhenToggledProperty, value); }
        }

        public GameLibrarySelectorControl()
        {
            _languageManager = LanguageManager.Instance;
            _languageManager.OnLanguageChanged += OnLanguageChanged;
            _toggleSwitchHandlers = new Dictionary<ToggleSwitch, string>();

            var grid = new Grid();
            List<GameLibrary> gameLibraryEnumList = Enum.GetValues<GameLibrary>().OrderBy(x => x).ToList();

            var currentRow = 0;
            foreach (var gameLibraryEnum in gameLibraryEnumList)
            {
                IGameLibrary gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                ToggleSwitch toggleSwitch = new ToggleSwitch();
                toggleSwitch.IsOn = gameLibrary.IsEnabled;
                toggleSwitch.OffContent = $"{gameLibrary.Name} {Disabled}";
                toggleSwitch.OnContent = $"{gameLibrary.Name} {Enabled}";
                toggleSwitch.Tag = gameLibraryEnum;
                toggleSwitch.Toggled += (sender, e) =>
                {
                    if (SavesWhenToggled)
                    {
                        Save();
                    }
                };
                _toggleSwitchHandlers.Add(toggleSwitch, gameLibrary.Name);
                Grid.SetRow(toggleSwitch, currentRow);
                grid.Children.Add(toggleSwitch);
                grid.RowDefinitions.Add(new RowDefinition());

                ++currentRow;
            }

            Content = grid;
        }

        internal void Save()
        {
            uint enabledGameLibraries = 0;

            // Loop over each toggle switch and if it is enabled use the value stored in its Tag property to toggle the enum flag of enabledGameLibraries
            if (Content is Grid grid)
            {
                foreach (ToggleSwitch toggleSwitch in grid.Children)
                {
                    if (toggleSwitch.IsOn)
                    {
                        enabledGameLibraries |= (uint)toggleSwitch.Tag;
                    }
                }
            }

            Settings.Instance.EnabledGameLibraries = enabledGameLibraries;
        }

        public string Disabled => ResourceHelper.GetString("Disabled");
        public string Enabled => ResourceHelper.GetString("Enabled");

        public void OnLanguageChanged()
        {
            foreach (KeyValuePair<ToggleSwitch, string> kvp in _toggleSwitchHandlers)
            {
                kvp.Key.OffContent = $"{kvp.Value} {Disabled}";
                kvp.Key.OnContent = $"{kvp.Value} {Enabled}";
            }
        }

        public void Dispose()
        {
            _languageManager.OnLanguageChanged -= OnLanguageChanged;
        }

        ~GameLibrarySelectorControl()
        {
            Dispose();
        }

        private readonly Dictionary<ToggleSwitch, string> _toggleSwitchHandlers;
        private readonly LanguageManager _languageManager;
    }
}
