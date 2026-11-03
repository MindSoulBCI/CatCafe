using UnityEngine;

namespace SpatialCatCafe
{
    /// <summary>Places one companion on a horizontal real-world mesh hit.</summary>
    public sealed class MrCatPlacementController : MonoBehaviour
    {
        [SerializeField] float maxPlacementDistance = 3.5f;
        [SerializeField, Range(.65f, .95f)] float minimumUpDot = .78f;

        Camera view;
        Transform catRoot;
        MrCatBrain brain;
        PicoMrHandSource hands;
        bool editorPreview, relocating, hasCandidate, placed;
        float relocateHold;
        Vector3 candidatePosition, candidateNormal;
        readonly RaycastHit[] hits = new RaycastHit[32];

        public bool IsPlaced => placed;
        public string Status { get; private set; }

        public void Configure(Camera cameraView, Transform companion, MrCatBrain catBrain,
            PicoMrHandSource handSource, bool isEditorPreview)
        {
            view = cameraView;
            catRoot = companion;
            brain = catBrain;
            hands = handSource;
            editorPreview = isEditorPreview;
            Status = editorPreview
                ? "桌面预览：猫咪已放在模拟表面上。"
                : "猫咪就在你面前。看向桌面再轻捏，可以把它挪过去。";
        }

        void Update()
        {
            if (!view || !catRoot || !brain) return;
            if (!editorPreview && !placed && !relocating)
            {
                PlaceInFrontOfWearer();
                return;
            }

#if UNITY_EDITOR
            if (editorPreview && placed && Input.GetKeyDown(KeyCode.R)) RequestRelocation();
#endif
            if (placed && !relocating)
            {
                relocateHold = hands && hands.BothHandsPinching ? relocateHold + Time.unscaledDeltaTime : 0;
                if (relocateHold >= 1.1f)
                {
                    relocateHold = 0;
                    RequestRelocation();
                }
                else return;
            }

            hasCandidate = TryFindSurface(out var hit);
            if (hasCandidate)
            {
                candidatePosition = hit.point;
                candidateNormal = hit.normal.normalized;
                Status = editorPreview
                    ? "按 R 重新定位；靠近猫咪即可触发互动预览。"
                    : "找到合适表面。轻捏手指，把猫咪放在这里。";
            }
            else
            {
                Status = editorPreview
                    ? "把视线移到模拟地面，再轻点鼠标放置。"
                    : "正在寻找平坦的地面或桌面……";
            }

            bool confirm = hands && hands.PinchDownThisFrame;
#if UNITY_EDITOR
            if (editorPreview) confirm |= Input.GetMouseButtonDown(0);
#endif
            if (hasCandidate && confirm) PlaceAt(candidatePosition, candidateNormal);
        }

        bool TryFindSurface(out RaycastHit best)
        {
            best = default;
            Ray ray = editorPreview
                ? view.ScreenPointToRay(Input.mousePosition)
                : new Ray(view.transform.position, view.transform.forward);
            int count = Physics.RaycastNonAlloc(ray,
                hits, maxPlacementDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (!hit.collider || hit.collider.transform == catRoot || hit.collider.transform.IsChildOf(catRoot))
                    continue;
                if (Vector3.Dot(hit.normal.normalized, Vector3.up) < minimumUpDot) continue;
                if (hit.distance >= closest) continue;
                closest = hit.distance;
                best = hit;
            }
            return closest < float.PositiveInfinity;
        }

        public void RequestRelocation()
        {
            relocating = true;
            placed = false;
            relocateHold = 0;
            hasCandidate = false;
            brain.SetPlaced(false);
            catRoot.gameObject.SetActive(false);
            Status = "看向新的地面或桌面，轻捏手指重新放置。";
        }

        void PlaceInFrontOfWearer()
        {
            catRoot.SetParent(view.transform, false);
            catRoot.localPosition = new Vector3(0f, -.25f, .75f);
            catRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
            catRoot.gameObject.SetActive(true);
            brain.SetPlaced(true);
            placed = true;
            relocating = false;
            Status = "猫咪在你眼前。";
        }

        public void PlaceForPreview(Vector3 position, Quaternion rotation)
        {
            candidatePosition = position;
            candidateNormal = Vector3.up;
            PlaceAt(position, Vector3.up, rotation);
        }

        void PlaceAt(Vector3 position, Vector3 normal)
        {
            Vector3 toViewer = view.transform.position - position;
            toViewer = Vector3.ProjectOnPlane(toViewer, normal);
            Quaternion rotation = toViewer.sqrMagnitude > .0001f
                ? Quaternion.LookRotation(-toViewer.normalized, normal)
                : Quaternion.identity;
            PlaceAt(position, normal, rotation);
        }

        void PlaceAt(Vector3 position, Vector3 normal, Quaternion rotation)
        {
            catRoot.gameObject.SetActive(true);
            catRoot.SetPositionAndRotation(position + normal * .008f, rotation);
            brain.SetPlaced(true);
            placed = true;
            relocating = false;
            hasCandidate = false;
            Status = "猫咪安顿好了。伸手靠近它，它会闻闻你的手。";
        }
    }
}
