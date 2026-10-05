using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Operator panel: difficulty, session rules, campaign branding, analytics, resets. Edits a copy and saves on request.</summary>
    public class AdminScreen : UIScreen
    {
        static readonly (string name, string hex)[] AccentPresets =
        {
            ("Gold", "#D4AF37"), ("Platinum", "#C9CED8"), ("Emerald", "#2ED3A3"), ("Ruby", "#E5484D"), ("Sapphire", "#4C8DFF"), ("Rose", "#E8A0BF"),
        };

        VaultConfig _draft;
        RectTransform _content;
        TMP_Text _analytics;
        readonly List<Action> _refreshers = new List<Action>();
        Action _onClose;
        RectTransform _editor;
        TextField _editorField;
        Action<string> _editorDone;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void VaultDownloadText(string name, string text);
        [DllImport("__Internal")] static extern void VaultCopyText(string text);
#endif

        protected override void Build()
        {
            UI.FullImage(Root, "Bg", Sprites.Square, Theme.Bg).raycastTarget = true;
            UI.Title(Root, "OPERATOR PANEL", 40, new Vector2(60, -40), new Vector2(800, 60), Theme.Accent, new Vector2(0f, 1f)).alignment = TextAlignmentOptions.Left;
            UI.Label(Root, "Sub", "Changes apply after Save. Difficulty values are what a player must reach to clear a stage.", 22, Theme.Muted, new Vector2(60, -95), new Vector2(1000, 40), TextAlignmentOptions.Left, FontStyles.Normal, new Vector2(0f, 1f));

            UI.Button(Root, "Save & close", new Vector2(-60, -50), new Vector2(300, 72), SaveAndClose, ButtonStyle.Primary, 26, new Vector2(1f, 1f));
            UI.Button(Root, "Close", new Vector2(-380, -50), new Vector2(200, 72), () => _onClose?.Invoke(), ButtonStyle.Ghost, 26, new Vector2(1f, 1f));

            // Scroll view
            var viewport = UI.Rect(Root, "Viewport");
            viewport.anchorMin = new Vector2(0, 0); viewport.anchorMax = new Vector2(1, 1);
            viewport.offsetMin = new Vector2(60, 40); viewport.offsetMax = new Vector2(-60, -140);
            var mask = viewport.gameObject.AddComponent<Image>(); mask.color = Color.clear; mask.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = UI.Rect(viewport, "Content");
            _content.anchorMin = new Vector2(0, 1); _content.anchorMax = new Vector2(1, 1); _content.pivot = new Vector2(0.5f, 1);
            _content.offsetMin = new Vector2(0, 0); _content.offsetMax = new Vector2(0, 0);
            var layout = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true; layout.childControlWidth = true; layout.childForceExpandHeight = false; layout.spacing = 6; layout.padding = new RectOffset(0, 0, 0, 40);
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = Root.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = _content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;

            // Text editor overlay (reuses the keyboard)
            _editor = UI.Full(Root, "Editor");
            var dim = UI.FullImage(_editor, "Dim", Sprites.Square, Theme.Bg.WithA(0.92f)); dim.raycastTarget = true;
            _editorField = new TextField(_editor, new Vector2(0, 230), new Vector2(1000, 110));
            _editorField.MaxLength = 40;
            var kb = new OnScreenKeyboard(_editor, new Vector2(0, -110), "Done");
            kb.OnChar = c => _editorField.Append(c);
            kb.OnBackspace = () => _editorField.Backspace();
            kb.OnDone = () => { _editor.gameObject.SetActive(false); _editorDone?.Invoke(_editorField.Value.Trim()); };
            _editor.gameObject.SetActive(false);
        }

        public void Open(VaultConfig current, Action onClose)
        {
            _draft = current.Clone();
            _onClose = onClose;
            Rebuild();
            Show();
        }

        void Rebuild()
        {
            foreach (Transform child in _content) Destroy(child.gameObject);
            _refreshers.Clear();
            var s = _draft.session; var r = _draft.reflex; var p = _draft.precision; var m = _draft.recall; var t = _draft.track; var d = _draft.dial; var c = _draft.campaign;

            Section("Session");
            Stepper("Entry fee", () => s.entryFee, v => s.entryFee = v, 0.5f, 0, 100, "0.00");
            Stepper("Second chances per session", () => s.secondChances, v => s.secondChances = (int)v, 1, 0, 3, "0");
            Stepper("Idle timeout (s)", () => s.idleTimeoutSec, v => s.idleTimeoutSec = v, 5, 15, 300, "0");
            Stepper("Result card auto-advance (s)", () => s.resultScreenSec, v => s.resultScreenSec = v, 1, 3, 30, "0");
            TextRow("Operator PIN", () => s.adminPin, v => { if (v.Length >= 4) s.adminPin = v; });

            Section("1 · Reflex");
            Stepper("Rounds", () => r.rounds, v => r.rounds = (int)v, 1, 1, 6, "0");
            Stepper("Pass: average reaction (ms)", () => r.passAverageMs, v => r.passAverageMs = v, 25, 200, 900, "0");
            Stepper("False start penalty (ms)", () => r.falseStartPenaltyMs, v => r.falseStartPenaltyMs = v, 100, 300, 2000, "0");
            Stepper("Round timeout (ms)", () => r.timeoutMs, v => r.timeoutMs = v, 100, 500, 5000, "0");
            Stepper("Wait before gold: min (s)", () => r.minWaitSec, v => { r.minWaitSec = v; if (r.maxWaitSec < v) r.maxWaitSec = v; }, 0.5f, 0.5f, 6, "0.0");
            Stepper("Wait before gold: max (s)", () => r.maxWaitSec, v => { r.maxWaitSec = v; if (r.minWaitSec > v) r.minWaitSec = v; }, 0.5f, 0.5f, 8, "0.0");

            Section("2 · Precision");
            Stepper("Targets", () => p.targets, v => p.targets = (int)v, 1, 3, 12, "0");
            Stepper("First target radius (px)", () => p.startRadiusPx, v => p.startRadiusPx = v, 10, 60, 240, "0");
            Stepper("Last target radius (px)", () => p.endRadiusPx, v => p.endRadiusPx = v, 10, 30, 200, "0");
            Stepper("Time per target (s)", () => p.timePerTargetSec, v => p.timePerTargetSec = v, 0.25f, 1, 6, "0.00");
            Stepper("Pass: average offset (px)", () => p.passAverageOffsetPx, v => p.passAverageOffsetPx = v, 5, 10, 120, "0");
            Stepper("Misses allowed", () => p.missesAllowed, v => p.missesAllowed = (int)v, 1, 0, 6, "0");

            Section("3 · Recall");
            Stepper("Grid size", () => m.gridSize, v => m.gridSize = (int)v, 1, 2, 4, "0");
            Stepper("First sequence length", () => m.startLength, v => m.startLength = (int)v, 1, 2, 8, "0");
            Stepper("Final sequence length", () => m.endLength, v => m.endLength = (int)v, 1, 2, 10, "0");
            Stepper("Show speed (s per tile)", () => m.showStepSec, v => m.showStepSec = v, 0.05f, 0.25f, 1.2f, "0.00");
            Stepper("Mistakes allowed", () => m.mistakesAllowed, v => m.mistakesAllowed = (int)v, 1, 0, 3, "0");

            Section("4 · Track");
            Stepper("Duration (s)", () => t.durationSec, v => t.durationSec = v, 1, 5, 30, "0");
            Stepper("Orb radius (px)", () => t.orbRadiusPx, v => t.orbRadiusPx = v, 10, 40, 160, "0");
            Stepper("Speed", () => t.speed, v => t.speed = v, 0.1f, 0.4f, 2.5f, "0.0");
            Stepper("Pass: time on target (%)", () => t.passCoverage * 100f, v => t.passCoverage = v / 100f, 5, 30, 100, "0");

            Section("5 · The Vault");
            Stepper("Locks", () => d.locks, v => d.locks = (int)v, 1, 1, 6, "0");
            Stepper("Start speed (°/s)", () => d.startSpeedDegPerSec, v => d.startSpeedDegPerSec = v, 20, 60, 600, "0");
            Stepper("Speed step per lock (°/s)", () => d.speedStepDegPerSec, v => d.speedStepDegPerSec = v, 10, 0, 200, "0");
            Stepper("Start window (°)", () => d.startWindowDeg, v => d.startWindowDeg = v, 5, 10, 120, "0");
            Stepper("Window step per lock (°)", () => d.windowStepDeg, v => d.windowStepDeg = v, 2, 0, 30, "0");
            Stepper("Misses allowed", () => d.missesAllowed, v => d.missesAllowed = (int)v, 1, 0, 3, "0");

            Section("Campaign & branding");
            TextRow("Campaign name", () => c.campaignName, v => c.campaignName = v);
            TextRow("Tagline", () => c.tagline, v => c.tagline = v);
            TextRow("Prize", () => c.prizeName, v => c.prizeName = v);
            TextRow("Currency symbol", () => c.currencySymbol, v => c.currencySymbol = v);
            ChoiceRow("Accent colour", () => AccentName(c.accentHex), () =>
            {
                int i = Array.FindIndex(AccentPresets, a => a.hex == c.accentHex);
                c.accentHex = AccentPresets[(i + 1) % AccentPresets.Length].hex;
            });

            Section("Analytics");
            var sum = Flow.Analytics.Summarise();
            _analytics = Row(220, out var row);
            _analytics.text = AnalyticsStore.Describe(sum);
            ButtonRow(("Export JSON", (Action)ExportAnalytics, ButtonStyle.Ghost),
                ("Clear analytics", (Action)(() => { Flow.Analytics.Clear(); Rebuild(); }), ButtonStyle.Danger),
                ("Reset leaderboard", (Action)(() => { Flow.Board.Clear(); Rebuild(); }), ButtonStyle.Danger));

            Section("Maintenance");
            ButtonRow(("Restore defaults", (Action)(() => { _draft = new VaultConfig(); Rebuild(); }), ButtonStyle.Ghost),
                ("Test sounds", (Action)(() => Sfx.Win()), ButtonStyle.Quiet));
            Spacer(60);
        }

        static string AccentName(string hex)
        {
            foreach (var a in AccentPresets) if (a.hex == hex) return a.name + "  " + hex;
            return hex;
        }

        // ---- row builders -------------------------------------------------
        RectTransform NewRow(float height)
        {
            var rt = UI.Rect(_content, "Row");
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            return rt;
        }

        void Spacer(float h) => NewRow(h);

        void Section(string title)
        {
            var rt = NewRow(84);
            var t = UI.Label(rt, "Section", title.ToUpperInvariant(), 24, Theme.Accent, new Vector2(0, 14), new Vector2(1200, 40), TextAlignmentOptions.Left, FontStyles.Bold, new Vector2(0f, 0f));
            t.rectTransform.pivot = new Vector2(0, 0);
            t.characterSpacing = 8;
            var line = UI.Line(rt, Vector2.zero, 0, null, new Vector2(0.5f, 0f)).rectTransform;
            line.anchorMin = new Vector2(0, 0); line.anchorMax = new Vector2(1, 0); line.sizeDelta = new Vector2(0, 2); line.anchoredPosition = Vector2.zero;
        }

        TMP_Text Row(float height, out RectTransform row)
        {
            row = NewRow(height);
            var bg = UI.FullImage(row, "Bg", Sprites.Rounded, Theme.Panel);
            var t = UI.Label(row, "Text", "", 24, Theme.Text, TextAlignmentOptions.TopLeft);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(30, 16); t.rectTransform.offsetMax = new Vector2(-30, -16);
            t.lineSpacing = 10;
            return t;
        }

        void Stepper(string label, Func<float> get, Action<float> set, float step, float min, float max, string fmt)
        {
            var row = NewRow(84);
            UI.FullImage(row, "Bg", Sprites.Rounded, Theme.Panel);
            var l = UI.Label(row, "Label", label, 26, Theme.Text, new Vector2(30, 0), new Vector2(800, 60), TextAlignmentOptions.Left, FontStyles.Normal, new Vector2(0f, 0.5f));
            l.rectTransform.pivot = new Vector2(0, 0.5f);
            var value = UI.Label(row, "Value", "", 30, Theme.Accent, new Vector2(-290, 0), new Vector2(220, 60), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(1f, 0.5f));
            Action refresh = () => value.text = get().ToString(fmt);
            UI.Button(row, "-", new Vector2(-440, 0), new Vector2(90, 64), () => { set(Mathf.Clamp(get() - step, min, max)); refresh(); }, ButtonStyle.Quiet, 34, new Vector2(1f, 0.5f));
            UI.Button(row, "+", new Vector2(-60, 0), new Vector2(90, 64), () => { set(Mathf.Clamp(get() + step, min, max)); refresh(); }, ButtonStyle.Quiet, 34, new Vector2(1f, 0.5f));
            refresh();
            _refreshers.Add(refresh);
        }

        void TextRow(string label, Func<string> get, Action<string> set)
        {
            var row = NewRow(84);
            UI.FullImage(row, "Bg", Sprites.Rounded, Theme.Panel);
            var l = UI.Label(row, "Label", label, 26, Theme.Text, new Vector2(30, 0), new Vector2(600, 60), TextAlignmentOptions.Left, FontStyles.Normal, new Vector2(0f, 0.5f));
            l.rectTransform.pivot = new Vector2(0, 0.5f);
            var value = UI.Label(row, "Value", get(), 26, Theme.Accent, new Vector2(-260, 0), new Vector2(700, 60), TextAlignmentOptions.Right, FontStyles.Normal, new Vector2(1f, 0.5f));
            UI.Button(row, "Edit", new Vector2(-60, 0), new Vector2(160, 64), () => EditText(get(), v => { set(v); value.text = get(); }), ButtonStyle.Quiet, 24, new Vector2(1f, 0.5f));
        }

        void ChoiceRow(string label, Func<string> get, Action next)
        {
            var row = NewRow(84);
            UI.FullImage(row, "Bg", Sprites.Rounded, Theme.Panel);
            var l = UI.Label(row, "Label", label, 26, Theme.Text, new Vector2(30, 0), new Vector2(600, 60), TextAlignmentOptions.Left, FontStyles.Normal, new Vector2(0f, 0.5f));
            l.rectTransform.pivot = new Vector2(0, 0.5f);
            var value = UI.Label(row, "Value", get(), 26, Theme.Accent, new Vector2(-260, 0), new Vector2(700, 60), TextAlignmentOptions.Right, FontStyles.Normal, new Vector2(1f, 0.5f));
            UI.Button(row, "Next", new Vector2(-60, 0), new Vector2(160, 64), () => { next(); value.text = get(); }, ButtonStyle.Quiet, 24, new Vector2(1f, 0.5f));
        }

        void ButtonRow(params (string label, Action action, ButtonStyle style)[] buttons)
        {
            var row = NewRow(96);
            float x = 0;
            foreach (var b in buttons)
            {
                var btn = UI.Button(row, b.label, new Vector2(x, 0), new Vector2(300, 68), b.action, b.style, 24, new Vector2(0f, 0.5f));
                btn.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
                x += 320;
            }
        }

        void EditText(string initial, Action<string> done)
        {
            _editorField.Set(initial);
            _editorDone = done;
            _editor.gameObject.SetActive(true);
            _editor.SetAsLastSibling();
        }

        void ExportAnalytics()
        {
            var json = Flow.Analytics.ExportJson();
            var name = "vault-analytics-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".json";
#if UNITY_WEBGL && !UNITY_EDITOR
            VaultDownloadText(name, json);
            VaultCopyText(json);
#else
            var path = System.IO.Path.Combine(Application.persistentDataPath, name);
            System.IO.File.WriteAllText(path, json);
            GUIUtility.systemCopyBuffer = json;
            Debug.Log("Analytics exported to " + path);
#endif
            _analytics.text = "Exported " + name + " (also copied to the clipboard)\n\n" + _analytics.text;
        }

        void SaveAndClose()
        {
            if (_draft.recall.endLength < _draft.recall.startLength) _draft.recall.endLength = _draft.recall.startLength;
            if (_draft.precision.endRadiusPx > _draft.precision.startRadiusPx) _draft.precision.endRadiusPx = _draft.precision.startRadiusPx;
            _draft.Save();
            Flow.ReloadConfig();
            _onClose?.Invoke();
        }
    }
}
