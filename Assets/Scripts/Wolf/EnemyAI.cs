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

    public static event Action<EnemyAI> OnEnemyDied;

    public State currentState = State.MovingToFence;

    public Transform fenceTarget;
    public Transform player;

    public float speed = 3.0f;
    public float aggroRadius = 5.0f;
    public float attackRange = 1.5f;
    public float losePlayerRange = 8f;

    public int maxHealth = 100;
    private int currentHealth;
    private bool isDead;

    public int attackDamage = 5;
    public float attackCooldown = 2f;
    private float lastAttackTime;

    [Header("Animation")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;
    public bool spriteFacesRight = true;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private bool isMoving;

    private void Awake()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Transform fence, Transform playerTarget, float difficultyMultiplier)
    {
        fenceTarget = fence;
        player = playerTarget;

        maxHealth = Mathf.RoundToInt(maxHealth * difficultyMultiplier);
        attackDamage = Mathf.RoundToInt(attackDamage * difficultyMultiplier);
        speed *= 1f + (difficultyMultiplier - 1f) * 0.3f;

        currentHealth = maxHealth;
    }

    private void Update()
    {
        isMoving = false;
        CheckTriggers();
        ExecuteState();

        if (animator != null)
            animator.SetBool(IsMovingHash, isMoving);
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
                    currentState = State.ChasingPlayer;
                break;

            case State.ChasingPlayer:
                if (distanceToPlayer > losePlayerRange)
                    currentState = State.MovingToFence;
                else if (distanceToPlayer <= attackRange)
                    currentState = State.AttackingPlayer;
                break;

            default:
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
        Vector2 current = transform.position;
        if (Vector2.Distance(current, targetPosition) > 0.01f)
        {
            isMoving = true;
            FaceTarget(targetPosition);
        }

        transform.position = Vector2.MoveTowards(current, targetPosition, speed * Time.deltaTime);
    }

    private void AttackTarget(Transform target)
    {
        if (Time.time < lastAttackTime + attackCooldown) return;
        lastAttackTime = Time.time;

        FaceTarget(target.position);
        if (animator != null)
            animator.SetTrigger(AttackHash);

        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(attackDamage);
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        if (spriteRenderer == null) return;

        float dx = targetPosition.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.01f) return;

        bool targetIsRight = dx > 0f;
        spriteRenderer.flipX = spriteFacesRight ? !targetIsRight : targetIsRight;
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
