using UnityEngine;

namespace Diorama
{
    /// <summary>
    /// Step 14j: the island's grab zone, pure (island-local in, tested). The zone hugs the glowing frame line (a rounded
    /// rectangle <see cref="Shape.lineOutset"/> outside the base's edge): from <see cref="Shape.outsideWorld"/> world m
    /// outside the line to <see cref="Shape.insideLocal"/> island-local m inside it (over the rock), between
    /// <see cref="Shape.bandTop"/> and <see cref="Shape.bandBottom"/> (island-local y). Never over the play area (inside
    /// the footprint above the cap). Corners: within <see cref="Shape.cornerSize"/> of a corner along both edges (the
    /// corner brackets); the lip: the front face between the corners, below <see cref="Shape.lipTop"/>.
    /// Replaces the step 8e collider shell + reach (up to ~5 cm out, and the palm counted during a pinch: ~10 cm).
    /// </summary>
    public static class GrabZone
    {
        public enum Kind { None, Corner, Lip }

        public struct Shape
        {
            public Vector2 half;        // the base's half footprint (local)
            public float capTop;        // local y of the cap top
            public float lineOutset;    // the frame line, local m outside the base edge
            public float lineRadius;    // its corner radius (local)
            public float scale;         // island lossy scale (local -> world)
            public float outsideWorld;  // reach outside the line (world m)
            public float insideLocal;   // depth inside the line (local m)
            public float bandTop, bandBottom; // local y
            public float cornerSize;    // local m along each edge
            public float lipTop;        // local y: the lip grabs below this
            public bool lipActive;
        }

        /// <summary>Signed horizontal distance (local m) from the frame line: + outside, - inside.</summary>
        public static float LineDistance(Vector3 local, in Shape s)
        {
            float hx = s.half.x + s.lineOutset, hz = s.half.y + s.lineOutset;
            float r = Mathf.Min(s.lineRadius, Mathf.Min(hx, hz) * 0.9f);
            float qx = Mathf.Abs(local.x) - (hx - r), qz = Mathf.Abs(local.z) - (hz - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qz, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qz), 0f) - r;
        }

        /// <summary>Over the play surface (inside the footprint, at or above the cap): never a grab.</summary>
        public static bool OverPlayArea(Vector3 local, in Shape s) =>
            Mathf.Abs(local.x) < s.half.x && Mathf.Abs(local.z) < s.half.y && local.y > s.capTop - 0.003f;

        /// <summary>In the band around the line (any side), ignoring corner / lip.</summary>
        public static bool InBand(Vector3 local, in Shape s)
        {
            if (OverPlayArea(local, s)) return false;
            if (local.y > s.bandTop || local.y < s.bandBottom) return false;
            float d = LineDistance(local, s);
            return d <= s.outsideWorld / Mathf.Max(1e-4f, s.scale) && d >= -s.insideLocal;
        }

        public static bool IsCorner(Vector3 local, in Shape s) =>
            Mathf.Abs(local.x) > s.half.x - s.cornerSize && Mathf.Abs(local.z) > s.half.y - s.cornerSize;

        public static Kind Classify(Vector3 local, in Shape s)
        {
            if (!InBand(local, s)) return Kind.None;
            if (IsCorner(local, s)) return Kind.Corner;
            bool front = local.z > 0f && Mathf.Abs(local.x) <= s.half.x - s.cornerSize && LineDistance(local, s) >= -s.insideLocal &&
                         local.z > s.half.y - s.insideLocal;
            return s.lipActive && front && local.y <= s.lipTop ? Kind.Lip : Kind.None;
        }

        /// <summary>A pinch point within a pod's reach (its radius + clearance, world m) is the pod's, never the island's.</summary>
        public static bool PodWins(Vector3 point, Vector3 podCentre, float reach) => (podCentre - point).sqrMagnitude < reach * reach;

        /// <summary>World distance from a point to the nearest handle zone (0 inside it): the frame lights by it.</summary>
        public static float DistanceToHandles(Vector3 local, in Shape s)
        {
            float sc = Mathf.Max(1e-4f, s.scale);
            float best = float.MaxValue;
            float outLocal = s.outsideWorld / sc;
            // Vertical gap to the band (the lip's band ends at lipTop).
            float bottom = s.bandBottom;
            float Gap(float top) => local.y > top ? local.y - top : local.y < bottom ? bottom - local.y : 0f;
            // Horizontal gap to the ring between -inside and +outside of the line.
            float d = LineDistance(local, s);
            float ring = d > outLocal ? d - outLocal : d < -s.insideLocal ? -s.insideLocal - d : 0f;
            // Along the edge, the gap to the nearest corner region / the lip span.
            float cx = Mathf.Max(0f, s.half.x - s.cornerSize - Mathf.Abs(local.x));
            float cz = Mathf.Max(0f, s.half.y - s.cornerSize - Mathf.Abs(local.z));
            float corner = new Vector3(ring, Gap(s.bandTop), 0f).magnitude;
            corner = Mathf.Sqrt(corner * corner + cx * cx + cz * cz);
            best = Mathf.Min(best, corner);
            if (s.lipActive)
            {
                float lipZ = local.z < s.half.y - s.insideLocal ? s.half.y - s.insideLocal - local.z : 0f;
                float lipRing = Mathf.Max(ring, lipZ);
                float lx = Mathf.Max(0f, Mathf.Abs(local.x) - (s.half.x - s.cornerSize));
                float lip = new Vector3(lipRing, Gap(Mathf.Min(s.bandTop, s.lipTop)), lx).magnitude;
                best = Mathf.Min(best, lip);
            }
            if (OverPlayArea(local, s)) best = Mathf.Max(best, local.y - (s.capTop - 0.003f));
            return best * sc;
        }
    }
}
