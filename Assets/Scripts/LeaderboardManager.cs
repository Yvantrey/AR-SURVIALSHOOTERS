using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SessionData
{
    public int score;
    public int enemiesDefeated;
    public float timeSurvived;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    const string Key = "Leaderboard";
    const int MaxEntries = 5;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SaveSession(int score, int enemies, float time)
    {
        var list = LoadAll();
        list.Insert(0, new SessionData { score = score, enemiesDefeated = enemies, timeSurvived = time });
        if (list.Count > MaxEntries) list.RemoveRange(MaxEntries, list.Count - MaxEntries);
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(new Wrapper { entries = list }));
        PlayerPrefs.Save();
    }

    public List<SessionData> LoadAll()
    {
        string json = PlayerPrefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return new List<SessionData>();
        return JsonUtility.FromJson<Wrapper>(json).entries ?? new List<SessionData>();
    }

    [Serializable] class Wrapper { public List<SessionData> entries; }
}
