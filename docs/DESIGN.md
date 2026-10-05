# THE VAULT — touchscreen skill-challenge prototype

Demo built for the Upwork brief "Premium Touchscreen Skill Challenge Platform". It is a
functional prototype, not a finished product: it shows the full session flow, the five
challenges, the admin panel, analytics, leaderboard and the payment seam.

## Session flow

```
Attract (idle loop, leaderboard ticker)
  -> Payment (IPaymentProvider; mock provider in the demo, EFTPOS later)
  -> Name entry (on-screen keyboard)
  -> for stage 1..5:
       Stage intro (name, rule, 3-2-1)
       -> Challenge
       -> Result (pass: metrics + Next | fail: Second chance if allowed, else Vault locked)
  -> Vault opens: prize, score, rank -> Leaderboard -> Attract
Idle timeout anywhere -> Attract (session recorded as abandoned)
Admin: tap the top-left corner 5x within 3 s -> PIN -> Admin panel
```

## The five challenges

| # | Name | Measures | Pass rule (defaults, all configurable) |
|---|------|----------|----------------------------------------|
| 1 | REFLEX | reaction time in ms | 3 rounds, average <= 450 ms, false start or no tap within 1.5 s = 1000 ms |
| 2 | PRECISION | touch offset from target centre in px | 6 targets, shrinking, average offset <= 30 px, max 1 miss |
| 3 | RECALL | sequence memory | 3x3 grid, sequence grows 3 -> 5, no mistakes |
| 4 | TRACK | hand-eye coordination | keep the finger on a moving orb, >= 80 % of 12 s |
| 5 | THE VAULT | timing + precision | stop the spinning marker inside the window, 3 locks, window narrows |

Each challenge returns `ChallengeResult { passed, score 0..1000, metrics, duration }`.
The final score is the sum of the five stage scores (max 5000).

Developer mode (`?dev=1` in the URL, or the Editor) logs each challenge's hidden state and
lets Track be held with one tap, so the whole flow can be driven by automated UI tests.

## Architecture

- `VaultConfig` — one serialisable object with campaign/branding, economy, session and
  per-stage difficulty. Defaults in code, admin edits saved as JSON in PlayerPrefs.
- `GameFlow` — state machine that owns the session and switches screens.
- `IChallenge` / `ChallengeBase` — every challenge is a self-contained panel with
  `Begin(context)` and an `OnComplete` callback; adding a sixth challenge is one class.
- `IPaymentProvider` — `MockPaymentProvider` in the demo; an EFTPOS integration
  implements the same interface (RequestPayment -> approved/declined/cancelled).
- `AnalyticsStore` — one `SessionRecord` per session (stages, attempts, metrics,
  outcome); summary stats in the admin panel; JSON export. The sink is an interface so
  the kiosk can post to a server later.
- `Leaderboard` — top 10 by score, persisted locally.
- UI is built in code (`UIFactory`) from procedurally generated sprites, so the whole
  prototype is one scene and plain C#; no art assets are required.

## Timing accuracy

Reaction time is measured from the frame the stimulus is shown to the pointer-down
event, using `Time.realtimeSinceStartupAsDouble`. On a 60 Hz display that gives about
±8 ms. For the production kiosk the Input System's event timestamps (hardware time)
remove most of the remaining frame quantisation, and the display latency can be
calibrated once per screen in the admin panel.

## Build

`unity build --target WebGL --execute-method VaultBuild.BuildWebGL -o Build/WebGL`
The same code runs as a Windows standalone build for the real touchscreen.
