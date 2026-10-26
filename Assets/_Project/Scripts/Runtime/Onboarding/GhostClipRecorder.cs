using System;
using System.Collections;
using System.IO;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Records a ghost hand clip from the real hand (step 8b): a 3-2-1 countdown floats in front of the player, then the
    /// tracked hand's root pose and joint-local poses (ISDK OpenXR joint set) are sampled at <see cref="rate"/> Hz for N
    /// seconds, relative to the view toward the island (grabs, pokes) or to the wrist's own starting pose (thumb gestures).
    /// In the Editor <see cref="Saved"/> (GhostClipTools) writes the asset into the library; on the headset the clip goes
    /// to <c>ghostclips/&lt;name&gt;.json</c> in the app's files folder (pull it with adb, then Tools > Pocket Isle >
    /// Ghost Hands > Import Clips). On the headset the dev panel's Tools > Record ghost records the next clip in
    /// <see cref="GhostClipLibrary.Wanted"/> that isn't recorded yet (step 9e: no more thumb-tap shortcut).
    /// </summary>
    public class GhostClipRecorder : MonoBehaviour
    {
        public IslandBuilder island;
        public GhostClipLibrary library;
        [Tooltip("Samples per second.")]
        public float rate = 30f;

        /// <summary>Editor: saves a finished clip as an asset (set by GhostClipTools). Null = write JSON.</summary>
        public static Action<GhostHandClip> Saved;

        public const string DeviceFolder = "ghostclips";

        public bool IsRecording { get; private set; }

        TextMeshPro _label;

        /// <summary>Records the next clip in <see cref="GhostClipLibrary.Wanted"/> that isn't recorded yet; its name, or null.</summary>
        public string RecordNextWanted()
        {
            if (IsRecording) return null;
            foreach (var w in GhostClipLibrary.Wanted)
            {
                if (library != null && library.Find(w.name) != null || File.Exists(DevicePath(w.name))) continue;
                Record(w.name, w.anchor, w.seconds, null);
                return w.name;
            }
            Debug.Log("[GhostClipRecorder] Every wanted clip is recorded.");
            return null;
        }

        public static string DevicePath(string name) => Path.Combine(Application.persistentDataPath, DeviceFolder, name + ".json");

        /// <summary>Records a clip after a countdown. <paramref name="hand"/> null = the right hand if tracked, else the left.</summary>
        public void Record(string clipName, GhostAnchor anchor, float seconds, Handedness? hand)
        {
            if (IsRecording)
            {
                Debug.LogWarning("[GhostClipRecorder] Already recording.");
                return;
            }
            StartCoroutine(Run(clipName, anchor, seconds, hand));
        }

        IEnumerator Run(string clipName, GhostAnchor anchor, float seconds, Handedness? which)
        {
            IsRecording = true;
            Debug.Log($"[GhostClipRecorder] {clipName}: get ready ({anchor}, {seconds:0.#} s).");
            for (int n = 3; n > 0; n--)
            {
                ShowLabel($"{clipName}\n{n}");
                yield return new WaitForSeconds(1f);
            }
            var hand = PickHand(which);
            if (hand == null)
            {
                ShowLabel($"{clipName}\nno hand!");
                Debug.LogError("[GhostClipRecorder] No tracked hand: nothing recorded.");
                yield return new WaitForSeconds(1.5f);
                ShowLabel(null);
                IsRecording = false;
                yield break;
            }
            ShowLabel($"{clipName}\nREC");

            var clip = ScriptableObject.CreateInstance<GhostHandClip>();
            clip.name = clip.clipName = clipName;
            clip.leftHand = hand.Handedness == Handedness.Left;
            clip.anchor = anchor;
            clip.fps = rate;
            clip.keyJoint = clipName == "poke" ? HandJointId.HandIndexTip : HandJointId.HandPalm;
            clip.keyTime = 0.4f;
            clip.gazeDotUntil = clipName == "pinch" || clipName == "grab_drop" ? 0.2f : 0f;
            if (clipName == "pinch") clip.placeOffset = new Vector3(0.07f, -0.05f, -0.05f);

            Pose frame = default;
            bool haveFrame = false;
            float step = 1f / Mathf.Max(1f, rate);
            int dropped = 0;
            for (float t = 0f; t <= seconds + 1e-4f; t += step)
            {
                if (hand.IsTrackedDataValid && hand.GetRootPose(out var root) && hand.GetJointPosesLocal(out var local))
                {
                    if (!haveFrame)
                    {
                        frame = anchor == GhostAnchor.Wrist ? root : new Pose(root.position, ViewYaw());
                        haveFrame = true;
                    }
                    var inv = Quaternion.Inverse(frame.rotation);
                    var f = new GhostFrame
                    {
                        rootPos = inv * (root.position - frame.position),
                        rootRot = inv * root.rotation,
                        rot = new Quaternion[(int)HandJointId.HandEnd],
                        pos = new Vector3[(int)HandJointId.HandEnd],
                    };
                    for (int i = 0; i < f.rot.Length; i++)
                    {
                        f.rot[i] = local[i].rotation;
                        f.pos[i] = local[i].position;
                    }
                    clip.frames.Add(f);
                }
                else if (clip.frames.Count > 0)
                {
                    // Tracking dropped for a moment: repeat the last pose so the timing holds.
                    clip.frames.Add(clip.frames[clip.frames.Count - 1]);
                    dropped++;
                }
                yield return new WaitForSeconds(step);
            }
            ShowLabel(null);
            IsRecording = false;
            if (clip.frames.Count < 2)
            {
                Debug.LogError($"[GhostClipRecorder] {clipName}: the hand wasn't tracked; nothing saved.");
                Destroy(clip);
                yield break;
            }
            Debug.Log($"[GhostClipRecorder] {clipName}: {clip.frames.Count} frames ({clip.RawDuration:0.0} s, {(clip.leftHand ? "left" : "right")} hand" +
                      $"{(dropped > 0 ? $", {dropped} untracked frames held" : "")}).");
            if (Saved != null)
            {
                Saved(clip);
                yield break;
            }
            try
            {
                var path = DevicePath(clipName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(clip));
                Debug.Log($"[GhostClipRecorder] Wrote {path}. Pull: adb pull /sdcard/Android/data/{Application.identifier}/files/{DeviceFolder}");
                if (library != null)
                {
                    // Used at once (until the app restarts); the Editor import makes it permanent.
                    library.clips.RemoveAll(c => c != null && c.clipName == clipName);
                    library.clips.Add(clip);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GhostClipRecorder] Couldn't write {clipName}: {e.Message}");
            }
        }

        Hand PickHand(Handedness? which)
        {
            var right = HandRegistry.Get(Handedness.Right);
            var left = HandRegistry.Get(Handedness.Left);
            if (which == Handedness.Left) return left;
            if (which == Handedness.Right) return right;
            return right != null && right.IsTrackedDataValid ? right : left != null && left.IsTrackedDataValid ? left : null;
        }

        /// <summary>The clip frame: x right, y up, z away from the player (the island's back; its front faces the player).</summary>
        Quaternion ViewYaw()
        {
            if (island == null) return Quaternion.identity;
            Vector3 fwd = -island.transform.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(fwd, Vector3.up) : Quaternion.identity;
        }

        void ShowLabel(string text)
        {
            if (text == null)
            {
                if (_label != null) _label.gameObject.SetActive(false);
                return;
            }
            if (_label == null)
            {
                var go = new GameObject("Ghost Clip Countdown");
                go.transform.SetParent(transform, false);
                _label = go.AddComponent<TextMeshPro>();
                _label.fontSize = 0.6f;
                _label.alignment = TextAlignmentOptions.Center;
                _label.color = new Color(0.85f, 0.95f, 1f);
                _label.rectTransform.sizeDelta = new Vector2(0.4f, 0.2f);
            }
            _label.gameObject.SetActive(true);
            _label.text = text;
            var cam = Camera.main;
            if (cam == null) return;
            var fwd = cam.transform.forward;
            _label.transform.position = cam.transform.position + fwd * 0.6f + Vector3.up * 0.12f;
            _label.transform.rotation = Quaternion.LookRotation(_label.transform.position - cam.transform.position, Vector3.up);
        }
    }
}
