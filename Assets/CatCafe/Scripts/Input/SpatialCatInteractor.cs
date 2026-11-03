using UnityEngine;

namespace SpatialCatCafe
{
    // SDK-independent world interaction bridge. An XR controller or hand adapter
    // can call these methods after its own tracking, permission and gesture checks.
    // This component itself does not initialize OpenXR or acquire hand data.
    public sealed class SpatialCatInteractor : MonoBehaviour
    {
        CafeSession session;
        readonly Collider[] nearby = new Collider[16];
        public void Configure(CafeSession value) { session = value; }

        public bool TryInteract(Ray worldRay, CatAction action = CatAction.Pet)
        {
            if (!session || !Physics.Raycast(worldRay, out var hit, 80)) return false;
            var cat = hit.collider.GetComponentInParent<CatActor>();
            if (!cat) return false;
            if (session.State.selectedCat != cat.Index) session.SelectCat(cat.Index);
            else session.Interact(action);
            return true;
        }

        public bool TryPetAtPosition(Vector3 worldPosition, float radius = .06f)
        {
            if (!session || radius <= 0) return false;
            int count = Physics.OverlapSphereNonAlloc(worldPosition, Mathf.Min(radius, .15f), nearby);
            CatActor nearest = null; float closest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var cat = nearby[i].GetComponentInParent<CatActor>();
                if (!cat) continue;
                float distance = (nearby[i].ClosestPoint(worldPosition) - worldPosition).sqrMagnitude;
                if (distance < closest) { nearest = cat; closest = distance; }
            }
            if (!nearest) return false;
            if (session.State.selectedCat != nearest.Index) session.SelectCat(nearest.Index);
            session.Interact(CatAction.Pet); return true;
        }

        public void InteractSelected(CatAction action) { if (session) session.Interact(action); }
    }
}
