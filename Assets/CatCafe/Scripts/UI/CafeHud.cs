using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpatialCatCafe
{
    public sealed class CafeHud : MonoBehaviour
    {
        CafeSession session;
        CafeOrbitInput orbit;
        Font font;
        Sprite rounded;
        Texture2D roundedTexture;
        CanvasScaler scaler;
        RectTransform safe, info, dock, header, toastPanel;
        Text nameLabel, description, bond, toast, timeText, soundText, counter;
        readonly Text[] statLabels = new Text[4], selectors = new Text[3];
        readonly Image[] statFills = new Image[4], selectorImages = new Image[3];
        Button feed, play;
        bool portrait;
        int width, height;
        Rect lastSafe;
        float toastUntil;
        readonly Color ink = SceneShapes.Hex("#344C43"), muted = SceneShapes.Hex("#78847A"),
            green = SceneShapes.Hex("#547866"), paper = SceneShapes.Hex("#FBF7ED"), peach = SceneShapes.Hex("#D5A394");

        public void Construct(CafeSession model, CafeOrbitInput input)
        {
            session = model; orbit = input;
            font = Resources.Load<Font>("Fonts/CafeChinese");
            if (!font) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            MakeRoundedSprite();
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
            safe = Rect("Safe area", transform); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            header = Rect("Header", safe);
            Label(header, "PURR & POUR", 19, green, new Vector2(0, 0), new Vector2(270, 28));
            Label(header, "空间猫咖", 37, ink, new Vector2(0, -32), new Vector2(300, 55));
            var time = Button(header, "午后时光", Vector2.zero, new Vector2(154, 48), () => session.ToggleEvening(), paper, ink);
            time.name = "Time button"; timeText = time.GetComponentInChildren<Text>();
            var rt = (RectTransform)time.transform; rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); rt.anchoredPosition = Vector2.zero;
            var audio = Button(header, "声音：开", Vector2.zero, new Vector2(125, 42), () => session.ToggleSound(), paper, ink);
            soundText = audio.GetComponentInChildren<Text>(); rt = (RectTransform)audio.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(1, 1); rt.anchoredPosition = new Vector2(0, -58);

            info = Panel("Cat profile", safe, paper, new Vector2(320, 350));
            Label(info, "今日的陪伴", 16, muted, new Vector2(24, -20), new Vector2(265, 28));
            nameLabel = Label(info, "拿铁", 36, ink, new Vector2(24, -55), new Vector2(265, 50));
            description = Label(info, "", 18, muted, new Vector2(24, -110), new Vector2(265, 68));
            bond = Label(info, "", 18, green, new Vector2(24, -181), new Vector2(265, 29));
            string[] names = { "亲密", "饱腹", "精力", "心情" };
            Color[] colors = { peach, SceneShapes.Hex("#C3AA70"), green, SceneShapes.Hex("#90AFA3") };
            for (int i = 0; i < 4; i++)
            {
                float y = -224 - i * 28;
                statLabels[i] = Label(info, names[i], 16, muted, new Vector2(24, y), new Vector2(92, 25));
                var track = Panel("Stat track", info, SceneShapes.Hex("#E8E8DC"), new Vector2(170, 7));
                track.anchoredPosition = new Vector2(120, y - 8);
                var fill = Panel("Stat fill", track, colors[i], new Vector2(170, 7));
                statFills[i] = fill.GetComponent<Image>();
            }

            dock = Panel("Interaction dock", safe, paper, new Vector2(720, 194));
            Label(dock, "和它一起，慢下来", 20, ink, new Vector2(24, -16), new Vector2(390, 32));
            for (int i = 0; i < 3; i++)
            {
                int catIndex = i;
                var button = Button(dock, CatCatalog.All[i].Name, new Vector2(24 + i * 109, -57), new Vector2(99, 40), () => session.SelectCat(catIndex), paper, green);
                selectorImages[i] = button.GetComponent<Image>(); selectors[i] = button.GetComponentInChildren<Text>();
            }
            counter = Label(dock, "", 15, muted, new Vector2(380, -62), new Vector2(310, 28)); counter.alignment = TextAnchor.MiddleRight;
            Button(dock, "摸摸头", new Vector2(24, -116), new Vector2(154, 54), () => session.Interact(CatAction.Pet), green, Color.white);
            feed = Button(dock, "喂小鱼干", new Vector2(190, -116), new Vector2(154, 54), () => session.Interact(CatAction.Feed), SceneShapes.Hex("#EAE2CF"), ink);
            play = Button(dock, "逗猫球", new Vector2(356, -116), new Vector2(154, 54), () => session.Interact(CatAction.Play), SceneShapes.Hex("#EAE2CF"), ink);
            Button(dock, "休息一下", new Vector2(522, -116), new Vector2(174, 54), () => session.Interact(CatAction.Rest), SceneShapes.Hex("#EAE2CF"), ink);

            toastPanel = Panel("Feedback", safe, new Color(paper.r, paper.g, paper.b, .97f), new Vector2(600, 51));
            toast = Label(toastPanel, "", 19, ink, new Vector2(16, -4), new Vector2(568, 43)); toast.alignment = TextAnchor.MiddleCenter;
            var hint = Label(safe, "拖动环顾 · 双指缩放 · 点猫咪摸摸头", 16, muted, Vector2.zero, new Vector2(600, 30));
            hint.name = "Gesture hint"; hint.alignment = TextAnchor.MiddleCenter;
            var reset = Button(safe, "重置视角", Vector2.zero, new Vector2(126, 43), () => orbit.ResetView(), paper, ink);
            reset.name = "Reset view";
            session.Changed += Refresh; session.Message += ShowMessage;
            Reflow(); Refresh(); ShowMessage("欢迎来到空间猫咖。选一只猫，开始今天的陪伴。");
        }

        void Update()
        {
            if (Screen.width != width || Screen.height != height || Screen.safeArea != lastSafe) Reflow();
            toastPanel.gameObject.SetActive(Time.unscaledTime < toastUntil);
        }
        void Reflow()
        {
            width = Screen.width; height = Screen.height; lastSafe = Screen.safeArea; portrait = height > width;
            scaler.referenceResolution = portrait ? new Vector2(900, 1440) : new Vector2(1440, 900);
            scaler.matchWidthOrHeight = portrait ? 0 : 1;
            safe.anchorMin = new Vector2(lastSafe.xMin / width, lastSafe.yMin / height);
            safe.anchorMax = new Vector2(lastSafe.xMax / width, lastSafe.yMax / height);
            header.anchorMin = new Vector2(0, 1); header.anchorMax = Vector2.one; header.pivot = new Vector2(0, 1);
            header.offsetMin = new Vector2(32, -136); header.offsetMax = new Vector2(-32, -24);
            info.anchorMin = info.anchorMax = new Vector2(0, 1); info.pivot = new Vector2(0, 1); info.anchoredPosition = new Vector2(32, -156);
            info.localScale = Vector3.one * (portrait ? .9f : .87f);
            dock.anchorMin = dock.anchorMax = new Vector2(.5f, 0); dock.pivot = new Vector2(.5f, 0); dock.anchoredPosition = new Vector2(0, 54);
            toastPanel.anchorMin = toastPanel.anchorMax = new Vector2(.5f, 0); toastPanel.pivot = new Vector2(.5f, 0); toastPanel.anchoredPosition = new Vector2(0, 265);
            var hint = (RectTransform)safe.Find("Gesture hint"); hint.anchorMin = hint.anchorMax = new Vector2(.5f, 0); hint.pivot = new Vector2(.5f, 0); hint.anchoredPosition = new Vector2(0, 14);
            var reset = (RectTransform)safe.Find("Reset view"); reset.anchorMin = reset.anchorMax = new Vector2(1, .5f); reset.pivot = new Vector2(1, .5f); reset.anchoredPosition = new Vector2(-32, -50);
        }
        void Refresh()
        {
            nameLabel.text = session.Profile.Name;
            description.text = session.Profile.Description + "\n" + session.Profile.Favorite;
            bond.text = session.Bond;
            float[] values = { session.Current.affection, session.Current.fullness, session.Current.energy, session.Current.happiness };
            string[] names = { "亲密", "饱腹", "精力", "心情" };
            for (int i = 0; i < 4; i++)
            {
                statLabels[i].text = names[i] + " " + Mathf.RoundToInt(values[i]);
                ((RectTransform)statFills[i].transform).sizeDelta = new Vector2(170 * values[i] / 100, 7);
            }
            for (int i = 0; i < 3; i++)
            {
                bool chosen = session.State.selectedCat == i;
                selectorImages[i].color = chosen ? green : SceneShapes.Hex("#ECECDF");
                selectors[i].color = chosen ? Color.white : ink;
            }
            timeText.text = session.State.evening ? "夜晚灯火" : "午后时光";
            soundText.text = session.State.sound ? "声音：开" : "声音：关";
            counter.text = "已陪伴互动 " + session.Current.interactions + " 次";
            feed.interactable = session.Current.fullness < 94;
            play.interactable = session.Current.energy >= 15;
        }
        void ShowMessage(string value) { toast.text = value; toastUntil = Time.unscaledTime + 4; }
        RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); return rect;
        }
        RectTransform Panel(string name, Transform parent, Color color, Vector2 size)
        {
            var rect = Rect(name, parent); rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = color;
            image.raycastTarget = true; return rect;
        }
        Text Label(Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var rect = Rect("Label", parent); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; return label;
        }
        Button Button(Transform parent, string title, Vector2 position, Vector2 size, Action onClick, Color fill, Color foreground)
        {
            var rect = Panel(title, parent, fill, size); rect.anchoredPosition = position;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => onClick());
            var colors = button.colors; colors.highlightedColor = new Color(.92f, .96f, .92f); colors.pressedColor = new Color(.80f, .86f, .80f); colors.disabledColor = new Color(.7f, .7f, .7f, .45f); button.colors = colors;
            var label = Label(rect, title, 19, foreground, Vector2.zero, size); label.alignment = TextAnchor.MiddleCenter;
            return button;
        }
        void MakeRoundedSprite()
        {
            const int side = 64; const float radius = 16;
            roundedTexture = new Texture2D(side, side, TextureFormat.RGBA32, false); roundedTexture.name = "Rounded UI";
            roundedTexture.wrapMode = TextureWrapMode.Clamp; roundedTexture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[side * side];
            for (int y = 0; y < side; y++) for (int x = 0; x < side; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (32 - radius), 0);
                float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (32 - radius), 0);
                pixels[y * side + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy)));
            }
            roundedTexture.SetPixels(pixels); roundedTexture.Apply();
            rounded = Sprite.Create(roundedTexture, new Rect(0, 0, side, side), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, Vector4.one * 18);
        }
        void OnDestroy()
        {
            if (session) { session.Changed -= Refresh; session.Message -= ShowMessage; }
            if (rounded) Destroy(rounded); if (roundedTexture) Destroy(roundedTexture);
        }
    }
}
