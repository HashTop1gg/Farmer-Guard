using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FenceSection : MonoBehaviour, IDamageable
{
    public static readonly List<FenceSection> All = new List<FenceSection>();

    public event Action<FenceSection, int, int> OnHealthChanged;
    public event Action<FenceSection> OnSectionDestroyed;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 50;

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Спрайти від цілого (індекс 0) до найбільш зламаного. Можна лишити порожнім.")]
    [SerializeField] private Sprite[] damageStages;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private float hitFlashDuration = 0.08f;

    private int currentHealth;
    private bool isDestroyed;
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    public Collider2D Collider { get; private set; }
    public bool IsDestroyed => isDestroyed;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        Collider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        currentHealth = maxHealth;
        UpdateVisuals();
    }

    private void OnEnable()
    {
        if (!All.Contains(this))
            All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public void TakeDamage(int damage)
    {
        if (isDestroyed || damage <= 0) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

        UpdateVisuals();
        PlayHitFlash();

        if (currentHealth <= 0)
            DestroySection();
    }

    public void Repair(int amount)
    {
        if (isDestroyed || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);
        UpdateVisuals();
    }
    public Vector2 GetClosestPoint(Vector2 from)
    {
        return Collider != null ? Collider.ClosestPoint(from) : (Vector2)transform.position;
    }
    public static FenceSection FindNearest(Vector2 from)
    {
        FenceSection best = null;
        float bestSqr = float.MaxValue;

        for (int i = 0; i < All.Count; i++)
        {
            FenceSection s = All[i];
            if (s == null || s.isDestroyed) continue;

            float sqr = (s.GetClosestPoint(from) - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = s;
            }
        }

        return best;
    }
    private void UpdateVisuals()
    {
        if (spriteRenderer == null || damageStages == null || damageStages.Length == 0) return;

        float healthPercent = (float)currentHealth / maxHealth;
        int stage = Mathf.Clamp(
            Mathf.FloorToInt((1f - healthPercent) * damageStages.Length),
            0,
            damageStages.Length - 1);

        spriteRenderer.sprite = damageStages[stage];
    }
    private void PlayHitFlash()
    {
        if (spriteRenderer == null || !gameObject.activeInHierarchy) return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(HitFlash());
    }
    private IEnumerator HitFlash()
    {
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = baseColor;
        flashRoutine = null;
    }
    private void DestroySection()
    {
        isDestroyed = true;
        All.Remove(this);
        OnSectionDestroyed?.Invoke(this);
        Destroy(gameObject);
    }
}