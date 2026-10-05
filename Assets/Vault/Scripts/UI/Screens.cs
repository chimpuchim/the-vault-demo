using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vault
{
    // ---------------------------------------------------------------- Attract
    public class AttractScreen : UIScreen
    {
        TMP_Text _title, _tagline, _prize, _fee, _board;
        Image _ringA, _ringB;
        TMP_Text _cta;

        protected override void Build()
        {
            var surface = UI.TouchSurface(Root);
            surface.gameObject.AddComponent<Button>().onClick.AddListener(() => Flow.StartSession());

            _ringA = UI.Image(Root, "RingA", Sprites.RingThin, Theme.Accent.WithA(0.35f), new Vector2(760, 760), new Vector2(0, 60));
            _ringA.type = Image.Type.Filled; _ringA.fillMethod = Image.FillMethod.Radial360; _ringA.fillAmount = 0.72f;
            _ringB = UI.Image(Root, "RingB", Sprites.RingThin, Theme.Accent.WithA(0.18f), new Vector2(900, 900), new Vector2(0, 60));
            _ringB.type = Image.Type.Filled; _ringB.fillMethod = Image.FillMethod.Radial360; _ringB.fillAmount = 0.55f;
            UI.Image(Root, "Glow", Sprites.Glow, Theme.Accent.WithA(0.12f), new Vector2(1500, 1500), new Vector2(0, 60));

            _title = UI.Title(Root, "THE VAULT", 128, new Vector2(0, 110), new Vector2(1600, 160), Theme.Accent);
            _title.characterSpacing = 28;
            _tagline = UI.Label(Root, "Tagline", "", 34, Theme.Text, new Vector2(0, 10), new Vector2(1200, 60));
            _tagline.characterSpacing = 6;
            _prize = UI.Label(Root, "Prize", "", 26, Theme.Muted, new Vector2(0, -50), new Vector2(1200, 50));
            _prize.characterSpacing = 4;

            _cta = UI.Label(Root, "CTA", "TOUCH TO BEGIN", 36, Theme.Text, new Vector2(0, -230), new Vector2(800, 60), TextAlignmentOptions.Center, FontStyles.Bold);
            _cta.characterSpacing = 14;
            _fee = UI.Label(Root, "Fee", "", 24, Theme.Muted, new Vector2(0, 50), new Vector2(800, 40), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 0f));
            _fee.characterSpacing = 4;

            var panel = UI.Panel(Root, "Board", new Vector2(-50, 50), new Vector2(420, 330), Theme.Panel.WithA(0.85f), new Vector2(1f, 0f));
            UI.Label(panel.transform, "Head", "LEADERBOARD", 22, Theme.Accent, new Vector2(0, -30), new Vector2(380, 40), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(0.5f, 1f)).characterSpacing = 8;
            UI.Line(panel.transform, new Vector2(0, -58), 340, null, new Vector2(0.5f, 1f));
            _board = UI.Label(panel.transform, "Rows", "", 26, Theme.Text, new Vector2(0, -80), new Vector2(350, 240), TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0.5f, 1f));
            _board.lineSpacing = 18;
        }

        protected override void OnShow()
        {
            var c = Flow.Config;
            _title.text = c.campaign.campaignName;
            _tagline.text = c.campaign.tagline;
            _prize.text = "PRIZE  ·  " + c.campaign.prizeName.ToUpperInvariant();
            _fee.text = "ENTRY " + c.campaign.currencySymbol + c.session.entryFee.ToString("0.00") + "   ·   FIVE CHALLENGES   ·   ONE SECOND CHANCE";
            var rows = "";
            int i = 1;
            foreach (var e in Flow.Board.Entries)
            {
                if (i > 5) break;
                rows += $"{i,2}.  {Truncate(e.name, 10),-10}  <color=#D4AF37>{e.score,5}</color>\n";
                i++;
            }
            _board.text = rows.Length == 0 ? "<color=#8A93A6>No scores yet.\nBe the first.</color>" : rows;
            Spin();
            Pulse();
        }

        static string Truncate(string s, int n) => s.Length <= n ? s : s.Substring(0, n);

        void Spin()
        {
            if (!Visible) return;
            Tween.To(_ringA, "spin", 14f, Ease.Linear, k => { if (_ringA != null) _ringA.rectTransform.localRotation = Quaternion.Euler(0, 0, -360f * k); }, Spin);
            Tween.To(_ringB, "spin", 22f, Ease.Linear, k => { if (_ringB != null) _ringB.rectTransform.localRotation = Quaternion.Euler(0, 0, 360f * k); });
        }

        void Pulse()
        {
            if (!Visible) return;
            Tween.To(_cta, "pulse", 1.6f, Ease.InOutSine, k => { if (_cta != null) _cta.color = Theme.Text.WithA(0.45f + 0.55f * Mathf.Sin(k * Mathf.PI)); }, Pulse);
        }
    }

    // ---------------------------------------------------------------- Payment
    public class PaymentScreen : UIScreen
    {
        TMP_Text _amount, _status, _provider;
        Image _spinner;
        Button _cancel;
        Coroutine _co;
        IPaymentProvider _payment;

        protected override void Build()
        {
            Backdrop(0.5f);
            var panel = UI.Panel(Root, "Panel", Vector2.zero, new Vector2(760, 620));
            UI.Label(panel.transform, "Head", "ENTRY", 26, Theme.Accent, new Vector2(0, -50), new Vector2(600, 40), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(0.5f, 1f)).characterSpacing = 10;
            _amount = UI.Label(panel.transform, "Amount", "", 120, Theme.Text, new Vector2(0, 120), new Vector2(600, 140), TextAlignmentOptions.Center, FontStyles.Bold);
            _spinner = UI.Image(panel.transform, "Spinner", Sprites.RingThin, Theme.Accent, new Vector2(120, 120), new Vector2(0, -30));
            _spinner.type = Image.Type.Filled; _spinner.fillMethod = Image.FillMethod.Radial360; _spinner.fillAmount = 0.3f;
            _status = UI.Label(panel.transform, "Status", "", 32, Theme.Text, new Vector2(0, -130), new Vector2(600, 50));
            _provider = UI.Label(panel.transform, "Provider", "", 20, Theme.Muted, new Vector2(0, -170), new Vector2(600, 40));
            _cancel = UI.Button(panel.transform, "Cancel", new Vector2(0, 50), new Vector2(260, 76), () => _payment?.Cancel(), ButtonStyle.Ghost, 26, new Vector2(0.5f, 0f));
        }

        public void Run(IPaymentProvider payment, float amount, string currency, Action<PaymentResult> onDone)
        {
            _payment = payment;
            _amount.text = currency + amount.ToString("0.00");
            _provider.text = payment.DisplayName;
            _status.text = "";
            Show();
            Spin();
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(payment.RequestPayment(amount, currency, s => _status.text = s, r =>
            {
                _status.text = r.message;
                _status.color = r.status == PaymentStatus.Approved ? Theme.Pass : Theme.Fail;
                if (r.status == PaymentStatus.Approved) Sfx.Good(); else Sfx.Bad();
                Tween.Delay(0.7f, () => onDone(r));
            }));
        }

        void Spin()
        {
            if (!Visible) return;
            Tween.To(_spinner, "spin", 1.1f, Ease.Linear, k => { if (_spinner != null) _spinner.rectTransform.localRotation = Quaternion.Euler(0, 0, -360f * k); }, Spin);
        }
    }

    // ---------------------------------------------------------------- Name entry
    public class NameScreen : UIScreen
    {
        TextField _field;
        Action<string> _onDone;

        protected override void Build()
        {
            UI.Title(Root, "YOUR NAME", 54, new Vector2(0, -90), new Vector2(1000, 70), Theme.Text, new Vector2(0.5f, 1f));
            UI.Label(Root, "Sub", "FOR THE LEADERBOARD", 24, Theme.Muted, new Vector2(0, -150), new Vector2(1000, 40), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f)).characterSpacing = 8;
            _field = new TextField(Root, new Vector2(0, 230), new Vector2(760, 110), "TAP THE KEYS");
            var kb = new OnScreenKeyboard(Root, new Vector2(0, -110));
            kb.OnChar = c => { Sfx.Tick(); _field.Append(c); };
            kb.OnBackspace = () => _field.Backspace();
            kb.OnDone = () => { if (_field.Value.Trim().Length > 0) _onDone?.Invoke(_field.Value.Trim()); };
        }

        public void Open(Action<string> onDone)
        {
            _onDone = onDone;
            _field.Set("");
            Show();
        }
    }

    // ---------------------------------------------------------------- Stage intro
    public class IntroScreen : UIScreen
    {
        TMP_Text _stage, _title, _rule, _count;
        Image[] _dots;
        Coroutine _co;

        protected override void Build()
        {
            _stage = UI.Label(Root, "Stage", "", 26, Theme.Accent, new Vector2(0, 170), new Vector2(800, 40), TextAlignmentOptions.Center, FontStyles.Bold);
            _stage.characterSpacing = 10;
            _title = UI.Title(Root, "", 110, new Vector2(0, 70), new Vector2(1400, 140));
            _rule = UI.Label(Root, "Rule", "", 34, Theme.Text, new Vector2(0, -30), new Vector2(1200, 60));
            _count = UI.Label(Root, "Count", "", 160, Theme.Accent, new Vector2(0, -200), new Vector2(400, 200), TextAlignmentOptions.Center, FontStyles.Bold);
            _dots = UI.Dots(Root, 5, new Vector2(0, 70), 36, 16, new Vector2(0.5f, 0f));
        }

        public void Open(int stage, int cleared, string title, string rule, Action onDone)
        {
            _stage.text = "CHALLENGE " + stage + " OF 5";
            _title.text = title;
            _rule.text = rule;
            for (int i = 0; i < 5; i++) _dots[i].color = i < cleared ? Theme.Accent : i == stage - 1 ? Theme.Text : Theme.Muted.WithA(0.4f);
            Show();
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(Countdown(onDone));
        }

        IEnumerator Countdown(Action onDone)
        {
            _count.text = "";
            yield return new WaitForSecondsRealtime(0.9f);
            for (int n = 3; n >= 1; n--)
            {
                _count.text = n.ToString();
                Tween.Pop(_count.transform, 0.3f);
                Sfx.Tick();
                yield return new WaitForSecondsRealtime(0.75f);
            }
            _count.text = "GO";
            Tween.Pop(_count.transform, 0.3f);
            Sfx.Go();
            yield return new WaitForSecondsRealtime(0.45f);
            onDone();
        }
    }

    // ---------------------------------------------------------------- Stage result
    public class ResultScreen : UIScreen
    {
        TMP_Text _status, _headline, _detail, _score, _auto;
        Button _primary, _secondary;
        Image _glow;
        Action _primaryAction, _secondaryAction;
        float _autoLeft;
        Action _autoAction;

        protected override void Build()
        {
            Backdrop(0.75f);
            _glow = UI.Image(Root, "Glow", Sprites.Glow, Theme.Pass.WithA(0.25f), new Vector2(1400, 1400), new Vector2(0, 60));
            _status = UI.Title(Root, "", 96, new Vector2(0, 150), new Vector2(1400, 120));
            _headline = UI.Label(Root, "Headline", "", 48, Theme.Text, new Vector2(0, 40), new Vector2(1400, 70), TextAlignmentOptions.Center, FontStyles.Bold);
            _detail = UI.Label(Root, "Detail", "", 28, Theme.Muted, new Vector2(0, -15), new Vector2(1400, 50));
            _score = UI.Label(Root, "Score", "", 40, Theme.Accent, new Vector2(0, -80), new Vector2(600, 60), TextAlignmentOptions.Center, FontStyles.Bold);
            _primary = UI.Button(Root, "Continue", new Vector2(0, -200), new Vector2(560, 90), () => _primaryAction?.Invoke());
            _secondary = UI.Button(Root, "End session", new Vector2(0, -310), new Vector2(420, 76), () => _secondaryAction?.Invoke(), ButtonStyle.Ghost, 26);
            _auto = UI.Label(Root, "Auto", "", 22, Theme.Muted, new Vector2(0, 40), new Vector2(800, 40), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 0f));
        }

        public void OpenPass(ChallengeResult r, float autoSec, Action next)
        {
            _status.text = "CLEARED";
            _status.color = Theme.Pass;
            _glow.color = Theme.Pass.WithA(0.22f);
            _headline.text = r.headline;
            _detail.text = r.detail;
            _score.text = "+" + r.score + " POINTS";
            UI.SetButtonText(_primary, "Continue");
            _primary.gameObject.SetActive(true);
            _secondary.gameObject.SetActive(false);
            _primaryAction = next;
            _autoAction = next;
            _autoLeft = autoSec;
            Show();
            Sfx.Good();
        }

        public void OpenFail(ChallengeResult r, int retriesLeft, float autoSec, Action retry, Action end)
        {
            _status.text = "FAILED";
            _status.color = Theme.Fail;
            _glow.color = Theme.Fail.WithA(0.2f);
            _headline.text = r.headline;
            _detail.text = r.detail;
            _score.text = "";
            if (retriesLeft > 0)
            {
                UI.SetButtonText(_primary, "Second chance");
                _primary.gameObject.SetActive(true);
                _secondary.gameObject.SetActive(true);
                _primaryAction = retry;
                _secondaryAction = end;
                _autoAction = end;
            }
            else
            {
                UI.SetButtonText(_primary, "Vault locked");
                _primary.gameObject.SetActive(true);
                _secondary.gameObject.SetActive(false);
                _primaryAction = end;
                _autoAction = end;
            }
            _autoLeft = autoSec + (retriesLeft > 0 ? 6f : 0f);
            Show();
            Sfx.Bad();
        }

        void Update()
        {
            if (!Visible || _autoAction == null) return;
            _autoLeft -= Time.unscaledDeltaTime;
            _auto.text = "CONTINUING IN " + Mathf.CeilToInt(Mathf.Max(0, _autoLeft));
            if (_autoLeft <= 0f)
            {
                var a = _autoAction;
                _autoAction = null;
                a();
            }
        }

        protected override void OnHide() => _autoAction = null;
    }

    // ---------------------------------------------------------------- Win / lose
    public class EndScreen : UIScreen
    {
        TMP_Text _title, _line1, _line2, _score;
        RectTransform _doorL, _doorR;
        Image _glow;
        Button _done;
        Action _onDone;

        protected override void Build()
        {
            _glow = UI.Image(Root, "Glow", Sprites.Glow, Theme.Accent.WithA(0.3f), new Vector2(1600, 1600), new Vector2(0, 40));
            _title = UI.Title(Root, "", 100, new Vector2(0, 230), new Vector2(1600, 130), Theme.Accent);
            _line1 = UI.Label(Root, "L1", "", 54, Theme.Text, new Vector2(0, 90), new Vector2(1400, 80), TextAlignmentOptions.Center, FontStyles.Bold);
            _line2 = UI.Label(Root, "L2", "", 30, Theme.Muted, new Vector2(0, 30), new Vector2(1400, 50));
            _score = UI.Label(Root, "Score", "", 36, Theme.Accent, new Vector2(0, -60), new Vector2(1500, 60), TextAlignmentOptions.Center, FontStyles.Bold);
            _done = UI.Button(Root, "Done", new Vector2(0, -220), new Vector2(360, 90), () => _onDone?.Invoke());

            // Door halves are oversized so they cover any aspect ratio, and slide fully off-screen.
            var doorSize = new Vector2(1920, 2600);
            _doorL = UI.Image(Root, "DoorL", Sprites.Square, Theme.PanelLight, doorSize, new Vector2(-960, 0)).rectTransform;
            _doorR = UI.Image(Root, "DoorR", Sprites.Square, Theme.PanelLight, doorSize, new Vector2(960, 0)).rectTransform;
            UI.Image(_doorL, "Edge", Sprites.Square, Theme.Accent, new Vector2(6, 2600), new Vector2(957, 0));
            UI.Image(_doorR, "Edge", Sprites.Square, Theme.Accent, new Vector2(6, 2600), new Vector2(-957, 0));
            UI.Image(_doorL, "Wheel", Sprites.Ring, Theme.Accent.WithA(0.8f), new Vector2(260, 260), new Vector2(960, 0));
        }

        public void OpenWin(string prize, int score, int rank, Action onDone)
        {
            _onDone = onDone;
            _title.text = "THE VAULT OPENS";
            _line1.text = prize.ToUpperInvariant();
            _line2.text = "All five challenges cleared. Collect your prize from the host.";
            _score.text = score + " POINTS" + (rank > 0 ? "   ·   #" + rank + " ON THE LEADERBOARD" : "");
            _doorL.gameObject.SetActive(true);
            _doorR.gameObject.SetActive(true);
            _doorL.anchoredPosition = new Vector2(-960, 0);
            _doorR.anchoredPosition = new Vector2(960, 0);
            Show();
            Sfx.Win();
            Tween.Delay(0.9f, () =>
            {
                Tween.Move(_doorL, new Vector2(-3000, 0), 1.4f, Ease.InOutSine);
                Tween.Move(_doorR, new Vector2(3000, 0), 1.4f, Ease.InOutSine);
            });
        }

        public void OpenLose(int cleared, int score, int rank, Action onDone)
        {
            _onDone = onDone;
            _title.text = "LOCKED";
            _line1.text = "YOU CLEARED " + cleared + " OF 5";
            _line2.text = "The vault stays shut this time. Thank you for playing.";
            _score.text = score + " POINTS" + (rank > 0 ? "   ·   #" + rank + " ON THE LEADERBOARD" : "");
            _doorL.gameObject.SetActive(false);
            _doorR.gameObject.SetActive(false);
            Show();
        }
    }

    // ---------------------------------------------------------------- Leaderboard
    public class LeaderboardScreen : UIScreen
    {
        TMP_Text _rows;
        Action _onDone;
        float _left;

        protected override void Build()
        {
            UI.Title(Root, "LEADERBOARD", 64, new Vector2(0, -80), new Vector2(1200, 80), Theme.Accent, new Vector2(0.5f, 1f));
            var panel = UI.Panel(Root, "Panel", new Vector2(0, 20), new Vector2(1000, 700));
            UI.Label(panel.transform, "Head", "<pos=0>#<pos=70>PLAYER<pos=560>STAGES<pos=760>SCORE", 22, Theme.Muted, new Vector2(0, -36), new Vector2(880, 30), TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0.5f, 1f)).characterSpacing = 6;
            UI.Line(panel.transform, new Vector2(0, -74), 900, null, new Vector2(0.5f, 1f));
            _rows = UI.Label(panel.transform, "Rows", "", 34, Theme.Text, new Vector2(0, -96), new Vector2(880, 560), TextAlignmentOptions.TopLeft, FontStyles.Normal, new Vector2(0.5f, 1f));
            _rows.lineSpacing = 22;
            UI.Button(Root, "Play again", new Vector2(0, 50), new Vector2(360, 86), () => _onDone?.Invoke(), ButtonStyle.Primary, 30, new Vector2(0.5f, 0f));
        }

        public void Open(int highlightRank, Action onDone)
        {
            _onDone = onDone;
            _left = 15f;
            var sb = new System.Text.StringBuilder();
            int i = 1;
            foreach (var e in Flow.Board.Entries)
            {
                bool hl = i == highlightRank;
                string c = hl ? "#D4AF37" : "#F4F5F7";
                sb.Append($"<color={c}><pos=0>{i}.<pos=70>{e.name}<pos=560>{e.stagesCleared} / 5<pos=760>{e.score}</color>\n");
                i++;
            }
            if (i == 1) sb.Append("<color=#8A93A6>No scores yet.</color>");
            _rows.text = sb.ToString();
            Show();
        }

        void Update()
        {
            if (!Visible || _onDone == null) return;
            _left -= Time.unscaledDeltaTime;
            if (_left <= 0f) { var a = _onDone; _onDone = null; a(); }
        }
    }

    // ---------------------------------------------------------------- PIN
    public class PinScreen : UIScreen
    {
        TMP_Text _dots, _msg;
        string _entered = "", _pin;
        Action _ok, _cancel;

        protected override void Build()
        {
            Backdrop(0.85f);
            var panel = UI.Panel(Root, "Panel", Vector2.zero, new Vector2(560, 820));
            UI.Label(panel.transform, "Head", "OPERATOR PIN", 24, Theme.Accent, new Vector2(0, -36), new Vector2(500, 40), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(0.5f, 1f)).characterSpacing = 8;
            _dots = UI.Label(panel.transform, "Dots", "", 60, Theme.Text, new Vector2(0, -100), new Vector2(500, 80), TextAlignmentOptions.Center, FontStyles.Bold, new Vector2(0.5f, 1f));
            _msg = UI.Label(panel.transform, "Msg", "", 22, Theme.Fail, new Vector2(0, -170), new Vector2(500, 40), TextAlignmentOptions.Center, FontStyles.Normal, new Vector2(0.5f, 1f));
            string keys = "123456789";
            for (int i = 0; i < 9; i++)
            {
                int d = i + 1;
                UI.Button(panel.transform, keys[i].ToString(), new Vector2(-130 + (i % 3) * 130, 40 - (i / 3) * 120), new Vector2(110, 100), () => Press(d.ToString()), ButtonStyle.Quiet, 40);
            }
            UI.Button(panel.transform, "0", new Vector2(0, -320), new Vector2(110, 100), () => Press("0"), ButtonStyle.Quiet, 40);
            UI.Button(panel.transform, "Del", new Vector2(130, -320), new Vector2(110, 100), () => { if (_entered.Length > 0) _entered = _entered.Substring(0, _entered.Length - 1); Refresh(); }, ButtonStyle.Quiet, 24);
            UI.Button(panel.transform, "Exit", new Vector2(-130, -320), new Vector2(110, 100), () => _cancel?.Invoke(), ButtonStyle.Ghost, 24);
        }

        public void Open(string pin, Action ok, Action cancel)
        {
            _pin = pin; _ok = ok; _cancel = cancel; _entered = ""; _msg.text = "";
            Refresh();
            Show();
        }

        void Press(string d)
        {
            if (_entered.Length >= 6) return;
            _entered += d;
            Refresh();
            if (_entered.Length < _pin.Length) return;
            if (_entered == _pin) { Sfx.Good(); _ok?.Invoke(); }
            else { Sfx.Bad(); _msg.text = "Incorrect PIN"; _entered = ""; Tween.Delay(0.4f, Refresh); }
        }

        void Refresh() => _dots.text = _entered.Length == 0 ? "<color=#8A93A6>· · · ·</color>" : string.Join(" ", new string('•', _entered.Length).ToCharArray());
    }
}
