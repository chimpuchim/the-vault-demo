using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>Stage 3: watch a sequence of tiles light up, then repeat it. The sequence grows each round.</summary>
    public class RecallChallenge : ChallengeBase
    {
        enum State { Showing, Input, Paused }

        public override string Title => "Recall";

        const float Tile = 150f, Gap = 22f;

        Image[] _tiles;
        TMP_Text _msg, _round;
        readonly List<int> _sequence = new List<int>();
        State _state = State.Paused;
        int _length, _step, _mistakes, _reached;

        int Count => Cfg.recall.gridSize * Cfg.recall.gridSize;
        float Span => Cfg.recall.gridSize * Tile + (Cfg.recall.gridSize - 1) * Gap;

        protected override void OnBegin()
        {
            int n = Cfg.recall.gridSize;
            _tiles = new Image[Count];
            for (int i = 0; i < Count; i++)
                _tiles[i] = UI.Image(transform, "Tile" + i, Sprites.Rounded, Theme.PanelLight, new Vector2(Tile, Tile), TilePos(i));

            _msg = UI.Label(transform, "Msg", "WATCH", 34, Theme.Muted, new Vector2(0, -24), new Vector2(600, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f));
            _msg.characterSpacing = 6;
            _round = UI.Label(transform, "Round", "", 28, Theme.Muted, new Vector2(0, 40), new Vector2(600, 50), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 0f));
            _length = Cfg.recall.startLength;
            Tween.Delay(0.6f, () => StartCoroutine(ShowSequence(true)));
        }

        Vector2 TilePos(int i)
        {
            int n = Cfg.recall.gridSize;
            int col = i % n, row = i / n;
            float start = -Span / 2 + Tile / 2;
            return new Vector2(start + col * (Tile + Gap), -(start + row * (Tile + Gap)) - 20);
        }

        int TileAt(Vector2 local)
        {
            for (int i = 0; i < Count; i++)
            {
                var p = TilePos(i);
                if (Mathf.Abs(local.x - p.x) <= Tile / 2 && Mathf.Abs(local.y - p.y) <= Tile / 2) return i;
            }
            return -1;
        }

        IEnumerator ShowSequence(bool fresh)
        {
            _state = State.Showing;
            _step = 0;
            if (fresh)
            {
                _sequence.Clear();
                int last = -1;
                for (int i = 0; i < _length; i++)
                {
                    int next;
                    do next = Random.Range(0, Count); while (next == last);
                    _sequence.Add(next);
                    last = next;
                }
            }
            Dev.Log("recall sequence " + string.Join(",", _sequence));
            _round.text = "SEQUENCE OF " + _length + "   ·   " + (_mistakes == 0 ? "NO MISTAKES" : _mistakes + " MISTAKE" + (_mistakes > 1 ? "S" : ""));
            _msg.text = "WATCH";
            _msg.color = Theme.Muted;
            yield return new WaitForSecondsRealtime(0.5f);
            float step = Cfg.recall.showStepSec;
            foreach (var idx in _sequence)
            {
                Flash(_tiles[idx], Theme.Accent, step * 0.65f);
                Sfx.Tick();
                yield return new WaitForSecondsRealtime(step);
            }
            _msg.text = "YOUR TURN";
            _msg.color = Theme.Accent;
            _state = State.Input;
        }

        void Flash(Image tile, Color color, float hold)
        {
            tile.color = color;
            Tween.Punch(tile.transform, 1.06f, 0.15f);
            Tween.Delay(hold, () => { if (tile != null) Tween.Color(tile, Theme.PanelLight, 0.15f); });
        }

        protected override void OnPointerDown(Vector2 local)
        {
            if (_state != State.Input) return;
            int idx = TileAt(local);
            if (idx < 0) return;

            if (idx == _sequence[_step])
            {
                Flash(_tiles[idx], Theme.Accent, 0.18f);
                Sfx.Tick();
                _step++;
                if (_step < _sequence.Count) return;

                _state = State.Paused;
                _reached = _length;
                Sfx.Good();
                _msg.text = "CORRECT";
                _msg.color = Theme.Pass;
                if (_length >= Cfg.recall.endLength) { Tween.Delay(0.7f, Complete); return; }
                _length++;
                Tween.Delay(0.8f, () => StartCoroutine(ShowSequence(true)));
                return;
            }

            _state = State.Paused;
            _mistakes++;
            Flash(_tiles[idx], Theme.Fail, 0.35f);
            Sfx.Bad();
            if (_mistakes > Cfg.recall.mistakesAllowed)
            {
                _msg.text = "WRONG TILE";
                _msg.color = Theme.Fail;
                Tween.Delay(0.9f, Complete);
                return;
            }
            _msg.text = "WATCH AGAIN";
            _msg.color = Theme.Fail;
            Tween.Delay(0.9f, () => StartCoroutine(ShowSequence(false)));
        }

        void Complete()
        {
            bool passed = _reached >= Cfg.recall.endLength && _mistakes <= Cfg.recall.mistakesAllowed;
            float score = _reached / (float)Cfg.recall.endLength - _mistakes * 0.1f;
            Finish(passed, score,
                passed ? "Full sequence of " + _reached : "Reached a sequence of " + _reached,
                "Target " + Cfg.recall.endLength + "  ·  " + _mistakes + " mistake" + (_mistakes == 1 ? "" : "s"),
                ("reachedLength", _reached), ("mistakes", _mistakes));
        }
    }
}
