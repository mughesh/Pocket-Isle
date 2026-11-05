using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 16: a small menu tablet on the near rim (a "≡" stone), front-left of the pods and past the lodge's Journal
    /// tablet: poke it (or look at it and pinch) and the pause menu opens. The left-hand menu gesture still works; this is
    /// for players who never find it (the judges' video). Made at runtime by <see cref="GameMenu"/>; hidden during the
    /// opening, the title, scene swaps, the finale and while a menu or board is open.
    /// </summary>
    public class MenuStone : MonoBehaviour
    {
        GameMenu _menu;
        PodRack _pods;
        TabletKit.View _slab;
        float _cooldown, _hover;
        readonly bool[] _wasPinching = { true, true };
        bool _spotLogged;

        /// <summary>World metres of the stone at island scale 0.6 (it grows with the island).</summary>
        public static readonly Vector2 Size = new Vector2(0.03f, 0.03f);
        /// <summary>The lodge Journal tablet's width (LodgeJournalBook.Size.x), kept free next to the pods.</summary>
        const float JournalWidth = 0.05f;
        /// <summary>Clear gap to the socket band and the Journal tablet (island-local m).</summary>
        const float RimGap = 0.016f;

        IslandBuilder Island => _menu != null ? _menu.island : null;

        public static MenuStone Ensure(GameMenu menu)
        {
            if (menu == null || menu.island == null) return null;
            var existing = menu.island.GetComponentInChildren<MenuStone>(true);
            if (existing != null) return existing;
            var go = new GameObject("Menu Stone");
            go.layer = menu.island.gameObject.layer;
            go.transform.SetParent(menu.island.transform, false);
            var s = go.AddComponent<MenuStone>();
            s._menu = menu;
            return s;
        }

        void Build()
        {
            var mat = _menu.runes != null ? _menu.runes.tabletMaterial : null;
            if (mat == null) return;
            _slab = TabletKit.Make(transform, mat, Size, TabletKit.GoldInlay);
            var go = new GameObject("Icon");
            go.layer = gameObject.layer;
            go.transform.SetParent(_slab.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, TabletKit.LabelZ);
            go.transform.localScale = Vector3.one * (Size.x / 0.03f);
            go.AddComponent<MeshFilter>().sharedMesh = TabletKit.MenuBars();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var block = new MaterialPropertyBlock();
            block.SetVector("_Size", Vector4.zero);
            block.SetVector("_State", Vector4.zero);
            var c = TabletKit.LabelColor;
            block.SetColor("_Inlay", new Color(c.r, c.g, c.b, 1f));
            mr.SetPropertyBlock(block);
        }

        public static bool Shown => !GameMenu.IsOpen && !Opening.InProgress && !TitleScreen.Showing && !SceneSwap.IsBusy &&
                                    !FinaleDirector.Running && !JournalBoard.IsOpen && !SettingsBoard.IsOpen;

        void LateUpdate()
        {
            if (Island == null || Island.map == null) return;
            if (_pods == null) _pods = FindFirstObjectByType<PodRack>();
            bool shown = Shown;
            if (_slab == null && shown) Build();
            if (_slab != null && _slab.renderer.gameObject.activeSelf != shown) _slab.renderer.gameObject.SetActive(shown);
            if (!shown || _slab == null) return;
            // Like the Journal tablet: on the near rim at the pods' height, face toward the player, top leaning back.
            transform.localPosition = RimSpot();
            transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(15f, 0f, 0f);
            transform.localScale = Vector3.one / 0.6f;
            _slab.Paint(_hover, 0f, 0f, 0f);
            Input();
        }

        void Input()
        {
            if (Time.time < _cooldown) return;
            Vector3 c = _slab.transform.position;
            float r = Size.x * 0.6f * Island.transform.lossyScale.x / 0.6f;
            bool gazed = HeadGaze.TryGetRay(out var ray) && Vector3.Cross(ray.direction, c - ray.origin).magnitude < r * 1.2f && Vector3.Dot(ray.direction, c - ray.origin) > 0f;
            _hover = Mathf.MoveTowards(_hover, gazed ? 1f : 0f, Time.deltaTime * 6f);
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                bool tracked = HandLookup.TryGetJoint(hand, HandJointId.HandIndexTip, out var tip);
                bool pinching = tracked && hand.GetIndexFingerIsPinching();
                bool busy = _menu.handle != null && _menu.handle.IsHandBusy(h == 0);
                if (tracked && !busy && (tip - c).magnitude < r) Open("poke");
                else if (pinching && !_wasPinching[h] && gazed && !busy) Open("look + pinch");
                _wasPinching[h] = pinching;
            }
        }

        /// <summary>Opens the pause menu (also the bridge).</summary>
        public void Open(string how)
        {
            if (GameMenu.IsOpen) return;
            _cooldown = Time.time + 1f;
            GameAudio.At(AudioIds.UiPebble, _slab != null ? _slab.transform.position : transform.position);
            Debug.Log($"[MenuStone] The menu stone ({how}).");
            _menu.TryOpen();
        }

        /// <summary>
        /// Island-local: on the player's left of the pods, past the room the lodge's Journal tablet takes there (so the two
        /// never overlap, inside or out); on the right of the pods when the left has no room.
        /// </summary>
        Vector3 RimSpot()
        {
            var island = Island;
            if (_pods == null) return new Vector3(island.FootprintHalfSize.x * 0.7f, island.CapTopLocal, island.FootprintHalfSize.y);
            var home = _pods.HomeLocal(0);
            if (!_pods.SocketBandLocal(out float minX, out float maxX, out _)) return home;
            float hw = Size.x / 0.6f * 0.5f, journal = JournalWidth / 0.6f;
            float x = maxX + RimGap + journal + RimGap + hw;
            bool right = x + hw > island.FootprintHalfSize.x;
            if (right) x = minX - RimGap - hw;
            if (!_spotLogged)
            {
                _spotLogged = true;
                Debug.Log($"[MenuStone] On the rim {(right ? "right" : "left")} of the pods{(right ? "" : " and the Journal's spot")}: x {x:0.000} " +
                          $"(socket band {minX:0.000}..{maxX:0.000}, island half width {island.FootprintHalfSize.x:0.000}).");
            }
            return new Vector3(x, home.y, home.z);
        }

        /// <summary>The stone's span along the rim (island-local x min / max), for the layout check.</summary>
        public Vector2 RimSpan()
        {
            float hw = Size.x / 0.6f * 0.5f, x = RimSpot().x;
            return new Vector2(x - hw, x + hw);
        }
    }
}
