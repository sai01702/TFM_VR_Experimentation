using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;

public class FuncionamientoPistola : MonoBehaviour
{
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public InputActionProperty shootAction;
    public float bulletSpeed = 10;
    public bool disparo = false;
    public bool disparado = true;
    public TMP_Text scoreText;

    [SerializeField] LocalizedString scoreLabel = new LocalizedString("ScoreShooting", "Score Shooting");

    string _cachedScoreLabel = "Score";

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

    void Update()
    {
        Puntaje();
        float shoot = shootAction.action.ReadValue<float>();
        if (shoot == 1)
            disparo = true;
        else
        {
            disparo = false;
            disparado = true;
        }

        if (disparo && disparado)
        {
            Shoot();
            disparado = false;
        }
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
