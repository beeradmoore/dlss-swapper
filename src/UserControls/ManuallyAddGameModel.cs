using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DLSS_Swapper.Helpers;
using DLSS_Swapper.Data.ManuallyAdded;
using System.Collections.Generic;
using DLSS_Swapper.Attributes;

namespace DLSS_Swapper.UserControls
{
    internal partial class ManuallyAddGameModel : ObservableObject, IDisposable
    {
        public ManuallyAddGameModel(ManuallyAddGameControl manuallyAddGameControl, string installPath)
        {
            _languageManager = LanguageManager.Instance;
            _languageManager.OnLanguageChanged += OnLanguageChanged;
            manuallyAddGameControlWeakReference = new WeakReference<ManuallyAddGameControl>(manuallyAddGameControl);

            game = new ManuallyAddedGame(Guid.NewGuid().ToString("D"))
            {
                Title = Path.GetFileName(installPath),
                InstallPath = PathHelpers.NormalizePath(installPath),
            };
        }

        ManuallyAddedGame game;
        public ManuallyAddedGame Game => game;

        WeakReference<ManuallyAddGameControl> manuallyAddGameControlWeakReference;

        [RelayCommand]
        async Task AddCoverImageAsync()
        {
            if (game.CoverImage == game.ExpectedCustomCoverImage)
            {
                await game.PromptToRemoveCustomCover();
                return;
            }

            await game.PromptToBrowseCustomCover();
        }

        #region LanguageProperties
        [LanguageProperty] public string AddCoverText => ResourceHelper.GetString("AddCover");
        [LanguageProperty] public string OptionalText => ResourceHelper.GetString("Optional");
        [LanguageProperty] public string NameText => ResourceHelper.GetString("Name");
        [LanguageProperty] public string InstallPathText => ResourceHelper.GetString("InstallPath");
        #endregion

        private void OnLanguageChanged()
        {
            Type currentClassType = GetType();
            IEnumerable<string> languageProperties = LanguageManager.GetClassLanguagePropertyNames(currentClassType);
            foreach (string propertyName in languageProperties)
            {
                OnPropertyChanged(propertyName);
            }
        }

        public void Dispose()
        {
            _languageManager.OnLanguageChanged -= OnLanguageChanged;
        }

        ~ManuallyAddGameModel()
        {
            Dispose();
        }

        private readonly LanguageManager _languageManager;
    }
}
