﻿using System;
using System.Collections.Generic;
using System.Linq;
using VRUI;
using UnityEngine.UI;
using HMUI;
using TMPro;
using UnityEngine;
using BeatSaverDownloader.Misc;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
namespace BeatSaverDownloader.UI.ViewControllers
{
    public enum TopButtonsState { Select, SortBy, Search };

    public class MoreSongsListViewController : CustomListViewController
    {
        public List<Song> songsList = new List<Song>();

        private int _lastSelectedRow;
        private GameObject _loadingIndicator;
        public event Action<int> didSelectRow;

        public event Action SearchButtonPressed;

        public event Action SortByTop;
        public event Action SortByNew;

        public event Action SortByTrending;
        public event Action SortByNewlyRanked;
        public event Action SortByDifficulty;

        public event Action pageUpPressed;
        public event Action pageDownPressed;

        private const string _mainButton = "CreditsButton";
        private Button _sortByButton;
        private Button _searchButton;

        private Button _topButton;
        private Button _newButton;

        private Button _trendingButton;
        private Button _newlyRankedButton;
        private Button _difficultyButton;

        private bool initialized = false;

        private float _offset = 25f;
        private bool _fixedOffset;
        public override void __Activate(ActivationType activationType)
        {
            base.__Activate(activationType);
            //
            if (!initialized && activationType == ActivationType.AddedToHierarchy)
            {
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0, 26);
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    pageUpPressed?.Invoke();
                });
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    pageDownPressed?.Invoke();
                });

                _loadingIndicator = BeatSaberUI.CreateLoadingSpinner(rectTransform);
                (_loadingIndicator.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
                _loadingIndicator.SetActive(true);

                CreateButtons();
                initialized = true;
                _customListTableView.didSelectCellWithIdxEvent += DidSelectRowEvent;
            }
            else
            {
                _customListTableView.ReloadData();
            }
        }

        internal void Refresh()
        {
            _customListTableView.ReloadData();
            if (_lastSelectedRow > -1)
                _customListTableView.SelectCellWithIdx(_lastSelectedRow);
        }

        public void SetContent(List<Song> songs)
        {
            if (songs == null && songsList != null)
                songsList.Clear();
            else
                songsList = new List<Song>(songs);


            _customListTableView.ReloadData();
            _customListTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            _lastSelectedRow = -1;

        }

        public void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(isLoading);
            }
        }

        public void TogglePageUpDownButtons(bool pageUpEnabled, bool pageDownEnabled)
        {
            _pageUpButton.interactable = pageUpEnabled;
            _pageDownButton.interactable = pageDownEnabled;
        }

        private void RevertButtonOffset(Button button)
        {
            //       Plugin.log.Info("RevertButtonOffset");
            RectTransform rectTransform = (button.transform as RectTransform);
            Vector3 currentPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(currentPosition.x + _offset, currentPosition.y);
        }

        private void ApplyButtonOffset(Button button)
        {
            //       Plugin.log.Info("RevertButtonOffset");
            RectTransform rectTransform = (button.transform as RectTransform);
            Vector3 currentPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(currentPosition.x - _offset, currentPosition.y);
        }

        public override TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = GetTableCell(false);

            _tableCell.reuseIdentifier = "MoreSongsTableCell";
            _tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = string.Format("{0} <size=80%>{1}</size>", songsList[row].songName, songsList[row].songSubName);
            _tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = songsList[row].songAuthorName + " <size=80%>[" + songsList[row].levelAuthorName + "]</size>";
            _tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            _tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            _tableCell.SetPrivateField("_bought", true);

            StartCoroutine(LoadScripts.LoadSpriteCoroutine(songsList[row].coverURL, (cover) => { _tableCell.GetPrivateField<UnityEngine.UI.RawImage>("_coverRawImage").texture = cover.texture; }));
            bool alreadyDownloaded = false;//SongDownloader.Instance.IsSongDownloaded(songsList[row]);

            if (alreadyDownloaded)
            {
                foreach (UnityEngine.UI.Image img in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>())
                {
                    img.color = new Color(1f, 1f, 1f, 0.2f);
                }
                foreach (TextMeshProUGUI text in _tableCell.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    text.faceColor = new Color(1f, 1f, 1f, 0.2f);
                }
            }

            return _tableCell;
        }

        private void CreateButtons()
        {
            _sortByButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(15f, 36.5f), new Vector2(30f, 6f), () => { SelectTopButtons(TopButtonsState.SortBy); }, "Sort by");
            _sortByButton.SetButtonTextSize(3f);

            _searchButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(-15, 36.5f), new Vector2(30f, 6f), () =>
            {
                SearchButtonPressed?.Invoke();
                SelectTopButtons(TopButtonsState.Search);
            }, "Search");
            _searchButton.SetButtonTextSize(3f);

            _topButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(-20f - _offset, 36.5f), new Vector2(20f, 6f), () =>
            {
                SortByTop?.Invoke();
                SelectTopButtons(TopButtonsState.Select);
            },
            "Hot");

            _topButton.SetButtonTextSize(3f);
            _topButton.ToggleWordWrapping(false);
            _topButton.gameObject.SetActive(false);

            _newButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(0f - _offset, 36.5f), new Vector2(20f, 6f), () =>
            {
                SortByNew?.Invoke();
                SelectTopButtons(TopButtonsState.Select);
            }, "Newest");

            _newButton.SetButtonTextSize(3f);
            _newButton.ToggleWordWrapping(false);
            _newButton.gameObject.SetActive(false);

            _trendingButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(20f - _offset, 36.5f), new Vector2(20f, 6f), () =>
            {
                SortByTrending?.Invoke();
                SelectTopButtons(TopButtonsState.Select);
            }, "Trending");

            _trendingButton.SetButtonTextSize(3f);
            _trendingButton.ToggleWordWrapping(false);
            _trendingButton.gameObject.SetActive(false);

            _newlyRankedButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(42f - _offset, 36.5f), new Vector2(25f, 6f), () =>
            {
                SortByNewlyRanked?.Invoke();
                SelectTopButtons(TopButtonsState.Select);
            },
           "Newly Ranked");
            _newlyRankedButton.SetButtonTextSize(3f);
            _newlyRankedButton.ToggleWordWrapping(false);
            _newlyRankedButton.gameObject.SetActive(false);

            _difficultyButton = BeatSaberUI.CreateUIButton(rectTransform, _mainButton, new Vector2(64f - _offset, 36.5f), new Vector2(20f, 6f), () =>
            {
                SortByDifficulty?.Invoke();
                SelectTopButtons(TopButtonsState.Select);
            },
           "Difficulty");
            _difficultyButton.SetButtonTextSize(3f);
            _difficultyButton.ToggleWordWrapping(false);
            _difficultyButton.gameObject.SetActive(false);

        }


        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);
                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);

                        _trendingButton.gameObject.SetActive(false);
                        _newlyRankedButton.gameObject.SetActive(false);
                        _difficultyButton.gameObject.SetActive(false);
                 //       TogglePageUpDownButtons(true, true);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _topButton.gameObject.SetActive(true);
                        _newButton.gameObject.SetActive(true);

                        _trendingButton.gameObject.SetActive(true);
                        _newlyRankedButton.gameObject.SetActive(true);
                        _difficultyButton.gameObject.SetActive(true);
                  //      TogglePageUpDownButtons(false, false);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);

                        _topButton.gameObject.SetActive(false);
                        _newButton.gameObject.SetActive(false);

                        _trendingButton.gameObject.SetActive(false);
                        _newlyRankedButton.gameObject.SetActive(false);
                        _difficultyButton.gameObject.SetActive(false);

                    }; break;
            }
        }

        public override float CellSize()
        {
            return 10f;
        }
        public override int NumberOfCells()
        {
            return Math.Min(songsList.Count, MoreSongsFlowCoordinator.songsPerPage);
        }
        new private void DidSelectRowEvent(TableView sender, int row)
        {

            if (!_fixedOffset)
            {
                RevertButtonOffset(_topButton);
                RevertButtonOffset(_newButton);
                RevertButtonOffset(_trendingButton);
                RevertButtonOffset(_newlyRankedButton);
                RevertButtonOffset(_difficultyButton);
                _fixedOffset = true;
            }

            _lastSelectedRow = row;
            didSelectRow?.Invoke(row);
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            if (_fixedOffset)
            {
                ApplyButtonOffset(_topButton);
                ApplyButtonOffset(_newButton);
                ApplyButtonOffset(_trendingButton);
                ApplyButtonOffset(_newlyRankedButton);
                ApplyButtonOffset(_difficultyButton);
                _fixedOffset = false;
            }
            _lastSelectedRow = -1;
        }
    }
}
