using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Windows.Speech;
using TMPro;

public class ReconocimientoVoz : MonoBehaviour
{
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 10;
    public TMP_Text scoreText;

    [SerializeField] LocalizedString scoreLabel = new LocalizedString("ScoreShooting", "Score Shooting");

    string _cachedScoreLabel = "Score";

    KeywordRecognizer keywordRecognizer;
    Dictionary<string, Action> wordsToActions;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshScoreLabel();
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale _) => RefreshScoreLabel();

    void RefreshScoreLabel()
    {
        var handle = scoreLabel.GetLocalizedStringAsync();
        handle.Completed += op =>
        {
            _cachedScoreLabel = op.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(op.Result)
                ? op.Result
                : "Score";
        };
    }

    void Start()
    {
        wordsToActions = new Dictionary<string, Action>();
        wordsToActions.Add("shoot", Shoot);
        keywordRecognizer = new KeywordRecognizer(wordsToActions.Keys.ToArray(), ConfidenceLevel.Low);
        keywordRecognizer.OnPhraseRecognized += WordRecognizer;
        keywordRecognizer.Start();
    }

    void Update()
    {
        Puntaje();
    }

    void WordRecognizer(PhraseRecognizedEventArgs word)
    {
        Debug.Log(word.text);
        wordsToActions[word.text].Invoke();
    }

    public void Shoot()
    {
        Debug.Log("Shoot");
        var bullet = PoolManager.Instance.GetBullet();
        bullet.transform.position = bulletSpawnPoint.position;
        bullet.transform.rotation = bulletSpawnPoint.rotation;
        bullet.SetActive(true);
        bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * bulletSpeed;
    }

    void Puntaje()
    {
        if (scoreText == null || ObjetivosManager.Instance == null)
            return;

        scoreText.text = $"{_cachedScoreLabel} {ObjetivosManager.Instance.puntos}";
    }
}
