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
    private bool stopped;
    private EnemyAI templateEnemy;

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<Player_Script>()?.transform;

        if (fence == null)
            fence = FindAnyObjectByType<Fence>()?.transform;

        templateEnemy = GetComponent<EnemyAI>();
        if (templateEnemy != null)
            wolfPrefab = templateEnemy;
        else if (wolfPrefab == null)
            wolfPrefab = FindAnyObjectByType<EnemyAI>();

        if (wolfPrefab != null)
            PrepareTemplate(wolfPrefab);

        if (spawnPoints == null || spawnPoints.Length == 0)
            CreateAutomaticSpawnPoints();
    }

    private void OnEnable() { EnemyAI.OnEnemyDied += HandleEnemyDied; }
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
        while (!stopped)
        {
            waveNumber++;
            OnWaveStarted?.Invoke(waveNumber);

            int count = baseWolvesPerWave + extraWolvesPerWave * (waveNumber - 1);
            for (int i = 0; i < count; i++)
            {
                SpawnWolf();
                yield return new WaitForSeconds(spawnInterval);
            }

            yield return new WaitUntil(() => aliveEnemies <= 0);

            bool isShopBreak = waveNumber % wavesBeforeShopBreak == 0;

            if (isShopBreak)
            {
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

        if (wolf != templateEnemy)
        {
            WaveSpawner spawner = wolf.GetComponent<WaveSpawner>();
            if (spawner != null)
                spawner.enabled = false;

            EnemyAI enemy = wolf.GetComponent<EnemyAI>();
            if (enemy != null)
                enemy.enabled = true;

            SpriteRenderer sprite = wolf.GetComponent<SpriteRenderer>();
            if (sprite != null)
                sprite.enabled = true;

            Animator animator = wolf.GetComponent<Animator>();
            if (animator != null)
                animator.enabled = true;

            Rigidbody2D rb = wolf.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }

        wolf.Initialize(fence, player, difficulty);
        aliveEnemies++;
    }

    private void PrepareTemplate(EnemyAI enemy)
    {
        if (enemy == null) return;

        EnemyAI enemyComponent = enemy.GetComponent<EnemyAI>();
        if (enemyComponent != null)
            enemyComponent.enabled = false;

        SpriteRenderer sprite = enemy.GetComponent<SpriteRenderer>();
        if (sprite != null)
            sprite.enabled = false;

        Animator animator = enemy.GetComponent<Animator>();
        if (animator != null)
            animator.enabled = false;

        Collider2D collider = enemy.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }

    private void CreateAutomaticSpawnPoints()
    {
        Vector2 center = transform.position;
        float radius = 6f;
        Transform parent = transform;
        spawnPoints = new Transform[4];

        Vector2[] offsets =
        {
            new Vector2(-radius, 0f),
            new Vector2(radius, 0f),
            new Vector2(0f, radius),
            new Vector2(0f, -radius)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject point = new GameObject($"WolfSpawnPoint_{i + 1}");
            point.transform.SetParent(parent);
            point.transform.position = center + offsets[i];
            spawnPoints[i] = point.transform;
        }
    }

    public void StopWaves()
    {
        stopped = true;
        StopAllCoroutines();
    }
}
