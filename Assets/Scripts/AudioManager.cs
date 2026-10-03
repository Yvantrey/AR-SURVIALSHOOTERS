using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip playerShootClip;
    public AudioClip playerDeathClip;
    public AudioClip enemySpawnClip;
    public AudioClip enemyShootClip;
    public AudioClip meleeDamageClip;

    AudioSource _source;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _source = gameObject.AddComponent<AudioSource>();
    }

    public void PlayPlayerShoot(Vector3 pos) => Play(playerShootClip);
    public void PlayPlayerDeath(Vector3 pos) => Play(playerDeathClip);
    public void PlayEnemySpawn(Vector3 pos) => Play(enemySpawnClip);
    public void PlayEnemyShoot(Vector3 pos) => Play(enemyShootClip);
    public void PlayMeleeDamage(Vector3 pos) => Play(meleeDamageClip);

    void Play(AudioClip clip)
    {
        if (clip != null) _source.PlayOneShot(clip);
    }
}
