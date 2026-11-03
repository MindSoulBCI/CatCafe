using UnityEngine;

namespace SpatialCatCafe
{
    public sealed class MrCatBrain : MonoBehaviour
    {
        [SerializeField] float attentionDistance = .65f;
        [SerializeField] float sniffDistance = .18f;
        [SerializeField] float lickDistance = .09f;
        [SerializeField] float releaseDistance = .14f;
        [SerializeField] float stableHandSeconds = .45f;
        [SerializeField] float maxLickSpeed = .22f;
        [SerializeField] float maxTrackingAge = .15f;
        [SerializeField] float headContactDistance = .065f;
        public NaturalCatAction Action { get; private set; }

        CatActor actor;
        MrHandSource hands;
        CafeSession session;
        MrHandSide chosenHand;
        bool placed, hasHand, hasPrevious, focused = true, paused;
        Vector3 previousPalm;
        float previousSampleTime, stableSince = -1, nextAffection;

        public void Configure(CatActor cat, MrHandSource source, CafeSession model)
        {
            actor = cat; hands = source; session = model;
            if (session) session.Acted += OnActed;
            SetPlaced(false);
        }

        void OnActed(CatAction action, int catIndex)
        {
            if (actor && catIndex == actor.Index) actor.React(action);
        }

        public void SetPlaced(bool value)
        {
            placed = value; ResetContact();
            if (actor) actor.SetNaturalPose(NaturalCatAction.None, Vector3.zero);
        }

        void Update()
        {
            if (!actor || !hands || !placed || !focused || paused)
            { ClearPose(); return; }
            float now = Time.unscaledTime;
            if (!FindHand(now, out var sample)) { ClearPose(); return; }

            float speed = float.PositiveInfinity;
            if (hasPrevious && sample.SampleTime > previousSampleTime)
            {
                float elapsed = sample.SampleTime - previousSampleTime;
                if (elapsed <= maxTrackingAge)
                    speed = Vector3.Distance(sample.PalmPosition, previousPalm) / elapsed;
            }
            // Display frames may be faster than tracking frames. Hold a fresh pose
            // between samples, but accumulate dwell only when a new sample arrives.
            if (hasPrevious && sample.SampleTime < previousSampleTime) { ClearPose(); return; }
            if (hasPrevious && sample.SampleTime == previousSampleTime)
            { actor.SetNaturalPose(Action, sample.PalmPosition); return; }
            previousPalm = sample.PalmPosition; previousSampleTime = sample.SampleTime; hasPrevious = true;

            float noseDistance = Vector3.Distance(sample.PalmPosition, actor.NosePosition);
            if (actor.IsPettingContact(sample.IndexTipPosition, headContactDistance) && speed >= .025f && speed <= .8f)
            {
                stableSince = -1;
                SetAction(NaturalCatAction.EnjoyPetting, sample.PalmPosition, now); return;
            }
            float allowedDistance = Action == NaturalCatAction.LickHand ? releaseDistance : lickDistance;
            if (noseDistance <= allowedDistance && speed <= maxLickSpeed)
            {
                if (stableSince < 0) stableSince = sample.SampleTime;
                SetAction(sample.SampleTime - stableSince >= stableHandSeconds ? NaturalCatAction.LickHand : NaturalCatAction.SniffHand,
                    sample.PalmPosition, now); return;
            }
            stableSince = -1;
            SetAction(noseDistance <= sniffDistance ? NaturalCatAction.SniffHand : NaturalCatAction.WatchHand,
                sample.PalmPosition, now);
        }

        bool FindHand(float now, out MrHandSample sample)
        {
            if (hasHand && hands.TryGetHand(chosenHand, out sample) && sample.IsFresh(now, maxTrackingAge)
                && Vector3.Distance(sample.PalmPosition, actor.NosePosition) <= attentionDistance) return true;

            bool left = hands.TryGetHand(MrHandSide.Left, out var l) && l.IsFresh(now, maxTrackingAge);
            bool right = hands.TryGetHand(MrHandSide.Right, out var r) && r.IsFresh(now, maxTrackingAge);
            float ld = left ? Vector3.Distance(l.PalmPosition, actor.NosePosition) : float.PositiveInfinity;
            float rd = right ? Vector3.Distance(r.PalmPosition, actor.NosePosition) : float.PositiveInfinity;
            if (Mathf.Min(ld, rd) > attentionDistance) { sample = default; return false; }
            chosenHand = ld <= rd ? MrHandSide.Left : MrHandSide.Right;
            sample = chosenHand == MrHandSide.Left ? l : r;
            hasHand = true; hasPrevious = false; stableSince = -1; return true;
        }

        void SetAction(NaturalCatAction value, Vector3 target, float now)
        {
            bool enteringContact = Action != value &&
                (value == NaturalCatAction.LickHand || value == NaturalCatAction.EnjoyPetting);
            Action = value; actor.SetNaturalPose(value, target);
            if (enteringContact && session && now >= nextAffection)
            { nextAffection = now + 6; session.Interact(CatAction.Pet); }
        }
        void ResetContact() { Action = NaturalCatAction.None; hasHand = hasPrevious = false; stableSince = -1; }
        void ClearPose() { ResetContact(); if (actor) actor.SetNaturalPose(NaturalCatAction.None, Vector3.zero); }
        void OnDisable() { ClearPose(); }
        void OnDestroy() { if (session) session.Acted -= OnActed; }
        void OnApplicationFocus(bool value) { focused = value; if (!value) ClearPose(); }
        void OnApplicationPause(bool value) { paused = value; if (value) ClearPose(); }
    }
}
