using UnityEngine;

namespace Slafurry.Systems.Audio
{
    [CreateAssetMenu(fileName = "MusicData", menuName = "Audio/Music Data")]
    public class MusicData : ScriptableObject
    {
        public MusicTrack[] tracks;

        public MusicTrack GetTrack(MusicReference music)
        {
            if (music == null)
            {
                Debug.LogError("[MusicData] MusicReference is null");
                return default;
            }

            if (tracks == null) return default;

            foreach (var track in tracks)
            {
                if (track.music == music)
                    return track;
            }
            return default;
        }

        public MusicTrack GetTrack(string trackName)
        {
            if (tracks == null || string.IsNullOrEmpty(trackName)) return default;

            foreach (var track in tracks)
            {
                if (track.TrackName == trackName)
                    return track;
            }
            return default;
        }

        public AudioClip GetClipFromName(MusicReference music)
        {
            return GetTrack(music).clip;
        }

        public AudioClip GetClipFromName(string trackName)
        {
            return GetTrack(trackName).clip;
        }
    }
}