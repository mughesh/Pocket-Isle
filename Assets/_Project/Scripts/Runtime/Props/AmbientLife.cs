using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Diorama
{
    /// <summary>
    /// Overnight Nov 5: small life over the island, by realm, more when it's restored (RealmState.Restored) than faded:
    /// hub butterflies drifting from flower to flower, Cinderwood fireflies, a bird crossing Galewick now and then (restored),
    /// Lilymere dragonflies darting over the water and sparkles on it (restored). Galewick's seeds and Cinderwood's rising
    /// embers already come from RealmDrift. Island-local, only inside the window, never inside a building or during a battle.
    /// Two draws: the winged critters (one instanced mesh, <see cref="PolishConfig.critterMaterial"/>) and the dots (one
    /// particle system with the battle fx's dot material). Added to the island by itself; switch: PolishConfig.ambientLife.
    /// </summary>
    public class AmbientLife : MonoBehaviour
    {
        enum Kind { Butterfly, Dragonfly, Bird }

        struct Critter
        {
            public Kind kind;
            public Vector3 pos, target, vel; // island-local
            public float rest, phase, life, fade;
            public Color color;
        }

        IslandBuilder _island;
        WorldWindow _window;
        readonly List<Critter> _critters = new List<Critter>();
        Mesh _mesh;
        readonly Matrix4x4[] _matrices = new Matrix4x4[16];
        readonly Vector4[] _colors = new Vector4[16], _flaps = new Vector4[16];
        MaterialPropertyBlock _block;
        ParticleSystem _dots;
        float _nextBird, _dotClock;
        string _realm;
        Vector3 _anchor;
        bool _hasAnchor;

        static readonly int WingColorId = Shader.PropertyToID("_WingColor"), FlapId = Shader.PropertyToID("_Flap");

        /// <summary>Last frame's CPU time in ms (the perf check).</summary>
        public static float LastMs { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            Attach();
        }

        static void OnLoaded(Scene s, LoadSceneMode m) => Attach();

        static void Attach()
        {
            var island = FindFirstObjectByType<IslandBuilder>();
            if (island != null && island.GetComponent<AmbientLife>() == null) island.gameObject.AddComponent<AmbientLife>();
        }

        void Start()
        {
            _island = GetComponent<IslandBuilder>();
            _window = FindFirstObjectByType<WorldWindow>();
            _block = new MaterialPropertyBlock();
            _mesh = BuildMesh();
            BuildDots();
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }

        float Scale => _island != null ? _island.transform.lossyScale.x : 1f;
        float Tile => _island != null && _island.Content != null ? _island.Content.tileSize : 0.05f;

        bool Showing
        {
            get
            {
                if (_island == null || _island.world == null || !PolishConfig.AmbientOn) return false;
                if (SceneSwap.IsInside || SceneSwap.IsBusy) return false;
                if (_window != null && _window.IsZoomed) return false; // battles zoom in
                return true;
            }
        }

        void Update()
        {
            float t0 = Time.realtimeSinceStartup;
            var cfg = PolishConfig.Get();
            string realm = Showing ? RealmOf(_island.world.name) : null;
            if (realm != _realm)
            {
                _realm = realm;
                _critters.Clear();
                if (_dots != null)
                {
                    _dots.Clear();
                    _dots.transform.localPosition = Vector3.zero;
                }
                _nextBird = Time.time + 4f;
                _hasAnchor = false;
            }
            if (realm != null) FollowMap();
            if (realm == null || cfg == null)
            {
                LastMs = (Time.realtimeSinceStartup - t0) * 1000f;
                return;
            }
            float restored = realm == "hub" ? 1f : Mathf.Clamp01(RealmState.Restored);
            float share = Mathf.Lerp(cfg.fadedShare, 1f, restored);
            switch (realm)
            {
                case "hub": Keep(Kind.Butterfly, Mathf.RoundToInt(cfg.hubButterflies * share)); break;
                case "lilymere": Keep(Kind.Dragonfly, Mathf.Max(1, Mathf.RoundToInt(cfg.lilymereDragonflies * share))); break;
                case "galewick":
                    if (restored > 0.5f && Time.time >= _nextBird)
                    {
                        _nextBird = Time.time + Random.Range(cfg.galewickBirdEvery.x, cfg.galewickBirdEvery.y);
                        SpawnBird();
                    }
                    break;
            }
            Move();
            Draw();
            Dots(realm, cfg, restored);
            LastMs = (Time.realtimeSinceStartup - t0) * 1000f;
        }

        /// <summary>
        /// Nov 5 fix: the map scrolls under the window when the explorer walks, so everything here moves with the ground
        /// (map tile (0,0)'s island-local spot), not with the window. Without this they rode along with the explorer.
        /// </summary>
        void FollowMap()
        {
            Vector3 anchor = _island.TileToLocal(Vector2.zero);
            Vector3 d = _hasAnchor ? anchor - _anchor : Vector3.zero;
            _anchor = anchor;
            _hasAnchor = true;
            d.y = 0f;
            if (d.sqrMagnitude < 1e-12f) return;
            for (int i = 0; i < _critters.Count; i++)
            {
                var c = _critters[i];
                c.pos += d;
                c.target += d;
                _critters[i] = c;
            }
            // The dots simulate in their own local space: move that space with the ground.
            if (_dots != null) _dots.transform.localPosition += d;
        }

        static string RealmOf(string map)
        {
            string m = map.ToLowerInvariant();
            if (m.Contains("still")) return "hub";
            if (m.Contains("cinder")) return "cinderwood";
            if (m.Contains("gale")) return "galewick";
            if (m.Contains("lily")) return "lilymere";
            return null;
        }

        // ---- the winged ones ----

        void Keep(Kind kind, int count)
        {
            int have = 0;
            foreach (var c in _critters)
                if (c.kind == kind) have++;
            for (; have < count && _critters.Count < _matrices.Length; have++)
                _critters.Add(New(kind, RandomSpot(kind), 0f));
            // Too many (the realm faded): the extra ones fly off (fade out).
            for (int i = _critters.Count - 1; i >= 0 && have > count; i--)
                if (_critters[i].kind == kind && _critters[i].fade >= 0f)
                {
                    var c = _critters[i];
                    c.fade = -1f;
                    _critters[i] = c;
                    have--;
                }
        }

        Critter New(Kind kind, Vector3 at, float life)
        {
            Color color = kind switch
            {
                Kind.Butterfly => Random.value < 0.4f ? new Color(1f, 0.62f, 0.2f) : Random.value < 0.5f ? new Color(1f, 0.92f, 0.45f) : new Color(0.78f, 0.62f, 1f),
                Kind.Dragonfly => new Color(0.35f, 0.85f, 0.95f),
                _ => new Color(0.95f, 0.95f, 0.98f),
            };
            return new Critter { kind = kind, pos = at, target = RandomSpot(kind), phase = Random.value * 6.28f, color = color, life = life, fade = 0f };
        }

        void SpawnBird()
        {
            if (_critters.Count >= _matrices.Length) return;
            var half = _island.FootprintHalfSize * 0.95f;
            float y = _island.CapTopLocal + Tile * 3.2f;
            bool leftToRight = Random.value < 0.5f;
            float z = Random.Range(-half.y * 0.6f, half.y * 0.3f);
            var from = new Vector3(leftToRight ? -half.x : half.x, y, z);
            var to = new Vector3(-from.x, y + Tile * 0.6f, z + Random.Range(-half.y, half.y) * 0.3f);
            var b = New(Kind.Bird, from, 0f);
            b.target = to;
            b.vel = (to - from).normalized * Tile * 2.2f;
            _critters.Add(b);
        }

        /// <summary>A spot in the window over the right ground: flowers / grass (butterflies), water (dragonflies).</summary>
        Vector3 RandomSpot(Kind kind)
        {
            var half = _island.FootprintHalfSize * 0.8f;
            float cap = _island.CapTopLocal;
            Vector3 best = new Vector3(Random.Range(-half.x, half.x), cap, Random.Range(-half.y, half.y));
            var world = _island.world;
            for (int i = 0; i < 12; i++)
            {
                var p = new Vector3(Random.Range(-half.x, half.x), cap, Random.Range(-half.y, half.y));
                var tile = Vector2Int.RoundToInt(_island.LocalToTile(p));
                if (tile.x < 0 || tile.y < 0 || tile.x >= world.Columns || tile.y >= world.Rows) continue;
                char k = world.At(tile.y, tile.x);
                bool good = kind == Kind.Dragonfly ? k == IslandMap.Water : k == IslandMap.Grass || k == IslandMap.TallGrass;
                best = p;
                if (good) break;
            }
            best.y = cap + Tile * (kind == Kind.Dragonfly ? Random.Range(0.6f, 1.1f) : Random.Range(0.5f, 1.4f));
            return best;
        }

        void Move()
        {
            float dt = Time.deltaTime, tile = Tile;
            var half = _island.FootprintHalfSize;
            for (int i = _critters.Count - 1; i >= 0; i--)
            {
                var c = _critters[i];
                c.life += dt;
                c.fade = c.fade < 0f ? c.fade - dt : Mathf.Min(1f, c.fade + dt * 1.5f);
                switch (c.kind)
                {
                    case Kind.Butterfly:
                    {
                        // Wanders to a flower, flutters there a moment, picks another.
                        if (c.rest > 0f) c.rest -= dt;
                        else
                        {
                            var to = c.target - c.pos;
                            if (to.magnitude < tile * 0.2f)
                            {
                                c.rest = Random.Range(0.6f, 2f);
                                c.target = RandomSpot(Kind.Butterfly);
                            }
                            c.vel = Vector3.Lerp(c.vel, to.normalized * tile * 0.9f, dt * 2f);
                        }
                        var bob = new Vector3(Mathf.Sin(c.life * 3.1f + c.phase), Mathf.Sin(c.life * 5.3f + c.phase) * 1.4f, Mathf.Cos(c.life * 2.7f + c.phase)) * tile * 0.25f;
                        c.pos += (c.rest > 0f ? bob * 0.6f : c.vel + bob) * dt;
                        break;
                    }
                    case Kind.Dragonfly:
                    {
                        // Hovers, then darts to another spot over the water.
                        if (c.rest > 0f)
                        {
                            c.rest -= dt;
                            c.pos += new Vector3(Mathf.Sin(c.life * 9f), Mathf.Sin(c.life * 13f), 0f) * tile * 0.03f * dt * 10f;
                        }
                        else
                        {
                            var to = c.target - c.pos;
                            if (to.magnitude < tile * 0.1f)
                            {
                                c.rest = Random.Range(0.8f, 2.4f);
                                c.target = RandomSpot(Kind.Dragonfly);
                            }
                            c.vel = to.normalized * tile * 4f;
                            c.pos += c.vel * dt;
                        }
                        break;
                    }
                    case Kind.Bird:
                        c.pos += c.vel * dt;
                        c.pos.y += Mathf.Sin(c.life * 2f) * tile * 0.05f * dt;
                        if (Mathf.Abs(c.pos.x) > half.x * 1.05f && c.life > 1f) c.fade = -2f;
                        break;
                }
                // Scrolled out of the window with the ground: gone (the shader clips it); Keep() brings a new one in.
                // Targets that scrolled out are picked again so they don't fly off chasing them.
                if (c.kind != Kind.Bird)
                {
                    if (Mathf.Abs(c.pos.x) > half.x * 0.95f || Mathf.Abs(c.pos.z) > half.y * 0.95f)
                    {
                        _critters.RemoveAt(i);
                        continue;
                    }
                    if (Mathf.Abs(c.target.x) > half.x * 0.85f || Mathf.Abs(c.target.z) > half.y * 0.85f)
                        c.target = RandomSpot(c.kind);
                }
                if (c.fade < -1.2f || c.life > 240f)
                {
                    _critters.RemoveAt(i);
                    continue;
                }
                _critters[i] = c;
            }
        }

        void Draw()
        {
            var cfg = PolishConfig.Get();
            if (_critters.Count == 0 || _mesh == null || cfg == null || cfg.critterMaterial == null) return;
            var t = _island.transform;
            float tile = Tile;
            int n = 0;
            foreach (var c in _critters)
            {
                float size = (c.kind == Kind.Bird ? 1.0f : c.kind == Kind.Dragonfly ? 0.5f : 0.55f) * tile;
                float show = c.fade >= 0f ? c.fade : Mathf.Clamp01(1f + c.fade);
                Vector3 dir = c.vel.sqrMagnitude > 1e-10f ? c.vel : Vector3.forward;
                dir.y *= 0.3f;
                var rot = t.rotation * Quaternion.LookRotation(dir.normalized, Vector3.up);
                _matrices[n] = Matrix4x4.TRS(t.TransformPoint(c.pos), rot, Vector3.one * size * show * t.lossyScale.x);
                _colors[n] = c.color;
                _flaps[n] = c.kind switch
                {
                    Kind.Butterfly => new Vector4(c.rest > 0f ? 2.5f : 6f, 0.9f, c.phase, 1f),
                    Kind.Dragonfly => new Vector4(22f, 0.35f, c.phase, 1.5f),
                    _ => new Vector4(2.2f, 0.6f, c.phase, 1.7f),
                };
                n++;
            }
            _block.SetVectorArray(WingColorId, _colors);
            _block.SetVectorArray(FlapId, _flaps);
            Graphics.DrawMeshInstanced(_mesh, 0, cfg.critterMaterial, _matrices, n, _block, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
        }

        /// <summary>A body along +z and two wings (uv.x = side, uv.y = out along the wing), unit size.</summary>
        static Mesh BuildMesh()
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i = v.Count;
                v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
                tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            // Body: a thin diamond (two crossed quads).
            Quad(new Vector3(0, 0, -0.5f), new Vector3(0.06f, 0, 0), new Vector3(0, 0, 0.45f), new Vector3(-0.06f, 0, 0), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            Quad(new Vector3(0, 0, -0.5f), new Vector3(0, 0.06f, 0), new Vector3(0, 0, 0.45f), new Vector3(0, -0.06f, 0), Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            // Wings: a fore and a hind lobe each side.
            foreach (float s in new[] { 1f, -1f })
            {
                Quad(new Vector3(0, 0, 0.25f), new Vector3(s * 0.55f, 0, 0.42f), new Vector3(s * 0.6f, 0, 0.05f), new Vector3(0, 0, -0.02f),
                    new Vector2(s, 0f), new Vector2(s, 1f), new Vector2(s, 1f), new Vector2(s, 0f));
                Quad(new Vector3(0, 0, 0f), new Vector3(s * 0.45f, 0, -0.05f), new Vector3(s * 0.38f, 0, -0.38f), new Vector3(0, 0, -0.25f),
                    new Vector2(s, 0f), new Vector2(s, 0.9f), new Vector2(s, 0.9f), new Vector2(s, 0f));
            }
            var m = new Mesh { name = "Ambient Critter" };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            m.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            return m;
        }

        // ---- the dots: fireflies, water sparkles ----

        void BuildDots()
        {
            var fx = FindFirstObjectByType<BattleFx>();
            if (fx == null || fx.particleMaterial == null) return;
            var go = new GameObject("Ambient Dots");
            go.transform.SetParent(transform, false);
            _dots = go.AddComponent<ParticleSystem>();
            _dots.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _dots.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            var emission = _dots.emission;
            emission.enabled = false;
            var shape = _dots.shape;
            shape.enabled = false;
            var col = _dots.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var noise = _dots.noise;
            noise.enabled = true;
            noise.strength = 0.02f;
            noise.frequency = 0.6f;
            noise.scrollSpeed = 0.2f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = fx.particleMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _dots.Play();
        }

        void Dots(string realm, PolishConfig cfg, float restored)
        {
            if (_dots == null) return;
            int want = realm == "cinderwood" ? Mathf.RoundToInt(cfg.cinderwoodFireflies * Mathf.Lerp(cfg.fadedShare, 1f, restored))
                : realm == "lilymere" && restored > 0.5f ? cfg.lilymereSparkles : 0;
            if (want <= 0) return;
            bool sparkle = realm == "lilymere";
            float life = sparkle ? 0.7f : 4.5f;
            // Steady state: `want` alive at a time.
            _dotClock += Time.deltaTime * want / life;
            var half = _island.FootprintHalfSize * 0.85f;
            float tile = Tile, cap = _island.CapTopLocal;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int guard = 0; _dotClock >= 1f && guard < 8; guard++)
            {
                _dotClock -= 1f;
                Vector3 at = Vector3.zero;
                bool found = false;
                for (int i = 0; i < 10 && !found; i++)
                {
                    at = new Vector3(Random.Range(-half.x, half.x), cap, Random.Range(-half.y, half.y));
                    var t = Vector2Int.RoundToInt(_island.LocalToTile(at));
                    if (t.x < 0 || t.y < 0 || t.x >= _island.world.Columns || t.y >= _island.world.Rows) continue;
                    char k = _island.world.At(t.y, t.x);
                    found = sparkle ? k == IslandMap.Water : k != IslandMap.Water;
                }
                if (!found) continue;
                at.y = cap + (sparkle ? tile * 0.05f : tile * Random.Range(0.3f, 1.6f));
                p.position = at - _dots.transform.localPosition;
                p.velocity = sparkle ? Vector3.zero : new Vector3(Random.Range(-1f, 1f), Random.Range(0.1f, 0.4f), Random.Range(-1f, 1f)) * tile * 0.15f;
                p.startColor = sparkle ? new Color(0.85f, 0.97f, 1f, 1f) : new Color(0.75f, 1f, 0.35f, 1f);
                p.startSize = tile * (sparkle ? Random.Range(0.12f, 0.2f) : Random.Range(0.12f, 0.18f));
                p.startLifetime = life * Random.Range(0.7f, 1.3f);
                _dots.Emit(p, 1);
            }
        }
    }
}
