using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Stage 4: keep a finger on an orb that glides around the screen. Measures coverage.</summary>
    public class TrackChallenge : ChallengeBase
    {
        public override string Title => "Track";

        RectTransform _orb;
        Image _core, _glow, _bar;
        TMP_Text _msg, _pct;
        bool _started;
        float _t, _inside;
        Vector2 _orbPos;

        float Radius => Cfg.track.orbRadiusPx;

        protected override void OnBegin()
        {
            var barBg = UI.Image(transform, "BarBg", Sprites.Square, Theme.Line, new Vector2(600, 4), new Vector2(0, 50), new Vector2(0.5f, 0f));
            _bar = UI.Image(barBg.transform, "Bar", Sprites.Square, Theme.Accent, new Vector2(0, 4));
            _bar.rectTransform.pivot = new Vector2(0, 0.5f);
            _bar.rectTransform.anchorMin = _bar.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _bar.rectTransform.anchoredPosition = Vector2.zero;

            _msg = UI.Label(transform, "Msg", "TOUCH AND HOLD THE ORB", 34, Theme.Muted, new Vector2(0, -24), new Vector2(800, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f));
            _msg.characterSpacing = 6;
            _pct = UI.Label(transform, "Pct", "", 30, Theme.Muted, new Vector2(0, 80), new Vector2(400, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 0f));

            _orb = UI.Centered(transform, "Orb", Vector2.zero, new Vector2(Radius * 2, Radius * 2));
            _glow = UI.Image(_orb, "Glow", Sprites.Glow, Theme.Accent.WithA(0.45f), new Vector2(Radius * 4.5f, Radius * 4.5f));
            _core = UI.Image(_orb, "Core", Sprites.Circle, Theme.Accent, new Vector2(Radius * 2, Radius * 2));
            UI.Image(_orb, "Inner", Sprites.Circle, Theme.AccentLight, new Vector2(Radius * 0.7f, Radius * 0.7f));
            _orbPos = Vector2.zero;
            Tween.Pop(_orb);
        }

        protected override void OnPointerDown(Vector2 local)
        {
            if (_started || Finished) return;
            if (Vector2.Distance(local, _orbPos) > Radius * 1.2f) return;
            _started = true;
            _msg.text = "STAY ON IT";
            _msg.color = Theme.Accent;
            Sfx.Go();
        }

        void Update()
        {
            if (!_started || Finished) return;
            float dt = Time.unscaledDeltaTime;
            _t += dt;
            var c = Cfg.track;
            var s = AreaSize;
            float ax = s.x / 2 - Radius - 60, ay = s.y / 2 - Radius - 90;
            float k = 0.9f * c.speed * _t;
            _orbPos = new Vector2(ax * Mathf.Sin(1.0f * k), ay * Mathf.Sin(1.6f * k + 1.2f) - 20);
            _orb.anchoredPosition = _orbPos;

            bool inside = Dev.Enabled || (PointerHeld && Vector2.Distance(Pointer, _orbPos) <= Radius * 1.15f);
            if (inside) _inside += dt;
            _core.color = inside ? Theme.Accent : Theme.Muted.WithA(0.6f);
            _glow.color = inside ? Theme.Accent.WithA(0.45f) : Theme.Fail.WithA(0.25f);

            float progress = Mathf.Clamp01(_t / c.durationSec);
            _bar.rectTransform.sizeDelta = new Vector2(600f * progress, 4);
            float coverage = _t > 0 ? _inside / _t : 0;
            _pct.text = (coverage * 100f).ToString("0") + "% ON TARGET";
            _pct.color = coverage >= c.passCoverage ? Theme.Pass : Theme.Fail;

            if (_t >= c.durationSec) Complete();
        }

        void Complete()
        {
            var c = Cfg.track;
            float coverage = Mathf.Clamp01(_inside / c.durationSec);
            bool passed = coverage >= c.passCoverage;
            Sfx.Good();
            Finish(passed, coverage,
                (coverage * 100f).ToString("0") + "% on target",
                "Pass mark " + (c.passCoverage * 100f).ToString("0") + "%  ·  " + c.durationSec.ToString("0") + " seconds",
                ("coverage", coverage), ("insideSec", _inside));
        }
    }
}
