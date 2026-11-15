using System;
using UnityEngine;

public class EnemyAI : MonoBehaviour, IDamageable
{
    public enum State
    {
        Idle,
        MovingToFence,
        AttackingFence,
        ChasingPlayer,
        AttackingPlayer
    }

    // Подія, на яку підписується спавнер, щоб знати, скільки вовків залишилось
    public static event Action<EnemyAI> OnEnemyDied;

    public State currentState = State.MovingToFence;

    // Targets (спавнер задає їх через Initialize)
    public Transform fenceTarget;
    public Transform player;

    // Movement + Aggro
    public float speed = 3.0f;
    public float aggroRadius = 5.0f;
    public float attackRange = 1.5f;
    public float losePlayerRange = 8f;

    // Stats
    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead;

    // Attack
    public int attackDamage = 5;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown; // перша атака без очікування
    }

    /// <summary>
    /// Викликається спавнером одразу після Instantiate.
    /// difficultyMultiplier: 1 = базові характеристики, 1.25 = на 25% сильніший тощо.
    /// </summary>
    public void Initialize(Transform fence, Transform playerTarget, float difficultyMultiplier)
    {
        fenceTarget = fence;
        player = playerTarget;

        maxHealth = Mathf.RoundToInt(maxHealth * difficultyMultiplier);
        attackDamage = Mathf.RoundToInt(attackDamage * difficultyMultiplier);
        // швидкість росте повільніше, щоб вовки не стали непереможними
        speed *= 1f + (difficultyMultiplier - 1f) * 0.3f;

        currentHealth = maxHealth;
    }

    private void Update()
    {
        CheckTriggers();
        ExecuteState();
    }

    private void CheckTriggers()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.AttackingPlayer:
                if (distanceToPlayer > losePlayerRange)
                    currentState = State.MovingToFence;
                else if (distanceToPlayer > attackRange)
                    currentState = State.ChasingPlayer; // гравець відійшов - наздоганяємо
                break;

            case State.ChasingPlayer:
                if (distanceToPlayer > losePlayerRange)
                    currentState = State.MovingToFence;
                else if (distanceToPlayer <= attackRange)
                    currentState = State.AttackingPlayer;
                break;

            default: // Idle, MovingToFence, AttackingFence
                if (distanceToPlayer <= aggroRadius)
                    currentState = distanceToPlayer <= attackRange ? State.AttackingPlayer : State.ChasingPlayer;
                break;
        }
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case State.Idle:
                break;

            case State.MovingToFence:
                if (fenceTarget == null)
                {
                    currentState = State.Idle;
                    break;
                }

                MoveTowards(fenceTarget.position);
                if (Vector2.Distance(transform.position, fenceTarget.position) <= attackRange)
                    currentState = State.AttackingFence;
                break;

            case State.AttackingFence:
                if (fenceTarget == null)
                {
                    currentState = State.Idle;
                    break;
                }
                AttackTarget(fenceTarget);
                break;

            case State.ChasingPlayer:
                if (player == null)
                {
                    currentState = State.MovingToFence;
                    break;
                }
                MoveTowards(player.position);
                break;

            case State.AttackingPlayer:
                if (player == null)
                {
                    currentState = State.MovingToFence;
                    break;
                }
                AttackTarget(player);
                break;
        }
    }

    private void MoveTowards(Vector2 targetPosition)
    {
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
    }

    private void AttackTarget(Transform target)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        // GetComponentInParent - на випадок, якщо колайдер на дочірньому об'єкті
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(attackDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Якщо вовка вдарили - він переключається на гравця (але не збиваємо поточну атаку)
        if (player != null && currentState != State.AttackingPlayer)
            currentState = State.ChasingPlayer;
    }

    private void Die()
    {
        isDead = true;
        OnEnemyDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, losePlayerRange);
    }
}