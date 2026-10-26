using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diorama.Audio
{
    /// <summary>One kind of small sound scattered over the island now and then (ember pop, drip, bird).</summary>
    [Serializable]
    public class SweetenerSet
    {
        [Tooltip("Cue id, e.g. sw.crackle. A cue without clips (awaiting the gen batch) is skipped.")] public string cue = "";
        [Range(0.5f, 60f)] public float minInterval = 3f;
        [Range(0.5f, 120f)] public float maxInterval = 15f;
        [Range(0f, 1.5f)] public float volume = 1f;
        [Tooltip("Random dip per play, 0..1 of the volume.")] [Range(0f, 1f)] public float volumeJitter = 0.3f;
        [Tooltip("Random pitch per play, +- this share.")] [Range(0f, 0.3f)] public float pitchJitter = 0.06f;
        [Range(1, 4)] public int maxAtOnce = 1;
    }

    /// <summary>
    /// The sound of one area: a 2D bed loop plus sweeteners that play as 3D one-shots at random points on the island surface.
    /// AmbienceDirector.SetArea(profile) switches to it.
    /// </summary>
    [CreateAssetMenu(menuName = "Pocket Isle/Audio/Ambience Profile", fileName = "Ambience")]
    public class AmbienceProfile : ScriptableObject
    {
        [Tooltip("Looping 2D bed cue (amb.*). Empty = no bed.")] public string bed = "";
        [Range(0f, 1.5f)] public float bedVolume = 1f;
        public List<SweetenerSet> sweeteners = new List<SweetenerSet>();
        [TextArea(1, 3)] public string notes = "";
    }
}
