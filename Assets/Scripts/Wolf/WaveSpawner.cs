using System;
using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyAI wolfPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform fence;
    [SerializeField] private Transform player;

    [Header("Waves")]
    [SerializeField] private int baseWolvesPerWave = 3;
    [SerializeField] private int extraWolvesPerWave = 1;   // +N вовків з кожною хвилею
    [SerializeField] private float spawnInterval = 1f;     // пауза між вовками в хвилі
    [SerializeField] private float timeBetweenWaves = 5f;  // коротка пауза між хвилями

    [Header("Shop break")]
    [SerializeField] private int wavesBeforeShopBreak = 3; // велика перерва кожні N хвиль
    [SerializeField] private float shopBreakDuration = 30f;
    [SerializeField] private float difficultyStep = 0.25f; // +25% до сили вовків після перерви

    // Події для UI / магазину
    public static event Action<int> OnWaveStarted;
    public static event Action<float> OnShopBreakStarted;
    public static event Action OnShopBreakEnded;

    private int aliveEnemies;
    private int waveNumber;
    private float difficulty = 1f;

    private void OnEnable()  { EnemyAI.OnEnemyDied += HandleEnemyDied; }
    private void OnDisable() { EnemyAI.OnEnemyDied -= HandleEnemyDied; }

    private void Start()
    {
        StartCoroutine(RunWaves());
    }

    private void HandleEnemyDied(EnemyAI enemy)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    private IEnumerator RunWaves()
    {
        while (true)
        {
            waveNumber++;
            OnWaveStarted?.Invoke(waveNumber);

            int count = baseWolvesPerWave + extraWolvesPerWave * (waveNumber - 1);
            for (int i = 0; i < count; i++)
            {
                SpawnWolf();
                yield return new WaitForSeconds(spawnInterval);
            }

            bool isShopBreak = waveNumber % wavesBeforeShopBreak == 0;

            if (isShopBreak)
            {
                // Перед магазином чекаємо, поки гравець переможе всіх вовків
                yield return new WaitUntil(() => aliveEnemies <= 0);

                difficulty += difficultyStep;
                OnShopBreakStarted?.Invoke(shopBreakDuration);
                yield return new WaitForSeconds(shopBreakDuration);
                OnShopBreakEnded?.Invoke();
            }
            else
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }
    }

    private void SpawnWolf()
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || wolfPrefab == null) return;

        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
        EnemyAI wolf = Instantiate(wolfPrefab, point.position, Quaternion.identity);
        wolf.Initialize(fence, player, difficulty);
        aliveEnemies++;
    }

    // Виклич при смерті гравця (екран програшу)
    public void StopWaves()
    {
        StopAllCoroutines();
    }
}
