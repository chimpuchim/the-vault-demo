using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Stage 1: tap the instant the ring turns gold. Measures reaction time in ms.</summary>
    public class ReflexChallenge : ChallengeBase
    {
        enum State { Waiting, Go, Shown }

        public override string Title => "Reflex";

        Image _ring, _glow;
        TMP_Text _msg, _ms;
        Image[] _dots;
        readonly List<float> _times = new List<float>();
        int _round;
        State _state;
        double _goTime;

        protected override void OnBegin()
        {
            _glow = UI.Image(transform, "Glow", Sprites.Glow, Theme.Accent.WithA(0f), new Vector2(1100, 1100));
            _ring = UI.Image(transform, "Ring", Sprites.Ring, Theme.Muted.WithA(0.45f), new Vector2(520, 520));
            _msg = UI.Label(transform, "Msg", "", 34, Theme.Muted, new Vector2(0, 40), new Vector2(460, 80));
            _msg.characterSpacing = 6;
            _ms = UI.Label(transform, "Ms", "", 110, Theme.Text, new Vector2(0, -40), new Vector2(500, 130), TextAlignmentOptions.Center, FontStyles.Bold);
            _dots = UI.Dots(transform, Cfg.reflex.rounds, new Vector2(0, 70), 36, 16, new Vector2(0.5f, 0f));
            StartRound();
        }

        void StartRound()
        {
            _round++;
            _state = State.Waiting;
            _ms.text = "";
            _msg.text = "WAIT FOR GOLD";
            _msg.color = Theme.Muted;
            Tween.Color(_ring, Theme.Muted.WithA(0.45f), 0.2f);
            Tween.Color(_glow, Theme.Accent.WithA(0f), 0.2f);
            for (int i = 0; i < _dots.Length; i++)
                _dots[i].color = i < _round - 1 ? Theme.Accent : i == _round - 1 ? Theme.Text : Theme.Muted.WithA(0.4f);
            StartCoroutine(WaitThenGo(Rand(Cfg.reflex.minWaitSec, Cfg.reflex.maxWaitSec)));
        }

        IEnumerator WaitThenGo(float wait)
        {
            yield return new WaitForSecondsRealtime(wait);
            if (_state != State.Waiting) yield break;
            _state = State.Go;
            _goTime = Time.realtimeSinceStartupAsDouble;
            _ring.color = Theme.Accent;
            _glow.color = Theme.Accent.WithA(0.75f);
            _msg.text = "TAP";
            _msg.color = Theme.Text;
            Tween.Punch(_ring.transform, 1.08f, 0.2f);
            Sfx.Go();

            yield return new WaitForSecondsRealtime(Cfg.reflex.timeoutMs / 1000f);
            if (_state != State.Go) yield break;
            _state = State.Shown;
            _times.Add(Cfg.reflex.falseStartPenaltyMs);
            _ring.color = Theme.Fail;
            _glow.color = Theme.Accent.WithA(0f);
            _msg.text = "TOO SLOW";
            _msg.color = Theme.Fail;
            _ms.text = "+" + Cfg.reflex.falseStartPenaltyMs.ToString("0") + " ms";
            Sfx.Bad();
            Tween.Delay(1.1f, Next);
        }

        protected override void OnPointerDown(Vector2 local)
        {
            if (_state == State.Waiting)
            {
                StopAllCoroutines();
                _state = State.Shown;
                _times.Add(Cfg.reflex.falseStartPenaltyMs);
                _ring.color = Theme.Fail;
                _msg.text = "FALSE START";
                _msg.color = Theme.Fail;
                _ms.text = "+" + Cfg.reflex.falseStartPenaltyMs.ToString("0") + " ms";
                Sfx.Bad();
                Tween.Delay(1.1f, Next);
                return;
            }
            if (_state != State.Go) return;

            StopAllCoroutines();
            float ms = (float)((Time.realtimeSinceStartupAsDouble - _goTime) * 1000.0);
            _state = State.Shown;
            _times.Add(ms);
            _ms.text = ms.ToString("0") + " ms";
            _msg.text = ms < 250 ? "LIGHTNING" : ms < Cfg.reflex.passAverageMs ? "SHARP" : "SLOW";
            _msg.color = ms < Cfg.reflex.passAverageMs ? Theme.Pass : Theme.Fail;
            Tween.Punch(_ms.transform, 1.15f, 0.25f);
            Tween.Color(_glow, Theme.Accent.WithA(0f), 0.4f);
            Sfx.Good();
            Tween.Delay(0.9f, Next);
        }

        void Next()
        {
            if (Finished) return;
            if (_round < Cfg.reflex.rounds) { StartRound(); return; }

            float avg = _times.Average();
            float best = _times.Min();
            bool passed = avg <= Cfg.reflex.passAverageMs;
            float score = Mathf.Clamp01((900f - avg) / 700f);
            int falseStarts = _times.Count(t => t >= Cfg.reflex.falseStartPenaltyMs);
            Finish(passed, score,
                "Average " + avg.ToString("0") + " ms",
                "Best " + best.ToString("0") + " ms  ·  pass mark " + Cfg.reflex.passAverageMs.ToString("0") + " ms",
                ("avgMs", avg), ("bestMs", best), ("falseStarts", falseStarts));
        }
    }
}
