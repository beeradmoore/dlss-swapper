using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DLSS_Swapper.Interfaces;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace DLSS_Swapper.UserControls
{
    internal class GameLibrarySelectorControl : UserControl
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
            var grid = new Grid();


            var gameLibraryEnumList = new List<GameLibrary>();
            foreach (GameLibrary gameLibraryEnum in Enum.GetValues(typeof(GameLibrary)))
            {
                gameLibraryEnumList.Add(gameLibraryEnum);
            }

            gameLibraryEnumList.Sort();

            var currentRow = 0;
            foreach (var gameLibraryEnum in gameLibraryEnumList)
            {
                var gameLibrary = IGameLibrary.GetGameLibrary(gameLibraryEnum);
                var toggleSwitch = new ToggleSwitch();
                toggleSwitch.IsOn = gameLibrary.IsEnabled();
                toggleSwitch.OffContent = $"{gameLibrary.Name} disabled";
                toggleSwitch.OnContent = $"{gameLibrary.Name} enabled";
                toggleSwitch.Tag = gameLibraryEnum;
                toggleSwitch.Toggled += (sender, e) =>
                {
                    if (SavesWhenToggled)
                    {
                        Save();
                    }
                };
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

            Settings.EnabledGameLibraries = enabledGameLibraries;
        }
    }
}
