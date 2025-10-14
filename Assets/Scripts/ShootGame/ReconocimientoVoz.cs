using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using TMPro;

public class ReconocimientoVoz : MonoBehaviour
{
    [Header("Shooting")]
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;       // make sure this is assigned if you use pooling fallback
    public float bulletSpeed = 10f;

    [Header("UI")]
    public TMP_Text scoreText;

    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, Action> wordsToActions;

    void Start()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (!PhraseRecognitionSystem.isSupported)
        {
            Debug.LogWarning("[Voice] PhraseRecognitionSystem not supported on this system.");
            return;
        }

        wordsToActions = new Dictionary<string, Action>(StringComparer.InvariantCultureIgnoreCase)
        {
            { "shoot", Shoot }
            // if your Windows Speech language is Spanish, also add:
            // { "disparar", Shoot }
        };

        var keywords = wordsToActions.Keys.ToArray();
        if (keywords.Length == 0)
        {
            Debug.LogError("[Voice] No keywords configured.");
            return;
        }

        try
        {
            // Use a forgiving confidence level
            keywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Low);
            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
            keywordRecognizer.Start();

            Debug.Log($"[Voice] KeywordRecognizer started. Language is set in Windows Speech settings. Keywords: {string.Join(", ", keywords)}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Voice] Failed to start KeywordRecognizer: {e.Message}");
        }
#else
        Debug.LogWarning("[Voice] UnityEngine.Windows.Speech works only on Windows.");
#endif
    }

    void Update()
    {
        if (scoreText != null)
            scoreText.text = "Puntuación " + ObjetivosManager.Instance.puntos;
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        // Normalize the recognized text
        var key = args.text.Trim().ToLowerInvariant();
        Debug.Log($"[Voice] Heard: {args.text} (conf: {args.confidence})");

        if (wordsToActions != null && wordsToActions.TryGetValue(key, out var action))
            action?.Invoke();
        else
            Debug.LogWarning($"[Voice] Unmapped keyword: '{args.text}'");
    }

    public void Shoot()
    {
        Debug.Log("[Voice] Shoot()");
        // If you use a pool:
        var bulletGO = PoolManager.Instance != null ? PoolManager.Instance.GetBullet() : Instantiate(bulletPrefab);
        if (bulletGO == null || bulletSpawnPoint == null) return;

        bulletGO.transform.SetPositionAndRotation(bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bulletGO.SetActive(true);

        var rb = bulletGO.GetComponent<Rigidbody>();
        if (rb != null) rb.velocity = bulletSpawnPoint.forward * bulletSpeed;
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null)
        {
            if (keywordRecognizer.IsRunning) keywordRecognizer.Stop();
            keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;
            keywordRecognizer.Dispose();
        }
    }
}
