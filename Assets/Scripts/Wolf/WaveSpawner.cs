using System;
using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public enum SpawnOrder
    {
        Random,
        Sequential
    }

    [Header("References")]
    [SerializeField] private EnemyAI wolfPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform fence;
    [SerializeField] private Transform player;
    [SerializeField] private SheepPen sheepPen;

    [Header("Spawn Settings")]
    [SerializeField] private SpawnOrder spawnOrder = SpawnOrder.Random;
    [Tooltip("")]
    [SerializeField] private float spawnJitter = 0.25f;

    [Header("Waves")]
    [SerializeField] private int maxWaves = 10;
    [SerializeField] private int baseWolvesPerWave = 3;
    [SerializeField] private int extraWolvesPerWave = 1;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float timeBetweenWaves = 5f;

    [Header("Rewards")]
    [SerializeField] private int baseWaveReward = 10;
    [SerializeField] private int extraRewardPerWave = 0;
    [Tooltip("Монет за кожну вівцю після завершення хвилі")]
    [SerializeField, Min(0)] private int sheepRewardPerWave = 20;

    [Header("Shop break")]
    [SerializeField] private int wavesBeforeShopBreak = 3;
    [SerializeField] private float shopBreakDuration = 30f;
    [SerializeField] private float difficultyStep = 0.25f;

    public static event Action<int, int> OnWaveStarted;
    public static event Action<float> OnShopBreakStarted;
    public static event Action OnShopBreakEnded;
    public static event Action OnVictory;

    private int aliveEnemies;
    private int waveNumber;
    private int sequentialIndex;
    private float difficulty = 1f;
    private bool stopped;
    private EnemyAI templateEnemy;

    public int CurrentWave => waveNumber;
    public int MaxWaves => maxWaves;

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<Player_Script>()?.transform;

        if (fence == null)
            fence = FindAnyObjectByType<Fence>()?.transform;

        if (sheepPen == null)
            sheepPen = FindAnyObjectByType<SheepPen>();

        templateEnemy = GetComponent<EnemyAI>();
        if (templateEnemy != null)
            wolfPrefab = templateEnemy;
        else if (wolfPrefab == null)
            wolfPrefab = FindAnyObjectByType<EnemyAI>();

        if (wolfPrefab != null)
            PrepareTemplate(wolfPrefab);

        if (spawnPoints == null || spawnPoints.Length == 0)
            Debug.LogWarning("WaveSpawner: Spawn Points!", this);

        if (sheepPen == null && sheepRewardPerWave > 0)
            Debug.LogWarning("WaveSpawner: SheepPen is missing; sheep bonus will not be awarded.", this);
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
        while (!stopped && waveNumber < maxWaves)
        {
            waveNumber++;
            OnWaveStarted?.Invoke(waveNumber, maxWaves);

            int count = baseWolvesPerWave + extraWolvesPerWave * (waveNumber - 1);
            for (int i = 0; i < count; i++)
            {
                SpawnWolf();
                yield return new WaitForSeconds(spawnInterval);
            }

            yield return new WaitUntil(() => aliveEnemies <= 0);

            if (stopped) yield break;

            int sheepBonus = sheepPen != null
                ? sheepPen.SheepCount * Mathf.Max(0, sheepRewardPerWave)
                : 0;
            int reward = baseWaveReward + extraRewardPerWave * (waveNumber - 1) + sheepBonus;
            CurrencyManager.Instance?.AddCoins(reward);

            if (waveNumber >= maxWaves)
            {
                stopped = true;
                OnVictory?.Invoke();
                yield break;
            }

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

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        if (spawnOrder == SpawnOrder.Sequential)
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform candidate = spawnPoints[sequentialIndex % spawnPoints.Length];
                sequentialIndex++;
                if (candidate != null) return candidate;
            }
            return null;
        }

        // ���������� ���� ����� ���������� �����
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Transform candidate = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            if (candidate != null) return candidate;
        }

        return null;
    }

    private void SpawnWolf()
    {
        if (wolfPrefab == null) return;

        Transform point = GetNextSpawnPoint();
        if (point == null) return;

        Vector2 offset = spawnJitter > 0f ? UnityEngine.Random.insideUnitCircle * spawnJitter : Vector2.zero;
        Vector3 spawnPosition = point.position + (Vector3)offset;

        EnemyAI wolf = Instantiate(wolfPrefab, spawnPosition, Quaternion.identity);

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

            CircleCollider2D movementCollider = wolf.GetComponent<CircleCollider2D>();
            foreach (Collider2D col in wolf.GetComponentsInChildren<Collider2D>(true))
                col.enabled = col == movementCollider;

            if (movementCollider != null)
                movementCollider.isTrigger = false;

            Rigidbody2D rb = wolf.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
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

        foreach (Collider2D collider in enemy.GetComponentsInChildren<Collider2D>(true))
            collider.enabled = false;

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;
    }

    public void StopWaves()
    {
        stopped = true;
        StopAllCoroutines();
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.red;
        foreach (Transform point in spawnPoints)
        {
            if (point != null)
                Gizmos.DrawWireSphere(point.position, 0.45f);
        }
    }
}