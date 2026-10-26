using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>
    /// Every cue in the game, looked up by id. Lives at Resources/AudioCueTable so AudioService can load it on first use.
    /// Built by Tools/Pocket Isle/Audio/Sync Cues; that tool also checks for duplicate ids and missing clips and writes AudioIds.cs.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Audio/Audio Cue Table", fileName = "AudioCueTable")]
    public class AudioCueTable : ScriptableObject
    {
        public const string ResourceName = "AudioCueTable";

        public List<AudioCue> cues = new List<AudioCue>();

        [NonSerialized] Dictionary<string, AudioCue> _byId;

        public AudioCue Find(string id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, AudioCue>();
                foreach (var c in cues)
                    if (c != null && !string.IsNullOrEmpty(c.id)) _byId[c.id] = c;
            }
            return id != null && _byId.TryGetValue(id, out var cue) ? cue : null;
        }

        void OnValidate() => _byId = null;
    }
}
