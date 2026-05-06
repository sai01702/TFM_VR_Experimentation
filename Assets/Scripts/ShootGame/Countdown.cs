using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using TMPro;

public class Countdown : MonoBehaviour
{
    public float tiempo = 60f;
    public TMP_Text countDownText;

    [SerializeField] LocalizedString timeLeftLabel = new LocalizedString("TimeLEftShooting", "Time Left");

    string _cachedTimeLeftLabel = "Time Left";
    float _remainingSeconds;
    bool _sessionEnded;

    void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshTimeLeftLabel();
        _remainingSeconds = tiempo;
        _sessionEnded = false;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale _) => RefreshTimeLeftLabel();

    void RefreshTimeLeftLabel()
    {
        var handle = timeLeftLabel.GetLocalizedStringAsync();
        handle.Completed += op =>
        {
            _cachedTimeLeftLabel = op.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(op.Result)
                ? op.Result
                : "Time Left";
        };
    }

    void Update()
    {
        if (_sessionEnded)
            return;

        if (countDownText == null)
            return;

        _remainingSeconds -= Time.deltaTime;
        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            countDownText.text = $"{_cachedTimeLeftLabel} 00:00";
            _sessionEnded = true;
            EndShooterSession();
            return;
        }

        int total = Mathf.FloorToInt(_remainingSeconds);
        int minutes = total / 60;
        int seconds = total % 60;

        countDownText.text = $"{_cachedTimeLeftLabel} {minutes:00}:{seconds:00}";
    }

    void EndShooterSession()
    {
        if (SceneTracker.Instance != null)
        {
            SceneTracker.Instance.PreviousScene = SceneManager.GetActiveScene().name;
            int score = ObjetivosManager.Instance != null ? ObjetivosManager.Instance.puntos : 0;
            SceneTracker.Instance.SetShooterResults(score);
        }

        SceneManager.LoadScene("GameOverScene");
    }
}
