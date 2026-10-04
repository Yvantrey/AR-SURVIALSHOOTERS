using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SessionData
{
    public int score;
    public int enemiesDefeated;
    public float timeSurvived;
    public bool won;
    public string difficulty;
    public string date;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    const string Key = "Leaderboard";
    const int MaxStored = 50;

    public SessionData LastSession { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    public void SaveSession(int score, int enemies, float time) => SaveSession(score, enemies, time, false, "");

    public void SaveSession(int score, int enemies, float time, bool won, string difficulty)
    {
        var list = LoadAll();
        LastSession = new SessionData
        {
            score = score, enemiesDefeated = enemies, timeSurvived = time,
            won = won, difficulty = difficulty, date = DateTime.Now.ToString("dd MMM HH:mm")
        };
        list.Insert(0, LastSession);
        if (list.Count > MaxStored) list.RemoveRange(MaxStored, list.Count - MaxStored);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Wrapper { entries = list }));
        PlayerPrefs.Save();
    }

    /// <summary>All sessions, newest first.</summary>
    public List<SessionData> LoadAll()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<SessionData>();
        return JsonUtility.FromJson<Wrapper>(json)?.entries ?? new List<SessionData>();
    }

    /// <summary>Best sessions by score (ties: more kills, then longer survival).</summary>
    public List<SessionData> LoadTop(int count)
    {
        var list = LoadAll();
        list.Sort((a, b) =>
        {
            int c = b.score.CompareTo(a.score);
            if (c != 0) return c;
            c = b.enemiesDefeated.CompareTo(a.enemiesDefeated);
            return c != 0 ? c : b.timeSurvived.CompareTo(a.timeSurvived);
        });
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }

    [Serializable] class Wrapper { public List<SessionData> entries; }
}
