using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
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
    private CircleCollider2D bodyCollider;
    private Vector2 pendingMoveTarget;
    private bool hasPendingMove;
    private readonly RaycastHit2D[] rayBuffer = new RaycastHit2D[8];
    private readonly RaycastHit2D[] obstacleHitBuffer = new RaycastHit2D[16];
    private readonly Collider2D[] obstacleOverlapBuffer = new Collider2D[16];

    private void Awake()
    {
        currentHealth = maxHealth;
        lastAttackTime = -attackCooldown;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<CircleCollider2D>();
        if (bodyCollider != null)
            bodyCollider.isTrigger = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.useFullKinematicContacts = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void OnEnable()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<CircleCollider2D>();

        if (bodyCollider != null)
        {
            bodyCollider.enabled = true;
            bodyCollider.isTrigger = false;
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
        Player_Script.RefreshGeneratedMapObstacleColliders();
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
        hasPendingMove = false;

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

                FenceSection playerBlocker = GetBlockingSection();
                if (playerBlocker != null)
                    AttackDamageable(playerBlocker, playerBlocker.GetClosestPoint(transform.position));
                else
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
            pendingMoveTarget = GetObstacleWaypoint(targetPosition);
            hasPendingMove = true;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
        }
    }

    private Vector2 GetObstacleWaypoint(Vector2 targetPosition)
    {
        Vector2 directionToTarget = targetPosition - rb.position;
        float distanceToTarget = directionToTarget.magnitude;
        if (distanceToTarget <= 0.01f)
            return targetPosition;

        float radius = GetAvoidanceRadius();
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int hitCount = Physics2D.CircleCast(
            rb.position,
            radius,
            directionToTarget / distanceToTarget,
            filter,
            obstacleHitBuffer,
            distanceToTarget);

        Collider2D blockingCollider = null;
        float nearestHit = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = obstacleHitBuffer[i].collider;
            if (hitCollider == null || hitCollider == bodyCollider)
                continue;

            if (Player_Script.IsGeneratedMapObstacleCollider(hitCollider)
                && obstacleHitBuffer[i].distance < nearestHit)
            {
                blockingCollider = hitCollider;
                nearestHit = obstacleHitBuffer[i].distance;
            }
        }

        if (blockingCollider == null)
            return targetPosition;

        Bounds bounds = blockingCollider.bounds;
        float clearance = radius + 0.1f;
        float bestPathLength = float.MaxValue;
        Vector2 bestWaypoint = targetPosition;
        bool foundWaypoint = false;

        for (int xSide = -1; xSide <= 1; xSide += 2)
        {
            for (int ySide = -1; ySide <= 1; ySide += 2)
            {
                Vector2 candidate = new Vector2(
                    bounds.center.x + xSide * (bounds.extents.x + clearance),
                    bounds.center.y + ySide * (bounds.extents.y + clearance));

                if (!IsClearPath(rb.position, candidate, radius, blockingCollider)
                    || IsOverlappingObstacle(candidate, radius))
                {
                    continue;
                }

                float pathLength = Vector2.Distance(rb.position, candidate)
                    + Vector2.Distance(candidate, targetPosition);
                if (pathLength < bestPathLength)
                {
                    bestPathLength = pathLength;
                    bestWaypoint = candidate;
                    foundWaypoint = true;
                }
            }
        }

        return foundWaypoint ? bestWaypoint : targetPosition;
    }

    private float GetAvoidanceRadius()
    {
        if (bodyCollider == null)
            return 0.25f;

        Vector3 scale = bodyCollider.transform.lossyScale;
        return bodyCollider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    private bool IsClearPath(Vector2 start, Vector2 end, float radius, Collider2D ignoredObstacle)
    {
        Vector2 direction = end - start;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int hitCount = Physics2D.CircleCast(
            start,
            radius,
            direction / distance,
            filter,
            obstacleHitBuffer,
            distance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = obstacleHitBuffer[i].collider;
            if (hitCollider == null || hitCollider == bodyCollider || hitCollider == ignoredObstacle)
                continue;

            if (Player_Script.IsGeneratedMapObstacleCollider(hitCollider))
                return false;
        }

        return true;
    }

    private bool IsOverlappingObstacle(Vector2 position, float radius)
    {
        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.OverlapCircle(position, radius, filter, obstacleOverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D overlap = obstacleOverlapBuffer[i];
            if (overlap == null || overlap == bodyCollider)
                continue;

            if (Player_Script.IsGeneratedMapObstacleCollider(overlap))
                return true;
        }

        return false;
    }

    private FenceSection GetBlockingSection()
    {
        Vector2 from = transform.position;
        Vector2 toPlayer = (Vector2)player.position - from;
        float distance = toPlayer.magnitude;
        if (distance <= 0.001f) return null;

        int count = Physics2D.Raycast(from, toPlayer.normalized, ContactFilter2D.noFilter, rayBuffer, distance);

        FenceSection blockingSection = null;
        float nearestFenceHit = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (rayBuffer[i].collider == null)
                continue;

            FenceSection section = rayBuffer[i].collider.GetComponentInParent<FenceSection>();
            if (section != null && !section.IsDestroyed && rayBuffer[i].distance < nearestFenceHit)
            {
                blockingSection = section;
                nearestFenceHit = rayBuffer[i].distance;
            }
        }

        return blockingSection;
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