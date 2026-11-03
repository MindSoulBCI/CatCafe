using UnityEngine;

namespace SpatialCatCafe
{
    public enum MrHandSide { Left, Right }
    public enum NaturalCatAction { None, WatchHand, SniffHand, LickHand, EnjoyPetting }

    public struct MrHandSample
    {
        public bool Tracked;
        public Vector3 PalmPosition;
        public Vector3 IndexTipPosition;
        public float SampleTime;

        public bool IsFresh(float now, float maxAge)
        {
            float age = now - SampleTime;
            return Tracked && age >= 0 && age <= maxAge && Finite(PalmPosition) && Finite(IndexTipPosition);
        }

        static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Implementations must return live, valid poses in Unity world space, in meters.
    // Timestamp only a newly observed frame; do not renew stale tracking data.
    public abstract class MrHandSource : MonoBehaviour
    {
        public abstract bool TryGetHand(MrHandSide side, out MrHandSample sample);
    }
}
