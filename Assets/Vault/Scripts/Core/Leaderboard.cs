using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vault
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public int stagesCleared;
        public string date;
    }

    [Serializable]
    class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>Top scores, persisted locally. Returns the rank of a new entry so the win screen can show it.</summary>
    public class Leaderboard
    {
        const string PrefsKey = "vault.leaderboard.v1";
        public const int Capacity = 10;

        readonly LeaderboardData _data;

        public Leaderboard()
        {
            var json = PlayerPrefs.GetString(PrefsKey, "");
            _data = string.IsNullOrEmpty(json) ? new LeaderboardData() : JsonUtility.FromJson<LeaderboardData>(json) ?? new LeaderboardData();
        }

        public IReadOnlyList<LeaderboardEntry> Entries => _data.entries;

        /// <summary>Adds the entry and returns its 1-based rank, or 0 when it did not make the board.</summary>
        public int Add(string name, int score, int stagesCleared)
        {
            var entry = new LeaderboardEntry
            {
                name = string.IsNullOrWhiteSpace(name) ? "PLAYER" : name.Trim().ToUpperInvariant(),
                score = score,
                stagesCleared = stagesCleared,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };
            _data.entries.Add(entry);
            _data.entries = _data.entries.OrderByDescending(e => e.score).ThenBy(e => e.date).Take(Capacity).ToList();
            Save();
            var rank = _data.entries.IndexOf(entry);
            return rank < 0 ? 0 : rank + 1;
        }

        public void Clear()
        {
            _data.entries.Clear();
            Save();
        }

        void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }
    }
}
