using Diorama.Audio;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// A cue loop that rides on an empty child of its piece (step audio-D): <see cref="Set"/> moves it to a world point and
    /// starts the loop (or switches the cue), <see cref="Off"/> fades it. The child dies with the piece, and AudioService
    /// ends a loop whose follow target is destroyed, so a scene swap never leaves a loop running.
    /// </summary>
    public sealed class LoopAnchor
    {
        readonly Transform _parent;
        readonly string _name;
        Transform _point;
        AudioHandle _handle;
        string _id;

        public LoopAnchor(Transform parent, string name)
        {
            _parent = parent;
            _name = name;
        }

        public AudioHandle Handle => _handle;
        public bool Playing => _handle.IsValid;

        /// <summary>Plays <paramref name="id"/> at <paramref name="world"/> (call every frame it should sound; it only starts once).</summary>
        public void Set(string id, Vector3 world, float fadeIn = 0.2f)
        {
            if (_parent == null) return;
            if (_point == null)
            {
                _point = new GameObject(_name).transform;
                _point.SetParent(_parent, false);
            }
            _point.position = world;
            if (_id == id) return;
            _handle.Stop(0.3f);
            _id = id;
            _handle = GameAudio.Loop(id, _point, fadeIn);
        }

        public void Off(float fade = 0.3f)
        {
            _handle.Stop(fade);
            _handle = default;
            _id = null;
        }
    }
}
