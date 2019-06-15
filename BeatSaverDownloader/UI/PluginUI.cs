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
            MoreSongsUI.OnLoad();
            MenuButtonUI.AddButton("More Songs", delegate () { MoreSongsUI.Instance.moreSongsMenu.Present(); MoreSongsUI.Instance.InitSongList();  });
        }



    }

}
