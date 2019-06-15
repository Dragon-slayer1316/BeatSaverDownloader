﻿using BeatSaverDownloader.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using VRUI;
using UnityEngine.UI;
using BeatSaverDownloader.UI.ViewControllers;
using CustomUI.BeatSaber;
using HMUI;
namespace BeatSaverDownloader.UI.ViewControllers
{
    enum DownloadState { Downloaded, Downloading, NotDownloaded };

    class SongDetailViewController : CustomViewController
    {
        public event Action<Song> downloadButtonPressed;
        private Song _currentSong;

        private TextMeshProUGUI songNameText;

        private TextMeshProUGUI difficulty1Text;
        private TextMeshProUGUI difficulty2Text;
        private TextMeshProUGUI difficulty3Text;
        private TextMeshProUGUI difficulty1Title;
        private TextMeshProUGUI difficulty2Title;
        private TextMeshProUGUI difficulty3Title;

        private TextMeshProUGUI downloadsText;
        private TextMeshProUGUI playsText;

        private RawImage coverImage;

        private LevelParamsPanel _levelParams;
        private StandardLevelDetailView _levelDetails;

        private Button _downloadButton;
        private Button _favoriteButton;

        private GameObject _loadingIndicator;


        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation)
            {
                gameObject.SetActive(true);
                _levelDetails = GetComponentsInChildren<StandardLevelDetailView>(true).First(x => x.name == "LevelDetail");
                _levelDetails.gameObject.SetActive(true);

                BeatmapDifficultySegmentedControlController beatmapDifficultySegmentedControl = GetComponentsInChildren<BeatmapDifficultySegmentedControlController>(true).First(x => x.name == "BeatmapDifficultySegmentedControl");
                beatmapDifficultySegmentedControl.gameObject.SetActive(false);

                RemoveCustomUIElements(rectTransform);

                _levelParams = GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel");

                foreach (HoverHint hint in _levelParams.transform.GetComponentsInChildren<HoverHint>())
                {
                    switch (hint.name)
                    {
                        case "Time":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.DownloadIcon;
                            }; break;
                        case "BPM":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.PlayIcon;
                            }; break;
                        case "NotesCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "MetronomeIcon");
                            }; break;
                        case "ObstaclesCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.ThumbUp;
                            }; break;
                        case "BombsCount":
                            {
                                hint.GetComponentInChildren<UnityEngine.UI.Image>().sprite = Sprites.ThumbDown;
                            }; break;
                    }

                    Destroy(hint);
                }

                RectTransform yourStats = GetComponentsInChildren<RectTransform>().First(x => x.name == "Stats");
                yourStats.gameObject.SetActive(true);

                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();

                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");

                    downloadsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "Time");
                    downloadsText.fontSize = 3f;

                    playsText = _textComponents.First(x => x.name == "ValueText" && x.transform.parent.name == "BPM");
                    playsText.fontSize = 3f;
                    //        foreach (var x in _textComponents)
                    //        {
                    //            Console.WriteLine(x.name);
                    //            Console.WriteLine(x.transform.parent.name);
                    //        }
                    difficulty1Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxRank"); //.text = "Expert/+";
                    difficulty1Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxRank");

                    difficulty2Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "Highscore");//.text = "Hard";
                    difficulty2Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "Highscore");

                    difficulty3Title = _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxCombo");//.text = "Easy/Normal";
                    difficulty3Text = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxCombo");
                }
                catch (Exception e)
                {
                    Plugin.log.Critical("Unable to convert detail view controller! Exception:  " + e);
                }

                _downloadButton = _levelDetails.playButton;
                _downloadButton.SetButtonText("DOWNLOAD");
                _downloadButton.ToggleWordWrapping(false);
                _downloadButton.onClick.RemoveAllListeners();
                _downloadButton.onClick.AddListener(() => { downloadButtonPressed?.Invoke(_currentSong); });
                (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(26f, 8.8f);

                coverImage = _levelDetails.GetPrivateField<RawImage>("_coverImage");

                _loadingIndicator = BeatSaberUI.CreateLoadingSpinner(rectTransform);
                (_loadingIndicator.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
                (_loadingIndicator.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);
            }
        }


        public void SetDownloadState(DownloadState state)
        {
            _downloadButton.SetButtonText(state == DownloadState.Downloading ? "QUEUED..." : (state == DownloadState.Downloaded ? "DELETE" : "DOWNLOAD"));
            _downloadButton.interactable = state != DownloadState.Downloading;
        }

        public void SetContent(MoreSongsUI sender, Song newSongInfo)
        {
            _currentSong = newSongInfo;

            songNameText.text = _currentSong.songName;

            downloadsText.text = _currentSong.downloads.ToString();
            _levelParams.bpm = (float)(_currentSong.plays);
            _levelParams.notesCount = (int)_currentSong.bpm;
            _levelParams.obstaclesCount = _currentSong.upVotes;
            _levelParams.bombsCount = _currentSong.downVotes;

            Polyglot.LocalizedTextMeshProUGUI localizer1 = difficulty1Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer1 != null)
                GameObject.Destroy(localizer1);
            Polyglot.LocalizedTextMeshProUGUI localizer2 = difficulty2Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer2 != null)
                GameObject.Destroy(localizer2);
            Polyglot.LocalizedTextMeshProUGUI localizer3 = difficulty3Title.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer3 != null)
                GameObject.Destroy(localizer3);
            difficulty1Title.text = "Expert/+";
            difficulty2Title.text = "Hard";
            difficulty3Title.text = "Easy/Normal";




            difficulty1Text.text = (_currentSong.metadata.difficulties.expert || _currentSong.metadata.difficulties.expertPlus) ? "Yes" : "No";
            difficulty2Text.text = (_currentSong.metadata.difficulties.hard) ? "Yes" : "No";
            difficulty3Text.text = (_currentSong.metadata.difficulties.easy || _currentSong.metadata.difficulties.normal) ? "Yes" : "No";

            StartCoroutine(LoadScripts.LoadSpriteCoroutine(_currentSong.coverURL, (cover) => { coverImage.texture = cover.texture; }));

        //    SetDownloadState((SongDownloader.Instance.IsSongDownloaded(_currentSong) ? DownloadState.Downloaded : (sender.IsDownloadingSong(_currentSong) ? DownloadState.Downloading : DownloadState.NotDownloaded)));
        }


        public void SetLoadingState(bool isLoading)
        {
            if (isLoading)
            {
                _downloadButton.SetButtonText("LOADING...");
                songNameText.text = "Loading...";
                downloadsText.text = "0";
                _levelParams.bpm = 0f;
                _levelParams.notesCount = 0;
                _levelParams.obstaclesCount = 0;
                _levelParams.bombsCount = 0;
            }

            if (_loadingIndicator != null)
            {
                _downloadButton.interactable = !isLoading;
                _levelDetails.gameObject.SetActive(!isLoading);
                _loadingIndicator.SetActive(isLoading);
            }
        }

        void RemoveCustomUIElements(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith("CustomUI"))
                {
                    Destroy(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    RemoveCustomUIElements(child);
                }
            }
        }
    }



}

