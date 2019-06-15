using System;
using UnityEngine.SceneManagement;
using BeatSaverDownloader.Misc;
using System.Collections.Generic;
using UnityEngine;
using BS_Utils.Gameplay;
using IPA;
using BeatSaverDownloader.UI;
namespace BeatSaverDownloader
{
    public class Plugin : IBeatSaberPlugin
    {
        public static Plugin instance;
        public static IPA.Logging.Logger log;
        
        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            log = logger;
        }

        public void OnApplicationQuit()
        {
            PluginConfig.SaveConfig();
        }
        
        public void OnApplicationStart()
        {
            instance = this;
            PluginConfig.LoadConfig();
            Sprites.ConvertToSprites();
            CustomUI.Utilities.BSEvents.menuSceneLoadedFresh += BSEvents_menuSceneLoadedFresh;
        }

        private void BSEvents_menuSceneLoadedFresh()
        {
            try
            {
                PluginUI.OnLoad();
                GetUserInfo.GetUserName();
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception on fresh menu scene change: " + e);
            }
        }

        private void OnMenuSceneLoadedFresh()
        {

        }

        

        public void OnUpdate()
        {

        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if(scene.name == "MenuCore")
            {

            }
        }

        public void OnSceneUnloaded(Scene scene)
        {
        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {
        }

        public void OnFixedUpdate()
        {
        }
    }
}
