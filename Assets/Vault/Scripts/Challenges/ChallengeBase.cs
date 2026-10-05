using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vault
{
    public class ChallengeResult
    {
        public bool passed;
        public int score;               // 0..1000
        public float durationSec;
        public string headline;         // "Average 312 ms"
        public string detail;           // "Pass mark 450 ms"
        public List<MetricEntry> metrics = new List<MetricEntry>();
    }

    /// <summary>Forwards pointer events from a full-rect graphic as local coordinates of the play area.</summary>
    public class PointerRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerMoveHandler, IDragHandler
    {
        public RectTransform Area;
        public Action<Vector2> Down, Up, Move;

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Area, e.position, e.pressEventCamera, out var local);
            return local;
        }

        public void OnPointerDown(PointerEventData e) => Down?.Invoke(Local(e));
        public void OnPointerUp(PointerEventData e) => Up?.Invoke(Local(e));
        public void OnPointerMove(PointerEventData e) => Move?.Invoke(Local(e));
        public void OnDrag(PointerEventData e) => Move?.Invoke(Local(e));
    }

    /// <summary>
    /// A challenge is a self-contained panel: it builds its own visuals inside the play area,
    /// receives pointer input in area-local pixels (origin at the centre) and reports one result.
    /// </summary>
    public abstract class ChallengeBase : MonoBehaviour
    {
        public abstract string Title { get; }

        protected VaultConfig Cfg;
        protected RectTransform Area;
        protected Vector2 AreaSize => Area.rect.size;
        protected bool PointerHeld;
        protected Vector2 Pointer;

        Action<ChallengeResult> _onDone;
        double _startTime;
        bool _finished;
        public bool Finished => _finished;

        public void Begin(VaultConfig cfg, RectTransform area, Action<ChallengeResult> onDone)
        {
            Cfg = cfg;
            Area = area;
            _onDone = onDone;
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var surface = UI.TouchSurface(transform);
            var relay = surface.gameObject.AddComponent<PointerRelay>();
            relay.Area = area;
            relay.Down = p => { PointerHeld = true; Pointer = p; if (!_finished) OnPointerDown(p); };
            relay.Up = p => { PointerHeld = false; Pointer = p; if (!_finished) OnPointerUp(p); };
            relay.Move = p => { Pointer = p; if (!_finished) OnPointerMove(p); };

            _startTime = Time.realtimeSinceStartupAsDouble;
            OnBegin();
        }

        protected abstract void OnBegin();
        protected virtual void OnPointerDown(Vector2 local) { }
        protected virtual void OnPointerUp(Vector2 local) { }
        protected virtual void OnPointerMove(Vector2 local) { }

        protected void Finish(bool passed, float score01, string headline, string detail, params (string key, float value)[] metrics)
        {
            if (_finished) return;
            _finished = true;
            StopAllCoroutines();
            var result = new ChallengeResult
            {
                passed = passed,
                score = Mathf.RoundToInt(Mathf.Clamp01(score01) * 1000f),
                durationSec = (float)(Time.realtimeSinceStartupAsDouble - _startTime),
                headline = headline,
                detail = detail,
            };
            foreach (var (key, value) in metrics) result.metrics.Add(new MetricEntry { key = key, value = value });
            var cb = _onDone;
            _onDone = null;
            cb?.Invoke(result);
        }

        public void Abort()
        {
            _finished = true;
            StopAllCoroutines();
            _onDone = null;
            Destroy(gameObject);
        }

        protected static float Rand(float min, float max) => UnityEngine.Random.Range(min, max);

        /// <summary>Random point inside the area with a margin on every side.</summary>
        protected Vector2 RandomPoint(float margin, float extraTop = 0)
        {
            var s = AreaSize;
            return new Vector2(Rand(-s.x / 2 + margin, s.x / 2 - margin), Rand(-s.y / 2 + margin, s.y / 2 - margin - extraTop));
        }

        /// <summary>Floating feedback text that rises and fades.</summary>
        protected void Float(string text, Vector2 at, Color color, float size = 44)
        {
            var t = UI.Label(transform, "Float", text, size, color, at, new Vector2(400, 80), TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            var g = UI.Group(t.gameObject);
            Tween.Move(t.rectTransform, at + new Vector2(0, 70), 0.8f, Ease.OutCubic);
            Tween.Fade(g, 0f, 0.8f, Ease.InQuad, () => { if (t != null) Destroy(t.gameObject); });
        }
    }
}
