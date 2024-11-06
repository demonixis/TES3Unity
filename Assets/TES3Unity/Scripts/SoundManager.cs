using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TES3Unity
{
    /// <summary>
    /// A proof of concept sound manager.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        private MusicPlayer _musicPlayer;
        private static readonly Dictionary<string, AudioClip> AudioClipStore = new();

        private void Update()
        {
            _musicPlayer?.Update();
        }

        public void Initialize(string dataPath)
        {
            _musicPlayer = new MusicPlayer();
            
            if (!GameSettings.Get().MusicEnabled) return;
  
            var songs = Directory.GetFiles(dataPath + "/Music/Explore", "*.mp3");

            if (songs.Length <= 0) return;
            
            foreach (var songFilePath in songs)
            {
                if (!songFilePath.Contains("Morrowind Title"))
                {
                    _musicPlayer.AddSong(songFilePath);
                }
            }

            _musicPlayer.Play();
        }

        public static AudioClip GetAudioClip(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (AudioClipStore.TryGetValue(id, out var audioClip))
            {
                return audioClip;
            }

            var path = Tes3Engine.DataReader.GetSound(id);
            if (!File.Exists(path))
            {
                return null;
            }

            var pcm = AudioUtils.ReadWAV(path);
            var clip = AudioUtils.CreateAudioClip(id, pcm);

            AudioClipStore.Add(id, clip);

            return clip;
        }
    }
}
