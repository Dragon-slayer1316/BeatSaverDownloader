using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using CustomUI.Utilities;
using CustomUI.MenuButton;
using CustomUI.Settings;
using CustomUI.BeatSaber;
using BeatSaverDownloader.UI.ViewControllers;
namespace BeatSaverDownloader.UI
{
    public class PluginUI : MonoBehaviour
    {
        public static PluginUI Instance;
        private MenuButton _moreSongsButton;
        public MoreSongsFlowCoordinator moreSongsFlowCoordinator;
        internal static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("DownloaderPluginUI").AddComponent<PluginUI>();
        }
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BSEvents.menuSceneLoadedFresh += HandleMenuSceneLoadedFresh;
            HandleMenuSceneLoadedFresh();

        }

        private void HandleMenuSceneLoadedFresh()
        {

            _moreSongsButton = MenuButtonUI.AddButton("More songs", "Download more songs from BeatSaver.com!", BeatSaverButtonPressed);
            _moreSongsButton.interactable = SongCore.Loader.AreSongsLoaded;

            if (!SongCore.Loader.AreSongsLoaded)
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            else
                SongLoader_SongsLoadedEvent(null, SongCore.Loader.CustomLevels);
        }

        public void BeatSaverButtonPressed()
        {
            if (moreSongsFlowCoordinator == null)
                moreSongsFlowCoordinator = new GameObject("MoreSongsFlowCoordinator").AddComponent<MoreSongsFlowCoordinator>();

            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

            mainFlow.InvokeMethod("PresentFlowCoordinator", moreSongsFlowCoordinator, null, false, false);
        }

        private void SongLoader_SongsLoadedEvent(SongCore.Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            SongCore.Loader.SongsLoadedEvent -= SongLoader_SongsLoadedEvent;
            _moreSongsButton.interactable = true;
        }
    }

}
