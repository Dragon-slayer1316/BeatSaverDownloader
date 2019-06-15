using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BeatSaverDownloader.Misc;
using CustomUI.BeatSaber;
using BeatSaverDownloader.UI.ViewControllers;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json.Linq;
namespace BeatSaverDownloader.UI
{
    public class MoreSongsUI : MonoBehaviour
    {
        public const int songsPerPage = 6;
        public static MoreSongsUI Instance;
        public CustomMenu moreSongsMenu;
        private MoreSongsListViewController _moreSongsListViewController;

        public int currentPage = 0;
        public string currentSortMode = "hot";
        public string currentSearchRequest = "";
        public int currentScoreSaberSortMode = 0;
        public bool scoreSaber = false;

        private List<Song> currentPageSongs = new List<Song>();
        private Song _lastSelectedSong;
        private Song _lastDeletedSong;
        internal static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("DownloaderMoreSongsUI").AddComponent<MoreSongsUI>();
        }
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PrepareMenu();

        }

        internal void InitSongList()
        {
            currentPage = 0;
            currentSortMode = "top";
            currentSearchRequest = "";
            StartCoroutine(GetPageScoreSaber(0, 0));
        }
        private void PrepareMenu()
        {
            if (moreSongsMenu == null)
            {
                moreSongsMenu = BeatSaberUI.CreateCustomMenu<CustomMenu>("More Songs");
                _moreSongsListViewController = BeatSaberUI.CreateViewController<MoreSongsListViewController>();
                _moreSongsListViewController.backButtonPressed += delegate () { moreSongsMenu.Dismiss(); };
                moreSongsMenu.SetMainViewController(_moreSongsListViewController, true);

                _moreSongsListViewController.pageDownPressed += _moreSongsListViewController_pageDownPressed;
                _moreSongsListViewController.pageUpPressed += _moreSongsListViewController_pageUpPressed;

                _moreSongsListViewController.SortByTop += () => { currentSortMode = "hot"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };
                _moreSongsListViewController.SortByNew += () => { currentSortMode = "latest"; currentPage = 0; StartCoroutine(GetPage(currentPage, currentSortMode)); currentSearchRequest = ""; };

                _moreSongsListViewController.SortByNewlyRanked += () => { currentScoreSaberSortMode = 1; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.SortByTrending += () => { currentScoreSaberSortMode = 0; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };
                _moreSongsListViewController.SortByDifficulty += () => { currentScoreSaberSortMode = 3; currentPage = 0; StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode)); };

                //     moreSongsList.SearchButtonPressed += _moreSongsListViewController_searchButtonPressed;
                _moreSongsListViewController.didSelectRow += _moreSongsListViewController_didSelectRow;

            }
        }
        private void _moreSongsListViewController_didSelectRow(int row)
        {
            //         if (!_songDetailViewController.isInViewControllerHierarchy)
            //         {
            //             PushViewControllerToNavigationController(_moreSongsNavigationController, _songDetailViewController);
            //         }

            if (!scoreSaber)
            {
                //            _songDetailViewController.SetContent(this, currentPageSongs[row]);
                //            _descriptionViewController.SetDescription(currentPageSongs[row].description);
                            _lastSelectedSong = currentPageSongs[row];
            }
            else
            {
                StartCoroutine(DidSelectRow(row));
            }

        }


        public IEnumerator DidSelectRow(int row)
        {

            yield return null;
            //          _songDetailViewController.SetLoadingState(true);
            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/by-hash/{currentPageSongs[row].hash}");
            www.timeout = 15;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {

                    Newtonsoft.Json.Linq.JObject jNode = JObject.Parse(www.downloadHandler.text);
                    currentPageSongs[row] = new Song((JObject)jNode, false);
                    //                _songDetailViewController.SetContent(this, currentPageSongs[row]);
                    //                _descriptionViewController.SetDescription(currentPageSongs[row].description);
                    _lastSelectedSong = currentPageSongs[row];
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            //     _songDetailViewController.SetLoadingState(false);
        }

        public IEnumerator GetPageScoreSaber(int page, int cat)
        {
            Plugin.log.Info("GetPageScoreSaber " + page);
            yield return null;
            scoreSaber = true;
            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);

            string url = $"{PluginConfig.scoresaberURL}/api.php?function=get-leaderboards&cat={cat}&limit=6&page={(page + 1)}&unique=1";
            if (cat == 3) { url = url + "&ranked=1"; }
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.scoresaberURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JObject jNode = JObject.Parse(www.downloadHandler.text);
                    currentPageSongs.Clear();
                    for (int i = 0; i < Math.Min(jNode["songs"].Children().Count(), songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song((JObject)jNode["songs"][i], true));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }

        public IEnumerator GetPage(int page, string sortBy)
        {
            Plugin.log.Info("GetPage " + page + sortBy);
            yield return null;
            scoreSaber = false;
            _moreSongsListViewController.SetLoadingState(true);
            _moreSongsListViewController.TogglePageUpDownButtons((page > 0), true);
            _moreSongsListViewController.SetContent(null);

            UnityWebRequest www = UnityWebRequest.Get($"{PluginConfig.beatsaverURL}/api/maps/{sortBy}/{(page)}");
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JObject jNode = JObject.Parse(www.downloadHandler.text);

                    currentPageSongs.Clear();

                    for (int i = 0; i < Math.Min(jNode["docs"].Children().Count(), songsPerPage); i++)
                    {
                        currentPageSongs.Add(new Song((JObject)jNode["docs"][i], false));
                    }

                    _moreSongsListViewController.SetContent(currentPageSongs);
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
            _moreSongsListViewController.SetLoadingState(false);
        }


        private void _moreSongsListViewController_pageDownPressed()
        {
            currentPage++;
            if (string.IsNullOrEmpty(currentSearchRequest))
                if (!scoreSaber)
                {
                    StartCoroutine(GetPage(currentPage, currentSortMode));
                }
                else
                {
                    StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode));
                }
  //          else
  //              StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));
            _moreSongsListViewController.TogglePageUpDownButtons(true, true);
        }

        private void _moreSongsListViewController_pageUpPressed()
        {
            if (currentPage > 0)
            {
                currentPage--;
                if (string.IsNullOrEmpty(currentSearchRequest))
                    if (!scoreSaber)
                    {
                        StartCoroutine(GetPage(currentPage, currentSortMode));
                    }
                    else
                    {
                        StartCoroutine(GetPageScoreSaber(currentPage, currentScoreSaberSortMode));
                    }
         //       else
         //           StartCoroutine(GetSearchResults(currentPage, currentSearchRequest));

                if (currentPage == 0)
                {
                    _moreSongsListViewController.TogglePageUpDownButtons(false, true);
                }
                else
                {
                    _moreSongsListViewController.TogglePageUpDownButtons(true, true);
                }
            }
        }


    }
}
