using CustomUI.Utilities;
using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace BeatSaverDownloader.Misc
{
    class LoadScripts
    {
        static public Dictionary<string, Sprite> _cachedSprites = new Dictionary<string, Sprite>();

        static public IEnumerator LoadSpriteCoroutine(string spritePath, Action<Sprite> finished)
        {
            Texture2D tex;

            if (_cachedSprites.ContainsKey(spritePath))
            {
                finished.Invoke(_cachedSprites[spritePath]);
                yield break;
            }
            UnityWebRequest www = UnityWebRequestTexture.GetTexture(spritePath);
            www.timeout = 15;
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
            tex = DownloadHandlerTexture.GetContent(www);
            var newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
            if (!_cachedSprites.ContainsKey(spritePath))
                _cachedSprites.Add(spritePath, newSprite);
            finished.Invoke(newSprite);
            }

        }

        static public IEnumerator LoadAudioCoroutine(string audioPath, object obj, string fieldName)
        {
            using (var www =  UnityWebRequestMultimedia.GetAudioClip(audioPath, AudioType.UNKNOWN))
            {
                www.timeout = 15;
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Plugin.log.Error($"Unable to connect to {PluginConfig.beatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
                }
                else
                {
                ReflectionUtil.SetPrivateField(obj, fieldName, DownloadHandlerAudioClip.GetContent(www));
                }

            }
        }

    }
}
