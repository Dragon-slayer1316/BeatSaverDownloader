
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
namespace BeatSaverDownloader.Misc
{
    public class SongDownloader : MonoBehaviour
    {
        public event Action<Song> songDownloaded;

        private static SongDownloader _instance = null;
        public static SongDownloader Instance
        {
            get
            {
                if (!_instance)
                    _instance = new GameObject("SongDownloader").AddComponent<SongDownloader>();
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        private List<Song> _alreadyDownloadedSongs;
        private static bool _extractingZip;

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);

            if (!SongCore.Loader.AreSongsLoaded)
            {
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }
            else
            {
                SongLoader_SongsLoadedEvent(null, SongCore.Loader.CustomLevels);
            }

        }
        //bananbread song id

        private void SongLoader_SongsLoadedEvent(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            _alreadyDownloadedSongs = levels.Values.Select(x => new Song(x)).ToList();
        }

        public IEnumerator DownloadSongCoroutine(Song songInfo)
        {
            songInfo.songQueueState = SongQueueState.Downloading;

            if (IsSongDownloaded(songInfo))
            {
                //        Plugin.log.Info("Song Already Downloaded, Skipping");
                songInfo.downloadingProgress = 1f;
                yield return new WaitForSeconds(0.1f);
                songInfo.songQueueState = SongQueueState.Downloaded;
                songDownloaded?.Invoke(songInfo);
                yield break;
            }
            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                www = UnityWebRequest.Get(songInfo.downloadURL);

                asyncRequest = www.SendWebRequest();
            }
            catch (Exception e)
            {
                Plugin.log.Error(e);
                songInfo.songQueueState = SongQueueState.Error;
                songInfo.downloadingProgress = 1f;

                yield break;
            }

            while ((!asyncRequest.isDone || songInfo.downloadingProgress < 1f) && songInfo.songQueueState != SongQueueState.Error)
            {
                yield return null;

                time += Time.deltaTime;

                if (time >= 5f && asyncRequest.progress <= float.Epsilon)
                {
                    www.Abort();
                    timeout = true;
                    Plugin.log.Error("Connection timed out!");
                }

                songInfo.downloadingProgress = asyncRequest.progress;
            }

            if (songInfo.songQueueState == SongQueueState.Error && (!asyncRequest.isDone || songInfo.downloadingProgress < 1f))
                www.Abort();

            if (www.isNetworkError || www.isHttpError || timeout || songInfo.songQueueState == SongQueueState.Error)
            {
                songInfo.songQueueState = SongQueueState.Error;
                Plugin.log.Error("Unable to download song! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                Plugin.log.Info("Received response from BeatSaver.com...");
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                Stream zipStream = null;

                try
                {
                    customSongsPath = CustomLevelPathHelper.customLevelsDirectoryPath;
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    zipStream = new MemoryStream(data);
                    Plugin.log.Info("Downloaded zip!");
                }
                catch (Exception e)
                {
                    Plugin.log.Critical(e);
                    songInfo.songQueueState = SongQueueState.Error;
                    yield break;
                }

                yield return new WaitWhile(() => _extractingZip); //because extracting several songs at once sometimes hangs the game

                Task extract = ExtractZipAsync(songInfo, zipStream, customSongsPath);
                yield return new WaitWhile(() => !extract.IsCompleted);
                songDownloaded?.Invoke(songInfo);
            }
        }

        private async Task ExtractZipAsync(Song songInfo, Stream zipStream, string customSongsPath)
        {
            try
            {
                Plugin.log.Info("Extracting...");
                _extractingZip = true;
                ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                string basePath = songInfo.key + " (" + songInfo.songName + " - " + songInfo.levelAuthorName + ")";
                basePath = string.Join("", basePath.Split((Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray())));
                string path = customSongsPath + "/" + basePath;

                if (Directory.Exists(path))
                {
                    int pathNum = 1;
                    while (Directory.Exists(path + $" ({pathNum})")) ++pathNum;
                    path += $" ({pathNum})";
                }
                Plugin.log.Info(path);
                await Task.Run(() => archive.ExtractToDirectory(path)).ConfigureAwait(false);
                archive.Dispose();
                songInfo.path = path;
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to extract ZIP! Exception: {e}");
                songInfo.songQueueState = SongQueueState.Error;
                _extractingZip = false;
                return;
            }
            zipStream.Close();

            //Correct subfolder
            /*
            try
            {
                string path = Directory.GetDirectories(customSongsPath).FirstOrDefault();
                SongCore.Utilities.Utils.GrantAccess(path);
                DirectoryInfo subfolder = new DirectoryInfo(path).GetDirectories().FirstOrDefault();
                if(subfolder != null)
                {
                    Console.WriteLine(path);
                    Console.WriteLine(subfolder.FullName);
                    string newPath = CustomLevelPathHelper.customLevelsDirectoryPath + "/" + songInfo.id + " " + subfolder.Name;
                    if (Directory.Exists(newPath))
                    {
                        int pathNum = 1;
                        while (Directory.Exists(newPath + $" ({pathNum})")) ++pathNum;
                        newPath = newPath + $" ({pathNum})";
                    }
                    Console.WriteLine(newPath);
                    Directory.Move(subfolder.FullName, newPath);
                    if (SongCore.Utilities.Utils.IsDirectoryEmpty(path))
                    {
                        Directory.Delete(path);
                    }
                    songInfo.path = newPath;
                }
                else
                    Console.WriteLine("subfoldern null");
            }
            catch(Exception ex)
            {
                Plugin.log.Error($"Unable to prepare Extracted Zip! \n {ex}");
            }
            */
            if (string.IsNullOrEmpty(songInfo.path))
            {
                songInfo.path = customSongsPath;
            }

            _extractingZip = false;
            songInfo.songQueueState = SongQueueState.Downloaded;
            _alreadyDownloadedSongs.Add(songInfo);
            Plugin.log.Info($"Extracted {songInfo.songName} {songInfo.songSubName}!");

            //       HMMainThreadDispatcher.instance.Enqueue(() => {
            //           try
            //          {

            //          string dirName = new DirectoryInfo(customSongsPath).Name;

            //             SongCore.Loader.SongsLoadedEvent -= Plugin.instance.SongCore_SongsLoadedEvent;
            //             Action<SongCore.Loader, Dictionary<string, CustomPreviewBeatmapLevel>> songsLoadedAction = null;
            //              songsLoadedAction = (arg1, arg2) =>
            //              {
            //                  SongCore.Loader.SongsLoadedEvent -= songsLoadedAction;
            //                  SongCore.Loader.SongsLoadedEvent += Plugin.instance.SongCore_SongsLoadedEvent;
            //              };
            //              SongCore.Loader.SongsLoadedEvent += songsLoadedAction;


            //          }
            //          catch (Exception e)
            //          {
            //              Plugin.log.Critical("Unable to load song! Exception: " + e);
            //          }
            //      });

        }

        public void DeleteSong(Song song)
        {
            try
            {

            SongCore.Loader.Instance.DeleteSong(song.path);
            Plugin.log.Info($"{_alreadyDownloadedSongs.RemoveAll(x => x.Compare(song))} song removed");

            }
            catch(Exception Ex)
            {
                Plugin.log.Error("Failed To Delete Song: " + song.path + "\n" + Ex);
            }


        }


        public bool IsSongDownloaded(Song song)
        {
            if (SongCore.Loader.AreSongsLoaded)
            {
                return _alreadyDownloadedSongs.Any(x => x.Compare(song));
            }
            else
                return false;
        }







    }
}
