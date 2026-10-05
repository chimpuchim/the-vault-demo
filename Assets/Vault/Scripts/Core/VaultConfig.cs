using System;
using UnityEngine;

namespace Vault
{
    /// <summary>Branding and campaign data: everything an operator changes between activations.</summary>
    [Serializable]
    public class CampaignConfig
    {
        public string campaignName = "THE VAULT";
        public string tagline = "Five challenges. One prize.";
        public string prizeName = "Grand Prize";
        public string prizeDescription = "Complete all five challenges to unlock it.";
        public string accentHex = "#D4AF37";
        public string currencySymbol = "$";
    }

    [Serializable]
    public class SessionConfig
    {
        public float entryFee = 5f;
        public int secondChances = 1;        // retries per session, across all stages
        public float idleTimeoutSec = 45f;   // no touch for this long -> back to attract
        public float resultScreenSec = 6f;   // auto-advance after a result card
        public string adminPin = "1234";
    }

    [Serializable]
    public class ReflexConfig
    {
        public int rounds = 3;
        public float passAverageMs = 450f;
        public float falseStartPenaltyMs = 1000f;
        public float timeoutMs = 1500f;      // no tap within this -> round counts as the penalty
        public float minWaitSec = 1.5f;
        public float maxWaitSec = 4f;
    }

    [Serializable]
    public class PrecisionConfig
    {
        public int targets = 6;
        public float startRadiusPx = 140f;
        public float endRadiusPx = 70f;
        public float timePerTargetSec = 2.5f;
        public float passAverageOffsetPx = 30f;
        public int missesAllowed = 1;
    }

    [Serializable]
    public class RecallConfig
    {
        public int gridSize = 3;
        public int startLength = 3;
        public int endLength = 5;
        public float showStepSec = 0.55f;
        public int mistakesAllowed = 0;
    }

    [Serializable]
    public class TrackConfig
    {
        public float durationSec = 12f;
        public float orbRadiusPx = 90f;
        public float speed = 1f;             // multiplier on the path speed
        public float passCoverage = 0.8f;    // fraction of time the finger must be on the orb
    }

    [Serializable]
    public class VaultDialConfig
    {
        public int locks = 3;
        public float startSpeedDegPerSec = 180f;
        public float speedStepDegPerSec = 60f;
        public float startWindowDeg = 40f;
        public float windowStepDeg = 10f;
        public int missesAllowed = 0;
    }

    /// <summary>
    /// The single configuration object. Defaults live here; the admin panel edits a copy and
    /// saves it as JSON, so a kiosk can be re-tuned on site without a new build.
    /// </summary>
    [Serializable]
    public class VaultConfig
    {
        public const string PrefsKey = "vault.config.v1";

        public CampaignConfig campaign = new CampaignConfig();
        public SessionConfig session = new SessionConfig();
        public ReflexConfig reflex = new ReflexConfig();
        public PrecisionConfig precision = new PrecisionConfig();
        public RecallConfig recall = new RecallConfig();
        public TrackConfig track = new TrackConfig();
        public VaultDialConfig dial = new VaultDialConfig();

        public Color AccentColor =>
            ColorUtility.TryParseHtmlString(campaign.accentHex, out var c) ? c : new Color(0.83f, 0.69f, 0.22f);

        public static VaultConfig Load()
        {
            var json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return new VaultConfig();
            try
            {
                var cfg = JsonUtility.FromJson<VaultConfig>(json);
                return cfg ?? new VaultConfig();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Config JSON unreadable, using defaults: " + e.Message);
                return new VaultConfig();
            }
        }

        public void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        public VaultConfig Clone() => JsonUtility.FromJson<VaultConfig>(JsonUtility.ToJson(this));
    }
}
