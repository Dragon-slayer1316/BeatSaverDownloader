using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IPA.Config;

namespace BeatSaverDownloader.Misc
{

    public class PluginConfig
    {

        static private BS_Utils.Utilities.Config config = new BS_Utils.Utilities.Config("BeatSaverDownloader");

        public static string beatsaverURL = "https://beatsaver.com";

        public static string scoresaberURL = "https://scoresaber.com";

        public static int maxSimultaneousDownloads = 3;


        public static void LoadConfig()
        {

            maxSimultaneousDownloads = config.GetInt("BeatSaverDownloader", "maxSimultaneousDownloads", 3, true);
            beatsaverURL = config.GetString("BeatSaverDownloader", "beatsaverURL", "https://beatsaver.com");
            scoresaberURL = config.GetString("BeatSaverDownloader", "scoresaberURL", "https://scoresaber.com");
        }

        public static void SaveConfig()
        {

            config.SetString("BeatSaverDownloader", "beatsaverURL", beatsaverURL);
            config.SetString("BeatSaverDownloader", "scoresaberURL", scoresaberURL);
            config.SetInt("BeatSaverDownloader", "maxSimultaneousDownloads", maxSimultaneousDownloads);

        }
    }
}