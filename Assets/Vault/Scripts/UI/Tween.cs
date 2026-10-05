using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vault
{
    public enum Ease { Linear, OutCubic, InOutSine, OutBack, OutQuad, InQuad }

    /// <summary>Minimal coroutine tweens on unscaled time. Enough for the prototype; no package needed.</summary>
    public class Tween : MonoBehaviour
    {
        static Tween _runner;
        static readonly Dictionary<(object, string), Coroutine> Active = new Dictionary<(object, string), Coroutine>();

        static Tween Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("~Tween");
                    DontDestroyOnLoad(go);
                    _runner = go.AddComponent<Tween>();
                }
                return _runner;
            }
        }

        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InQuad: return t * t;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f, c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                default: return t;
            }
        }

        /// <summary>Runs `apply(0..1)` over `duration`; one tween per (target, channel) at a time.</summary>
        public static Coroutine To(object target, string channel, float duration, Ease ease, Action<float> apply, Action onDone = null)
        {
            var key = (target, channel);
            if (Active.TryGetValue(key, out var running) && running != null) Runner.StopCoroutine(running);
            var co = Runner.StartCoroutine(Run(key, duration, ease, apply, onDone));
            Active[key] = co;
            return co;
        }

        static IEnumerator Run((object, string) key, float duration, Ease ease, Action<float> apply, Action onDone)
        {
            float t = 0;
            if (duration <= 0f) { apply(1f); }
            else
            {
                while (t < duration)
                {
                    t += Time.unscaledDeltaTime;
                    apply(Evaluate(ease, t / duration));
                    yield return null;
                }
            }
            apply(1f);
            Active.Remove(key);
            onDone?.Invoke();
        }

        public static void Kill(object target, string channel)
        {
            var key = (target, channel);
            if (Active.TryGetValue(key, out var running) && running != null) Runner.StopCoroutine(running);
            Active.Remove(key);
        }

        public static void Scale(Transform tr, float to, float duration, Ease ease = Ease.OutCubic, Action onDone = null)
        {
            if (tr == null) return;
            var from = tr.localScale;
            var target = Vector3.one * to;
            To(tr, "scale", duration, ease, k => { if (tr != null) tr.localScale = Vector3.LerpUnclamped(from, target, k); }, onDone);
        }

        public static void Fade(CanvasGroup g, float to, float duration, Ease ease = Ease.OutCubic, Action onDone = null)
        {
            if (g == null) return;
            var from = g.alpha;
            To(g, "fade", duration, ease, k => { if (g != null) g.alpha = Mathf.Lerp(from, to, k); }, onDone);
        }

        public static void Color(UnityEngine.UI.Graphic gr, Color to, float duration, Ease ease = Ease.OutCubic)
        {
            if (gr == null) return;
            var from = gr.color;
            To(gr, "color", duration, ease, k => { if (gr != null) gr.color = UnityEngine.Color.Lerp(from, to, k); });
        }

        public static void Move(RectTransform rt, Vector2 to, float duration, Ease ease = Ease.OutCubic, Action onDone = null)
        {
            if (rt == null) return;
            var from = rt.anchoredPosition;
            To(rt, "move", duration, ease, k => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, onDone);
        }

        /// <summary>Pop-in: scale 0.7 -> 1 with overshoot.</summary>
        public static void Pop(Transform tr, float duration = 0.35f)
        {
            if (tr == null) return;
            tr.localScale = Vector3.one * 0.7f;
            Scale(tr, 1f, duration, Ease.OutBack);
        }

        /// <summary>Punch: quick scale up and back, for hits.</summary>
        public static void Punch(Transform tr, float amount = 1.15f, float duration = 0.25f)
        {
            if (tr == null) return;
            To(tr, "scale", duration, Ease.Linear, k =>
            {
                if (tr == null) return;
                float s = k < 0.4f ? Mathf.Lerp(1f, amount, k / 0.4f) : Mathf.Lerp(amount, 1f, (k - 0.4f) / 0.6f);
                tr.localScale = Vector3.one * s;
            });
        }

        public static Coroutine Delay(float seconds, Action action) => Runner.StartCoroutine(DelayCo(seconds, action));

        static IEnumerator DelayCo(float seconds, Action action)
        {
            yield return new WaitForSecondsRealtime(seconds);
            action?.Invoke();
        }

        public static void Stop(Coroutine co)
        {
            if (co != null && _runner != null) _runner.StopCoroutine(co);
        }
    }
}
