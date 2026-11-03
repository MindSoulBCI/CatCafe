using UnityEngine;
using UnityEngine.UI;

namespace SpatialCatCafe
{
    /// <summary>A small head-locked MR status card; it keeps the room and cat as the focus.</summary>
    public sealed class MrCompanionHud : MonoBehaviour
    {
        readonly Color ink = SceneShapes.Hex("#263B35");
        readonly Color muted = SceneShapes.Hex("#60736C");
        readonly Color accent = SceneShapes.Hex("#568270");
        readonly Color paper = new Color(.96f, .97f, .92f, .92f);

        Camera view;
        CafeSession session;
        MrCatPlacementController placement;
        PicoMrHandSource hands;
        Text title, profile, status, affectionLabel;
        Image affectionFill;
        RectTransform root;
        RectTransform feedButton, playButton;
        string message;
        float messageUntil;
        bool editorPreview;

        public void Configure(Camera cameraView, CafeSession model,
            MrCatPlacementController placementController, PicoMrHandSource handSource, bool isEditorPreview)
        {
            view = cameraView;
            session = model;
            placement = placementController;
            hands = handSource;
            editorPreview = isEditorPreview;
            Build();
            session.Changed += Refresh;
            session.Message += OnMessage;
            Refresh();
        }

        void Build()
        {
            var canvasObject = new GameObject("MR Companion Status");
            canvasObject.transform.SetParent(view.transform, false);
            // Keep the cat's body and face clear in the center of the MR view.
            canvasObject.transform.localPosition = new Vector3(0, -.52f, 1.15f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * .00095f;
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = view;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 8;
            canvasObject.AddComponent<GraphicRaycaster>();

            root = canvasObject.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(600, 226);
            var background = canvasObject.AddComponent<Image>();
            background.color = paper;
            background.raycastTarget = false;

            title = Label(canvasObject.transform, "今天的陪伴", 25, ink,
                new Vector2(22, -15), new Vector2(260, 34));
            profile = Label(canvasObject.transform, "", 18, muted,
                new Vector2(22, -50), new Vector2(540, 28));
            affectionLabel = Label(canvasObject.transform, "", 17, accent,
                new Vector2(22, -83), new Vector2(140, 28));

            var trackObject = new GameObject("Affection Track", typeof(RectTransform), typeof(Image));
            trackObject.transform.SetParent(canvasObject.transform, false);
            var track = (RectTransform)trackObject.transform;
            track.anchorMin = track.anchorMax = new Vector2(0, 1);
            track.pivot = new Vector2(0, 1);
            track.anchoredPosition = new Vector2(166, -95);
            track.sizeDelta = new Vector2(380, 12);
            trackObject.GetComponent<Image>().color = new Color(.73f, .80f, .76f, .75f);
            var fillObject = new GameObject("Affection Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            var fillRect = (RectTransform)fillObject.transform;
            fillRect.anchorMin = new Vector2(0, 0); fillRect.anchorMax = new Vector2(0, 1);
            fillRect.pivot = new Vector2(0, .5f); fillRect.sizeDelta = new Vector2(0, 12);
            affectionFill = fillObject.GetComponent<Image>(); affectionFill.color = accent;

            status = Label(canvasObject.transform, "", 16, muted,
                new Vector2(22, -119), new Vector2(555, 27));
            status.alignment = TextAnchor.MiddleLeft;

            feedButton = ActionButton(canvasObject.transform, "喂食 · 小鱼干",
                new Vector2(20, -164), new Vector2(270, 46), () => session.Interact(CatAction.Feed));
            playButton = ActionButton(canvasObject.transform, "玩耍 · 逗猫球",
                new Vector2(310, -164), new Vector2(270, 46), () => session.Interact(CatAction.Play));
        }

        RectTransform ActionButton(Transform parent, string caption, Vector2 position,
            Vector2 dimensions, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObject = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var rect = (RectTransform)buttonObject.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var image = buttonObject.GetComponent<Image>();
            image.color = SceneShapes.Hex("#DDE9E1");
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            var label = Label(buttonObject.transform, caption, 18, ink,
                Vector2.zero, dimensions);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return rect;
        }

        Text Label(Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var child = new GameObject("Label", typeof(RectTransform), typeof(Text));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var label = child.GetComponent<Text>();
            label.font = Resources.Load<Font>("Fonts/CafeChinese") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = value;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        void Update()
        {
            if (!placement || !status) return;
            if (!editorPreview && placement.IsPlaced && hands && hands.TryGetPinchDownPosition(out var pinchTip))
            {
                if (PinchInside(feedButton, pinchTip)) session.Interact(CatAction.Feed);
                else if (PinchInside(playButton, pinchTip)) session.Interact(CatAction.Play);
            }
            string inputHint = editorPreview
                ? "按住鼠标引导猫咪 · R 重新放置"
                : placement.IsPlaced
                    ? "伸出食指指向地面引导猫咪 · 抚摸头背与它互动 · 双手捏合保持 1 秒可重新放置"
                    : placement.Status;
            status.text = !string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil
                ? message
                : placement.IsPlaced && !editorPreview
                    ? inputHint
                    : placement.Status + (editorPreview ? "  " + inputHint : "");
        }

        bool PinchInside(RectTransform button, Vector3 worldPosition)
        {
            if (!button) return false;
            Vector3 local = button.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.z) <= 18 && button.rect.Contains(new Vector2(local.x, local.y));
        }

        void OnMessage(string value)
        {
            message = value;
            messageUntil = Time.unscaledTime + 2.4f;
        }

        void Refresh()
        {
            if (!session || !profile) return;
            var current = session.Current;
            profile.text = session.Profile.Name + "  ·  " + session.Profile.Favorite + "  ·  " + session.Bond;
            affectionLabel.text = "亲密 " + Mathf.RoundToInt(current.affection) + "%";
            var rect = (RectTransform)affectionFill.transform;
            rect.sizeDelta = new Vector2(380 * current.affection / 100f, 12);
        }

        void OnDestroy()
        {
            if (!session) return;
            session.Changed -= Refresh;
            session.Message -= OnMessage;
        }
    }
}
