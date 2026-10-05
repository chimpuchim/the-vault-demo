using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Stage 5: a marker spins around the vault dial; stop it inside the gold window to engage each lock.</summary>
    public class VaultDialChallenge : ChallengeBase
    {
        public override string Title => "The Vault";

        const float DialSize = 640f;
        const float MarkerRadius = 297f;

        Image _window, _marker, _markerGlow;
        Image[] _locks;
        TMP_Text _msg, _lockText;
        float _angle, _speed, _windowDeg, _windowCentre, _dir = 1f;
        int _lock, _misses;
        bool _running, _settling;
        readonly List<float> _precision = new List<float>();

        protected override void OnBegin()
        {
            var d = Cfg.dial;
            UI.Image(transform, "Dial", Sprites.Ring, Theme.PanelLight, new Vector2(DialSize, DialSize), new Vector2(0, -20));
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                var tick = UI.Image(transform, "Tick", Sprites.Square, Theme.Muted.WithA(0.5f), new Vector2(2, 18),
                    new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * (DialSize / 2 - 60) + new Vector2(0, -20));
                tick.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 30f);
            }
            _window = UI.Image(transform, "Window", Sprites.Ring, Theme.Accent, new Vector2(DialSize, DialSize), new Vector2(0, -20));
            _window.type = Image.Type.Filled;
            _window.fillMethod = Image.FillMethod.Radial360;
            _window.fillOrigin = (int)Image.Origin360.Top;
            _window.fillClockwise = true;

            _markerGlow = UI.Image(transform, "MarkerGlow", Sprites.Glow, Theme.AccentLight.WithA(0.6f), new Vector2(120, 120));
            _marker = UI.Image(transform, "Marker", Sprites.Circle, Theme.Text, new Vector2(34, 34));

            _msg = UI.Label(transform, "Msg", "TAP TO STOP THE MARKER IN THE GOLD WINDOW", 30, Theme.Muted, new Vector2(0, -24), new Vector2(900, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f));
            _msg.characterSpacing = 5;
            _lockText = UI.Label(transform, "Lock", "", 42, Theme.Text, new Vector2(0, -20), new Vector2(400, 60), TextAlignmentOptions.Center, FontStyles.Bold);
            _locks = UI.Dots(transform, d.locks, new Vector2(0, 60), 50, 22, new Vector2(0.5f, 0f));

            _speed = d.startSpeedDegPerSec;
            _windowDeg = d.startWindowDeg;
            Tween.Delay(0.5f, StartLock);
        }

        void StartLock()
        {
            if (Finished) return;
            _lock++;
            _lockText.text = "LOCK " + _lock + " / " + Cfg.dial.locks;
            float away;
            do _windowCentre = Rand(0f, 360f); while (Mathf.Abs(away = Mathf.DeltaAngle(_angle, _windowCentre)) < 70f);
            if (Dev.Enabled) _windowCentre = Mathf.Repeat(_angle + _dir * 180f, 360f); // fixed layout for automated runs
            _window.fillAmount = _windowDeg / 360f;
            _window.rectTransform.localRotation = Quaternion.Euler(0, 0, -(_windowCentre - _windowDeg / 2f));
            _window.color = Theme.Accent;
            Tween.Punch(_window.transform, 1.03f, 0.2f);
            for (int i = 0; i < _locks.Length; i++) if (i == _lock - 1) _locks[i].color = Theme.Text;
            _running = true;
            Dev.Log($"dial lock {_lock}: window centre {_windowCentre:0} width {_windowDeg:0} speed {_speed:0} dir {_dir} marker {_angle:0}");
        }

        void Update()
        {
            if (!_running && !_settling) return;
            if (_running) _angle = Mathf.Repeat(_angle + _dir * _speed * Time.unscaledDeltaTime, 360f);
            float a = _angle * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * MarkerRadius + new Vector2(0, -20);
            _marker.rectTransform.anchoredPosition = pos;
            _markerGlow.rectTransform.anchoredPosition = pos;
        }

        protected override void OnPointerDown(Vector2 local)
        {
            if (!_running) return;
            _running = false;
            _settling = true;
            float delta = Mathf.Abs(Mathf.DeltaAngle(_angle, _windowCentre));
            bool hit = delta <= _windowDeg / 2f;
            if (hit)
            {
                float precision = 1f - delta / (_windowDeg / 2f);
                _precision.Add(precision);
                _window.color = Theme.Pass;
                _locks[_lock - 1].color = Theme.Accent;
                Tween.Punch(_marker.transform, 1.5f, 0.25f);
                Float(precision > 0.8f ? "PERFECT" : "LOCKED", new Vector2(0, DialSize / 2 + 20), Theme.Pass);
                Sfx.Good();
            }
            else
            {
                _misses++;
                _window.color = Theme.Fail;
                _locks[_lock - 1].color = Theme.Fail;
                Float("MISSED BY " + (delta - _windowDeg / 2f).ToString("0") + "°", new Vector2(0, DialSize / 2 + 20), Theme.Fail);
                Sfx.Bad();
            }
            Tween.Delay(0.8f, NextLock);
        }

        void NextLock()
        {
            if (Finished) return;
            _settling = false;
            if (_lock >= Cfg.dial.locks) { Complete(); return; }
            var d = Cfg.dial;
            _speed += d.speedStepDegPerSec;
            _windowDeg = Mathf.Max(8f, _windowDeg - d.windowStepDeg);
            _dir = -_dir;
            StartLock();
        }

        void Complete()
        {
            var d = Cfg.dial;
            int hits = _precision.Count;
            bool passed = _misses <= d.missesAllowed;
            float avg = hits > 0 ? _precision.Average() : 0f;
            float score = (hits / (float)d.locks) * (0.5f + 0.5f * avg) - _misses * 0.15f;
            if (passed) Sfx.Win();
            Finish(passed, score,
                hits + " of " + d.locks + " locks engaged",
                "Precision " + (avg * 100f).ToString("0") + "%  ·  " + _misses + " miss" + (_misses == 1 ? "" : "es"),
                ("locksHit", hits), ("misses", _misses), ("avgPrecision", avg));
        }
    }
}
