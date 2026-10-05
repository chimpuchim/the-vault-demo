using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Palette. Accent is replaced by the campaign colour at start-up.</summary>
    public static class Theme
    {
        public static Color Accent = Hex("#D4AF37");
        public static readonly Color AccentLight = Hex("#F3E5AB");
        public static readonly Color Bg = Hex("#07090F");
        public static readonly Color Panel = Hex("#10141F");
        public static readonly Color PanelLight = Hex("#1A2030");
        public static readonly Color Line = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Text = Hex("#F4F5F7");
        public static readonly Color Muted = Hex("#8A93A6");
        public static readonly Color Pass = Hex("#2ED3A3");
        public static readonly Color Fail = Hex("#E5484D");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        public static Color WithA(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Procedural sprites so the prototype needs no art files.</summary>
    public static class Sprites
    {
        static Sprite _circle, _rounded, _glow, _ring, _ringThin, _square;

        public static Sprite Circle => _circle ??= MakeCircle(256, 0f);
        public static Sprite Ring => _ring ??= MakeCircle(512, 0.86f);
        public static Sprite RingThin => _ringThin ??= MakeCircle(512, 0.95f);
        public static Sprite Glow => _glow ??= MakeGlow(256);
        public static Sprite Rounded => _rounded ??= MakeRounded(96, 28);
        public static Sprite Square => _square ??= MakeSquare();

        static Sprite MakeSquare()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
        }

        /// <summary>Anti-aliased disc; innerFraction &gt; 0 cuts a hole and gives a ring.</summary>
        static Sprite MakeCircle(int size, float innerFraction)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float r = size * 0.5f - 1f;
            float ri = r * innerFraction;
            var c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                float a = Mathf.Clamp01(r - d + 0.5f);
                if (innerFraction > 0) a *= Mathf.Clamp01(d - ri + 0.5f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite MakeGlow(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            float r = size * 0.5f;
            var c = new Vector2(r, r);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / r;
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                px[y * size + x] = new Color(1, 1, 1, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius - d + 0.5f));
            }
            tex.SetPixels(px);
            tex.Apply();
            var b = radius + 2;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }
    }

    /// <summary>Scale-on-press feedback for anything tappable.</summary>
    public class PressFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public float pressedScale = 0.95f;
        public void OnPointerDown(PointerEventData e) => Tween.Scale(transform, pressedScale, 0.08f);
        public void OnPointerUp(PointerEventData e) => Tween.Scale(transform, 1f, 0.16f, Ease.OutBack);
    }

    public enum ButtonStyle { Primary, Ghost, Danger, Quiet }

    /// <summary>Builds uGUI in code: one canvas, panels, labels, buttons, icons. All sizes are in 1920x1080 units.</summary>
    public static class UI
    {
        public static TMP_FontAsset Font;

        public static Canvas CreateCanvas(string name = "Canvas")
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Stretches to fill the parent.</summary>
        public static RectTransform Full(Transform parent, string name = "Full")
        {
            var rt = Rect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        /// <summary>Fixed-size element anchored at a point of the parent (anchor 0..1), offset in px.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = offset;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Centered(Transform parent, string name, Vector2 offset, Vector2 size)
            => Place(Rect(parent, name), new Vector2(0.5f, 0.5f), offset, size);

        public static Image Image(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 offset = default, Vector2? anchor = null)
        {
            var rt = Rect(parent, name);
            Place(rt, anchor ?? new Vector2(0.5f, 0.5f), offset, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sprite == Sprites.Rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        public static Image FullImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var rt = Full(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sprite == Sprites.Rounded ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        public static Image Panel(Transform parent, string name, Vector2 offset, Vector2 size, Color? color = null, Vector2? anchor = null)
        {
            var img = Image(parent, name, Sprites.Rounded, color ?? Theme.Panel, size, offset, anchor);
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        /// <summary>1 px hairline, horizontal.</summary>
        public static Image Line(Transform parent, Vector2 offset, float width, Color? color = null, Vector2? anchor = null)
            => Image(parent, "Line", Sprites.Square, color ?? Theme.Line, new Vector2(width, 2), offset, anchor);

        public static TMP_Text Label(Transform parent, string name, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (Font != null) t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        public static TMP_Text Label(Transform parent, string name, string text, float size, Color color, Vector2 offset, Vector2 boxSize,
            TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal, Vector2? anchor = null)
        {
            var t = Label(parent, name, text, size, color, align, style);
            Place(t.rectTransform, anchor ?? new Vector2(0.5f, 0.5f), offset, boxSize);
            return t;
        }

        /// <summary>Display heading: uppercase, wide tracking.</summary>
        public static TMP_Text Title(Transform parent, string text, float size, Vector2 offset, Vector2 boxSize, Color? color = null, Vector2? anchor = null)
        {
            var t = Label(parent, "Title", text, size, color ?? Theme.Text, offset, boxSize, TextAlignmentOptions.Center, FontStyles.UpperCase | FontStyles.Bold, anchor);
            t.characterSpacing = 12f;
            return t;
        }

        public static Button Button(Transform parent, string text, Vector2 offset, Vector2 size, Action onClick,
            ButtonStyle style = ButtonStyle.Primary, float fontSize = 34f, Vector2? anchor = null)
        {
            var rt = Rect(parent, "Button " + text);
            Place(rt, anchor ?? new Vector2(0.5f, 0.5f), offset, size);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = Sprites.Rounded;
            bg.type = UnityEngine.UI.Image.Type.Sliced;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            rt.gameObject.AddComponent<PressFx>();

            Color fill, textColor;
            switch (style)
            {
                case ButtonStyle.Primary: fill = Theme.Accent; textColor = Theme.Bg; break;
                case ButtonStyle.Danger: fill = Theme.Fail; textColor = Theme.Text; break;
                case ButtonStyle.Quiet: fill = Theme.PanelLight; textColor = Theme.Text; break;
                default: fill = Theme.Panel; textColor = Theme.Accent; break;
            }
            bg.color = fill;
            if (style == ButtonStyle.Ghost)
            {
                var border = FullImage(rt, "Border", Sprites.Rounded, Theme.Accent.WithA(0.7f));
                var inner = FullImage(rt, "Inner", Sprites.Rounded, Theme.Panel);
                inner.rectTransform.offsetMin = new Vector2(2, 2);
                inner.rectTransform.offsetMax = new Vector2(-2, -2);
                border.raycastTarget = false;
            }
            var label = Label(rt, "Label", text, fontSize, textColor, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.UpperCase);
            label.characterSpacing = 4f;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            if (onClick != null) btn.onClick.AddListener(() => { Sfx.Tap(); onClick(); });
            return btn;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = text;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var g = go.GetComponent<CanvasGroup>();
            return g != null ? g : go.AddComponent<CanvasGroup>();
        }

        /// <summary>Invisible full-rect graphic that receives pointer events.</summary>
        public static Image TouchSurface(Transform parent, string name = "TouchSurface")
        {
            var img = FullImage(parent, name, null, Color.clear);
            img.raycastTarget = true;
            return img;
        }

        /// <summary>Five small dots, one per stage.</summary>
        public static Image[] Dots(Transform parent, int count, Vector2 offset, float spacing, float size, Vector2? anchor = null)
        {
            var dots = new Image[count];
            float start = -(count - 1) * spacing * 0.5f;
            for (int i = 0; i < count; i++)
                dots[i] = Image(parent, "Dot" + i, Sprites.Circle, Theme.Muted.WithA(0.4f), new Vector2(size, size), offset + new Vector2(start + i * spacing, 0), anchor);
            return dots;
        }
    }
}
