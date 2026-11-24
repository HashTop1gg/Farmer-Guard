using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("UI Text")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Victory Screen")]
    [SerializeField] private GameObject victoryPanel;

    [Header("Format")]
    [SerializeField] private bool showMaxHealth = false;

    [Header("References")]
    [SerializeField] private Player_Script player;
    [SerializeField] private CurrencyManager currencyManager;
    [SerializeField] private WaveSpawner waveSpawner;

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<Player_Script>();

        if (currencyManager == null)
            currencyManager = FindAnyObjectByType<CurrencyManager>();

        if (waveSpawner == null)
            waveSpawner = FindAnyObjectByType<WaveSpawner>();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (player != null)
            player.OnHealthChanged += UpdateHealthUI;

        if (currencyManager != null)
            currencyManager.OnCurrencyChanged += UpdateCurrencyUI;

        WaveSpawner.OnWaveStarted += UpdateWaveUI;
        WaveSpawner.OnVictory += ShowVictoryScreen;
    }

    private void Start()
    {
        if (player != null)
            UpdateHealthUI(player.CurrentHealth, player.MaxHealth);

        if (currencyManager != null)
            UpdateCurrencyUI(currencyManager.CurrentCoins);

        if (waveSpawner != null && waveSpawner.CurrentWave > 0)
            UpdateWaveUI(waveSpawner.CurrentWave, waveSpawner.MaxWaves);
    }

    private void OnDisable()
    {
        if (player != null)
            player.OnHealthChanged -= UpdateHealthUI;

        if (currencyManager != null)
            currencyManager.OnCurrencyChanged -= UpdateCurrencyUI;

        WaveSpawner.OnWaveStarted -= UpdateWaveUI;
        WaveSpawner.OnVictory -= ShowVictoryScreen;
    }

    private void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthText == null) return;

        healthText.text = showMaxHealth
            ? $"{currentHealth}/{maxHealth}"
            : currentHealth.ToString();
    }

    private void UpdateCurrencyUI(int amount)
    {
        if (currencyText == null) return;

        currencyText.text = amount.ToString();
    }

    private void UpdateWaveUI(int currentWave, int maxWaves)
    {
        if (waveText == null) return;

        waveText.text = $"Wave {currentWave}/{maxWaves}";
    }

    private void ShowVictoryScreen()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}