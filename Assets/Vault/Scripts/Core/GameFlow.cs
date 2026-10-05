using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    /// <summary>
    /// The session state machine. Owns config, leaderboard, analytics and the payment provider,
    /// builds every screen once, and moves a player through attract -> pay -> name -> 5 stages -> end.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        struct StageDef
        {
            public string title, rule;
            public Func<GameObject, ChallengeBase> create;
        }

        public VaultConfig Config { get; private set; }
        public Leaderboard Board { get; private set; }
        public AnalyticsStore Analytics { get; private set; }
        public IPaymentProvider Payment { get; private set; }

        Canvas _canvas;
        RectTransform _root, _area, _corner;
        AttractScreen _attract;
        PaymentScreen _payment;
        NameScreen _name;
        IntroScreen _intro;
        ResultScreen _result;
        EndScreen _end;
        LeaderboardScreen _leaderboard;
        PinScreen _pin;
        AdminScreen _admin;

        // HUD
        RectTransform _hud;
        TMP_Text _hudStage, _hudRight;
        Image[] _hudDots;
        Button _quit;
        float _quitArmedUntil;

        ChallengeBase _current;
        SessionRecord _session;
        int _stage, _attempt, _retries, _total, _cleared;
        bool _inSession, _adminOpen;
        float _lastInput;
        int _cornerTaps;
        float _cornerWindow;

        static readonly StageDef[] Stages =
        {
            new StageDef { title = "Reflex", rule = "Tap the instant the ring turns gold. Three rounds.", create = go => go.AddComponent<ReflexChallenge>() },
            new StageDef { title = "Precision", rule = "Hit the centre of each target before it fades.", create = go => go.AddComponent<PrecisionChallenge>() },
            new StageDef { title = "Recall", rule = "Watch the sequence, then repeat it.", create = go => go.AddComponent<RecallChallenge>() },
            new StageDef { title = "Track", rule = "Keep your finger on the moving orb.", create = go => go.AddComponent<TrackChallenge>() },
            new StageDef { title = "The Vault", rule = "Stop the marker inside the gold window. Three locks.", create = go => go.AddComponent<VaultDialChallenge>() },
        };

        void Start()
        {
            Config = VaultConfig.Load();
            Board = new Leaderboard();
            Analytics = new AnalyticsStore();
            Payment = new MockPaymentProvider();
            BuildUI();
            ShowAttract();
        }

        public void ReloadConfig()
        {
            Config = VaultConfig.Load();
            if (Theme.Accent != Config.AccentColor) { Theme.Accent = Config.AccentColor; RebuildUI(); }
        }

        // ---------------------------------------------------------------- UI
        void BuildUI()
        {
            Theme.Accent = Config.AccentColor;
            _canvas = UI.CreateCanvas();
            _root = _canvas.GetComponent<RectTransform>();
            UI.FullImage(_root, "Background", Sprites.Square, Theme.Bg);
            UI.Image(_root, "GlowA", Sprites.Glow, Theme.Accent.WithA(0.07f), new Vector2(1800, 1800), new Vector2(-500, 300));
            UI.Image(_root, "GlowB", Sprites.Glow, Theme.Accent.WithA(0.05f), new Vector2(1600, 1600), new Vector2(600, -400));

            // play area + HUD (hidden until a stage runs)
            _area = UI.Full(_root, "PlayArea");
            _area.offsetMax = new Vector2(0, -120);
            _hud = UI.Full(_root, "HUD");
            _hud.anchorMin = new Vector2(0, 1); _hud.anchorMax = new Vector2(1, 1); _hud.pivot = new Vector2(0.5f, 1);
            _hud.sizeDelta = new Vector2(0, 120); _hud.anchoredPosition = Vector2.zero;
            UI.Image(_hud, "Rule", Sprites.Square, Theme.Line, new Vector2(1800, 2), new Vector2(0, 0), new Vector2(0.5f, 0f));
            _hudStage = UI.Label(_hud, "Stage", "", 28, Theme.Text, new Vector2(60, 0), new Vector2(700, 60), TextAlignmentOptions.Left, FontStyles.Bold, new Vector2(0f, 0.5f));
            _hudStage.rectTransform.pivot = new Vector2(0, 0.5f);
            _hudStage.characterSpacing = 6;
            _hudDots = UI.Dots(_hud, 5, new Vector2(0, 0), 40, 16, new Vector2(0.5f, 0.5f));
            _hudRight = UI.Label(_hud, "Right", "", 24, Theme.Muted, new Vector2(-260, 0), new Vector2(600, 60), TextAlignmentOptions.Right, FontStyles.Normal, new Vector2(1f, 0.5f));
            _hudRight.rectTransform.pivot = new Vector2(1, 0.5f);
            _hudRight.characterSpacing = 4;
            _quit = UI.Button(_hud, "Quit", new Vector2(-60, 0), new Vector2(160, 56), OnQuitPressed, ButtonStyle.Ghost, 20, new Vector2(1f, 0.5f));
            _quit.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
            _hud.gameObject.SetActive(false);

            _attract = UIScreen.Create<AttractScreen>(_root, this);
            _payment = UIScreen.Create<PaymentScreen>(_root, this);
            _name = UIScreen.Create<NameScreen>(_root, this);
            _intro = UIScreen.Create<IntroScreen>(_root, this);
            _result = UIScreen.Create<ResultScreen>(_root, this);
            _end = UIScreen.Create<EndScreen>(_root, this);
            _leaderboard = UIScreen.Create<LeaderboardScreen>(_root, this);
            _pin = UIScreen.Create<PinScreen>(_root, this);
            _admin = UIScreen.Create<AdminScreen>(_root, this);

            // operator hot corner: 5 taps top-left
            _corner = UI.Rect(_root, "HotCorner");
            UI.Place(_corner, new Vector2(0, 1), Vector2.zero, new Vector2(160, 160));
            var cornerImg = _corner.gameObject.AddComponent<Image>();
            cornerImg.color = Color.clear;
            _corner.gameObject.AddComponent<Button>().onClick.AddListener(OnCornerTap);
            KeepOverlaysOnTop();
        }

        /// <summary>Screens raise themselves when shown; the hot corner must stay above them.</summary>
        public void KeepOverlaysOnTop()
        {
            if (_corner != null) _corner.SetAsLastSibling();
        }

        void RebuildUI()
        {
            AbortChallenge();
            if (_canvas != null) Destroy(_canvas.gameObject);
            BuildUI();
            ShowAttract();
        }

        void HideAll()
        {
            _attract.Hide(); _payment.Hide(); _name.Hide(); _intro.Hide(); _result.Hide(); _end.Hide(); _leaderboard.Hide(); _pin.Hide(); _admin.Hide();
            _hud.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- flow
        void ShowAttract()
        {
            AbortChallenge();
            HideAll();
            _inSession = false;
            _attract.Show();
        }

        public void StartSession()
        {
            if (_inSession || _adminOpen) return;
            _inSession = true;
            _lastInput = Time.unscaledTime;
            _attract.Hide();
            _payment.Run(Payment, Config.session.entryFee, Config.campaign.currencySymbol, r =>
            {
                _payment.Hide();
                if (r.status != PaymentStatus.Approved) { ShowAttract(); return; }
                _session = new SessionRecord
                {
                    id = Guid.NewGuid().ToString("N"),
                    startedAt = DateTime.Now.ToString("s"),
                    paid = r.amount,
                };
                _name.Open(OnNameEntered);
            });
        }

        void OnNameEntered(string name)
        {
            _session.player = name;
            _name.Hide();
            _stage = 1; _attempt = 1; _total = 0; _cleared = 0;
            _retries = Config.session.secondChances;
            RunStage();
        }

        void RunStage()
        {
            var def = Stages[_stage - 1];
            HideAll();
            _intro.Open(_stage, _cleared, def.title, def.rule, () =>
            {
                _intro.Hide();
                ShowHud(def.title);
                var go = new GameObject("Challenge " + def.title, typeof(RectTransform));
                go.transform.SetParent(_area, false);
                _current = def.create(go);
                _current.Begin(Config, _area, OnStageDone);
            });
        }

        void ShowHud(string title)
        {
            _hud.gameObject.SetActive(true);
            _hudStage.text = "0" + _stage + "  ·  " + title.ToUpperInvariant();
            for (int i = 0; i < 5; i++) _hudDots[i].color = i < _cleared ? Theme.Accent : i == _stage - 1 ? Theme.Text : Theme.Muted.WithA(0.4f);
            _hudRight.text = (_attempt > 1 ? "SECOND CHANCE  ·  " : "") + "RETRIES LEFT " + _retries + "   ·   " + _total + " PTS";
            UI.SetButtonText(_quit, "Quit");
            _quitArmedUntil = 0;
        }

        void OnStageDone(ChallengeResult r)
        {
            var def = Stages[_stage - 1];
            var rec = new StageRecord { stage = _stage, name = def.title, attempt = _attempt, passed = r.passed, score = r.score, durationSec = r.durationSec, metrics = r.metrics };
            _session.stages.Add(rec);
            Analytics.Record(_session);
            AbortChallenge();

            if (r.passed)
            {
                _total += r.score;
                _cleared = _stage;
                _result.OpenPass(r, Config.session.resultScreenSec, () =>
                {
                    _result.Hide();
                    if (_stage >= Stages.Length) { Win(); return; }
                    _stage++; _attempt = 1;
                    RunStage();
                });
                return;
            }

            _result.OpenFail(r, _retries, Config.session.resultScreenSec,
                retry: () => { _result.Hide(); _retries--; _attempt++; RunStage(); },
                end: Lose);
        }

        void Win()
        {
            HideAll();
            _session.outcome = "won"; _session.totalScore = _total; _session.stagesCleared = _cleared; _session.endedAt = DateTime.Now.ToString("s");
            Analytics.Record(_session);
            int rank = Board.Add(_session.player, _total, _cleared);
            _end.OpenWin(Config.campaign.prizeName, _total, rank, () => { _end.Hide(); _leaderboard.Open(rank, ShowAttract); });
        }

        void Lose()
        {
            HideAll();
            _session.outcome = "lost"; _session.totalScore = _total; _session.stagesCleared = _cleared; _session.endedAt = DateTime.Now.ToString("s");
            Analytics.Record(_session);
            int rank = _total > 0 ? Board.Add(_session.player, _total, _cleared) : 0;
            _end.OpenLose(_cleared, _total, rank, () => { _end.Hide(); _leaderboard.Open(rank, ShowAttract); });
        }

        void Abandon()
        {
            if (_session != null && _session.outcome == "in_progress")
            {
                _session.outcome = "abandoned"; _session.totalScore = _total; _session.stagesCleared = _cleared; _session.endedAt = DateTime.Now.ToString("s");
                Analytics.Record(_session);
            }
            Payment.Cancel();
            ShowAttract();
        }

        void AbortChallenge()
        {
            if (_current != null) _current.Abort();
            _current = null;
            _hud.gameObject.SetActive(false);
        }

        void OnQuitPressed()
        {
            if (Time.unscaledTime < _quitArmedUntil) { Abandon(); return; }
            _quitArmedUntil = Time.unscaledTime + 2.5f;
            UI.SetButtonText(_quit, "Tap again");
            Tween.Delay(2.5f, () => { if (_quit != null && Time.unscaledTime >= _quitArmedUntil) UI.SetButtonText(_quit, "Quit"); });
        }

        // ---------------------------------------------------------------- idle + admin
        void Update()
        {
            if (Input.touchCount > 0 || Input.GetMouseButton(0)) _lastInput = Time.unscaledTime;
            if (_inSession && !_adminOpen && Time.unscaledTime - _lastInput > Config.session.idleTimeoutSec) Abandon();
            if (_cornerTaps > 0 && Time.unscaledTime > _cornerWindow) _cornerTaps = 0;
        }

        void OnCornerTap()
        {
            if (_adminOpen) return;
            if (_cornerTaps == 0) _cornerWindow = Time.unscaledTime + 3f;
            _cornerTaps++;
            if (_cornerTaps < 5) return;
            _cornerTaps = 0;
            _adminOpen = true;
            _pin.Open(Config.session.adminPin,
                ok: () => { _pin.Hide(); _admin.Open(Config, CloseAdmin); },
                cancel: () => { _pin.Hide(); _adminOpen = false; });
        }

        void CloseAdmin()
        {
            _adminOpen = false;
            if (_admin != null) _admin.Hide();
            if (!_inSession && _attract != null && !_attract.Visible) ShowAttract();
            else if (!_inSession) _attract.Show();
            _lastInput = Time.unscaledTime;
        }
    }
}
