using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
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

    private FenceSection targetFence;
    private IDamageable playerDamageable;

    private Rigidbody2D rb;
    private Vector2 pendingMoveTarget;
    private bool hasPendingMove;
    private readonly RaycastHit2D[] rayBuffer = new RaycastHit2D[8];

    private void Awake()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null || !hasPendingMove) return;

        rb.MovePosition(Vector2.MoveTowards(rb.position, pendingMoveTarget, speed * Time.fixedDeltaTime));
        hasPendingMove = false;
    }

    public void Initialize(Transform fence, Transform playerTarget, float difficultyMultiplier)
    {
        fenceTarget = fence;
        targetFence = fence != null ? fence.GetComponentInParent<FenceSection>() : null;

        player = playerTarget;
        playerDamageable = player != null ? player.GetComponentInParent<IDamageable>() : null;

        maxHealth = Mathf.RoundToInt(maxHealth * difficultyMultiplier);
        attackDamage = Mathf.RoundToInt(attackDamage * difficultyMultiplier);
        speed *= 1f + (difficultyMultiplier - 1f) * 0.3f;

        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (isDead) return;

        isMoving = false;

        ResolveFence();
        CheckTriggers();
        ExecuteState();

        if (animator != null)
            animator.SetBool(IsMovingHash, isMoving);
    }

    private void ResolveFence()
    {
        if (targetFence != null && !targetFence.IsDestroyed) return;

        targetFence = FenceSection.FindNearest(transform.position);
        fenceTarget = targetFence != null ? targetFence.transform : null;
    }

    private void CheckTriggers()
    {
        if (player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= aggroRadius)
        {
            currentState = distanceToPlayer <= attackRange
                ? State.AttackingPlayer
                : State.ChasingPlayer;
            return;
        }

        if (distanceToPlayer > losePlayerRange)
            currentState = targetFence != null
                ? State.MovingToFence
                : State.Idle;
        else if (currentState == State.AttackingPlayer || currentState == State.ChasingPlayer)
            currentState = targetFence != null
                ? State.MovingToFence
                : State.Idle;
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case State.Idle:
                if (targetFence != null)
                    currentState = State.MovingToFence;
                break;

            case State.MovingToFence:
                if (targetFence == null)
                {
                    currentState = State.Idle;
                    break;
                }

                Vector2 fencePoint = targetFence.GetClosestPoint(transform.position);
                MoveTowards(fencePoint);

                if (Vector2.Distance(transform.position, fencePoint) <= attackRange)
                    currentState = State.AttackingFence;
                break;

            case State.AttackingFence:
                if (targetFence == null)
                {
                    currentState = State.MovingToFence;
                    break;
                }

                Vector2 attackPoint = targetFence.GetClosestPoint(transform.position);
                if (Vector2.Distance(transform.position, attackPoint) > attackRange * 1.2f)
                {
                    currentState = State.MovingToFence;
                    break;
                }

                AttackDamageable(targetFence, attackPoint);
                break;

            case State.ChasingPlayer:
                if (player == null)
                {
                    currentState = State.MovingToFence;
                    break;
                }

                FenceSection blocker = GetBlockingSection();
                if (blocker != null)
                    AttackDamageable(blocker, blocker.GetClosestPoint(transform.position));
                else
                    MoveTowards(player.position);
                break;

            case State.AttackingPlayer:
                if (player == null)
                {
                    currentState = State.MovingToFence;
                    break;
                }

                AttackDamageable(playerDamageable, player.position);
                break;
        }
    }

    private void MoveTowards(Vector2 targetPosition)
    {
        if (Vector2.Distance(transform.position, targetPosition) > 0.01f)
        {
            isMoving = true;
            FaceTarget(targetPosition);
        }

        if (rb != null)
        {
            pendingMoveTarget = targetPosition;
            hasPendingMove = true;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }

    private FenceSection GetBlockingSection()
    {
        Vector2 from = transform.position;
        Vector2 toPlayer = (Vector2)player.position - from;
        float distance = Mathf.Min(toPlayer.magnitude, attackRange);
        if (distance <= 0.001f) return null;

        int count = Physics2D.Raycast(from, toPlayer.normalized, ContactFilter2D.noFilter, rayBuffer, distance);

        for (int i = 0; i < count; i++)
        {
            FenceSection section = rayBuffer[i].collider.GetComponentInParent<FenceSection>();
            if (section != null && !section.IsDestroyed)
                return section;
        }

        return null;
    }

    private void AttackDamageable(IDamageable target, Vector2 targetPosition)
    {
        if (target == null) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        FaceTarget(targetPosition);
        if (animator != null)
            animator.SetTrigger(AttackHash);

        target.TakeDamage(attackDamage);
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