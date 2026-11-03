using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace SpatialCatCafe
{
    /// <summary>Lets a pointing finger guide the placed cat across detected horizontal surfaces.</summary>
    public sealed class MrCatGuideController : MonoBehaviour
    {
        [SerializeField] float guideDistance = 3.5f;
        [SerializeField] float walkSpeed = .48f;
        [SerializeField, Range(.65f, .95f)] float minimumUpDot = .78f;

        Camera view;
        Transform catRoot;
        CatActor actor;
        MrCatBrain brain;
        PicoMrHandSource hands;
        bool editorPreview, hasDestination;
        Vector3 destination, editorGuideOrigin, surfaceNormal = Vector3.up;
        LineRenderer beam;
        Material beamMaterial;
        readonly RaycastHit[] hits = new RaycastHit[32];

        public void Configure(Camera cameraView, Transform companion, CatActor cat,
            MrCatBrain catBrain, PicoMrHandSource handSource, bool isEditorPreview)
        {
            view = cameraView;
            catRoot = companion;
            actor = cat;
            brain = catBrain;
            hands = handSource;
            editorPreview = isEditorPreview;
            editorGuideOrigin = companion ? companion.position : Vector3.zero;
            CreateBeam();
        }

        void CreateBeam()
        {
            var beamObject = new GameObject("Finger Guide Ray");
            beam = beamObject.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.startWidth = .009f;
            beam.endWidth = .004f;
            beam.numCapVertices = 4;
            beam.shadowCastingMode = ShadowCastingMode.Off;
            beam.receiveShadows = false;
            var mint = SceneShapes.Hex("#79E8CE");
            beam.startColor = mint;
            beam.endColor = new Color(mint.r, mint.g, mint.b, .55f);
            beamMaterial = new Material(Shader.Find("Sprites/Default"));
            beam.sharedMaterial = beamMaterial;
            beam.enabled = false;
        }

        void Update()
        {
            if (!view || !catRoot || !actor || !catRoot.gameObject.activeInHierarchy)
            {
                HideBeam();
                if (actor) actor.SetWalking(false);
                return;
            }

            bool hasRay = false;
            Ray ray = default;
#if UNITY_EDITOR
            if (editorPreview)
            {
                // The HUD's translucent panel is a raycast target across its full bounds.
                // IsPointerOverGameObject therefore swallowed every drag over the card, even
                // when the pointer was not on a button. Only actual buttons should consume it.
                if (Input.GetMouseButton(0) && !IsPointerOverButton())
                {
                    ray = view.ScreenPointToRay(Input.mousePosition);
                    hasRay = true;
                }
            }
#endif
            if (!editorPreview && hands) hasRay = hands.TryGetPointingRay(out ray);

            if (hasRay)
            {
                beam.enabled = true;
                Vector3 lineStart = ray.origin + ray.direction * .015f;
                if (TryFindSurface(ray, out RaycastHit hit))
                {
                    surfaceNormal = hit.normal.normalized;
                    destination = hit.point + surfaceNormal * .008f;
                    hasDestination = true;
                    beam.SetPosition(0, lineStart);
                    beam.SetPosition(1, destination);
                }
                else
                {
                    hasDestination = false;
                    beam.SetPosition(0, lineStart);
                    beam.SetPosition(1, ray.GetPoint(guideDistance));
                }
            }
            else HideBeam();

            // Pause movement during close hand contact so petting stays comfortable.
            if (brain && (brain.Action == NaturalCatAction.EnjoyPetting ||
                brain.Action == NaturalCatAction.LickHand)) hasDestination = false;

            if (!hasDestination)
            {
                actor.SetWalking(false);
                return;
            }

            Vector3 delta = Vector3.ProjectOnPlane(destination - catRoot.position, surfaceNormal);
            if (delta.magnitude <= .07f)
            {
                actor.SetWalking(false);
                return;
            }

            actor.SetWalking(true);
            // Cat meshes face local -Z, so orient their back axis toward the travel vector.
            Quaternion facing = Quaternion.LookRotation(-delta.normalized, surfaceNormal);
            catRoot.rotation = Quaternion.RotateTowards(catRoot.rotation, facing, 540 * Time.deltaTime);
            catRoot.position = Vector3.MoveTowards(catRoot.position, destination, walkSpeed * Time.deltaTime);
        }

        bool TryFindSurface(Ray ray, out RaycastHit best)
        {
            best = default;
#if UNITY_EDITOR
            // The Editor preview's invisible placement disk is deliberately small and can sit
            // behind the cat collider. Project the mouse ray onto the preview floor instead,
            // then keep the goal nearby so the cat stays in view while following the cursor.
            if (editorPreview && ray.direction.y < -.02f)
            {
                float distanceToFloor = -ray.origin.y / ray.direction.y;
                if (distanceToFloor > 0)
                {
                    Vector3 offset = Vector3.ProjectOnPlane(ray.GetPoint(distanceToFloor) - editorGuideOrigin, Vector3.up);
                    if (offset.sqrMagnitude < .0004f) offset = Vector3.ProjectOnPlane(ray.direction, Vector3.up) * .25f;
                    offset = Vector3.ClampMagnitude(offset, 1.15f);
                    best.point = editorGuideOrigin + offset;
                    best.normal = Vector3.up;
                    best.distance = distanceToFloor;
                    return true;
                }
            }
#endif
            int count = Physics.RaycastNonAlloc(ray, hits, guideDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (!hit.collider || hit.collider.transform == catRoot || hit.collider.transform.IsChildOf(catRoot)
                    || Vector3.Dot(hit.normal.normalized, Vector3.up) < minimumUpDot || hit.distance >= closest)
                    continue;
                closest = hit.distance;
                best = hit;
            }
            return closest < float.PositiveInfinity;
        }

#if UNITY_EDITOR
        static bool IsPointerOverButton()
        {
            if (!EventSystem.current) return false;
            var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            foreach (var result in results)
                if (result.gameObject && result.gameObject.GetComponentInParent<Button>()) return true;
            return false;
        }
#endif

        void HideBeam()
        {
            if (beam) beam.enabled = false;
        }

        void OnDestroy()
        {
            if (beamMaterial) Destroy(beamMaterial);
        }
    }
}
