using System;
using System.Collections;
using Diorama.Audio;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Diorama
{
    /// <summary>
    /// Growth outside the battle flow. Step 14h: a party Isling that is <b>due</b> to evolve (any cause; never saved, see
    /// <see cref="Progression.NextDue"/>) makes its rim pod glow gold and pulse, and when calm the Spirit says
    /// <c>evolve.ready</c> (pinned, chip "Hold it"). The palm moment: grab the glowing pod and turn the hand palm up: the pod
    /// settles on the palm, opens, the Isling stands there and evolves with the full show, then curls back into its pod,
    /// which flies home. Fallbacks: look + pinch the glowing pod (the show floats over the island), or held 6 s without a
    /// palm up (the show runs over the hand). One at a time; never forced.
    /// Debug (Editor and development builds, also BattleDebug): X = +50 XP to the lead (outside battles; in battle X
    /// switches), L = the lead levels up.
    /// </summary>
    public class GrowthDirector : MonoBehaviour
    {
        public const string ReadyLine = "evolve.ready", FirstLine = "evolve.first", DoneLine = "evolve.done", Chip = "Hold it";

        public Party party;
        public PodRack pods;
        public EncounterDirector encounters;
        public ProgressionConfig progression;

        [Header("Step 14h: evolving in the palm")]
        [Tooltip("Pop text for the new name (found in the scene when empty).")]
        public BattleHud hud;
        [Tooltip("The Isling on the palm: this map zoom (about its battle size).")]
        public float palmZoom = 1.6f;
        public PalmUpTimer palmUp = new PalmUpTimer();
        [Tooltip("Held this long without a palm up: the show runs above the hand (s).")]
        public float palmTimeout = 6f;
        [Tooltip("The new form's stats card stays this long (s).")]
        public float cardSeconds = 3f;

        public static GrowthDirector Instance { get; private set; }

        /// <summary>The pod due to evolve now (glowing), or -1.</summary>
        public int Due { get; private set; } = -1;

        /// <summary>The evolution show is playing.</summary>
        public bool Showing { get; private set; }

        /// <summary>Bridge / tests: a synthetic hand holding the due pod at this world point (palm up when <see cref="debugPalmUp"/> ≥ 0.55).</summary>
        [NonSerialized] public Vector3? debugPalm;
        [NonSerialized] public float debugPalmUp = 1f;

        float _heldFor;
        int _loggedDue = -2;
        Func<Vector3> _anchor;
        Func<Vector3> _up;
        Palmed _old, _new;

        static readonly Color Gold = new Color(1f, 0.84f, 0.35f);

        /// <summary>A creature standing on the palm (not on the map: true size, scaled like a battle actor).</summary>
        sealed class Palmed
        {
            public GameObject go;
            public CreatureView view;
            public float grow;
        }

        void Awake() => Instance = this;

        void Start()
        {
            if (party == null || pods == null || encounters == null || progression == null)
            {
                Debug.LogError("[GrowthDirector] Needs the party, pod rack, encounter director and progression config " +
                               "(Tools > Pocket Isle > Set Up MR_Main (7 progression)).", this);
                enabled = false;
                return;
            }
            if (hud == null) hud = FindFirstObjectByType<BattleHud>();
            pods.DuePicked += OnDuePicked;
            Dialogue.RegisterTarget("evolvePod", () => Due >= 0 && !Showing ? pods.SlotWorld(Due) : null, k => { },
                () => pods.Pod(Mathf.Max(0, Due)) != null ? pods.Pod(Mathf.Max(0, Due)).WorldRadius * 1.3f : 0.02f);
        }

        void OnDestroy()
        {
            if (pods != null) pods.DuePicked -= OnDuePicked;
            if (Instance == this) Instance = null;
            Clear(ref _old);
            Clear(ref _new);
        }

        void OnDisable()
        {
            if (pods != null) pods.DueSlot = -1;
        }

        // ---- due ----

        /// <summary>Pods may glow and be grabbed for evolving: no battle, not the lodge, no finale, no scene change, no tutorial.</summary>
        bool Available => !encounters.InBattle && !pods.InBattle && !pods.LodgeMode && !FinaleDirector.Running && !SceneSwap.IsBusy &&
                          !Onboarding.Running && !GameMenu.IsOpen && !JournalBoard.IsOpen;

        /// <summary>The Spirit may start the "ready" line: available, no menu, no other lesson pinned.</summary>
        bool Calm => Available && !Dialogue.Held && (!Dialogue.IsPinned || Dialogue.CurrentObjective == ReadyLine);

        void Update()
        {
            DebugKeys();
            if (Showing) return;
            int due = Available ? Progression.NextDue(party.members) : -1;
            if (due != Due || _loggedDue == -2)
            {
                var c = party.At(due);
                if (due >= 0) Debug.Log($"[GrowthDirector] {c.Name} (pod {due + 1}, Lv {c.level}) is ready to evolve into {c.species.evolvesTo.displayName}: its pod glows.");
                else if (Due >= 0) Debug.Log("[GrowthDirector] No pod is ready to evolve now.");
                _loggedDue = due;
            }
            Due = due;
            pods.DueSlot = due;
            UpdateLine();
            if (due < 0)
            {
                _heldFor = 0f;
                palmUp.Reset();
                return;
            }
            bool held = pods.IsHeld(due) || debugPalm.HasValue;
            if (!held)
            {
                _heldFor = 0f;
                palmUp.Reset();
                return;
            }
            _heldFor += Time.deltaTime;
            bool tracked = ReadPalm(due, out float up, out var palm, out var normal);
            if (palmUp.Step(Time.deltaTime, tracked, up))
            {
                Debug.Log($"[GrowthDirector] Palm up with pod {due + 1}: it settles on the palm.");
                Begin(due, "palm", () => PalmPoint(due, palm), () => PalmNormal(due, normal));
            }
            else if (_heldFor >= palmTimeout)
            {
                Debug.Log($"[GrowthDirector] Pod {due + 1} held {palmTimeout:0} s without a palm up: the show runs above the hand.");
                Vector3 last = pods.Pod(due).transform.position;
                Begin(due, "above the hand", () => (PodHand(due, out var p) ? p : last) + Vector3.up * Lift(0.05f), () => Vector3.up);
            }
        }

        /// <summary>The ready line: pinned while a pod is due and it's calm; gone when nothing is due or a battle starts.</summary>
        void UpdateLine()
        {
            bool ours = Dialogue.CurrentObjective == ReadyLine;
            if (Due < 0 || encounters.InBattle)
            {
                if (ours) Dialogue.Unpin();
                return;
            }
            if (ours || !Calm || Dialogue.IsPinned) return;
            var c = party.At(Due);
            Dialogue.SetVar("name", c.Name);
            Dialogue.Pin(new[] { ReadyLine }, Chip);
            Dialogue.Repeat(ReadyLine);
        }

        void OnDuePicked(int slot)
        {
            if (Showing || slot != Due || pods.IsHeld(slot)) return;
            var isl = pods.island;
            var t = isl.transform;
            Debug.Log($"[GrowthDirector] Look + pinch on pod {slot + 1}: it floats over the island and evolves there.");
            // The middle of the window (its view centre), a little above the play surface.
            Begin(slot, "over the island", () => t.TransformPoint(isl.TileToLocal(isl.ViewCenterTile, isl.Content.capHeight)) + t.up * Lift(0.06f), () => t.up);
        }

        // ---- hands ----

        /// <summary>World metres at the island's scale.</summary>
        float Lift(float metres) => metres * Mathf.Max(0.3f, pods.island.transform.lossyScale.x / 0.6f);

        /// <summary>The hand holding pod <paramref name="slot"/> (the palm nearest it): its palm point.</summary>
        bool PodHand(int slot, out Vector3 palm)
        {
            palm = default;
            if (debugPalm.HasValue)
            {
                palm = debugPalm.Value;
                return true;
            }
            var pod = pods.Pod(slot);
            if (pod == null) return false;
            float best = 0.25f * 0.25f;
            bool found = false;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid || !HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var pose)) continue;
                float d = (pose.position - pod.transform.position).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                palm = pose.position;
                found = true;
            }
            return found;
        }

        /// <summary>The holding hand's palm: how far up it faces (palm normal · up), where it is and its normal.</summary>
        bool ReadPalm(int slot, out float up, out Vector3 palm, out Vector3 normal)
        {
            up = -1f;
            palm = default;
            normal = Vector3.up;
            if (debugPalm.HasValue)
            {
                up = debugPalmUp;
                palm = debugPalm.Value;
                normal = Vector3.up * Mathf.Sign(debugPalmUp);
                return true;
            }
            var pod = pods.Pod(slot);
            if (pod == null) return false;
            float best = 0.25f * 0.25f;
            bool found = false;
            for (int h = 0; h < 2; h++)
            {
                var hand = HandRegistry.Get(h == 0 ? Handedness.Left : Handedness.Right);
                if (hand == null || !hand.IsTrackedDataValid || !HandLookup.TryGetJointPose(hand, HandJointId.HandPalm, out var pose)) continue;
                float d = (pose.position - pod.transform.position).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                // The palmar side is the palm joint's -Y for both hands (ISDK v207, as the finale's palm).
                normal = pose.rotation * Vector3.down;
                up = Vector3.Dot(normal, Vector3.up);
                palm = pose.position;
                found = true;
            }
            return found;
        }

        /// <summary>On the palm (it follows the hand; the last point if tracking drops).</summary>
        Vector3 PalmPoint(int slot, Vector3 fallback)
        {
            if (ReadPalm(slot, out _, out var p, out var n)) _lastPalm = p + n * Lift(0.012f);
            else if (_lastPalm == default) _lastPalm = fallback;
            return _lastPalm;
        }

        Vector3 PalmNormal(int slot, Vector3 fallback)
        {
            if (ReadPalm(slot, out float up, out _, out var n) && up > 0.2f) _lastNormal = Vector3.Slerp(Vector3.up, n, 0.5f);
            else if (_lastNormal == default) _lastNormal = fallback.y > 0.2f ? fallback : Vector3.up;
            return _lastNormal;
        }

        Vector3 _lastPalm, _lastNormal;

        // ---- the show ----

        void Begin(int slot, string where, Func<Vector3> anchor, Func<Vector3> up)
        {
            if (Showing) return;
            _lastPalm = _lastNormal = default;
            palmUp.Reset();
            _heldFor = 0f;
            StartCoroutine(Show(slot, where, anchor, up));
        }

        IEnumerator Show(int slot, string where, Func<Vector3> anchor, Func<Vector3> up)
        {
            var c = party.At(slot);
            if (!Progression.CanEvolve(c)) yield break;
            var from = c.species;
            var to = from.evolvesTo;
            Showing = true;
            pods.DueSlot = -1;
            // The ready line goes; the first evolution gets its line.
            Dialogue.Unpin(Dialogue.HasSaid(FirstLine) ? null : FirstLine);
            if (!pods.Claim(slot) || from.Prefab == null || to.Prefab == null)
            {
                if (to.Prefab == null) Debug.LogError($"[GrowthDirector] {to.displayName} has no baked prefab; evolving without the show.");
                Progression.Evolve(c);
                party.NotifyChanged();
                Showing = false;
                yield break;
            }
            string oldName = c.Name;
            _anchor = anchor;
            _up = up;
            var pod = pods.Pod(slot);
            var fx = pods.fx;

            // 1. The pod settles on the palm (or floats to its spot).
            Vector3 start = pod.transform.position;
            yield return Tween(0.35f, p =>
            {
                if (pod != null) pod.transform.position = Vector3.Lerp(start, anchor() + up() * pod.WorldRadius, Smooth(p));
            });
            // 2. It opens: the Isling grows out of it while the pod shrinks away.
            GameAudio.At(AudioIds.PodOpen, anchor());
            _old = Spawn(from);
            Vector3 podScale = pod.BaseScale;
            if (fx != null) fx.Burst(anchor() + up() * pod.WorldRadius, 12, Color.white, Lift(0.03f), 0.4f, Lift(0.003f));
            yield return Tween(0.4f, p =>
            {
                if (pod != null)
                {
                    pod.transform.position = anchor() + up() * pod.WorldRadius;
                    pod.BaseScale = podScale * (1f - Smooth(p));
                }
                _old.grow = EaseOutBack(p);
            });
            if (pod != null) pod.gameObject.SetActive(false);
            _old.view.Hop();
            yield return Wait(0.5f);

            // 3. The full show: white, the forms swap faster and faster, a burst, the new form's cry and name.
            _new = Spawn(to);
            _new.grow = 0f;
            float same = _old.view.height / Mathf.Max(1e-4f, _new.view.height);
            var rise = AudioService.PlayLoop(AudioIds.EvoRise, _old.go.transform, 0.3f);
            yield return Tween(0.5f, p => _old.view.SetFlash(p));
            _new.view.SetFlash(1f);
            float[] holds = { 0.5f, 0.42f, 0.34f, 0.26f, 0.2f, 0.14f, 0.1f };
            bool showNew = false;
            foreach (float h in holds)
            {
                showNew = !showNew;
                _old.grow = showNew ? 0f : 1f;
                _new.grow = showNew ? same : 0f;
                yield return Wait(h);
            }
            _old.grow = 0f;
            _new.grow = same;
            Vector3 center = Center(_new);
            rise.Stop(0.1f);
            GameAudio.At(AudioIds.EvoBurst, center);
            GameAudio.Ui(AudioIds.JingleEvolve);
            MusicDirector.Jingle(3f);
            if (fx != null)
            {
                fx.Burst(center, 34, Color.white, Lift(0.07f), 0.6f, Lift(0.005f));
                fx.Burst(center, 24, Gold, Lift(0.05f), 0.8f, Lift(0.004f), Vector3.up * Lift(0.03f));
            }
            StartCoroutine(GoldRing());
            Progression.Evolve(c);
            party.NotifyChanged();
            Clear(ref _old);
            yield return Tween(0.4f, p =>
            {
                _new.grow = Mathf.Lerp(same, 1f, EaseOutBack(p));
                _new.view.SetFlash(1f - p);
            });
            _new.grow = 1f;
            _new.view.SetFlash(0f);
            string cry = AudioIds.Cry(SpeciesRegistry.IdOf(to));
            if (AudioService.Has(cry)) GameAudio.On(cry, _new.go.transform);
            _new.view.Hop();
            Vector3 top = anchor() + up() * (_new.view.height * Scale);
            if (hud != null) hud.PopText(hud.transform.InverseTransformPoint(top + up() * Lift(0.02f)), c.Name + "!", Gold, 1.4f, 1.8f);
            Debug.Log($"[GrowthDirector] {oldName} evolved into {to.displayName} {where} (Lv {c.level}, HP {c.currentHp}/{c.MaxHp}).");
            yield return Wait(0.6f);
            var nu = _new;
            pods.ShowCardFor(c, () => nu != null && nu.go != null ? anchor() + up() * (nu.view.height * Scale + Lift(0.015f)) + Vector3.up * PodCard.HalfHeight : (Vector3?)null, cardSeconds);
            yield return Wait(cardSeconds);

            // 4. It curls back into its pod, which flies home.
            if (pod != null)
            {
                pod.gameObject.SetActive(true);
                pod.Show(c);
            }
            yield return Tween(0.4f, p =>
            {
                _new.grow = 1f - Smooth(p);
                if (pod != null)
                {
                    pod.transform.position = anchor() + up() * pod.WorldRadius;
                    pod.BaseScale = podScale * Smooth(p);
                }
            });
            Clear(ref _new);
            _anchor = null;
            if (pod != null) pod.BaseScale = podScale;
            yield return pods.ReturnClaimed(slot);
            Dialogue.SetVar("name", c.Name);
            Dialogue.Say(DoneLine);
            Showing = false;
        }

        float Scale => palmZoom * pods.island.transform.lossyScale.x;

        Palmed Spawn(IslingSpecies s)
        {
            var go = Instantiate(s.Prefab);
            go.name = $"Evolving {s.displayName}";
            var v = go.GetComponent<CreatureView>();
            var a = new Palmed { go = go, view = v, grow = 0f };
            Place(a);
            return a;
        }

        Vector3 Center(Palmed a) => _anchor() + _up() * (a.view.centerHeight * Scale * a.grow);

        void LateUpdate()
        {
            if (_anchor == null) return;
            Place(_old);
            Place(_new);
        }

        /// <summary>Feet on the anchor, standing along its up, facing the player.</summary>
        void Place(Palmed a)
        {
            if (a == null || a.go == null || _anchor == null) return;
            Vector3 at = _anchor(), up = _up();
            var cam = Camera.main;
            Vector3 face = cam != null ? Vector3.ProjectOnPlane(cam.transform.position - at, up) : Vector3.forward;
            if (face.sqrMagnitude < 1e-6f) face = Vector3.ProjectOnPlane(Vector3.forward, up);
            a.go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(face.normalized, up));
            a.go.transform.localScale = Vector3.one * (Scale * Mathf.Max(0.0001f, a.grow));
        }

        static void Clear(ref Palmed a)
        {
            if (a != null && a.go != null) Destroy(a.go);
            a = null;
        }

        IEnumerator GoldRing()
        {
            var fx = pods.fx;
            if (fx == null || _new == null) yield break;
            var ring = fx.Get(BattleFx.Shape.Ring);
            if (ring == null) yield break;
            ring.Set(Color.white, Gold, 1f);
            var fxScale = fx.transform.lossyScale.x;
            yield return Tween(0.6f, p =>
            {
                if (_anchor == null) return;
                ring.transform.SetPositionAndRotation(_anchor() + _up() * Lift(0.003f), Quaternion.FromToRotation(Vector3.up, _up()));
                ring.transform.localScale = Vector3.one * (Lift(0.015f + 0.045f * p) / Mathf.Max(1e-4f, fxScale));
                ring.SetAlpha(1f - p);
            });
            fx.Release(ring);
        }

        // ---- debug ----

        void DebugKeys()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            var kb = Keyboard.current;
            if (kb == null || encounters.InBattle) return;
            if (kb.xKey.wasPressedThisFrame) AddXpToLead(50);
            if (kb.lKey.wasPressedThisFrame) LevelUpLead();
        }

        /// <summary>Debug: XP to the lead creature; any level-ups follow (a due evolution makes its pod glow).</summary>
        public void AddXpToLead(int amount)
        {
            var c = party.Lead;
            if (c == null || !enabled) return;
            int was = c.level;
            var ups = Progression.AddXp(c, amount, progression);
            Debug.Log($"[GrowthDirector] Debug: {c.Name} +{amount} XP{(ups.Count > 0 ? $", Lv {was} -> {c.level}" : "")} ({c.xp}/{progression.XpToNext(c.level)}).");
            party.NotifyChanged();
            EvolveDue();
        }

        /// <summary>Debug: exactly enough XP for the lead's next level.</summary>
        public void LevelUpLead()
        {
            var c = party.Lead;
            if (c == null || !enabled) return;
            if (c.level >= progression.levelCap)
            {
                Debug.Log($"[GrowthDirector] Debug: {c.Name} is at the level cap ({progression.levelCap}).");
                return;
            }
            AddXpToLead(progression.XpToNext(c.level) - c.xp);
        }

        /// <summary>
        /// Step 14h: re-checks at once which pod is due (the Dev panel and debug calls; the check also runs every frame).
        /// Nothing evolves by itself any more: the pod glows and waits for the player.
        /// </summary>
        public void EvolveDue()
        {
            _loggedDue = -2;
            if (isActiveAndEnabled) Update();
        }

        /// <summary>Bridge: evolve the due pod now as if looked at + pinched (false when none is due or a show is on).</summary>
        public bool DebugPinchDue()
        {
            if (Showing || Due < 0) return false;
            OnDuePicked(Due);
            return true;
        }

        // ---- helpers ----

        static IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime) yield return null;
        }

        static IEnumerator Tween(float seconds, Action<float> step)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                step(t / seconds);
                yield return null;
            }
            step(1f);
        }

        static float Smooth(float p) => p * p * (3f - 2f * p);

        static float EaseOutBack(float p)
        {
            const float c = 1.7f;
            float q = p - 1f;
            return 1f + (c + 1f) * q * q * q + c * q * q;
        }
    }
}
