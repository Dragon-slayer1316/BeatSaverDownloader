﻿using BS_Utils.Utilities;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SongCore.OverrideClasses;
namespace BeatSaverDownloader.Misc
{
    class PlaylistLevelPackSO : SongCoreCustomBeatmapLevelPack
    {
        public Playlist playlist { get { return _playlist; } set { _playlist = value;  UpdateDataFromPlaylist(); } }

        private Playlist _playlist;

        public static PlaylistLevelPackSO CreatePackFromPlaylist(Playlist playlist)
        {
            return new PlaylistLevelPackSO($"Playlist_{playlist.playlistTitle}_{playlist.playlistAuthor}", playlist.playlistTitle, playlist.icon, new SongCoreCustomLevelCollection(playlist.songs.Where(x => x.level != null).Select(x => x.level).ToArray()));


        }
        public PlaylistLevelPackSO(string packID, string packName, Sprite coverImage, CustomBeatmapLevelCollection customBeatmapLevelCollection) : base(packID, packName, coverImage, customBeatmapLevelCollection)
        {
        }

        public void UpdateDataFromPlaylist()
        {
            _packName = _playlist.playlistTitle;
            _coverImage = _playlist.icon;
            _packID = $"Playlist_{playlist.playlistTitle}_{playlist.playlistAuthor}";
      //      _isPackAlwaysOwned = false;

            PlaylistsCollection.MatchSongsForPlaylist(playlist);
            //bananabread playlist
            CustomPreviewBeatmapLevel[] levels = playlist.songs.Where(x => x.level != null).Select(x => x.level).ToArray();

            SongCoreCustomLevelCollection levelCollection = new SongCoreCustomLevelCollection(levels);

            _customBeatmapLevelCollection = levelCollection;
        }

    }
}
