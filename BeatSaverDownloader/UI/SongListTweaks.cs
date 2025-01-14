﻿using CustomUI.BeatSaber;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BeatSaverDownloader.UI.ViewControllers;
using VRUI;
using System.IO;
using BeatSaverDownloader.Misc;
using HMUI;
using BeatSaverDownloader.UI.FlowCoordinators;
using TMPro;
using Harmony;
using System.Reflection;
using CustomUI.Utilities;
using UnityEngine.Networking;
using SimpleJSON;
using SongCore.OverrideClasses;
using Newtonsoft.Json.Linq;
namespace BeatSaverDownloader.UI
{
    public enum SortMode { Default, Difficulty, Newest, Author };

    public class SongListTweaks : MonoBehaviour
    {

        public bool initialized = false;

        public static SortMode lastSortMode
        {
            get
            {
                return _lastSortMode;
            }
            set
            {
                _lastSortMode = value;
                PluginConfig.lastSelectedSortMode = _lastSortMode;
                PluginConfig.SaveConfig();
            }
        }
        private static SortMode _lastSortMode;

        public static IBeatmapLevelPack lastPack
        {
            get
            {
                return _lastPack;
            }
            set
            {
                _lastPack = value;
                if (_lastPack != null)
                {
                    Plugin.log.Info($"Selected pack: {_lastPack.packName}");
                    PluginConfig.lastSelectedPack = _lastPack.packID;
                    PluginConfig.SaveConfig();
                }
            }
        }
        private static IBeatmapLevelPack _lastPack;

        private static SongListTweaks _instance = null;
        public static SongListTweaks Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = new GameObject("SongListTweaks").AddComponent<SongListTweaks>();
                    DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
            private set
            {
                _instance = value;
            }
        }

        public FlowCoordinator freePlayFlowCoordinator;

        private MainFlowCoordinator _mainFlowCoordinator;
        internal LevelPackLevelsViewController _levelListViewController;
        internal StandardLevelDetailViewController _detailViewController;
        internal LevelPacksViewController _levelPacksViewController;
        internal LevelPackDetailViewController _levelPackDetailViewController;
        private SearchKeyboardViewController _searchViewController;
        private SimpleDialogPromptViewController _simpleDialog;

        private Button _buyPackButton;
        private TextMeshProUGUI _buyPackText;
        private Button.ButtonClickedEvent _originalBuyButtonEvent;
        private Button.ButtonClickedEvent _downloadButtonEvent;
        private bool _downloadingPlaylist;

        private DownloadQueueViewController _downloadQueueViewController;

        private Button _fastPageUpButton;
        private Button _fastPageDownButton;

        private Button _randomButton;
        private Button _searchButton;
        private Button _sortByButton;

        private Button _defButton;
        private Button _newButton;
        private Button _difficultyButton;

        private Button _favoriteButton;
        private Button _deleteButton;
        private Button _authorButton;
        //    private TextMeshProUGUI _starStatText;
        //    private TextMeshProUGUI _upvoteStatText;
        //    private TextMeshProUGUI _downvoteStatText;

        public void OnLoad()
        {
            initialized = false;
            SetupTweaks();

            if (PluginConfig.disableSongListTweaks)
                return;

            if (SongCore.Loader.AreSongsLoaded)
            {
                AddDefaultPlaylists();
            }
            else
            {
                SongCore.Loader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;
            }

        }

        private void SongLoader_SongsLoadedEvent(SongCore.Loader arg1, Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            SongCore.Loader.SongsLoadedEvent -= SongLoader_SongsLoadedEvent;
            AddDefaultPlaylists();
        }

        private void SetupTweaks()
        {
            _mainFlowCoordinator = FindObjectOfType<MainFlowCoordinator>();
            _mainFlowCoordinator.GetPrivateField<MainMenuViewController>("_mainMenuViewController").didFinishEvent += MainMenuViewController_didFinishEvent;

            RectTransform viewControllersContainer = FindObjectsOfType<RectTransform>().First(x => x.name == "ViewControllers");

            if (initialized) return;

            Plugin.log.Info("Setting up song list tweaks...");

            try
            {
                var harmony = HarmonyInstance.Create("BeatSaverDownloaderHarmonyInstance");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                Plugin.log.Info("Unable to patch level list! Exception: " + e);
            }


            _levelListViewController = viewControllersContainer.GetComponentInChildren<LevelPackLevelsViewController>(true);
            _levelPacksViewController = viewControllersContainer.GetComponentInChildren<LevelPacksViewController>(true);
            _levelPackDetailViewController = viewControllersContainer.GetComponentInChildren<LevelPackDetailViewController>(true);

            _detailViewController = viewControllersContainer.GetComponentsInChildren<StandardLevelDetailViewController>(true).First(x => x.name == "LevelDetailViewController");

            _simpleDialog = ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(_mainFlowCoordinator, "_simpleDialogPromptViewController");
            _simpleDialog = Instantiate(_simpleDialog.gameObject, _simpleDialog.transform.parent).GetComponent<SimpleDialogPromptViewController>();


            if (!PluginConfig.disableSongListTweaks)
            {

                _levelListViewController.didSelectLevelEvent += _levelListViewController_didSelectLevelEvent;

                _detailViewController.didChangeDifficultyBeatmapEvent += _difficultyViewController_didSelectDifficultyEvent;

                TableView _songSelectionTableView = _levelListViewController.GetComponentsInChildren<TableView>(true).First();
                RectTransform _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "LevelsTableView");

                _tableViewRectTransform.sizeDelta = new Vector2(0f, -20.5f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

                Button _pageUp = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageUpButton");
                (_pageUp.transform as RectTransform).anchoredPosition = new Vector2(0f, -1.75f);

                Button _pageDown = _tableViewRectTransform.GetComponentsInChildren<Button>(true).First(x => x.name == "PageDownButton");
                (_pageDown.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);

                _fastPageUpButton = Instantiate(_pageUp, _tableViewRectTransform, false);
                (_fastPageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_fastPageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_fastPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, 0.25f);
                (_fastPageUpButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
                _fastPageUpButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
                _fastPageUpButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
                _fastPageUpButton.onClick.AddListener(delegate ()
                {
                    FastScrollUp(_songSelectionTableView, PluginConfig.fastScrollSpeed);
                });

                _fastPageDownButton = Instantiate(_pageDown, _tableViewRectTransform, false);
                (_fastPageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_fastPageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_fastPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, -1f);
                (_fastPageDownButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
                _fastPageDownButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
                _fastPageDownButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Sprites.DoubleArrow;
                _fastPageDownButton.onClick.AddListener(delegate ()
                {
                    FastScrollDown(_songSelectionTableView, PluginConfig.fastScrollSpeed);
                });

                _randomButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), _levelListViewController.rectTransform, false);
                _randomButton.onClick = new Button.ButtonClickedEvent();
                _randomButton.onClick.AddListener(() =>
                {
                    int randomRow = UnityEngine.Random.Range(0, _songSelectionTableView.dataSource.NumberOfCells());
                    _songSelectionTableView.ScrollToCellWithIdx(randomRow, TableView.ScrollPositionType.Beginning, false);
                    _songSelectionTableView.SelectCellWithIdx(randomRow, true);
                });
                _randomButton.name = "CustomUIButton";

                (_randomButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_randomButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_randomButton.transform as RectTransform).anchoredPosition = new Vector2(24f, 36.5f);
                (_randomButton.transform as RectTransform).sizeDelta = new Vector2(12f, 6f);

                _randomButton.SetButtonText("");
                _randomButton.SetButtonIcon(Sprites.RandomIcon);
                _randomButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Stroke").sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRectSmallStroke");

                var _randomIconLayout = _randomButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
                _randomIconLayout.padding = new RectOffset(0, 0, 0, 0);

                _searchButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-18f, 36.5f), new Vector2(24f, 6f), SearchPressed, "Search");
                _searchButton.SetButtonTextSize(3f);
                _searchButton.ToggleWordWrapping(false);

                _sortByButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(6f, 36.5f), new Vector2(24f, 6f), () =>
                {
                    SelectTopButtons(TopButtonsState.SortBy);
                }, "Sort By");
                _sortByButton.SetButtonTextSize(3f);
                _sortByButton.ToggleWordWrapping(false);

                _defButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(-20f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    SelectTopButtons(TopButtonsState.Select);
                    SetLevels(SortMode.Default, "");
                },
                    "Default");

                _defButton.SetButtonTextSize(3f);
                _defButton.ToggleWordWrapping(false);
                _defButton.gameObject.SetActive(false);

                _newButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(0f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    SelectTopButtons(TopButtonsState.Select);
                    SetLevels(SortMode.Newest, "");
                }, "Newest");

                _newButton.SetButtonTextSize(3f);
                _newButton.ToggleWordWrapping(false);
                _newButton.gameObject.SetActive(false);


                //    _difficultyButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(20f, 36.5f), new Vector2(20f, 6f), () =>
                //    {
                //        SelectTopButtons(TopButtonsState.Select);
                //         SetLevels(SortMode.Difficulty, "");
                //     }, "Difficulty");

                //     _difficultyButton.SetButtonTextSize(3f);
                //     _difficultyButton.ToggleWordWrapping(false);
                //     _difficultyButton.gameObject.SetActive(false);

                _authorButton = _levelListViewController.CreateUIButton("CreditsButton", new Vector2(20f, 36.5f), new Vector2(20f, 6f), () =>
                {
                    SelectTopButtons(TopButtonsState.Select);
                    SetLevels(SortMode.Author, "");
                }, "Song Author");

                _authorButton.SetButtonTextSize(3f);
                _authorButton.ToggleWordWrapping(false);
                _authorButton.gameObject.SetActive(false);

                RectTransform buttonsRect = _detailViewController.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "PlayButtons");

                _favoriteButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), buttonsRect, false);
                _favoriteButton.onClick = new Button.ButtonClickedEvent();
                _favoriteButton.onClick.AddListener(() =>
                {
                    if (PluginConfig.favoriteSongs.Any(x => x.Contains(_detailViewController.selectedDifficultyBeatmap.level.levelID.Split('_')[2])))
                    {
                        PluginConfig.favoriteSongs.Remove(_detailViewController.selectedDifficultyBeatmap.level.levelID.Split('_')[2]);
                        PluginConfig.SaveConfig();
                        _favoriteButton.SetButtonIcon(Sprites.AddToFavorites);
                        PlaylistsCollection.RemoveLevelFromPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), _detailViewController.selectedDifficultyBeatmap.level.levelID.Split('_')[2]);
                    }
                    else
                    {
                        PluginConfig.favoriteSongs.Add(_detailViewController.selectedDifficultyBeatmap.level.levelID.Split('_')[2]);
                        PluginConfig.SaveConfig();
                        _favoriteButton.SetButtonIcon(Sprites.RemoveFromFavorites);
                        PlaylistsCollection.AddSongToPlaylist(PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == "Your favorite songs"), new PlaylistSong() { hash = _detailViewController.selectedDifficultyBeatmap.level.levelID.Split('_')[2], songName = _detailViewController.selectedDifficultyBeatmap.level.songName, level = SongDownloader.GetLevel(_detailViewController.selectedDifficultyBeatmap.level.levelID) });
                    }
                });
                _favoriteButton.name = "CustomUIButton";
                _favoriteButton.SetButtonIcon(Sprites.AddToFavorites);
                (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(12f, 8.8f);
                var _favoriteIconLayout = _favoriteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
                _favoriteIconLayout.padding = new RectOffset(3, 3, 0, 0);
                _favoriteButton.transform.SetAsFirstSibling();

                Button practiceButton = buttonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
                (practiceButton.transform as RectTransform).sizeDelta = new Vector2(12f, 8.8f);
                var _practiceIconLayout = practiceButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
                _practiceIconLayout.padding = new RectOffset(3, 3, 0, 0);

                _deleteButton = Instantiate(viewControllersContainer.GetComponentsInChildren<Button>(true).First(x => x.name == "PracticeButton"), buttonsRect, false);
                _deleteButton.onClick = new Button.ButtonClickedEvent();
                _deleteButton.onClick.AddListener(DeletePressed);
                _deleteButton.name = "CustomUIButton";
                _deleteButton.SetButtonIcon(Sprites.DeleteIcon);
                _deleteButton.interactable = !PluginConfig.disableDeleteButton;
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(8.8f, 8.8f);
                _deleteButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Stroke").sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRectSmallStroke");

                var _deleteIconLayout = _deleteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(x => x.name == "Content");
                _deleteIconLayout.padding = new RectOffset(0, 0, 1, 1);

                _deleteButton.transform.SetAsLastSibling();

                //based on https://github.com/halsafar/BeatSaberSongBrowser/blob/master/SongBrowserPlugin/UI/Browser/SongBrowserUI.cs#L416
                /*
                var statsPanel = _detailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView").GetPrivateField<LevelParamsPanel>("_levelParamsPanel");
                var statTransforms = statsPanel.GetComponentsInChildren<RectTransform>();
                var valueTexts = statsPanel.GetComponentsInChildren<TextMeshProUGUI>().Where(x => x.name == "ValueText").ToList();

                RectTransform panelRect = (statsPanel.transform as RectTransform);
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x * 1.2f, panelRect.sizeDelta.y * 1.2f);

                for (int i = 0; i < statTransforms.Length; i++)
                {
                    var r = statTransforms[i];
                    if (r.name == "Separator")
                    {
                        continue;
                    }
                    r.sizeDelta = new Vector2(r.sizeDelta.x * 0.75f, r.sizeDelta.y * 0.75f);
                }

                for (int i = 0; i < valueTexts.Count; i++)
                {
                    var text = valueTexts[i];
                    text.fontSize = 3.25f;
                }
                */

                //     var _starStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
                //     _starStatText = _starStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                //     _starStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.StarFull;
                //     _starStatText.text = "--";

                //     var _upvoteStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
                //     _upvoteStatText = _upvoteStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                //     _upvoteStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.ThumbUp;
                //     _upvoteStatText.text = "--";

                //     var _downvoteStatTransform = Instantiate(statTransforms[1], statsPanel.transform, false);
                //     _downvoteStatText = _downvoteStatTransform.GetComponentInChildren<TextMeshProUGUI>(true);
                //     _downvoteStatTransform.GetComponentInChildren<UnityEngine.UI.Image>(true).sprite = Sprites.ThumbDown;
                //     _downvoteStatText.text = "--";

                ResultsViewController _standardLevelResultsViewController = viewControllersContainer.GetComponentsInChildren<ResultsViewController>(true).First(x => x.name == "StandardLevelResultsViewController");
                _standardLevelResultsViewController.continueButtonPressedEvent += _standardLevelResultsViewController_continueButtonPressedEvent;


            }

            _levelListViewController.didSelectPackEvent += _levelListViewController_didSelectPackEvent;
            _levelPacksViewController.didSelectPackEvent += _levelPacksViewController_didSelectPackEvent;

            var packDetailViewController = viewControllersContainer.GetComponentsInChildren<LevelPackDetailViewController>(true).First(x => x.name == "LevelPackDetailViewController");
            _buyPackText = packDetailViewController.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name.EndsWith("InfoText"));
            _buyPackButton = packDetailViewController.GetComponentsInChildren<Button>(true).FirstOrDefault(x => x.name == "BuyPackButton");
            _originalBuyButtonEvent = _buyPackButton.onClick;


            initialized = true;
        }

        public void AddDefaultPlaylists()
        {
            try
            {
                Plugin.log.Info("Creating default playlist...");

                var levels = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.SelectMany(x => x.beatmapLevelCollection.beatmapLevels).ToList();

                Playlist _favPlaylist = new Playlist() { playlistTitle = "Your favorite songs", playlistAuthor = "", image = Sprites.SpriteToBase64(Sprites.BeastSaberLogo), icon = Sprites.BeastSaberLogo, fileLoc = "" };
                _favPlaylist.songs = new List<PlaylistSong>();
                _favPlaylist.songs.AddRange(levels.Where(x => x is CustomPreviewBeatmapLevel && PluginConfig.favoriteSongs.Contains(x.levelID.Split('_')[2])).Select(x => new PlaylistSong() { songName = $"{x.songName} {x.songSubName}", level = x as CustomPreviewBeatmapLevel, oneSaber = x.beatmapCharacteristics.Any(y => y.serializedName == "OneSaber"), path = "", key = "", levelId = x.levelID, hash = x.levelID.Split('_')[2] }));
                Plugin.log.Info($"Created \"{_favPlaylist.playlistTitle}\" playlist with {_favPlaylist.songs.Count} songs!");

                if (PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == "Your favorite songs"))
                {
                    PlaylistsCollection.loadedPlaylists.RemoveAll(x => x.playlistTitle == "Your favorite songs");
                }

                PlaylistsCollection.loadedPlaylists.Insert(0, _favPlaylist);

                _favPlaylist.SavePlaylist("Playlists\\favorites.json");
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to create default playlist! Exception: {e}");
            }
            UpdateLevelPacks();
        }

        public void UpdateLevelPacks()
        {

            SongCoreBeatmapLevelPackCollectionSO newCollection = SongCore.Loader.CustomBeatmapLevelPackCollectionSO;

            List<CustomBeatmapLevelPack> _customBeatmapLevelPacks = newCollection.GetPrivateField<List<CustomBeatmapLevelPack>>("_customBeatmapLevelPacks");
            List<IBeatmapLevelPack> _allBeatmapLevelPacks = newCollection.GetPrivateField<IBeatmapLevelPack[]>("_allBeatmapLevelPacks").ToList();

            _customBeatmapLevelPacks.RemoveAll(x => x.packID.StartsWith("Playlist_"));
            _allBeatmapLevelPacks.RemoveAll(x => x.packID.StartsWith("Playlist_"));

            newCollection.SetPrivateField("_customBeatmapLevelPacks", _customBeatmapLevelPacks);
            newCollection.SetPrivateField("_allBeatmapLevelPacks", _allBeatmapLevelPacks.ToArray());

            foreach (var playlist in PlaylistsCollection.loadedPlaylists)
            {
                PlaylistLevelPackSO levelPack = PlaylistLevelPackSO.CreatePackFromPlaylist(playlist);
                levelPack.playlist = playlist;

                newCollection.AddLevelPack(levelPack);
            }

            Plugin.log.Info("Updating level packs... New level packs count: " + newCollection.beatmapLevelPacks.Length);
            //      SongLoaderPlugin.SongLoader.Instance.InvokeMethod("ReloadHashes");
        }

        private void MainMenuViewController_didFinishEvent(MainMenuViewController sender, MainMenuViewController.MenuButton result)
        {
          //  lastPack = null;
            if (result == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                freePlayFlowCoordinator = FindObjectOfType<SoloFreePlayFlowCoordinator>();
                (freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent += soloFreePlayFlowCoordinator_didFinishEvent;
                SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;

                if (PluginConfig.rememberLastPackAndSong)
                {
                    StartCoroutine(SelectLastPackAndSong());
                }
                else
                {
                    int lastPackNum = _levelPacksViewController.GetPrivateField<int>("_selectedPackNum");
                    if (lastPackNum < SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length && lastPackNum >= 0)
                        lastPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO?.beatmapLevelPacks[lastPackNum];
                }
            }
            else if (result == MainMenuViewController.MenuButton.Party)
            {
                freePlayFlowCoordinator = FindObjectOfType<PartyFreePlayFlowCoordinator>();
                (freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent += partyFreePlayFlowCoordinator_didFinishEvent;
                SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
                SongDownloader.Instance.songDownloaded += SongDownloader_songDownloaded;

                if (PluginConfig.rememberLastPackAndSong)
                {
                    StartCoroutine(SelectLastPackAndSong());
                }
                else
                {
                    int lastPackNum = _levelPacksViewController.GetPrivateField<int>("_selectedPackNum");
                    if (lastPackNum < SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length && lastPackNum >= 0)
                        lastPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO?.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
                }
            }
            else
            {
                freePlayFlowCoordinator = null;
            }
        }

        private IEnumerator SelectLastPackAndSong()
        {
            yield return null;
            yield return null;
            if (PluginConfig.disableSongListTweaks) yield break;
            lastSortMode = PluginConfig.lastSelectedSortMode;

            if (!string.IsNullOrEmpty(PluginConfig.lastSelectedPack) && SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Any(x => x.packID == PluginConfig.lastSelectedPack))
            {
                int packIndex = Array.FindIndex(SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks, x => x.packID == PluginConfig.lastSelectedPack);

                if (packIndex < 0)
                {
                    Plugin.log.Warn($"Unable to find last selected pack with ID \"{PluginConfig.lastSelectedPack}\"");
                    lastPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
                    yield break;
                }
                if (packIndex < SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length)
                    lastPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[packIndex];

                SetLevels(lastSortMode, "");

                yield return null;

                if (!string.IsNullOrEmpty(PluginConfig.lastSelectedSong) && lastPack.beatmapLevelCollection.beatmapLevels.Any(x => x.levelID == PluginConfig.lastSelectedSong))
                {

                    var levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");

                    int songIndex = Array.FindIndex(_levelListViewController.GetPrivateField<IBeatmapLevelPack>("_levelPack").beatmapLevelCollection.beatmapLevels, x => x.levelID == PluginConfig.lastSelectedSong);

                    if (songIndex < 0)
                    {
                        Plugin.log.Warn($"Unable to find last selected song with ID \"{PluginConfig.lastSelectedSong}\"");
                        yield break;
                    }

                    if (levelsTableView.GetPrivateField<bool>("_showLevelPackHeader"))
                    {
                        songIndex++;
                    }

                    var tableView = levelsTableView.GetPrivateField<TableView>("_tableView");
                    tableView.ScrollToCellWithIdx(songIndex, TableView.ScrollPositionType.Beginning, false);
                    tableView.SelectCellWithIdx(songIndex, true);
                }
            }
            else
            {
                int lastPackNum = _levelPacksViewController.GetPrivateField<int>("_selectedPackNum");
                if (lastPackNum < SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.Length && lastPackNum >= 0)
                    lastPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks[_levelPacksViewController.GetPrivateField<int>("_selectedPackNum")];
            }

        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);
                        _randomButton.gameObject.SetActive(true);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        //            _difficultyButton.gameObject.SetActive(false);
                        _authorButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _randomButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);
                        //                _difficultyButton.gameObject.SetActive(true);
                        _authorButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        _randomButton.gameObject.SetActive(false);

                        _defButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);
                        //            _difficultyButton.gameObject.SetActive(false);
                        _authorButton.gameObject.SetActive(false);

                    }; break;
            }
        }

        private void _difficultyViewController_didSelectDifficultyEvent(StandardLevelDetailViewController sender, IDifficultyBeatmap beatmap)
        {
            _favoriteButton.SetButtonIcon(PluginConfig.favoriteSongs.Any(x => x.Contains(beatmap.level.levelID)) ? Sprites.RemoveFromFavorites : Sprites.AddToFavorites);
            _favoriteButton.interactable = !(beatmap.level is PreviewBeatmapLevelSO);

            _deleteButton.interactable = !PluginConfig.disableDeleteButton && (beatmap.level is CustomPreviewBeatmapLevel);

            //Banana id
            /*
            if (beatmap.level is CustomPreviewBeatmapLevel)
            {
                var levelhash = SongCore.Collections.hashForLevelID((beatmap.level as CustomPreviewBeatmapLevel).levelID);
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == levelhash);
                if (song != null)
                {
                    _upvoteStatText.text = song.Upvotes.ToString();
                    _downvoteStatText.text = song.Downvotes.ToString();
                    if (song.Diffs.Any())
                        _starStatText.text = song.Diffs.Max(x => x.Stars).ToString();
                    else
                        _starStatText.text = "--";
                }
                else
                {
                    _starStatText.text = "--";
                    _upvoteStatText.text = "--";
                    _starStatText.text = "--";
                }
            }
            else
            {
                _starStatText.text = "--";
                _upvoteStatText.text = "--";
                _downvoteStatText.text = "--";
            }
            */
        }

        private void _levelPacksViewController_didSelectPackEvent(LevelPacksViewController arg1, IBeatmapLevelPack arg2)
        {
            lastPack = arg2;

            if (arg2 is PlaylistLevelPackSO)
            {
                StartCoroutine(ShowDownloadQueue(arg2));
            }
            else
            {
                StartCoroutine(HideDownloadQueue());
            }
        }

        private void _levelListViewController_didSelectPackEvent(LevelPackLevelsViewController arg1, IBeatmapLevelPack arg2)
        {
       //     if (PluginConfig.disableSongListTweaks)
       //     lastPack = arg2;
            //    Plugin.log.Info("Pack: " + arg2.packName);
            bool isPlaylist = PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == lastPack?.packName);
            Plugin.log.Info("Selected pack header! IsPlaylist=" + isPlaylist);

            if (isPlaylist)
            {
                StartCoroutine(ShowDownloadQueue(lastPack));
            }
            else
            {
                StartCoroutine(HideDownloadQueue());
            }
        }

        private void partyFreePlayFlowCoordinator_didFinishEvent(PartyFreePlayFlowCoordinator obj)
        {
            (freePlayFlowCoordinator as PartyFreePlayFlowCoordinator).didFinishEvent -= partyFreePlayFlowCoordinator_didFinishEvent;
            SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            StartCoroutine(HideDownloadQueue());
    //        lastPack = null;
        }

        private void soloFreePlayFlowCoordinator_didFinishEvent(SoloFreePlayFlowCoordinator obj)
        {
            (freePlayFlowCoordinator as SoloFreePlayFlowCoordinator).didFinishEvent -= soloFreePlayFlowCoordinator_didFinishEvent;
            SongDownloader.Instance.songDownloaded -= SongDownloader_songDownloaded;
            StartCoroutine(HideDownloadQueue());
    //        lastPack = null;
        }

        private IEnumerator HideDownloadQueue()
        {
            _buyPackButton.interactable = true;
            _buyPackButton.SetButtonText("BUY MUSIC PACK");
            _buyPackButton.onClick = _originalBuyButtonEvent;
            _buyPackText.gameObject.SetActive(true);

            yield return null;

            if (_downloadQueueViewController != null)
            {
                _downloadingPlaylist = false;
                _downloadQueueViewController.AbortDownloads();
            }

            yield return null;
            yield return null;
        }

        private IEnumerator ShowDownloadQueue(IBeatmapLevelPack pack)
        {
            if (_downloadButtonEvent == null)
                _downloadButtonEvent = new Button.ButtonClickedEvent();
            Playlist playlist = pack is PlaylistLevelPackSO ? (pack as PlaylistLevelPackSO).playlist : PlaylistsCollection.loadedPlaylists.First(x => x.playlistTitle == pack.packName);
            _downloadButtonEvent.RemoveAllListeners();
            _downloadButtonEvent.AddListener(() =>
            {
                StartCoroutine(DownloadPlaylist(playlist));
            });

            _buyPackButton.interactable = (playlist.songs.Count > pack.beatmapLevelCollection.beatmapLevels.Length);
            _buyPackButton.SetButtonText("DOWNLOAD");
            _buyPackButton.onClick = _downloadButtonEvent;
            _buyPackText.gameObject.SetActive(false);

            if (_downloadQueueViewController == null)
                _downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

            yield return null;

            freePlayFlowCoordinator.InvokePrivateMethod("SetRightScreenViewController", new object[] { _downloadQueueViewController, false });

            yield return null;
            yield return null;

            if (_downloadQueueViewController != null)
            {
                _downloadingPlaylist = false;
                _downloadQueueViewController.AbortDownloads();
            }
        }

        private void _levelListViewController_didSelectLevelEvent(LevelPackLevelsViewController sender, IPreviewBeatmapLevel beatmap)
        {
            PluginConfig.lastSelectedSong = beatmap.levelID;
            PluginConfig.SaveConfig();
            if(beatmap is CustomPreviewBeatmapLevel)
            _favoriteButton.SetButtonIcon(PluginConfig.favoriteSongs.Any(x => x.Contains(beatmap.levelID.Split('_')[2])) ? Sprites.RemoveFromFavorites : Sprites.AddToFavorites);
            _favoriteButton.interactable = (beatmap is CustomPreviewBeatmapLevel);

            _deleteButton.interactable = !PluginConfig.disableDeleteButton && (beatmap is CustomPreviewBeatmapLevel);
            /*
            if (beatmap is CustomPreviewBeatmapLevel)
            {
                var levelhash = SongCore.Utilities.Hashing.GetCustomLevelHash(beatmap as CustomPreviewBeatmapLevel);
                ScrappedSong song = ScrappedData.Songs.FirstOrDefault(x => x.Hash == levelhash);
                if (song != null)
                {
                    _upvoteStatText.text = song.Upvotes.ToString();
                    _downvoteStatText.text = song.Downvotes.ToString();
                    if (song.Diffs.Any())
                        _starStatText.text = song.Diffs.Max(x => x.Stars).ToString();
                    else
                        _starStatText.text = "--";
                }
                else
                {
                    _starStatText.text = "--";
                    _upvoteStatText.text = "--";
                    _downvoteStatText.text = "--";
                }
            }
            else
            {
                _starStatText.text = "--";
                _upvoteStatText.text = "--";
                _downvoteStatText.text = "--";
            }
            */
            StartCoroutine(HideDownloadQueue());
        }


        public void DeletePressed()
        {
            IBeatmapLevel level = _detailViewController.selectedDifficultyBeatmap.level;
            _simpleDialog.Init($"Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"? \n Folder - \"{new DirectoryInfo((level as CustomPreviewBeatmapLevel).customLevelPath).Name}\"", "Delete", "Cancel",
                (selectedButton) =>
                {
                    freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _simpleDialog, null, false });
                    if (selectedButton == 0)
                    {
                        try
                        {
                            var levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");

                            List<IPreviewBeatmapLevel> levels = levelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID);

                            SongDownloader.Instance.DeleteSong(new Song(SongCore.Loader.CustomLevels.Values.First(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID)));

                            if (selectedIndex > -1)
                            {
                                int removedLevels = levels.RemoveAll(x => x.levelID == _detailViewController.selectedDifficultyBeatmap.level.levelID);
                                Plugin.log.Info("Removed " + removedLevels + " level(s) from song list!");

                                _levelListViewController.SetData(CustomHelpers.GetLevelPackWithLevels(levels.Cast<CustomPreviewBeatmapLevel>().ToArray(), lastPack?.packName ?? "Custom Songs", lastPack?.coverImage));
                                TableView listTableView = levelsTableView.GetPrivateField<TableView>("_tableView");
                                listTableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Beginning, false);
                                levelsTableView.SetPrivateField("_selectedRow", selectedIndex);
                                listTableView.SelectCellWithIdx(selectedIndex, true);
                            }
                        }
                        catch (Exception e)
                        {
                            Plugin.log.Error("Unable to delete song! Exception: " + e);
                        }
                    }
                });
            freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _simpleDialog, null, false });
        }

        private void SearchPressed()
        {
            if (_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchViewController.backButtonPressed += _searchViewController_backButtonPressed;
                _searchViewController.searchButtonPressed += _searchViewController_searchButtonPressed;
            }

            freePlayFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _searchViewController, null, false });
        }

        private void _searchViewController_searchButtonPressed(string request)
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
            SetLevels(SortMode.Default, request);
        }

        private void _searchViewController_backButtonPressed()
        {
            freePlayFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });
        }

        public void SetLevels(SortMode sortMode, string searchRequest)
        {
            lastSortMode = sortMode;
            IPreviewBeatmapLevel[] packlevels;
            CustomPreviewBeatmapLevel[] levels = null;
            if (lastPack != null)
            {
                packlevels = lastPack.beatmapLevelCollection.beatmapLevels;
                if (lastPack.beatmapLevelCollection.beatmapLevels.First() is CustomPreviewBeatmapLevel)
                    levels = packlevels.Cast<CustomPreviewBeatmapLevel>().ToArray();
                else
                    return;
            }
            else
            {
                levels = SongCore.Loader.CustomLevelsPack.beatmapLevelCollection.beatmapLevels.Cast<CustomPreviewBeatmapLevel>().ToArray();
            }

            if (string.IsNullOrWhiteSpace(searchRequest))
            {
                switch (sortMode)
                {
                    case SortMode.Newest: { levels = SortLevelsByCreationTime(levels); }; break;
                    case SortMode.Difficulty:
                        {
                            levels = levels.AsParallel().OrderBy(x => { int index = ScrappedData.Songs.FindIndex(y => x.levelID.StartsWith(y.Hash)); return (index == -1 ? (int.MaxValue - 1) : index); }).ToArray();
                        }; break;
                    case SortMode.Author:
                        {

                            levels = levels.ToList().OrderBy(x => x.songAuthorName).ToArray();
                        };
                        break;
                }
            }
            else
            {
                levels = levels.Where(x => ($"{x.songName} {x.songSubName} {x.levelAuthorName} {x.songAuthorName}".ToLower().Contains(searchRequest))).ToArray();
            }
            _levelListViewController.SetData(CustomHelpers.GetLevelPackWithLevels(levels, lastPack?.packName ?? "Custom Songs", lastPack?.coverImage));
            PopDifficultyAndDetails();
        }

        public CustomPreviewBeatmapLevel[] SortLevelsByCreationTime(CustomPreviewBeatmapLevel[] levels)
        {
            DirectoryInfo customSongsFolder = new DirectoryInfo(CustomLevelPathHelper.customLevelsDirectoryPath);

            List<string> sortedFolders = customSongsFolder.GetDirectories().OrderByDescending(x => x.CreationTime.Ticks).Select(x => x.FullName).ToList();

            List<string> sortedLevelPaths = new List<string>();

            for (int i = 0; i < sortedFolders.Count; i++)
            {
                //      Plugin.log.Info(sortedFolders[i]);
                if (SongCore.Loader.CustomLevels.TryGetValue(sortedFolders[i], out var song))
                {
                    sortedLevelPaths.Add(song.customLevelPath);
                }
            }
            List<CustomPreviewBeatmapLevel> notSorted = new List<CustomPreviewBeatmapLevel>(levels);

            List<CustomPreviewBeatmapLevel> sortedLevels = new List<CustomPreviewBeatmapLevel>();

            for (int i2 = 0; i2 < sortedLevelPaths.Count; i2++)
            {
                CustomPreviewBeatmapLevel data = notSorted.FirstOrDefault(x => x.customLevelPath == sortedLevelPaths[i2]);
                if (data != null)
                {
                    sortedLevels.Add(data);
                }

            }
            sortedLevels.AddRange(notSorted.Except(sortedLevels));
            return sortedLevels.ToArray();

        }

        private void PopDifficultyAndDetails()
        {
            bool isSolo = (freePlayFlowCoordinator is SoloFreePlayFlowCoordinator);

            if (isSolo)
            {
                SoloFreePlayFlowCoordinator soloCoordinator = freePlayFlowCoordinator as SoloFreePlayFlowCoordinator;
                int controllers = 0;
                if (soloCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
                if (controllers > 0)
                {
                    soloCoordinator.InvokePrivateMethod("PopViewControllersFromNavigationController", new object[] { soloCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController"), controllers, null, false });
                }
            }
            else
            {
                PartyFreePlayFlowCoordinator partyCoordinator = freePlayFlowCoordinator as PartyFreePlayFlowCoordinator;
                int controllers = 0;
                if (partyCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController").isInViewControllerHierarchy)
                {
                    controllers++;
                }
                if (controllers > 0)
                {
                    partyCoordinator.InvokePrivateMethod("PopViewControllersFromNavigationController", new object[] { partyCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController"), controllers, null, false });
                }
            }
        }

        private void _standardLevelResultsViewController_continueButtonPressedEvent(ResultsViewController sender)
        {
            try
            {
                TableView _levelListTableView = _levelListViewController.GetComponentInChildren<TableView>();

                _levelListTableView.RefreshTable();
            }
            catch (Exception e)
            {
                Plugin.log.Warn("Unable to refresh song list! Exception: " + e);
            }
        }

        private void FastScrollUp(TableView tableView, int pages)
        {
            float targetPosition = tableView.GetProperty<float>("position") - (Mathf.Max(1f, tableView.GetNumberOfVisibleCells() - 1f) * tableView.GetPrivateField<float>("_cellSize") * pages);
            if (targetPosition < 0f)
            {
                targetPosition = 0f;
            }

            tableView.SetPrivateField("_targetPosition", targetPosition);

            tableView.enabled = true;
            tableView.RefreshScrollButtons();
        }

        private void FastScrollDown(TableView tableView, int pages)
        {
            float num = (tableView.GetPrivateField<TableView.TableType>("_tableType") != TableView.TableType.Vertical) ? tableView.GetPrivateField<RectTransform>("_scrollRectTransform").rect.width : tableView.GetPrivateField<RectTransform>("_scrollRectTransform").rect.height;
            float num2 = tableView.GetPrivateField<int>("_numberOfCells") * tableView.GetPrivateField<float>("_cellSize") - num;

            float targetPosition = tableView.GetProperty<float>("position") + (Mathf.Max(1f, tableView.GetNumberOfVisibleCells() - 1f) * tableView.GetPrivateField<float>("_cellSize") * pages);

            if (targetPosition > num2)
            {
                targetPosition = num2;
            }

            tableView.SetPrivateField("_targetPosition", targetPosition);

            tableView.enabled = true;
            tableView.RefreshScrollButtons();
        }

        private void SongDownloader_songDownloaded(Song song)
        {
            if (lastPack is PlaylistLevelPackSO)// || PlaylistsCollection.loadedPlaylists.Any(x => x.playlistTitle == lastPack.packName))
            {
                Playlist playlist = (lastPack as PlaylistLevelPackSO).playlist;

                if (playlist.songs.Any(x => x.Compare(song)))
                {
                    (lastPack as PlaylistLevelPackSO).UpdateDataFromPlaylist();
                    TableView levelsTableView = _levelListViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView").GetPrivateField<TableView>("_tableView");
                    levelsTableView.ReloadData();
                    levelsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                    levelsTableView.SelectCellWithIdx(0, false);
                }
            }
        }

        public IEnumerator DownloadPlaylist(Playlist playlist)
        {
            PlaylistsCollection.MatchSongsForPlaylist(playlist, true);

            List<PlaylistSong> needToDownload = playlist.songs.Where(x => x.level == null).ToList();
            Plugin.log.Info($"Need to download {needToDownload.Count} songs for playlist {playlist.playlistTitle} by {playlist.playlistAuthor}");

            _downloadingPlaylist = true;
            foreach (var item in needToDownload)
            {
                if (!_downloadingPlaylist)
                    yield break;

                Song beatSaverSong = null;

                if (String.IsNullOrEmpty(playlist.customArchiveUrl))
                {
                    Plugin.log.Info("Obtaining hash and url for " + item.key + ": " + item.songName);
                    yield return GetInfoForSong(playlist, item, (Song song) => { beatSaverSong = song; });
                }
                else
                {
                    string archiveUrl = playlist.customArchiveUrl.Replace("[KEY]", item.key);

                    beatSaverSong = new Song()
                    {
                        songName = item.songName,
                        key = item.key,
                        downloadingProgress = 0f,
                        hash = (item.hash == null ? "" : item.hash),
                        downloadURL = archiveUrl
                    };
                }
                //     Plugin.log.Info($"Info grabbed, url {beatSaverSong.downloadURL}");
                if (!_downloadingPlaylist)
                    yield break;
                //bananabread playlists id
                if (beatSaverSong != null)
                {
       //             Plugin.log.Info("Adding Song with Hash: " + beatSaverSong.hash + " to queue");
                        _downloadQueueViewController.EnqueueSongAtStart(beatSaverSong, false);

                }
                else
                {
                    if (beatSaverSong == null)
                        Plugin.log.Info("null song");
                }
            }
            _downloadQueueViewController.DownloadAllSongsFromQueue();
            _downloadingPlaylist = false;
            PlaylistsCollection.MatchSongsForPlaylist(playlist, true);
      //      UpdateLevelPacks();
     //            PlaylistLevelPackSO playlistPack = SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks.FirstOrDefault(x => x.packName == playlist.playlistTitle) as PlaylistLevelPackSO;
     //            playlistPack?.UpdateDataFromPlaylist();
     //       SongCore.Loader.Instance.RefreshLevelPacks();
     //       int index = Array.IndexOf(SongCore.Loader.CustomBeatmapLevelPackCollectionSO.beatmapLevelPacks, playlistPack);
     //       var _levelPacksTableView = Instance._levelPacksViewController.GetField<LevelPacksTableView>("_levelPacksTableView");
     //       var tableView = _levelPacksTableView.GetPrivateField<TableView>("_tableView");
     //      _levelPacksTableView.SelectCellWithIdx(index);
     //      tableView.SelectCellWithIdx(index, true);
     //      tableView.ScrollToCellWithIdx(index, TableView.ScrollPositionType.Beginning, false);
     //      for (int i = 0; i < index; i++)
     //      {
     //          tableView.PageScrollDown();
     //      }
     //         Instance._levelListViewController.SetData(playlistPack);
     //         Instance._levelPackDetailViewController.SetData(playlistPack);
     //         if (lastPack is PlaylistLevelPackSO)
     //             (lastPack as PlaylistLevelPackSO).UpdateDataFromPlaylist();
        }

        public IEnumerator GetInfoForSong(Playlist playlist, PlaylistSong song, Action<Song> songCallback)
        {
            string url = "";
            bool _usingHash = false;

                if (!string.IsNullOrEmpty(song.key))
            {
                url = $"{PluginConfig.beatsaverURL}/api/maps/detail/{song.key}";
                if (!string.IsNullOrEmpty(playlist.customDetailUrl))
                {
                    url = playlist.customDetailUrl + song.key;
                }
            }
            else if (!string.IsNullOrEmpty(song.hash))
            {
                url = $"{PluginConfig.beatsaverURL}/api/maps/by-hash/{song.hash}";
                _usingHash = true;
            }
            else if (!string.IsNullOrEmpty(song.levelId))
            {
                string hash = song.levelId.Split('_')[2];
                url = $"{PluginConfig.beatsaverURL}/api/maps/by-hash/{hash}";
                _usingHash = true;
            }
            else
            {
                yield break;
            }

            UnityWebRequest www = UnityWebRequest.Get(url);
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

                    if (_usingHash)
                    {
                        if (jNode.Children().Count() == 0)
                        {
                            Plugin.log.Error($"Song {song.songName} doesn't exist on BeatSaver!");
                            songCallback?.Invoke(null);
                            yield break;
                        }
                        var newSong = new Song(jNode, false);
                            UpdatePlaylistSongEntry(playlist, song, newSong);
                        songCallback?.Invoke(newSong);
                    }
                    else
                    {
                        var newSong = new Song(jNode, false);
                            UpdatePlaylistSongEntry(playlist, song, newSong);
                        songCallback?.Invoke(newSong);
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to parse response! Exception: " + e);
                }
            }
        }

        public void UpdatePlaylistSongEntry(Playlist playlist, PlaylistSong song, Song newSong)
        {
           song.key = newSong.key;
            song.hash = newSong.hash;
            var json = JSON.Parse(File.ReadAllText(playlist.fileLoc));
            foreach (JSONNode node in json["songs"].AsArray)
            {
                if (node["songName"] == song.songName)
                {
                    //             Plugin.log.Info("Updating: " + song.songName);
                    if (node["key"] != null)
                        node["key"] = null;
                    node["hash"] = newSong.hash;
                }

            }
            File.WriteAllText(playlist.fileLoc, json.ToString());


        }
    }

    [HarmonyPatch(typeof(LevelPackLevelsTableView))]
    [HarmonyPatch("CellForIdx")]
    [HarmonyPatch(new Type[] { typeof(int) })]
    class LevelListTableViewPatch
    {

        static TableCell Postfix(TableCell __result, LevelPackLevelsTableView __instance, int row)
        {
            try
            {
                if (!PluginConfig.enableSongIcons) return __result;

                bool showHeader = __instance.GetPrivateField<bool>("_showLevelPackHeader");

                if (row == 0 && showHeader)
                {
                    return __result;
                }
                var level = __instance.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels[(showHeader ? (row - 1) : row)];
                string levelId;
                if (level is CustomPreviewBeatmapLevel)
                    levelId = SongCore.Utilities.Hashing.GetCustomLevelHash(level as CustomPreviewBeatmapLevel).ToLower();
                else
                    levelId = level.levelID;

                UnityEngine.UI.Image icon = null;

                UnityEngine.UI.Image[] levelIcons = __result.GetPrivateField<UnityEngine.UI.Image[]>("_beatmapCharacteristicImages");
                float[] levelIconAlphas = __result.GetPrivateField<float[]>("_beatmapCharacteristicAlphas");

                if (levelIcons.Any(x => x.name == "LevelTypeIconExtra"))
                {
                    icon = levelIcons.First(x => x.name == "LevelTypeIconExtra");
                }
                else
                {
                    icon = GameObject.Instantiate(__result.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "LevelTypeIcon0"), __result.transform, true);

                    (icon.transform as RectTransform).anchoredPosition = new Vector2(-14.5f, 0f);
                    icon.transform.name = "LevelTypeIconExtra";

                    levelIcons = levelIcons.AddToArray(icon);
                    __result.SetPrivateField("_beatmapCharacteristicImages", levelIcons);

                    levelIconAlphas = levelIconAlphas.AddToArray(0.1f);
                    __result.SetPrivateField("_beatmapCharacteristicAlphas", levelIconAlphas);

                    foreach (var levelIcon in levelIcons)
                    {
                        levelIcon.rectTransform.anchoredPosition = new Vector2(levelIcon.rectTransform.anchoredPosition.x, -2f);
                    }
                }

                if (PluginConfig.favoriteSongs.Any(x => x.StartsWith(levelId)))
                {
                    levelIconAlphas[3] = 1f;
                    icon.sprite = Sprites.StarFull;
                }
                else if (PluginConfig.votedSongs.ContainsKey(levelId))
                {
                    switch (PluginConfig.votedSongs[levelId].voteType)
                    {
                        case VoteType.Upvote:
                            {
                                levelIconAlphas[3] = 1f;
                                icon.sprite = Sprites.ThumbUp;
                            }
                            break;
                        case VoteType.Downvote:
                            {
                                levelIconAlphas[3] = 1f;
                                icon.sprite = Sprites.ThumbDown;
                            }
                            break;
                    }
                }
                else
                {
                    levelIconAlphas[3] = 0.1f;
                    icon.sprite = Sprites.StarFull;
                }
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Unable to create extra icon! Exception: " + e);
            }
            return __result;
        }
    }

    [HarmonyPatch(typeof(LevelPackTableCell))]
    [HarmonyPatch("SetDataFromPack")]
    [HarmonyPatch(new Type[] { typeof(IBeatmapLevelPack) })]
    class LevelPackTableCellSetDataPatch
    {

        static bool Prefix(LevelPackTableCell __instance, IBeatmapLevelPack pack)
        {
            try
            {
                if (pack is PlaylistLevelPackSO)
                {
                    Playlist playlist = ((PlaylistLevelPackSO)pack).playlist;
                    __instance.GetPrivateField<TextMeshProUGUI>("_packNameText").text = pack.packName;
                    __instance.GetPrivateField<TextMeshProUGUI>("_infoText").text = (playlist.songs.Count > pack.beatmapLevelCollection.beatmapLevels.Length) ? string.Format("Songs {0} | Downloaded {1}", playlist.songs.Count, pack.beatmapLevelCollection.beatmapLevels.Length) : string.Format("Songs {0}", pack.beatmapLevelCollection.beatmapLevels.Length);
                    __instance.GetPrivateField<UnityEngine.UI.Image>("_coverImage").sprite = pack.coverImage;

                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception in LevelPackTableCellSetData patch: " + e);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(LevelPackTableCell))]
    [HarmonyPatch("RefreshAvailabilityAsync")]
    [HarmonyPatch(new Type[] { typeof(AdditionalContentModelSO), typeof(IBeatmapLevelPack) })]
    class LevelPackTableCellRefreshAvailabilityPatch
    {

        static bool Prefix(LevelPackTableCell __instance, AdditionalContentModelSO contentModel, IBeatmapLevelPack pack)
        {
            try
            {
                return !(pack is PlaylistLevelPackSO);
            }
            catch (Exception e)
            {
                Plugin.log.Critical("Exception in LevelPackTableCellRefreshAvailability patch: " + e);
                return true;
            }
        }
    }


}
