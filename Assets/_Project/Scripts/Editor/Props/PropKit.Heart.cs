using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Diorama.EditorTools.StarterKit;

namespace Diorama.EditorTools
{
    /// <summary>
    /// Step 13 B: the Heart of the World, the statue come alive. A shrine guardian (the sitting temple lion-dog kind,
    /// made our own): on its haunches on the statue's plinth, chest high, head raised; mossy grey-green stone with golden
    /// veins glowing from a core in its chest; a mane of chunky carved stone curls in three rings with moss and tiny
    /// flowers; a curled stone tail ending in a ribbon of wind; a crown of the three elements (ember crest, lotus,
    /// cloud-wing plumes); folded cloud-wings on its back; an empty round setting in its forehead for the Spirit; kind
    /// heavy-lidded eyes that open gold; stone-clawed paws; a carved collar with the three shard gems.
    /// Rigid groups, each one baked prop (Diorama/Prop, 2 draws each): <see cref="HeartBodyName"/> (body, plinth, tail
    /// with its swaying wind ribbon, collar, veins, moss), the head eyes closed / open (<see cref="HeartHeadName"/>,
    /// <see cref="HeartHeadOpenName"/>, swapped: a blink is a swap), <see cref="HeartWingName"/> (drawn twice, mirrored).
    /// 4 views = 8 draws, about 23k triangles. Shape units = tiles; the body's origin is the statue's centre at the
    /// plinth's foot, +Z toward the player; the head turns about <see cref="HeadPivot"/>, the wings about
    /// <see cref="WingHinge"/> (the left one, +X; the right mirrored).
    /// </summary>
    public static partial class PropKit
    {
        public const string HeartBodyName = "HeartBody", HeartHeadName = "HeartHead", HeartHeadOpenName = "HeartHeadOpen", HeartWingName = "HeartWing";

        /// <summary>Head and wing pivots (body shape units). Mirror the runtime's HeartView.</summary>
        public static readonly Vector3 HeadPivot = new Vector3(0f, 1.86f, 0.1f);
        public static readonly Vector3 WingHinge = new Vector3(0.34f, 1.62f, -0.3f);
        /// <summary>The forehead setting's middle (head, eyes forward).</summary>
        public static readonly Vector3 ForeheadSocket = new Vector3(0f, 2.37f, 0.57f);

        // Mossy grey-green stone, gold veins, the three element colours.
        static readonly PropColor HeartStone = H("#A4AD9C");
        static readonly PropColor HeartMoss = H("#6E9A4C");
        static readonly PropColor HeartGold = H("#FFC95A", 0.9f);
        static readonly PropColor HeartFire = H("#F06A3A", 0.35f);
        static readonly PropColor HeartWater = H("#5FA8E8", 0.3f);
        static readonly PropColor HeartLotus = H("#F2A3C4", 0.25f);
        static readonly PropColor HeartWind = H("#A8E59A", 0.2f);

        /// <summary>Writes and bakes the Heart's four props (the finale setup).</summary>
        internal static void WriteHeart()
        {
            DioramaSceneSetup.EnsureFolder(Root);
            DioramaSceneSetup.EnsureFolder($"{Root}/Shapes");
            foreach (var r in new[] { HeartBody(), HeartHead(false), HeartHead(true), HeartWing() })
            {
                EditorUtility.SetDirty(r);
                AssetDatabase.SaveAssets();
                var report = PropBaker.Bake(r);
                Debug.Log($"[PropKit] {r.name}: {report.triangles} tris ({report.pieces} pieces).");
            }
        }

        /// <summary>A piece with its own share of the recipe's triangle budget (relative weight).</summary>
        static PropPiece W(PropPiece p, float weight)
        {
            p.budgetWeight = weight;
            return p;
        }

        /// <summary>
        /// A small piece baked <paramref name="grow"/> times bigger round <paramref name="c"/> and scaled back (the
        /// simplifier can't thin out very small triangles: a 17 cm gem stayed at 7k). Keep c.x = 0 for mirrored primitives.
        /// </summary>
        static PropPiece Small(string name, Vector3 c, float grow, PropSlot slot, PropMotion motion, float amount, float weight, params SdfPrimitive[] prims)
        {
            var list = new SdfPrimitive[prims.Length];
            for (int i = 0; i < prims.Length; i++)
            {
                var p = prims[i];
                if (p.kind != SdfKind.Plane)
                {
                    p.a = (p.a - c) * grow;
                    if (p.kind == SdfKind.RoundCone) p.b = (p.b - c) * grow;
                    p.size *= grow;
                    if (p.kind == SdfKind.RoundBox) p.b *= grow;
                }
                p.k *= grow;
                p.bumps *= grow;
                if (p.bumpFrequency > 0f) p.bumpFrequency /= grow;
                list[i] = p;
            }
            var piece = Piece(PShape(name, list), slot, motion, amount, c);
            piece.scale = 1f / grow;
            piece.budgetWeight = weight;
            return piece;
        }

        // ---- body ----

        static PropRecipe HeartBody()
        {
            var body = Recipe(HeartBodyName, new Vector2Int(3, 3), true, false, 7600);
            body.palette = Palette(HeartFire, HeartWater, HeartStone, HeartMoss, HeartGold, HeartWind);
            body.yawJitter = 0f;
            body.scaleJitter = 0f;
            body.resolution = 76;
            body.outline = 0.03f;

            var stonePrims = new List<SdfPrimitive>
            {
                // Haunches, a deep torso rising to a high, proud chest.
                Ell(new Vector3(0.5f, 0.46f, -0.3f), new Vector3(0.4f, 0.4f, 0.54f), 0.15f).Mirror(),
                Ell(new Vector3(0f, 0.95f, -0.14f), new Vector3(0.56f, 0.72f, 0.5f), 0.24f),
                Ell(new Vector3(0f, 1.27f, 0.17f), new Vector3(0.5f, 0.52f, 0.38f), 0.18f),
                // Straight front legs, broad paws; back paws tucked beside the haunches.
                Cone(new Vector3(0.27f, 1.05f, 0.3f), new Vector3(0.3f, 0.22f, 0.54f), 0.15f, 0.12f, 0.12f).Mirror(),
                Ell(new Vector3(0.3f, 0.13f, 0.64f), new Vector3(0.17f, 0.11f, 0.22f), 0.08f).Mirror(),
                Ell(new Vector3(0.75f, 0.12f, 0.16f), new Vector3(0.15f, 0.1f, 0.25f), 0.08f).Mirror(),
                // The neck up to the head.
                Cone(new Vector3(0f, 1.5f, 0f), new Vector3(0f, 1.88f, 0.1f), 0.34f, 0.27f, 0.15f),
                Floor(0.1f),
            };
            var stone = PShape("HeartBody_Stone", stonePrims.ToArray());
            var eval = new SdfEvaluator(stonePrims);

            var claws = new List<SdfPrimitive>();
            foreach (float x in new[] { 0.3f, -0.3f })
                foreach (float dx in new[] { -0.085f, 0f, 0.085f })
                    claws.Add(Cone(new Vector3(x + dx, 0.09f, 0.8f), new Vector3(x + dx, 0.05f, 0.9f), 0.036f, 0.012f, 0f));

            // The curled tail at its side (the player's right), ending in a ribbon of wind.
            var tailPts = new[]
            {
                (new Vector3(-0.45f, 0.24f, -0.82f), 0.17f), (new Vector3(-0.82f, 0.44f, -0.72f), 0.15f), (new Vector3(-0.98f, 0.78f, -0.52f), 0.13f),
                (new Vector3(-0.92f, 1.06f, -0.32f), 0.11f), (new Vector3(-0.74f, 1.18f, -0.24f), 0.09f), (new Vector3(-0.66f, 1.08f, -0.22f), 0.07f),
            };
            var tail = new List<SdfPrimitive>();
            for (int i = 0; i + 1 < tailPts.Length; i++)
                tail.Add(Cone(tailPts[i].Item1, tailPts[i + 1].Item1, tailPts[i].Item2, tailPts[i + 1].Item2, i == 0 ? 0f : 0.06f).Bumps(0.008f, 12f));
            var ribbon = new[]
            {
                Ell(new Vector3(-0.6f, 1.33f, -0.24f), new Vector3(0.035f, 0.17f, 0.13f), 0f, new Vector3(20f, 0f, -15f)),
                Ell(new Vector3(-0.5f, 1.52f, -0.32f), new Vector3(0.03f, 0.15f, 0.11f), 0.05f, new Vector3(-12f, 0f, 12f)),
                Ell(new Vector3(-0.57f, 1.7f, -0.4f), new Vector3(0.025f, 0.12f, 0.08f), 0.05f, new Vector3(18f, 0f, -22f)),
                Ell(new Vector3(-0.5f, 1.84f, -0.46f), new Vector3(0.02f, 0.08f, 0.05f), 0.04f, new Vector3(-10f, 0f, 15f)),
            };

            // The carved collar draped on the chest, three settings at its front: fire, water, wind (left to right as
            // the player sees them: fire on its right paw's side, like the statue's sockets).
            var collar = new List<SdfPrimitive>();
            for (int i = 0; i < 26; i++)
            {
                // Round the neck, low at the front (on the chest), high at the back.
                float a = i / 26f * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                float y = Mathf.Lerp(1.36f, 1.66f, (1f - Mathf.Cos(a)) * 0.5f);
                var axis = new Vector3(0f, y, 0.06f);
                if (eval.Surface(axis + dir * 1.2f, -dir, out var bp, out var bn))
                    collar.Add(Sphere(bp + bn * 0.012f, 0.062f, collar.Count == 0 ? 0f : 0.03f).Bumps(0.006f, 20f));
            }
            var gems = new Vector3[3];
            var gemRot = new Vector3[3];
            var gemAt = new[] { new Vector2(0.25f, 1.36f), new Vector2(0f, 1.31f), new Vector2(-0.25f, 1.36f) };
            for (int i = 0; i < 3; i++)
            {
                if (!eval.Surface(new Vector3(gemAt[i].x, gemAt[i].y, 1.5f), Vector3.back, out var gp, out var gn)) gp = new Vector3(gemAt[i].x, gemAt[i].y, 0.5f);
                gems[i] = gp + gn * 0.035f;
                gemRot[i] = Quaternion.FromToRotation(Vector3.forward, gn).eulerAngles;
                collar.Add(Torus(gems[i] - gn * 0.012f, 0.1f, 0.026f, Quaternion.FromToRotation(Vector3.up, gn).eulerAngles, 0.02f));
            }

            body.pieces = new List<PropPiece>
            {
                W(Piece(stone, PropSlot.Stone), 4300f),
                W(Piece(PShape("HeartBody_Plinth",
                    Ell(new Vector3(0f, 0.04f, 0f), new Vector3(1.35f, 0.1f, 1.35f), 0f).Bumps(0.006f, 10f),
                    Torus(new Vector3(0f, 0.13f, 0f), 1.22f, 0.035f, Vector3.zero, 0.02f),
                    Floor(0f)), PropSlot.Stone), 700f),
                W(Piece(PShape("HeartBody_Claws", claws.ToArray()), PropSlot.Stone), 260f),
                W(Piece(PShape("HeartBody_Tail", tail.ToArray()), PropSlot.Stone), 700f),
                W(Piece(PShape("HeartBody_Wind", ribbon), PropSlot.Accent, PropMotion.Sway, 0.12f), 360f),
                W(Piece(PShape("HeartBody_Collar", collar.ToArray()), PropSlot.Stone), 700f),
                Small("HeartBody_GemFire", gems[0], 5f, PropSlot.Clay, PropMotion.None, 0f, 120f, Ell(gems[0], new Vector3(0.08f, 0.1f, 0.05f), 0f, gemRot[0])),
                Small("HeartBody_GemWater", gems[1], 5f, PropSlot.Wood, PropMotion.None, 0f, 120f, Ell(gems[1], new Vector3(0.085f, 0.105f, 0.05f), 0f, gemRot[1])),
                Small("HeartBody_GemWind", gems[2], 5f, PropSlot.Accent, PropMotion.None, 0f, 120f, Ell(gems[2], new Vector3(0.08f, 0.1f, 0.05f), 0f, gemRot[2])),
                W(Piece(PShape("HeartBody_Veins", HeartVeins(eval).ToArray()), PropSlot.Glow, PropMotion.Flicker, 0.001f), 900f),
                W(Piece(PShape("HeartBody_Moss",
                    Ell(new Vector3(0f, 1.64f, -0.12f), new Vector3(0.54f, 0.12f, 0.44f), 0f).Bumps(0.016f, 18f),
                    Ell(new Vector3(0.52f, 0.8f, -0.4f), new Vector3(0.22f, 0.07f, 0.3f), 0.06f).Bumps(0.012f, 18f),
                    Ell(new Vector3(0.9f, 0.14f, 0.72f), new Vector3(0.3f, 0.05f, 0.2f), 0.05f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.78f, 0.14f, 0.62f), new Vector3(0.26f, 0.05f, 0.2f), 0.05f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.7f, 0.14f, -0.95f), new Vector3(0.34f, 0.05f, 0.22f), 0.05f).Bumps(0.012f, 20f)), PropSlot.Leaf, PropMotion.Sway, 0.02f), 900f),
                W(Piece(PShape("HeartBody_Flowers", Flowers(new[]
                {
                    new Vector3(0.36f, 1.74f, -0.02f), new Vector3(-0.3f, 1.75f, 0.02f), new Vector3(0.6f, 0.86f, -0.3f),
                    new Vector3(0.96f, 0.19f, 0.78f), new Vector3(-0.8f, 0.19f, 0.66f), new Vector3(-0.62f, 0.19f, -0.9f),
                })), PropSlot.Accent), 300f),
            };
            return body;
        }

        /// <summary>Gold veins: from a core in the chest down the front legs and the belly, cracks over the haunches.</summary>
        static List<SdfPrimitive> HeartVeins(SdfEvaluator eval)
        {
            var veins = new List<SdfPrimitive>();
            // The core: half sunk in the chest below the collar.
            if (eval.Surface(new Vector3(0f, 1.0f, 1.5f), Vector3.back, out var core, out _))
                veins.Add(Sphere(core + Vector3.back * 0.02f, 0.075f, 0f));
            void Path(Vector3 axis, params Vector2[] pts)
            {
                // Points on the face of the body seen along -axis (front: x, y; side: z, y), projected onto the surface.
                var hits = new List<(Vector3 p, Vector3 n)>();
                foreach (var q in pts)
                {
                    Vector3 from = axis == Vector3.forward ? new Vector3(q.x, q.y, 1.5f) : axis == Vector3.right ? new Vector3(1.5f, q.y, q.x) : new Vector3(-1.5f, q.y, q.x);
                    if (eval.Surface(from, -axis, out var p, out var n)) hits.Add((p, n));
                }
                for (int i = 0; i + 1 < hits.Count; i++)
                {
                    Vector3 a = hits[i].p, b = hits[i + 1].p, d = b - a;
                    if (d.sqrMagnitude < 1e-6f) continue;
                    var rot = Quaternion.LookRotation(d.normalized, hits[i].n).eulerAngles;
                    veins.Add(Ell((a + b) * 0.5f + hits[i].n * 0.006f, new Vector3(0.03f, 0.018f, d.magnitude * 0.5f + 0.025f), 0f, rot));
                }
            }
            Path(Vector3.forward, new Vector2(0.03f, 1.0f), new Vector2(0.15f, 0.9f), new Vector2(0.22f, 0.72f), new Vector2(0.27f, 0.52f), new Vector2(0.31f, 0.34f));
            Path(Vector3.forward, new Vector2(-0.03f, 1.0f), new Vector2(-0.13f, 0.86f), new Vector2(-0.24f, 0.78f), new Vector2(-0.28f, 0.6f));
            Path(Vector3.forward, new Vector2(0f, 0.95f), new Vector2(0.06f, 0.8f), new Vector2(0.03f, 0.64f));
            Path(Vector3.right, new Vector2(-0.05f, 0.75f), new Vector2(-0.25f, 0.6f), new Vector2(-0.45f, 0.66f), new Vector2(-0.62f, 0.5f));
            Path(Vector3.left, new Vector2(-0.1f, 0.68f), new Vector2(-0.3f, 0.52f), new Vector2(-0.52f, 0.6f));
            Path(Vector3.forward, new Vector2(0.2f, 1.5f), new Vector2(0.32f, 1.38f));
            Path(Vector3.forward, new Vector2(-0.2f, 1.5f), new Vector2(-0.33f, 1.4f));
            return veins;
        }

        /// <summary>Tiny five-petal blossoms (one shape, union of little spheres).</summary>
        static SdfPrimitive[] Flowers(Vector3[] at)
        {
            var list = new List<SdfPrimitive>();
            foreach (var c in at)
                for (int i = 0; i < 5; i++)
                {
                    var d = Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward;
                    list.Add(Sphere(c + d * 0.028f + Vector3.up * 0.01f, 0.022f, list.Count == 0 ? 0f : 0.004f));
                }
            return list.ToArray();
        }

        // ---- head ----

        /// <summary>The head (same shape space as the body; it turns about <see cref="HeadPivot"/>). Open = gold eyes.</summary>
        static PropRecipe HeartHead(bool open)
        {
            var head = Recipe(open ? HeartHeadOpenName : HeartHeadName, new Vector2Int(3, 3), false, false, 8000);
            head.palette = Palette(HeartFire, HeartLotus, HeartStone, HeartMoss, HeartGold, HeartWind);
            head.yawJitter = 0f;
            head.scaleJitter = 0f;
            head.resolution = 80;
            head.outline = 0.026f;

            var skull = new List<SdfPrimitive>
            {
                Ell(new Vector3(0f, 2.14f, 0.24f), new Vector3(0.39f, 0.35f, 0.36f), 0f),
                // A broad muzzle with puffy jowls and a big soft nose, well out of the mane.
                Ell(new Vector3(0f, 1.94f, 0.57f), new Vector3(0.24f, 0.15f, 0.25f), 0.12f),
                Ell(new Vector3(0.13f, 1.9f, 0.68f), new Vector3(0.1f, 0.09f, 0.1f), 0.06f).Mirror(),
                Ell(new Vector3(0f, 2.02f, 0.8f), new Vector3(0.1f, 0.065f, 0.065f), 0.05f),
                // Heavy (but not hiding) brows; small drooping ears.
                Ell(new Vector3(0.16f, 2.275f, 0.53f), new Vector3(0.13f, 0.035f, 0.08f), 0.05f, new Vector3(0f, 0f, -12f)).Mirror(),
                Ell(new Vector3(0.37f, 2.17f, 0.16f), new Vector3(0.07f, 0.17f, 0.12f), 0.06f, new Vector3(0f, 0f, 28f)).Mirror(),
                // A gentle smile, the eye sockets, the empty round setting in its forehead.
                Ell(new Vector3(0f, 1.86f, 0.77f), new Vector3(0.06f, 0.016f, 0.05f), 0.01f).Sub(),
                Ell(new Vector3(0.085f, 1.875f, 0.752f), new Vector3(0.042f, 0.014f, 0.045f), 0.01f, new Vector3(0f, -20f, 28f)).Sub().Mirror(),
                Ell(new Vector3(0.155f, 2.15f, 0.58f), new Vector3(0.1f, 0.078f, 0.065f), 0.01f).Sub().Mirror(),
                Sphere(ForeheadSocket + new Vector3(0f, 0f, -0.03f), 0.068f, 0.01f).Sub(),
            };

            var pieces = new List<PropPiece>
            {
                W(Piece(PShape("HeartHead_Skull", skull.ToArray()), PropSlot.Stone), 2600f),
                W(Piece(PShape("HeartHead_Mane", Mane().ToArray()), PropSlot.Stone), 3600f),
                Small("HeartHead_Socket", ForeheadSocket, 5f, PropSlot.Glow, PropMotion.None, 0f, 160f,
                    Torus(ForeheadSocket + new Vector3(0f, 0f, -0.012f), 0.074f, 0.019f, new Vector3(90f, 0f, 0f), 0f)),
                W(Piece(PShape("HeartHead_Moss",
                    Ell(new Vector3(0.24f, 2.6f, 0.2f), new Vector3(0.16f, 0.06f, 0.14f), 0f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.56f, 2.3f, 0.18f), new Vector3(0.08f, 0.16f, 0.14f), 0.04f).Bumps(0.012f, 20f),
                    Ell(new Vector3(0.62f, 2.0f, 0.12f), new Vector3(0.08f, 0.15f, 0.14f), 0.04f).Bumps(0.012f, 20f),
                    Ell(new Vector3(-0.3f, 2.66f, 0.05f), new Vector3(0.15f, 0.06f, 0.13f), 0.04f).Bumps(0.012f, 20f),
                    Ell(new Vector3(0.4f, 1.62f, 0.38f), new Vector3(0.14f, 0.06f, 0.11f), 0.04f).Bumps(0.012f, 20f)), PropSlot.Leaf, PropMotion.Sway, 0.015f), 700f),
                W(Piece(PShape("HeartHead_Flowers", Flowers(new[]
                {
                    new Vector3(0.24f, 2.66f, 0.25f), new Vector3(-0.6f, 2.4f, 0.24f), new Vector3(0.66f, 2.1f, 0.2f), new Vector3(0.42f, 1.67f, 0.42f),
                })), PropSlot.Accent), 300f),
                // The crown of the three elements: an ember crest in the middle, the lotus and the cloud-wing plumes beside it.
                Small("HeartHead_Ember", new Vector3(0f, 2.68f, 0.24f), 3f, PropSlot.Clay, PropMotion.Flicker, 0.02f, 420f,
                    Cone(new Vector3(0f, 2.48f, 0.3f), new Vector3(0f, 2.95f, 0.18f), 0.13f, 0.012f, 0f),
                    Cone(new Vector3(0.1f, 2.47f, 0.28f), new Vector3(0.21f, 2.78f, 0.17f), 0.075f, 0.01f, 0.04f).Mirror(),
                    Sphere(new Vector3(0f, 2.48f, 0.3f), 0.11f, 0.05f)),
                Small("HeartHead_Lotus", new Vector3(0.36f, 2.5f, 0.2f), 3f, PropSlot.Wood, PropMotion.None, 0f, 450f, Lotus(new Vector3(0.36f, 2.44f, 0.2f), 1.6f).ToArray()),
                Small("HeartHead_Plumes", new Vector3(-0.4f, 2.56f, 0.1f), 2f, PropSlot.Accent, PropMotion.Sway, 0.03f, 450f,
                    Ell(new Vector3(-0.38f, 2.62f, 0.1f), new Vector3(0.07f, 0.25f, 0.1f), 0f, new Vector3(-25f, 0f, 30f)),
                    Ell(new Vector3(-0.52f, 2.54f, 0.05f), new Vector3(0.065f, 0.22f, 0.09f), 0.04f, new Vector3(-30f, 0f, 50f)),
                    Ell(new Vector3(-0.27f, 2.7f, 0.14f), new Vector3(0.055f, 0.19f, 0.075f), 0.04f, new Vector3(-20f, 0f, 14f)),
                    Sphere(new Vector3(-0.33f, 2.45f, 0.17f), 0.1f, 0.05f),
                    Sphere(new Vector3(-0.44f, 2.43f, 0.11f), 0.085f, 0.05f)),
            };
            if (open)
            {
                // Kind gold eyes under heavy lids, a stone pupil in each.
                var eyes = new Vector3(0f, 2.15f, 0.58f);
                pieces.Add(Small("HeartHead_Eyes", eyes, 3f, PropSlot.Glow, PropMotion.None, 0f, 320f, Ell(new Vector3(0.155f, 2.15f, 0.565f), new Vector3(0.09f, 0.07f, 0.055f), 0f).Mirror()));
                pieces.Add(Small("HeartHead_Pupils", eyes, 3f, PropSlot.Stone, PropMotion.None, 0f, 120f, Ell(new Vector3(0.15f, 2.14f, 0.617f), new Vector3(0.028f, 0.04f, 0.012f), 0f).Mirror()));
                pieces.Add(Small("HeartHead_UpperLids", eyes, 3f, PropSlot.Stone, PropMotion.None, 0f, 320f, Ell(new Vector3(0.155f, 2.2f, 0.59f), new Vector3(0.1f, 0.03f, 0.06f), 0f, new Vector3(0f, 0f, -10f)).Mirror()));
            }
            else
            {
                // Closed: stone lids fill the sockets, a thin gold line glows along each.
                var eyes = new Vector3(0f, 2.15f, 0.59f);
                pieces.Add(Small("HeartHead_Lids", eyes, 3f, PropSlot.Stone, PropMotion.None, 0f, 340f, Ell(new Vector3(0.155f, 2.15f, 0.572f), new Vector3(0.095f, 0.074f, 0.057f), 0f).Mirror()));
                pieces.Add(Small("HeartHead_Slits", eyes, 3f, PropSlot.Glow, PropMotion.None, 0f, 120f, Ell(new Vector3(0.155f, 2.14f, 0.626f), new Vector3(0.072f, 0.012f, 0.02f), 0f, new Vector3(0f, 0f, -10f)).Mirror()));
            }
            head.pieces = pieces;
            return head;
        }

        /// <summary>The mane: chunky carved stone curls in three rings round the face (each a ball with a carved spiral).</summary>
        static List<SdfPrimitive> Mane()
        {
            var list = new List<SdfPrimitive>();
            var face = new Vector3(0f, 2.08f, 0.34f);
            void Ring(float radius, float z, int count, float ball, float from, float to, float lean)
            {
                for (int i = 0; i < count; i++)
                {
                    float a = Mathf.Lerp(from, to, count == 1 ? 0.5f : i / (float)(count - 1)) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), Mathf.Cos(a), 0f);
                    var c = face + dir * radius + Vector3.forward * z;
                    list.Add(Sphere(c, ball, list.Count == 0 ? 0f : 0.07f).Bumps(0.006f, 16f));
                    // The curl: a groove carved round its outer face, tipped outward, and a nub in the middle.
                    var outward = (dir * 0.6f + Vector3.forward * lean).normalized;
                    var rot = Quaternion.FromToRotation(Vector3.up, outward).eulerAngles;
                    list.Add(Torus(c + outward * (ball * 0.86f), ball * 0.48f, ball * 0.13f, rot, 0.015f).Sub());
                    list.Add(Sphere(c + outward * (ball * 0.82f), ball * 0.26f, 0.03f));
                }
            }
            Ring(0.5f, 0.0f, 11, 0.12f, -160f, 160f, 0.9f);
            Ring(0.67f, -0.13f, 12, 0.135f, -168f, 168f, 0.5f);
            Ring(0.7f, -0.34f, 7, 0.135f, 105f, 255f, 0.1f);
            return list;
        }

        /// <summary>A small lotus (six petals round a bud) at <paramref name="c"/>, leaning out to the side.</summary>
        static List<SdfPrimitive> Lotus(Vector3 c, float size = 1f)
        {
            var list = new List<SdfPrimitive> { Sphere(c + new Vector3(0f, 0.04f, 0f) * size, 0.045f * size, 0f) };
            for (int i = 0; i < 6; i++)
            {
                var d = Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward;
                list.Add(Ell(c + (d * 0.065f + Vector3.up * 0.055f) * size, new Vector3(0.038f, 0.08f, 0.024f) * size, 0f, new Vector3(35f, i * 60f, 0f)));
            }
            return list;
        }

        // ---- wings ----

        /// <summary>
        /// One cloud-wing (the left; the right is drawn mirrored), hinge at the origin: a stone arm reaching out along +X
        /// and back, cloud puffs hanging below it as feathers, a gold vein along the arm.
        /// </summary>
        static PropRecipe HeartWing()
        {
            var wing = Recipe(HeartWingName, new Vector2Int(1, 1), false, false, 2600);
            wing.palette = Palette(HeartFire, HeartWater, HeartStone, HeartMoss, HeartGold, HeartWind);
            wing.yawJitter = 0f;
            wing.scaleJitter = 0f;
            wing.resolution = 56;
            wing.outline = 0.024f;
            Vector3 a = Vector3.zero, m = new Vector3(0.75f, 0.32f, -0.18f), tip = new Vector3(1.35f, 0.14f, -0.46f);
            Vector3 Arm(float t) => t < 0.5f ? Vector3.Lerp(a, m, t * 2f) : Vector3.Lerp(m, tip, t * 2f - 1f);
            var puffs = new List<SdfPrimitive>();
            for (int i = 0; i < 10; i++)
            {
                float t = 0.08f + 0.9f * i / 9f;
                var p = Arm(t);
                puffs.Add(Sphere(p + new Vector3(0.02f, -0.13f - 0.08f * t, -0.04f), 0.16f - 0.06f * t, puffs.Count == 0 ? 0f : 0.09f).Bumps(0.01f, 9f));
                if (i % 2 == 0) puffs.Add(Sphere(p + new Vector3(-0.03f, -0.33f - 0.06f * t, -0.06f), 0.12f - 0.05f * t, 0.09f));
            }
            wing.pieces = new List<PropPiece>
            {
                Piece(PShape("HeartWing_Arm",
                    Cone(a, m, 0.085f, 0.055f, 0f),
                    Cone(m, tip, 0.055f, 0.028f, 0.05f),
                    Sphere(a, 0.1f, 0.05f)), PropSlot.Stone),
                Piece(PShape("HeartWing_Cloud", puffs.ToArray()), PropSlot.Accent),
                Piece(PShape("HeartWing_Vein",
                    Ell((a + m) * 0.5f + new Vector3(0f, 0.06f, 0.03f), new Vector3((m - a).magnitude * 0.5f, 0.014f, 0.016f), 0f,
                        Quaternion.FromToRotation(Vector3.right, (m - a).normalized).eulerAngles)), PropSlot.Glow),
            };
            return wing;
        }
    }
}
