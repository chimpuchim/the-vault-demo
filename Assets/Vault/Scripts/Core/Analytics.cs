using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Vault
{
    [Serializable]
    public class MetricEntry
    {
        public string key;
        public float value;
    }

    [Serializable]
    public class StageRecord
    {
        public int stage;            // 1..5
        public string name;
        public int attempt;          // 1 = first try, 2 = second chance
        public bool passed;
        public int score;
        public float durationSec;
        public List<MetricEntry> metrics = new List<MetricEntry>();

        public float Metric(string key)
        {
            var m = metrics.FirstOrDefault(x => x.key == key);
            return m == null ? float.NaN : m.value;
        }
    }

    [Serializable]
    public class SessionRecord
    {
        public string id;
        public string startedAt;     // ISO 8601, local time
        public string endedAt;
        public string player;
        public float paid;
        public string outcome = "in_progress"; // won | lost | abandoned
        public int totalScore;
        public int stagesCleared;
        public List<StageRecord> stages = new List<StageRecord>();
    }

    [Serializable]
    public class AnalyticsSummary
    {
        public int sessions;
        public int won;
        public int lost;
        public int abandoned;
        public float revenue;
        public float completionRate;      // won / finished
        public float[] stagePassRate = new float[5];
        public float avgReactionMs;
        public float avgPrecisionPx;
        public float avgRecallLength;
        public float avgTrackCoverage;
        public float avgDialLocks;
    }

    /// <summary>Where finished sessions go. The demo keeps them locally; a kiosk would also post them.</summary>
    public interface IAnalyticsSink
    {
        void Record(SessionRecord session);
    }

    [Serializable]
    class SessionList
    {
        public List<SessionRecord> items = new List<SessionRecord>();
    }

    /// <summary>Local analytics store: last N sessions in PlayerPrefs, summary stats, JSON export.</summary>
    public class AnalyticsStore : IAnalyticsSink
    {
        const string PrefsKey = "vault.sessions.v1";
        const int MaxSessions = 300;

        readonly SessionList _data;

        public AnalyticsStore()
        {
            var json = PlayerPrefs.GetString(PrefsKey, "");
            _data = string.IsNullOrEmpty(json) ? new SessionList() : JsonUtility.FromJson<SessionList>(json) ?? new SessionList();
        }

        public IReadOnlyList<SessionRecord> Sessions => _data.items;

        public void Record(SessionRecord session)
        {
            var existing = _data.items.FindIndex(s => s.id == session.id);
            if (existing >= 0) _data.items[existing] = session;
            else _data.items.Add(session);
            while (_data.items.Count > MaxSessions) _data.items.RemoveAt(0);
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        public void Clear()
        {
            _data.items.Clear();
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
        }

        public string ExportJson() => JsonUtility.ToJson(_data, true);

        public AnalyticsSummary Summarise()
        {
            var s = new AnalyticsSummary();
            var items = _data.items;
            s.sessions = items.Count;
            s.won = items.Count(x => x.outcome == "won");
            s.lost = items.Count(x => x.outcome == "lost");
            s.abandoned = items.Count(x => x.outcome == "abandoned");
            s.revenue = items.Sum(x => x.paid);
            var finished = s.won + s.lost;
            s.completionRate = finished == 0 ? 0 : (float)s.won / finished;

            for (int i = 0; i < 5; i++)
            {
                var attempts = items.SelectMany(x => x.stages).Where(st => st.stage == i + 1).ToList();
                s.stagePassRate[i] = attempts.Count == 0 ? 0 : (float)attempts.Count(a => a.passed) / attempts.Count;
            }

            s.avgReactionMs = Average(items, 1, "avgMs");
            s.avgPrecisionPx = Average(items, 2, "avgOffsetPx");
            s.avgRecallLength = Average(items, 3, "reachedLength");
            s.avgTrackCoverage = Average(items, 4, "coverage");
            s.avgDialLocks = Average(items, 5, "locksHit");
            return s;
        }

        static float Average(List<SessionRecord> items, int stage, string key)
        {
            var values = items.SelectMany(x => x.stages)
                .Where(st => st.stage == stage)
                .Select(st => st.Metric(key))
                .Where(v => !float.IsNaN(v))
                .ToList();
            return values.Count == 0 ? 0 : values.Average();
        }

        public static string Describe(AnalyticsSummary s)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Sessions: {s.sessions}   Won: {s.won}   Lost: {s.lost}   Abandoned: {s.abandoned}");
            sb.AppendLine($"Completion rate: {s.completionRate:P0}   Revenue: {s.revenue:0.00}");
            sb.AppendLine();
            string[] names = { "Reflex", "Precision", "Recall", "Track", "Vault" };
            for (int i = 0; i < 5; i++) sb.AppendLine($"Stage {i + 1} {names[i],-10} pass rate {s.stagePassRate[i]:P0}");
            sb.AppendLine();
            sb.AppendLine($"Avg reaction: {s.avgReactionMs:0} ms");
            sb.AppendLine($"Avg precision offset: {s.avgPrecisionPx:0.0} px");
            sb.AppendLine($"Avg recall length reached: {s.avgRecallLength:0.0}");
            sb.AppendLine($"Avg track coverage: {s.avgTrackCoverage:P0}");
            sb.AppendLine($"Avg vault locks hit: {s.avgDialLocks:0.0}");
            return sb.ToString();
        }
    }
}
