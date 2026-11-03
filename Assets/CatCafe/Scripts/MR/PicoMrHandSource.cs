using UnityEngine;
using UnityEngine.XR;
#if ENABLE_PICO_XR_SDK
using ByteDance.PICO.XR;
#endif

namespace SpatialCatCafe
{
    /// <summary>Adapts PICO's tracked palm and index tip joints to the app's MR hand contract.</summary>
    public sealed class PicoMrHandSource : MrHandSource
    {
        int pinchDownFrame = -10;
        Vector3 pinchDownPosition;
        public bool PinchDownThisFrame => Time.frameCount - pinchDownFrame <= 1;
        public bool BothHandsPinching { get; private set; }

        public bool TryGetPinchDownPosition(out Vector3 worldPosition)
        {
            worldPosition = default;
#if ENABLE_PICO_XR_SDK
            if (!PinchDownThisFrame || Time.frameCount - pinchDownFrame > 1) return false;
            worldPosition = pinchDownPosition;
            return true;
#else
            return false;
#endif
        }

        public bool TryGetPointingRay(out Ray ray)
        {
            ray = default;
#if ENABLE_PICO_XR_SDK
            if (!XRSettings.isDeviceActive || trackedHands == null) return false;
            if (TryGetPointingRay(HandType.HandRight, out ray)) return true;
            return TryGetPointingRay(HandType.HandLeft, out ray);
#else
            return false;
#endif
        }

#if ENABLE_PICO_XR_SDK
        PXR_Hand[] trackedHands;
        bool wasPinching;

        void Awake() => RefreshHands();

        void Update()
        {
            if (!XRSettings.isDeviceActive)
            {
                wasPinching = false;
                BothHandsPinching = false;
#if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0)) pinchDownFrame = Time.frameCount;
#endif
                return;
            }

            bool leftPinching = false, rightPinching = false;
            if (trackedHands == null || trackedHands.Length == 0) RefreshHands();
            foreach (var hand in trackedHands)
            {
                if (!IsPinching(hand)) continue;
                if (hand.handType == HandType.HandLeft) leftPinching = true;
                if (hand.handType == HandType.HandRight) rightPinching = true;
            }
            bool pinching = leftPinching || rightPinching;
            BothHandsPinching = leftPinching && rightPinching;
            if (pinching && !wasPinching)
            {
                foreach (var hand in trackedHands)
                {
                    if (!IsPinching(hand)) continue;
                    int tipIndex = (int)HandJoint.JointIndexTip;
                    if (hand.handJoints.Count <= tipIndex || !hand.handJoints[tipIndex]) continue;
                    pinchDownPosition = hand.handJoints[tipIndex].position;
                    pinchDownFrame = Time.frameCount;
                    break;
                }
            }
            wasPinching = pinching;
        }

        static bool IsPinching(PXR_Hand hand)
        {
            int thumbTip = (int)HandJoint.JointThumbTip;
            int indexTip = (int)HandJoint.JointIndexTip;
            return hand && hand.isActiveAndEnabled && hand.Computed && hand.handJoints.Count > indexTip
                && hand.handJoints[thumbTip] && hand.handJoints[indexTip]
                && Vector3.Distance(hand.handJoints[thumbTip].position, hand.handJoints[indexTip].position) < .035f;
        }

        public override bool TryGetHand(MrHandSide side, out MrHandSample sample)
        {
            sample = default;
            if (!XRSettings.isDeviceActive) return false;
            if (trackedHands == null || trackedHands.Length == 0) RefreshHands();

            HandType requested = side == MrHandSide.Left ? HandType.HandLeft : HandType.HandRight;
            foreach (var hand in trackedHands)
            {
                if (!hand || !hand.isActiveAndEnabled || hand.handType != requested) continue;
                var locations = new HandJointLocations();
                if (!hand.Computed || !PXR_HandTracking.GetJointLocations(requested, ref locations)
                    || locations.isActive == 0 || locations.jointCount <= (uint)HandJoint.JointIndexTip
                    || locations.jointLocations == null) return false;

                const ulong trackedPosition = XrSpaceLocationFlags.XR_SPACE_LOCATION_POSITION_TRACKED_BIT;
                var palmStatus = locations.jointLocations[(int)HandJoint.JointPalm].locationStatus;
                var tipStatus = locations.jointLocations[(int)HandJoint.JointIndexTip].locationStatus;
                if (((ulong)palmStatus & trackedPosition) == 0 || ((ulong)tipStatus & trackedPosition) == 0)
                    return false;

                int palmIndex = (int)HandJoint.JointPalm;
                int tipIndex = (int)HandJoint.JointIndexTip;
                if (hand.handJoints.Count <= tipIndex || !hand.handJoints[palmIndex] || !hand.handJoints[tipIndex])
                    return false;

                sample = new MrHandSample
                {
                    Tracked = true,
                    PalmPosition = hand.handJoints[palmIndex].position,
                    IndexTipPosition = hand.handJoints[tipIndex].position,
                    SampleTime = Time.unscaledTime
                };
                return true;
            }
            return false;
        }

        void RefreshHands() => trackedHands = FindObjectsByType<PXR_Hand>(FindObjectsSortMode.None);

        bool TryGetPointingRay(HandType side, out Ray ray)
        {
            ray = default;
            foreach (var hand in trackedHands)
            {
                if (!hand || !hand.isActiveAndEnabled || hand.handType != side || !hand.Computed)
                    continue;

                int palm = (int)HandJoint.JointPalm;
                int indexProximal = (int)HandJoint.JointIndexProximal;
                int indexTip = (int)HandJoint.JointIndexTip;
                int middleTip = (int)HandJoint.JointMiddleTip;
                int ringTip = (int)HandJoint.JointRingTip;
                int littleTip = (int)HandJoint.JointLittleTip;
                if (hand.handJoints.Count <= littleTip || !hand.handJoints[palm]
                    || !hand.handJoints[indexProximal] || !hand.handJoints[indexTip]
                    || !hand.handJoints[middleTip] || !hand.handJoints[ringTip] || !hand.handJoints[littleTip])
                    continue;

                Vector3 palmPosition = hand.handJoints[palm].position;
                Vector3 indexPosition = hand.handJoints[indexTip].position;
                float indexReach = Vector3.Distance(palmPosition, indexPosition);
                bool indexExtended = indexReach > .13f;
                bool otherFingersCurled =
                    Vector3.Distance(palmPosition, hand.handJoints[middleTip].position) < indexReach * .82f &&
                    Vector3.Distance(palmPosition, hand.handJoints[ringTip].position) < indexReach * .82f &&
                    Vector3.Distance(palmPosition, hand.handJoints[littleTip].position) < indexReach * .82f;
                Vector3 direction = indexPosition - hand.handJoints[indexProximal].position;
                if (!indexExtended || !otherFingersCurled || direction.sqrMagnitude < .0001f) continue;

                ray = new Ray(indexPosition, direction.normalized);
                return true;
            }
            return false;
        }
#else
        public override bool TryGetHand(MrHandSide side, out MrHandSample sample)
        {
            sample = default;
            return false;
        }
#endif
    }
}
