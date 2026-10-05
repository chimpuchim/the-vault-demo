using UnityEngine;

namespace Vault
{
    /// <summary>
    /// Developer mode for automated testing: `?dev=1` in the WebGL URL (always on in the Editor).
    /// Logs the hidden state of each challenge and lets Track be held with a single tap.
    /// </summary>
    public static class Dev
    {
        public static bool Enabled { get; private set; }

        public static void Init()
        {
            Enabled = Application.isEditor || (Application.absoluteURL ?? "").Contains("dev=1");
            if (!Enabled) return;
            Random.InitState(20261006); // reproducible targets, sequences and dial windows for automated runs
            Debug.Log("[DEV] developer mode on, seeded");
        }

        public static void Log(string message)
        {
            if (Enabled) Debug.Log("[DEV] " + message);
        }
    }
}
