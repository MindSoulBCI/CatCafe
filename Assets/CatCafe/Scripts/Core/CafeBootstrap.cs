using UnityEngine;
using UnityEngine.EventSystems;

namespace SpatialCatCafe
{
    [DisallowMultipleComponent]
    public sealed class CafeBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 72;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadowDistance = 8;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.pixelLightCount = 1;

            var session = gameObject.AddComponent<CafeSession>();
            var shapes = gameObject.AddComponent<SceneShapes>();
            // The desktop Editor always gets the mouse driven preview, even when the PICO XR
            // simulator reports an active XR device. Device behavior is selected in the Android build.
            bool editorPreview = Application.isEditor;
            BuildCompanion(session, shapes, editorPreview);
            gameObject.AddComponent<CafeAudio>().Configure(session);
        }

        void BuildCompanion(CafeSession session, SceneShapes shapes, bool editorPreview)
        {
            Camera view = Camera.main;
            if (!view)
            {
                var cameraObject = new GameObject("MR Camera");
                cameraObject.tag = "MainCamera";
                view = cameraObject.AddComponent<Camera>();
            }

            view.clearFlags = CameraClearFlags.SolidColor;
            if (editorPreview)
            {
                view.backgroundColor = SceneShapes.Hex("#DCE4E1");
                view.nearClipPlane = .05f;
                view.farClipPlane = 20;
                view.transform.position = new Vector3(0, 1.52f, -2.35f);
                view.transform.rotation = Quaternion.Euler(14, 0, 0);
                if (!view.GetComponent<AudioListener>()) view.gameObject.AddComponent<AudioListener>();
                CreatePreviewLighting(shapes);
            }
            else
            {
                view.backgroundColor = new Color(0, 0, 0, 0);
                CreatePreviewLighting(shapes);
            }

            var catRoot = new GameObject("MR Companion Cat");
            catRoot.transform.SetParent(transform, false);
            catRoot.transform.position = new Vector3(0, 0, 0);
            var cat = catRoot.AddComponent<CatActor>();
            cat.Construct(shapes, session.State.selectedCat);
            cat.SetSelected(true);

            var hands = gameObject.AddComponent<PicoMrHandSource>();
            var brain = catRoot.AddComponent<MrCatBrain>();
            brain.Configure(cat, hands, session);
            var placement = gameObject.AddComponent<MrCatPlacementController>();
            placement.Configure(view, catRoot.transform, brain, hands, editorPreview);
            var guide = gameObject.AddComponent<MrCatGuideController>();
            guide.Configure(view, catRoot.transform, cat, brain, hands, editorPreview);

            if (editorPreview)
            {
                CreatePreviewSurface(shapes);
                placement.PlaceForPreview(Vector3.zero, Quaternion.identity);
            }

            if (editorPreview)
            {
                var hud = gameObject.AddComponent<MrCompanionHud>();
                hud.Configure(view, session, placement, hands, true);
            }

            // The XR hand or controller rays are the interaction affordance in MR.
            // This EventSystem stays only in the desktop preview for mouse clicks.
            if (editorPreview)
            {
                var events = new GameObject("EventSystem");
                events.transform.SetParent(transform, false);
                events.AddComponent<EventSystem>();
                events.AddComponent<StandaloneInputModule>();
                var interactor = gameObject.AddComponent<SpatialCatInteractor>();
                interactor.Configure(session);
            }
        }

        void CreatePreviewLighting(SceneShapes shapes)
        {
            var light = new GameObject("Preview daylight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SceneShapes.Hex("#FFF2D8");
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48, -28, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = SceneShapes.Hex("#AABAB5");
        }

        void CreatePreviewSurface(SceneShapes shapes)
        {
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            surface.name = "Editor-only placement surface";
            surface.transform.position = new Vector3(0, -.045f, 0);
            surface.transform.localScale = new Vector3(1.45f, .025f, 1.45f);
            var renderer = surface.GetComponent<Renderer>();
            renderer.material.color = SceneShapes.Hex("#C5D0CC");
        }
    }
}
