using System.Collections.Generic;
using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Battle effects: one pooled particle system (additive dots) plus a few pooled shapes (orbs, bubbles, rings),
    /// all drawn with Diorama/Fx and recoloured per call. Lives on a child of the island; shape positions and sizes
    /// are island-local, particle calls take world space.
    /// </summary>
    public class BattleFx : MonoBehaviour
    {
        public Material orbMaterial;
        public Material ringMaterial;
        public Material particleMaterial;
        [Min(16)] public int maxParticles = 500;
        [Tooltip("Scales every particle's size (tune the look in one place).")]
        public float particleSize = 1.7f;

        public enum Shape { Orb, Ring }

        /// <summary>A pooled effect shape; set its transform and colours, call <see cref="BattleFx.Release"/> when done.</summary>
        public class Item
        {
            public Transform transform;
            public MeshRenderer renderer;
            public Shape shape;
            public readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

            public void Set(Color core, Color edge, float alpha, float power = 1f, float coreAlpha = 1f, float noise = 0.3f, float ripple = 0f)
            {
                block.SetColor(ColorId, core);
                block.SetColor(EdgeId, edge);
                block.SetFloat(AlphaId, alpha);
                block.SetFloat(PowerId, power);
                block.SetFloat(CoreAlphaId, coreAlpha);
                block.SetFloat(NoiseId, noise);
                block.SetFloat(RippleId, ripple);
                renderer.SetPropertyBlock(block);
            }

            public void SetAlpha(float alpha)
            {
                block.SetFloat(AlphaId, alpha);
                renderer.SetPropertyBlock(block);
            }
        }

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EdgeId = Shader.PropertyToID("_EdgeColor");
        static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        static readonly int PowerId = Shader.PropertyToID("_Power");
        static readonly int CoreAlphaId = Shader.PropertyToID("_CoreAlpha");
        static readonly int NoiseId = Shader.PropertyToID("_Noise");
        static readonly int RippleId = Shader.PropertyToID("_Ripple");

        readonly List<Item> _free = new List<Item>();
        ParticleSystem _particles;
        Mesh _sphere, _quad;

        void Awake()
        {
            if (orbMaterial == null || ringMaterial == null || particleMaterial == null)
                Debug.LogError("[BattleFx] Missing effect materials (run Tools > Diorama > Step 5 > 2. Add Battle to MR_Main).", this);
            _sphere = UvSphere(12, 18);
            _quad = Quad();
            BuildParticles();
        }

        void OnDestroy()
        {
            Destroy(_sphere);
            Destroy(_quad);
        }

        void BuildParticles()
        {
            var go = new GameObject("Battle Particles");
            go.transform.SetParent(transform, false);
            _particles = go.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSpeed = 0f;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;
            var fade = _particles.colorOverLifetime;
            fade.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = g;
            var size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.4f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = particleMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _particles.Play();
        }

        /// <summary>
        /// Sprays <paramref name="count"/> dots from a world point. Speed and size are world metres;
        /// <paramref name="up"/> adds a shared drift (world, per second), <paramref name="gravity"/> pulls down.
        /// </summary>
        public void Burst(Vector3 position, int count, Color color, float speed, float life, float size, Vector3 up = default, float gravity = 0f)
        {
            if (_particles == null) return;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                p.position = position;
                p.velocity = Random.onUnitSphere * (speed * Random.Range(0.35f, 1f)) + up - Vector3.up * (gravity * 0.3f);
                p.startColor = color;
                p.startSize = size * particleSize * Random.Range(0.6f, 1.2f);
                p.startLifetime = life * Random.Range(0.6f, 1.2f);
                _particles.Emit(p, 1);
            }
        }

        public Item Get(Shape shape)
        {
            Item item = null;
            for (int i = _free.Count - 1; i >= 0; i--)
                if (_free[i].shape == shape)
                {
                    item = _free[i];
                    _free.RemoveAt(i);
                    break;
                }
            if (item == null)
            {
                var go = new GameObject(shape == Shape.Orb ? "Fx Orb" : "Fx Ring");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = shape == Shape.Orb ? _sphere : _quad;
                item = new Item { transform = go.transform, renderer = go.AddComponent<MeshRenderer>(), shape = shape };
                item.renderer.sharedMaterial = shape == Shape.Orb ? orbMaterial : ringMaterial;
                item.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                item.renderer.receiveShadows = false;
            }
            item.transform.gameObject.SetActive(true);
            item.transform.localRotation = Quaternion.identity;
            return item;
        }

        public void Release(Item item)
        {
            if (item == null || item.transform == null) return;
            item.transform.gameObject.SetActive(false);
            _free.Add(item);
        }

        static Mesh UvSphere(int rings, int segments)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings, phi = v * Mathf.PI;
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments, theta = u * Mathf.PI * 2f;
                    verts.Add(new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta)));
                    uvs.Add(new Vector2(u, v));
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * (segments + 1) + s, b = a + segments + 1;
                    tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            var mesh = new Mesh { name = "Fx Sphere" };
            mesh.SetVertices(verts);
            mesh.SetNormals(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Unit quad lying flat (XZ), facing up, for rings on the ground; rotate it to stand up.</summary>
        static Mesh Quad()
        {
            var mesh = new Mesh { name = "Fx Quad" };
            mesh.SetVertices(new[] { new Vector3(-1f, 0f, -1f), new Vector3(1f, 0f, -1f), new Vector3(-1f, 0f, 1f), new Vector3(1f, 0f, 1f) });
            mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
