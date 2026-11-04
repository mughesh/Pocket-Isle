using System;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14h: a two-tablet question over the near rim ("Go in" / "Not yet" at a temple door), on its own board made at
    /// runtime beside the game menu's (its settings copied). The explorer waits until a tablet is picked; the game menu
    /// opening closes it as "no".
    /// </summary>
    public class ChoiceBoard : MonoBehaviour
    {
        RuneBoard _runes;
        Action _yes, _no;
        GestureTestPiece _explorer;
        bool _wasLocked;

        public static ChoiceBoard Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance._yes != null;

        static ChoiceBoard Ensure()
        {
            if (Instance != null) return Instance;
            var menu = FindFirstObjectByType<GameMenu>();
            var template = menu != null ? menu.runes : null;
            if (template == null)
            {
                Debug.LogError("[ChoiceBoard] No game menu board to copy: the question can't show.");
                return null;
            }
            var go = new GameObject("Choice Board");
            go.SetActive(false);
            go.transform.SetParent(template.transform.parent, false);
            go.transform.SetLocalPositionAndRotation(template.transform.localPosition, template.transform.localRotation);
            go.layer = template.gameObject.layer;
            var board = go.AddComponent<RuneBoard>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), board);
            var c = go.AddComponent<ChoiceBoard>();
            c._runes = board;
            c._explorer = menu.explorer;
            go.SetActive(true);
            return c;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows <paramref name="yes"/> (primary, gold) and <paramref name="no"/>; false when it can't show.</summary>
        public static bool Ask(string yes, Action onYes, string no, Action onNo)
        {
            var c = Ensure();
            if (c == null || IsOpen) return false;
            c._yes = onYes ?? (() => { });
            c._no = onNo;
            var size = new Vector2(0.072f, 0.034f);
            c._runes.Clear();
            var y = c._runes.Add(yes, new Vector2(-0.042f, 0f), size, () => c.Pick(true));
            y.primary = true;
            c._runes.Add(no, new Vector2(0.042f, 0f), size, () => c.Pick(false));
            c._runes.DefaultFocus = y;
            c._runes.Show();
            if (c._explorer != null)
            {
                c._wasLocked = c._explorer.Locked;
                c._explorer.Locked = true;
            }
            Debug.Log($"[ChoiceBoard] Asking: {yes} / {no}.");
            return true;
        }

        void Pick(bool yes)
        {
            var a = yes ? _yes : _no;
            _yes = _no = null;
            _runes.Hide();
            _runes.Clear();
            if (_explorer != null && !_wasLocked) _explorer.Locked = false;
            Debug.Log($"[ChoiceBoard] Picked {(yes ? "yes" : "no")}.");
            a?.Invoke();
        }

        void Update()
        {
            if (IsOpen && GameMenu.IsOpen) Pick(false);
        }

        /// <summary>Bridge: picks a side.</summary>
        public static void DebugPick(bool yes)
        {
            if (IsOpen) Instance.Pick(yes);
        }
    }
}
