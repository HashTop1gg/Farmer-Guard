using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Player_Script))]
public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private int damage = 25;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRadius = 0.8f;
    [Tooltip("Наскільки далеко перед гравцем центр удару")]
    [SerializeField] private float attackOffset = 0.8f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private Player_Script player;
    private float lastAttackTime = float.NegativeInfinity;
    private readonly HashSet<EnemyAI> hitThisSwing = new HashSet<EnemyAI>();

    private void Awake()
    {
        player = GetComponent<Player_Script>();
    }

    private void Update()
    {
        if (player.IsDead || Game_Input.Instance == null) return;

        if (Game_Input.Instance.WasAttackPressedThisFrame() &&
            Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            Attack();
        }
    }

    private void Attack()
    {
        Vector2 center = GetAttackCenter();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackRadius, enemyLayers);

        hitThisSwing.Clear();

        foreach (Collider2D hit in hits)
        {
            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || !hitThisSwing.Add(enemy)) continue;

            enemy.TakeDamage(damage);
        }
    }
    private Vector2 GetAttackCenter()
    {
        return (Vector2)transform.position + player.FacingDirection * attackOffset;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 dir = Application.isPlaying && player != null ? player.FacingDirection : Vector2.right;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere((Vector2)transform.position + dir * attackOffset, attackRadius);
    }
}