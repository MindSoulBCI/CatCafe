using UnityEngine;
using UnityEngine.EventSystems;

namespace SpatialCatCafe
{
    public sealed class CafeOrbitInput : MonoBehaviour
    {
        SpatialCatInteractor interactor;
        Camera view;
        Vector3 target = new Vector3(0, .9f, .1f);
        float yaw = -12, pitch = 27, distance = 11;
        Vector2 press, previous;
        bool tracking, blocked, dragged;
        int lastTouchCount;
        public bool Portrait => Screen.height > Screen.width;
        public void Configure(SpatialCatInteractor target, Camera camera) { interactor = target; view = camera; ResetView(); }
        public void ResetView() { yaw = -12; pitch = 27; distance = 11; }
        bool OverUI(int finger = -1) => EventSystem.current && (finger < 0 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(finger));
        void Update()
        {
            if (UnityEngine.Input.touchCount > 0)
            {
                if (UnityEngine.Input.touchCount == 2)
                {
                    var a = UnityEngine.Input.GetTouch(0); var b = UnityEngine.Input.GetTouch(1);
                    tracking = false;
                    if (!OverUI(a.fingerId) && !OverUI(b.fingerId) && lastTouchCount == 2)
                    {
                        float old = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                        distance = Mathf.Clamp(distance - ((a.position - b.position).magnitude - old) * .012f, 6.5f, 15);
                    }
                }
                else if (UnityEngine.Input.touchCount == 1)
                {
                    var touch = UnityEngine.Input.GetTouch(0);
                    if (touch.phase == TouchPhase.Began) Begin(touch.position, OverUI(touch.fingerId));
                    if (touch.phase == TouchPhase.Moved) Move(touch.position);
                    if (touch.phase == TouchPhase.Ended) End(touch.position);
                    if (touch.phase == TouchPhase.Canceled) tracking = false;
                }
                else tracking = false;
                lastTouchCount = UnityEngine.Input.touchCount;
            }
            else
            {
                lastTouchCount = 0;
                if (UnityEngine.Input.GetMouseButtonDown(0)) Begin(UnityEngine.Input.mousePosition, OverUI());
                if (UnityEngine.Input.GetMouseButton(0)) Move(UnityEngine.Input.mousePosition);
                if (UnityEngine.Input.GetMouseButtonUp(0)) End(UnityEngine.Input.mousePosition);
                if (!OverUI()) distance = Mathf.Clamp(distance - UnityEngine.Input.mouseScrollDelta.y * .6f, 6.5f, 15);
            }
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            float fittedDistance = distance * (Portrait ? 1.40f : 1);
            view.transform.position = Vector3.Lerp(view.transform.position, target + rotation * new Vector3(0, 0, -fittedDistance), 1 - Mathf.Exp(-Time.unscaledDeltaTime * 12));
            view.transform.LookAt(target);
        }
        void Begin(Vector2 p, bool ui) { tracking = true; blocked = ui; dragged = false; press = previous = p; }
        void Move(Vector2 p)
        {
            if (!tracking || blocked) return;
            if (Vector2.Distance(press, p) > Mathf.Max(10, Screen.width * .009f)) dragged = true;
            if (dragged)
            {
                var delta = p - previous;
                yaw = Mathf.Clamp(yaw - delta.x / Screen.width * 130, -60, 60);
                pitch = Mathf.Clamp(pitch + delta.y / Screen.height * 70, 12, 55);
            }
            previous = p;
        }
        void End(Vector2 p)
        {
            if (tracking && !blocked && !dragged) interactor.TryInteract(view.ScreenPointToRay(p));
            tracking = false;
        }
    }
}
