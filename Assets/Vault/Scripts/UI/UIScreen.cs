using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>A full-screen page with fade/scale transitions. Subclasses build their content once in Build().</summary>
    public abstract class UIScreen : MonoBehaviour
    {
        protected RectTransform Root;
        protected GameFlow Flow;
        CanvasGroup _group;

        public bool Visible => gameObject.activeSelf;

        public static T Create<T>(Transform parent, GameFlow flow) where T : UIScreen
        {
            var rt = UI.Full(parent, typeof(T).Name);
            var s = rt.gameObject.AddComponent<T>();
            s.Root = rt;
            s.Flow = flow;
            s._group = UI.Group(rt.gameObject);
            s.Build();
            rt.gameObject.SetActive(false);
            return s;
        }

        protected abstract void Build();
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }

        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Flow.KeepOverlaysOnTop();
            _group.alpha = 0f;
            Root.localScale = Vector3.one * 1.03f;
            Tween.Fade(_group, 1f, 0.28f);
            Tween.Scale(Root, 1f, 0.4f, Ease.OutCubic);
            OnShow();
        }

        public void Hide()
        {
            if (!gameObject.activeSelf) return;
            OnHide();
            Tween.Fade(_group, 0f, 0.18f, Ease.OutCubic, () => { if (this != null) gameObject.SetActive(false); });
        }

        /// <summary>Dim backdrop that also swallows touches behind a modal.</summary>
        protected Image Backdrop(float alpha = 0.6f)
        {
            var img = UI.FullImage(Root, "Backdrop", Sprites.Square, Theme.Bg.WithA(alpha));
            img.raycastTarget = true;
            return img;
        }
    }

    /// <summary>Touch keyboard for names and admin text fields.</summary>
    public class OnScreenKeyboard
    {
        public readonly RectTransform Root;
        public Action<char> OnChar;
        public Action OnBackspace;
        public Action OnDone;

        public OnScreenKeyboard(Transform parent, Vector2 offset, string doneLabel = "Continue")
        {
            Root = UI.Centered(parent, "Keyboard", offset, new Vector2(1240, 420));
            string[] rows = { "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };
            const float key = 100f, gap = 12f;
            for (int r = 0; r < rows.Length; r++)
            {
                var row = rows[r];
                float width = row.Length * key + (row.Length - 1) * gap;
                float y = 150 - r * (key + gap);
                for (int i = 0; i < row.Length; i++)
                {
                    char ch = row[i];
                    float x = -width / 2 + key / 2 + i * (key + gap);
                    UI.Button(Root, ch.ToString(), new Vector2(x, y), new Vector2(key, key), () => OnChar?.Invoke(ch), ButtonStyle.Quiet, 36);
                }
                if (r == 2)
                {
                    UI.Button(Root, "Delete", new Vector2(width / 2 + gap + 80, y), new Vector2(160, key), () => OnBackspace?.Invoke(), ButtonStyle.Quiet, 26);
                }
            }
            float bottom = 150 - 3 * (key + gap);
            UI.Button(Root, "Space", new Vector2(-180, bottom), new Vector2(560, key), () => OnChar?.Invoke(' '), ButtonStyle.Quiet, 30);
            UI.Button(Root, doneLabel, new Vector2(260, bottom), new Vector2(300, key), () => OnDone?.Invoke(), ButtonStyle.Primary, 30);
        }
    }

    /// <summary>Caret-blinking text field display (read-only; the keyboard edits it).</summary>
    public class TextField
    {
        public readonly RectTransform Root;
        readonly TMP_Text _text;
        readonly Image _caret;
        public string Value { get; private set; } = "";
        public int MaxLength = 12;

        public TextField(Transform parent, Vector2 offset, Vector2 size, string placeholder = "")
        {
            Root = UI.Panel(parent, "Field", offset, size, Theme.Panel).rectTransform;
            UI.Line(Root, new Vector2(0, 0), size.x - 40, Theme.Accent.WithA(0.6f), new Vector2(0.5f, 0f));
            _text = UI.Label(Root, "Text", "", 56, Theme.Text, Vector2.zero, new Vector2(size.x - 60, size.y), TextAlignmentOptions.Center, FontStyles.Bold);
            _text.characterSpacing = 6;
            _caret = UI.Image(Root, "Caret", Sprites.Square, Theme.Accent, new Vector2(4, 60));
            _placeholder = placeholder;
            Refresh();
            Blink();
        }

        readonly string _placeholder;

        void Blink()
        {
            if (_caret == null) return;
            Tween.To(_caret, "blink", 1f, Ease.Linear, k => { if (_caret != null) _caret.color = Theme.Accent.WithA(k < 0.5f ? 1f : 0f); }, Blink);
        }

        public void Set(string value)
        {
            Value = value ?? "";
            Refresh();
        }

        public void Append(char c)
        {
            if (Value.Length >= MaxLength) return;
            if (c == ' ' && (Value.Length == 0 || Value.EndsWith(" "))) return;
            Value += c;
            Refresh();
        }

        public void Backspace()
        {
            if (Value.Length == 0) return;
            Value = Value.Substring(0, Value.Length - 1);
            Refresh();
        }

        void Refresh()
        {
            bool empty = Value.Length == 0;
            _text.text = empty ? _placeholder : Value;
            _text.color = empty ? Theme.Muted.WithA(0.5f) : Theme.Text;
            _text.ForceMeshUpdate();
            float w = empty ? 0 : _text.preferredWidth;
            _caret.rectTransform.anchoredPosition = new Vector2(w / 2 + 10, 0);
        }
    }
}
