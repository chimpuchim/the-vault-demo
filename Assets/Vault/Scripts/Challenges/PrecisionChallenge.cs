using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Stage 2: hit the centre of shrinking targets before they fade. Measures touch offset in px.</summary>
    public class PrecisionChallenge : ChallengeBase
    {
        public override string Title => "Precision";

        RectTransform _target;
        Image _outer, _mid, _fuse;
        TMP_Text _counter;
        Vector2 _targetPos;
        float _radius, _timeLeft;
        bool _active;
        int _index, _misses;
        readonly List<float> _offsets = new List<float>();

        protected override void OnBegin()
        {
            _counter = UI.Label(transform, "Counter", "", 30, Theme.Muted, new Vector2(0, -24), new Vector2(400, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f));
            _counter.characterSpacing = 6;
            var fuseBg = UI.Image(transform, "FuseBg", Sprites.Square, Theme.Line, new Vector2(600, 4), new Vector2(0, 50), new Vector2(0.5f, 0f));
            _fuse = UI.Image(fuseBg.transform, "Fuse", Sprites.Square, Theme.Accent, new Vector2(600, 4));
            _fuse.rectTransform.pivot = new Vector2(0, 0.5f);
            _fuse.rectTransform.anchorMin = _fuse.rectTransform.anchorMax = new Vector2(0, 0.5f);
            _fuse.rectTransform.anchoredPosition = Vector2.zero;

            _target = UI.Centered(transform, "Target", Vector2.zero, new Vector2(10, 10));
            _outer = UI.Image(_target, "Outer", Sprites.Ring, Theme.Accent, new Vector2(10, 10));
            _mid = UI.Image(_target, "Mid", Sprites.RingThin, Theme.Accent.WithA(0.6f), new Vector2(10, 10));
            UI.Image(_target, "Dot", Sprites.Circle, Theme.AccentLight, new Vector2(14, 14));
            UI.Image(_target, "H", Sprites.Square, Theme.Accent.WithA(0.5f), new Vector2(60, 2));
            UI.Image(_target, "V", Sprites.Square, Theme.Accent.WithA(0.5f), new Vector2(2, 60));
            _target.gameObject.SetActive(false);
            Tween.Delay(0.4f, NextTarget);
        }

        void NextTarget()
        {
            if (Finished) return;
            if (_index >= Cfg.precision.targets) { Complete(); return; }

            var p = Cfg.precision;
            float t = p.targets <= 1 ? 1f : _index / (float)(p.targets - 1);
            _radius = Mathf.Lerp(p.startRadiusPx, p.endRadiusPx, t);
            _targetPos = RandomPoint(_radius + 40, 60);
            _index++;
            _counter.text = "TARGET " + _index + " / " + p.targets;

            _target.anchoredPosition = _targetPos;
            _outer.rectTransform.sizeDelta = Vector2.one * _radius * 2;
            _mid.rectTransform.sizeDelta = Vector2.one * _radius * 1.1f;
            _outer.color = Theme.Accent;
            _target.gameObject.SetActive(true);
            Tween.Pop(_target, 0.25f);
            _timeLeft = p.timePerTargetSec;
            _active = true;
            Dev.Log($"precision target {_index} at ({_targetPos.x:0},{_targetPos.y:0}) r={_radius:0}");
        }

        void Update()
        {
            if (!_active) return;
            _timeLeft -= Time.unscaledDeltaTime;
            _fuse.rectTransform.sizeDelta = new Vector2(600f * Mathf.Clamp01(_timeLeft / Cfg.precision.timePerTargetSec), 4);
            if (_timeLeft <= 0f) Miss("TOO SLOW", _targetPos);
        }

        protected override void OnPointerDown(Vector2 local)
        {
            if (!_active) return;
            float d = Vector2.Distance(local, _targetPos);
            if (d > _radius) { Miss("MISS", local); return; }

            _active = false;
            _offsets.Add(d);
            _outer.color = Theme.Pass;
            Tween.Punch(_target, 1.12f, 0.2f);
            Float(d < 10 ? "BULLSEYE" : d.ToString("0") + " px", _targetPos + new Vector2(0, _radius + 20), d < 10 ? Theme.AccentLight : Theme.Pass);
            Sfx.Good();
            Tween.Delay(0.4f, Hide);
        }

        void Miss(string label, Vector2 at)
        {
            _active = false;
            _misses++;
            _outer.color = Theme.Fail;
            Float(label, at + new Vector2(0, 40), Theme.Fail);
            Sfx.Bad();
            Tween.Delay(0.5f, Hide);
        }

        void Hide()
        {
            if (Finished) return;
            _target.gameObject.SetActive(false);
            Tween.Delay(0.25f, NextTarget);
        }

        void Complete()
        {
            var p = Cfg.precision;
            float avg = _offsets.Count > 0 ? _offsets.Average() : p.startRadiusPx;
            bool passed = _misses <= p.missesAllowed && avg <= p.passAverageOffsetPx;
            float score = Mathf.Clamp01(1f - avg / (2f * p.passAverageOffsetPx)) - _misses * 0.1f;
            Finish(passed, score,
                "Average offset " + avg.ToString("0.0") + " px",
                _offsets.Count + " hits  ·  " + _misses + " missed  ·  pass mark " + p.passAverageOffsetPx.ToString("0") + " px",
                ("avgOffsetPx", avg), ("hits", _offsets.Count), ("misses", _misses));
        }
    }
}
