using System.Collections.Generic;
using UnityEngine;

public static class LeaderboardManager
{
    public const int MaxEntries = 10;
    public const int MaxNameLength = 10;

    const string NameKeyPrefix = "Leaderboard_Name_";
    const string ScoreKeyPrefix = "Leaderboard_Score_";

    public struct Entry
    {
        public string Name;
        public int Score;
    }

    public static List<Entry> GetEntries()
    {
        var entries = new List<Entry>();
        for (int i = 0; i < MaxEntries; i++)
        {
            int score = PlayerPrefs.GetInt(ScoreKeyPrefix + i, -1);
            if (score < 0) continue;
            entries.Add(new Entry { Name = PlayerPrefs.GetString(NameKeyPrefix + i, "---"), Score = score });
        }
        return entries;
    }

    public static bool QualifiesForLeaderboard(int score)
    {
        var entries = GetEntries();
        return entries.Count < MaxEntries || score > entries[entries.Count - 1].Score;
    }

    public static void AddScore(string name, int score)
    {
        name = SanitizeName(name);

        var entries = GetEntries();
        entries.Add(new Entry { Name = name, Score = score });
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);

        for (int i = 0; i < entries.Count; i++)
        {
            PlayerPrefs.SetString(NameKeyPrefix + i, entries[i].Name);
            PlayerPrefs.SetInt(ScoreKeyPrefix + i, entries[i].Score);
        }
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        for (int i = 0; i < MaxEntries; i++)
        {
            PlayerPrefs.DeleteKey(NameKeyPrefix + i);
            PlayerPrefs.DeleteKey(ScoreKeyPrefix + i);
        }
        PlayerPrefs.Save();
    }

    static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "---";
        name = name.Trim().ToUpperInvariant();
        if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
        return name;
    }
}
