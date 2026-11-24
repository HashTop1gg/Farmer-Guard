using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player_Script))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRadius = 1.25f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private Player_Script player;
    private Animator animator;
    private float lastAttackTime = float.NegativeInfinity;
    private readonly HashSet<EnemyAI> hitThisSwing = new HashSet<EnemyAI>();

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int AttackDirectionHash = Animator.StringToHash("AttackDirection");

    private void Awake()
    {
        player = GetComponent<Player_Script>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (player.IsDead) return;

        bool attackPressed = Game_Input.Instance != null &&
            Game_Input.Instance.WasAttackPressedThisFrame();
        attackPressed |= Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        attackPressed |= Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        if (attackPressed &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }

    private void Attack()
    {
        PlayAttackAnimation();

        hitThisSwing.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayers);
        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            DamageEnemy(enemy);
        }

        foreach (EnemyAI enemy in FindObjectsByType<EnemyAI>())
        {
            if (enemy == null || !enemy.isActiveAndEnabled || hitThisSwing.Contains(enemy))
                continue;

            if (Vector2.Distance(transform.position, enemy.transform.position) <= attackRadius)
                DamageEnemy(enemy);
        }
    }

    private void DamageEnemy(EnemyAI enemy)
    {
        if (enemy == null || !hitThisSwing.Add(enemy)) return;
        enemy.TakeDamage(damage);
    }

    private void PlayAttackAnimation()
    {
        if (animator == null) return;

        Vector2 direction = player.FacingDirection;
        int attackDirection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? 0
            : direction.y < 0f ? 1 : 2;

        animator.SetInteger(AttackDirectionHash, attackDirection);
        animator.ResetTrigger(AttackTriggerHash);
        animator.SetTrigger(AttackTriggerHash);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}