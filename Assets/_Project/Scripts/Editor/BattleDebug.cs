using System.IO;
using UnityEditor;
using UnityEngine;

namespace Diorama.EditorTools
{
    /// <summary>Play mode helpers for the Claude bridge (exec:Diorama.EditorTools.BattleDebug.Grass, ...).</summary>
    static class BattleDebug
    {
        const string ShotDir = "Logs/BattleShots";
        static int _shot;

        static EncounterDirector Director => Object.FindFirstObjectByType<EncounterDirector>();
        static BattleRunes Runes => Object.FindFirstObjectByType<BattleRunes>(FindObjectsInactive.Include);

        public static void Grass() => Start(Habitat.Grass);
        public static void Water() => Start(Habitat.Water);
        public static void Move1() => Choose(0);
        public static void Move2() => Choose(1);
        public static void Move3() => Choose(2);
        public static void Run() => Choose(BattleRunes.RunIndex);
        public static void Catch() => ThrowCatchPod();

        static GrowthDirector Growth => Object.FindFirstObjectByType<GrowthDirector>();

        /// <summary>Like key X: +50 XP to the lead (outside battles).</summary>
        public static void AddXp50()
        {
            var g = Growth;
            if (g != null) g.AddXpToLead(50);
        }

        /// <summary>Like key L: the lead levels up.</summary>
        public static void LevelUp()
        {
            var g = Growth;
            if (g != null) g.LevelUpLead();
        }

        /// <summary>The lead one XP short of its next level (the next win levels it up on the pad).</summary>
        public static void LeadAlmostLevel()
        {
            var party = Object.FindFirstObjectByType<Party>();
            var d = Director;
            var c = party != null ? party.Lead : null;
            if (c == null || d == null || d.progression == null) return;
            c.xp = d.progression.XpToNext(c.level) - 1;
            Debug.Log($"[BattleDebug] {c.Name} Lv {c.level}, XP {c.xp}/{d.progression.XpToNext(c.level)}.");
        }

        /// <summary>Scene swap into the Hearth Lodge (no shrine needed).</summary>
        public static void EnterLodge()
        {
            var s = Object.FindFirstObjectByType<SceneSwap>();
            var lodge = s != null ? s.FindInterior("Lodge") : null;
            if (lodge == null) { Debug.LogWarning("[BattleDebug] No SceneSwap or Lodge interior."); return; }
            Debug.Log($"[BattleDebug] Enter lodge: {s.Enter(lodge, lodge.startTile)}");
        }

        /// <summary>Scene swap back outside.</summary>
        public static void LeaveLodge()
        {
            var s = Object.FindFirstObjectByType<SceneSwap>();
            if (s != null) Debug.Log($"[BattleDebug] Leave lodge: {s.Return()}");
        }

        static LodgeRoom Room => Object.FindFirstObjectByType<LodgeRoom>();

        /// <summary>Lodge: the first room Isling hops onto the pedestal (its card shows).</summary>
        public static void LodgeInspect()
        {
            var r = Room;
            if (r != null) r.Inspect(r.First);
        }

        /// <summary>Lodge: the pedestal Isling swaps with rim pod 2 (look + pinch path).</summary>
        public static void LodgeSwapPod2()
        {
            var r = Room;
            if (r != null) Debug.Log($"[BattleDebug] Pedestal swap with pod 2: {r.DebugPedestalSwap(1)}");
        }

        /// <summary>Lodge: rim pod 2 dropped on the floor (into the room).</summary>
        public static void LodgeDropPod2()
        {
            var r = Room;
            if (r != null) r.DebugDropRimPod(1);
        }

        /// <summary>Lodge: rim pod 1 dropped on the floor (refused when it's the last one).</summary>
        public static void LodgeDropPod1()
        {
            var r = Room;
            if (r != null) r.DebugDropRimPod(0);
        }

        /// <summary>
        /// Rough per-eye draw cost right now: the island's instanced draws plus every enabled scene renderer's material
        /// passes (a Toon material's own Outline pass counts as a second draw), and the triangles they submit.
        /// </summary>
        public static void DrawCost()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            int draws = island != null ? island.DrawCallCount : 0, rendererDraws = 0;
            long tris = 0;
            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer) { rendererDraws++; continue; }
                var mf = r.GetComponent<MeshFilter>();
                var mesh = mf != null ? mf.sharedMesh : r is SkinnedMeshRenderer smr ? smr.sharedMesh : null;
                int mats = 0;
                foreach (var m in r.sharedMaterials)
                    if (m != null) mats += m.passCount > 1 && m.GetShaderPassEnabled("SRPDefaultUnlit") && m.FindPass("Outline") >= 0 ? 2 : 1;
                rendererDraws += mats;
                if (mesh != null) tris += (long)mesh.triangles.Length / 3 * mats;
            }
            Debug.Log($"[BattleDebug] Draw cost: island {draws} instanced draws ({(island != null ? island.InstanceTotal : 0)} instances), " +
                      $"{rendererDraws} renderer draws ({tris / 1000}k tris in plain renderers), inside: {SceneSwap.IsInside}.");
        }

        /// <summary>Lodge: the first honeycomb sleeper comes out.</summary>
        public static void LodgeWake()
        {
            var r = Room;
            if (r != null) r.Wake(0);
        }

        /// <summary>Like key K: save now.</summary>
        public static void SaveNow()
        {
            var s = Object.FindFirstObjectByType<GameSave>();
            if (s != null) s.Save("BattleDebug", true);
        }

        /// <summary>Like Shift+K: delete the save.</summary>
        public static void DeleteSave()
        {
            var s = Object.FindFirstObjectByType<GameSave>();
            if (s != null) s.DeleteSave();
            else SaveGame.Delete(GameSave.SavePath);
        }

        /// <summary>Like key P: pause / resume.</summary>
        public static void Pause() => SetPause(true);
        public static void Resume() => SetPause(false);

        static void SetPause(bool on)
        {
            var p = Object.FindFirstObjectByType<GamePause>();
            if (p != null) p.SetPaused(on);
        }

        /// <summary>Walks the explorer to the nearest Hearth Shrine's front tile (it rests there).</summary>
        public static void GoToShrine()
        {
            var s = Object.FindFirstObjectByType<HearthShrines>();
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (s == null || e == null || s.Shrines.Count == 0) return;
            var best = s.Shrines[0];
            foreach (var t in s.Shrines)
                if ((t - e.Tile).sqrMagnitude < (best - e.Tile).sqrMagnitude) best = t;
            var front = best + new Vector2Int(0, 2);
            Debug.Log($"[BattleDebug] GoTo shrine front c{front.x} r{front.y}: {e.GoTo(front)}");
        }

        /// <summary>Like look + pinch on the nearest shrine: walks next to it if needed, then goes into the lodge.</summary>
        public static void PickShrine()
        {
            var s = Object.FindFirstObjectByType<HearthShrines>();
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (s == null || e == null || s.Shrines.Count == 0) return;
            int best = 0;
            for (int i = 1; i < s.Shrines.Count; i++)
                if ((s.Shrines[i] - e.Tile).sqrMagnitude < (s.Shrines[best] - e.Tile).sqrMagnitude) best = i;
            s.Pick(best, "BattleDebug");
        }

        /// <summary>Puts 14 level 4 creatures from the grass table in storage (two shelf pages).</summary>
        public static void FillStorage()
        {
            var party = Object.FindFirstObjectByType<Party>();
            var d = Director;
            if (party == null || d == null || d.grassTable == null) return;
            var rnd = new SeededRandom(11);
            for (int i = 0; i < 14 && d.grassTable.Roll(rnd, out var c, out _); i++)
            {
                c.level = 4;
                c.Heal();
                party.storage.Add(c);
            }
            party.NotifyChanged();
            Debug.Log($"[BattleDebug] Storage now {party.storage.Count}.");
        }

        /// <summary>Wild HP to 20% (in the red: the cradle pod glows and has a real chance).</summary>
        public static void WildToRed() => WildHp(0.2f);

        /// <summary>Wild HP to 1 (the next hit makes it dazed).</summary>
        public static void WildToOne() => WildHp(0f);

        static void WildHp(float share)
        {
            var d = Director;
            var b = d != null ? d.CurrentBattle : null;
            if (b == null) { Debug.LogWarning("[BattleDebug] No battle running."); return; }
            b.wild.isling.currentHp = Mathf.Max(1, Mathf.FloorToInt(b.wild.MaxHp * share));
            Debug.Log($"[BattleDebug] Wild {b.wild.isling.Name} HP {b.wild.Hp}/{b.wild.MaxHp}.");
        }

        /// <summary>Look + pinch on the cradle pod: an aimed catch throw.</summary>
        public static void ThrowCatchPod() => Pick(PodRack.CradleSlot);

        /// <summary>Look + pinch on the next party pod that can fight: a switch (or the send-out).</summary>
        public static void SwitchNext()
        {
            var party = Object.FindFirstObjectByType<Party>();
            var rack = Object.FindFirstObjectByType<PodRack>();
            if (party == null || rack == null) return;
            Pick(party.NextFighter(rack.ActiveSlot));
        }

        /// <summary>Edit mode: builds a pod card for every species and logs whether the name fits (panel width, name size).</summary>
        public static void CardFit()
        {
            // Building the card marks the open scene dirty, and a later test run then stops on a "save?" dialog:
            // a clean scene is saved again afterwards (nothing in it changed once the card is gone).
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool wasClean = !scene.isDirty;
            var hud = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Battle/Hud.mat");
            var quad = BattleHud.VerticalQuad();
            var card = new PodCard(hud, quad);
            foreach (var guid in AssetDatabase.FindAssets("t:IslingSpecies"))
            {
                var species = AssetDatabase.LoadAssetAtPath<IslingSpecies>(AssetDatabase.GUIDToAssetPath(guid));
                card.Show(new IslingInstance(species, 12), Vector3.zero, 1f);
                Debug.Log($"[CardFit] {species.displayName}: {card.Describe()}");
            }
            var root = GameObject.Find("Pod Card");
            if (root != null) Object.DestroyImmediate(root);
            Object.DestroyImmediate(quad);
            if (wasClean && !Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        }

        static void Pick(int slot)
        {
            var d = Director;
            if (d != null) d.DebugPick(slot);
        }

        /// <summary>Fills the empty pods with level 4 creatures from the grass table (party-full testing).</summary>
        public static void FillParty()
        {
            var party = Object.FindFirstObjectByType<Party>();
            var d = Director;
            if (party == null || d == null || d.grassTable == null) return;
            var rnd = new SeededRandom(5);
            while (!party.IsFull && d.grassTable.Roll(rnd, out var c, out _))
            {
                c.level = 4;
                c.Heal();
                party.Add(c);
            }
            Debug.Log($"[BattleDebug] Party now {party.members.Count}/{Party.Size}.");
        }

        /// <summary>Every pod creature but the first faints (losing / forced switch testing).</summary>
        public static void FaintMostOfParty()
        {
            var party = Object.FindFirstObjectByType<Party>();
            if (party == null) return;
            for (int i = 1; i < party.members.Count; i++) party.members[i].currentHp = 0;
            Debug.Log("[BattleDebug] Pods 2+ fainted.");
        }

        static void Start(Habitat habitat)
        {
            var d = Director;
            if (d != null) d.StartEncounter(habitat);
            else Debug.LogError("[BattleDebug] No EncounterDirector (Play mode in MR_Main?).");
        }

        static void Choose(int index)
        {
            var r = Runes;
            if (r != null) r.Choose(index);
        }

        /// <summary>
        /// Flat Play has no head pose, so the camera sits at the floor. This holds the main camera where a seated
        /// player's eyes would be (0.55 m above the island, 0.45 m back), looking at the island, until Play stops.
        /// </summary>
        public static void SeatCamera()
        {
            EditorApplication.update -= HoldCamera;
            EditorApplication.update += HoldCamera;
            HoldCamera();
        }

        static void HoldCamera()
        {
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (!EditorApplication.isPlaying || cam == null || island == null)
            {
                EditorApplication.update -= HoldCamera;
                return;
            }
            var t = island.transform;
            cam.transform.position = t.position + t.rotation * new Vector3(0f, 0.55f, 0.45f);
            var rack = _lookPod >= 0 ? Object.FindFirstObjectByType<PodRack>() : null;
            var pod = rack != null ? rack.Pod(_lookPod) : null;
            Vector3 look = pod != null ? pod.transform.position : t.position + t.rotation * new Vector3(0f, 0f, 0.05f);
            // The gaze ray pitches down from head forward: aim the head that much higher so the ray hits the target.
            var gaze = Object.FindFirstObjectByType<HeadGaze>();
            cam.transform.LookAt(look);
            if (pod != null && gaze != null) cam.transform.Rotate(-gaze.CurrentOffset, 0f, 0f, Space.Self);
        }

        static int _lookPod = -1;

        /// <summary>Seated camera aims its gaze at rim pod 2 (or back at the island): tests the gaze-grow.</summary>
        public static void LookAtPod2() { _lookPod = 1; SeatCamera(); }
        public static void LookAtIsland() { _lookPod = -1; SeatCamera(); }

        /// <summary>A shot every 0.12 s for 2.4 s (strip_00..19), to see effects mid-flight.</summary>
        public static void Strip()
        {
            _stripStart = EditorApplication.timeSinceStartup;
            _stripCount = 0;
            EditorApplication.update -= StripTick;
            EditorApplication.update += StripTick;
        }

        static double _stripStart;
        static int _stripCount;

        static void StripTick()
        {
            if (!EditorApplication.isPlaying || _stripCount >= 20)
            {
                EditorApplication.update -= StripTick;
                return;
            }
            if (EditorApplication.timeSinceStartup - _stripStart < _stripCount * 0.12) return;
            Render($"{ShotDir}/strip_{_stripCount++:00}.png");
        }

        /// <summary>
        /// Fires a long Spirit line for shots. The view clamp follows the real head (a Link headset on the desk points
        /// anywhere), so it's switched off here.
        /// </summary>
        public static void SayTest()
        {
            var bubble = Object.FindFirstObjectByType<DialogueBubble>();
            if (bubble != null) bubble.maxViewAngle = 180f;
            Dialogue.Say("intro.explorer");
        }

        /// <summary>Fires a targeted line (ignoring once) to see the gold focus, beam, Spirit and bubble placement.</summary>
        public static void SayPedestal() => SayForced("lodge.swap.first");
        public static void SayLeadPod() => SayForced("lodge.last");
        public static void SayShrine() => SayForced("shrine.first");

        static void SayForced(string id)
        {
            var bubble = Object.FindFirstObjectByType<DialogueBubble>();
            if (bubble != null) bubble.maxViewAngle = 180f;
            var spirit = Object.FindFirstObjectByType<LanternSpirit>();
            if (spirit != null) spirit.fovHalfAngle = 180f;
            var cam = Camera.main;
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (cam != null && island != null)
                Debug.Log($"[BattleDebug] Head in island space: {island.transform.InverseTransformPoint(cam.transform.position)}, forward {island.transform.InverseTransformDirection(cam.transform.forward)}.");
            var d = Object.FindFirstObjectByType<Dialogue>();
            if (d == null) return;
            d.ClearAll();
            d.Fire(id, true);
        }

        /// <summary>Realm restoration at once (no fade), for shots of both states.</summary>
        public static void Unrestored() => RealmState.Restored = 0f;
        public static void Restored() => RealmState.Restored = 1f;

        /// <summary>Seated view and a close view of the island, rendered from island-relative poses (works with a headset on Link, which owns the main camera).</summary>
        public static void IslandShots()
        {
            var island = Object.FindFirstObjectByType<IslandBuilder>();
            if (island == null) return;
            var t = island.transform;
            foreach (var (name, eye, look) in new[]
                     {
                         ("seated", new Vector3(0f, 0.55f, 0.45f), new Vector3(0f, 0f, 0.05f)),
                         ("close", new Vector3(0f, 0.26f, 0.26f), new Vector3(0f, 0f, -0.02f)),
                     })
            {
                var pos = t.position + t.rotation * eye;
                var rot = Quaternion.LookRotation(t.position + t.rotation * look - pos, Vector3.up);
                string path = $"{ShotDir}/island_{name}_{(RealmState.Restored > 0.5f ? "restored" : "unrestored")}.png";
                Render(path, pos, rot);
                Debug.Log($"[BattleDebug] Screenshot {path}");
            }
        }

        /// <summary>Runs the island placer's recenter path (as if the Quest recentered).</summary>
        public static void Recenter()
        {
            var p = Object.FindFirstObjectByType<IslandPlacer>();
            if (p != null) p.SendMessage("OnRecentered");
        }

        /// <summary>Turns the island 60 degrees away from the head, to test idle facing.</summary>
        public static void TurnIsland()
        {
            var i = Object.FindFirstObjectByType<IslandBuilder>();
            if (i != null) i.transform.rotation *= Quaternion.Euler(0f, 60f, 0f);
        }

        /// <summary>GoTo the farthest reachable tile within 6 steps (tests pathfinding in Play mode).</summary>
        public static void GoTo()
        {
            var e = Object.FindFirstObjectByType<GestureTestPiece>();
            if (e == null) return;
            Vector2Int best = e.Tile;
            for (int dx = -6; dx <= 6; dx++)
                for (int dy = -6; dy <= 6; dy++)
                {
                    var t = e.Tile + new Vector2Int(dx, dy);
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > Mathf.Abs(best.x - e.Tile.x) + Mathf.Abs(best.y - e.Tile.y) && e.CanReach(t)) best = t;
                }
            Debug.Log($"[BattleDebug] GoTo c{best.x} r{best.y}: {e.GoTo(best)}");
        }

        public static void Move1Strip() { Choose(0); Strip(); }
        public static void Move2Strip() { Choose(1); Strip(); }
        public static void Move3Strip() { Choose(2); Strip(); }

        /// <summary>Renders the main camera's view (narrower lens, bigger image) to Logs/BattleShots.</summary>
        /// <summary>Logs every PokeInteractor with its path and which object on the path is switched off.</summary>
        public static void ListPoke()
        {
            foreach (var p in Object.FindObjectsByType<Oculus.Interaction.PokeInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var path = new System.Text.StringBuilder();
                for (var t = p.transform; t != null; t = t.parent)
                    path.Insert(0, "/" + t.name + (t.gameObject.activeSelf ? "" : "[OFF]"));
                Debug.Log($"[BattleDebug] {path} enabled={p.enabled}");
            }
        }

        public static void Shot()
        {
            var main = Camera.main;
            if (main == null) return;
            string path = $"{ShotDir}/battle_{_shot++:00}.png";
            Render(path);
            Debug.Log($"[BattleDebug] Screenshot {path} (camera at {main.transform.position:F2})");
        }

        static void Render(string path)
        {
            var main = Camera.main;
            if (main == null) return;
            Render(path, main.transform.position, main.transform.rotation);
        }

        static void Render(string path, Vector3 position, Quaternion rotation)
        {
            Directory.CreateDirectory(ShotDir);
            var go = new GameObject("Shot Camera");
            var cam = go.AddComponent<Camera>();
            // Never an XR camera: with Link running, every new stereo camera takes an XR reference space and the
            // runtime crashed Unity once the limit was reached (Oct 24).
            cam.stereoTargetEye = StereoTargetEyeMask.None;
            cam.transform.SetPositionAndRotation(position, rotation);
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.01f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.19f);
            var rt = new RenderTexture(1280, 900, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            RenderTexture.active = null;
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(go);
        }
    }
}
