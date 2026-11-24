using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SheepPen : MonoBehaviour
{
    public event Action<int, int> OnSheepCountChanged;

    [SerializeField] private SheepIdle sheepPrefab;
    [SerializeField] private int maxSheep = 10;
    [Tooltip("Мінімальна відстань між вівцями")]
    [SerializeField] private float minSpacing = 1f;
    [Tooltip("Відступ від країв зони, щоб вівці не стояли в паркані")]
    [SerializeField] private float edgePadding = 0.5f;
    [SerializeField] private int placementAttempts = 30;

    private BoxCollider2D zone;
    private readonly List<SheepIdle> sheep = new List<SheepIdle>();

    public int SheepCount
    {
        get { sheep.RemoveAll(s => s == null); return sheep.Count; }
    }
    public int MaxSheep => maxSheep;
    public bool IsFull => SheepCount >= maxSheep;

    private void Awake()
    {
        zone = GetComponent<BoxCollider2D>();
        zone.isTrigger = true;
    }

    public bool TrySpawnSheep()
    {
        if (sheepPrefab == null || IsFull) return false;
        if (!TryFindFreePoint(out Vector2 pos)) return false;

        SheepIdle s = Instantiate(sheepPrefab, pos, Quaternion.identity, transform);
        sheep.Add(s);
        OnSheepCountChanged?.Invoke(sheep.Count, maxSheep);
        return true;
    }

    private bool TryFindFreePoint(out Vector2 result)
    {
        Bounds b = zone.bounds;
        float minX = b.min.x + edgePadding, maxX = b.max.x - edgePadding;
        float minY = b.min.y + edgePadding, maxY = b.max.y - edgePadding;

        for (int i = 0; i < placementAttempts; i++)
        {
            Vector2 candidate = new Vector2(UnityEngine.Random.Range(minX, maxX),
                                            UnityEngine.Random.Range(minY, maxY));
            if (IsFarFromOthers(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = default;
        return false;
    }

    private bool IsFarFromOthers(Vector2 point)
    {
        foreach (SheepIdle s in sheep)
            if (s != null && Vector2.Distance(s.transform.position, point) < minSpacing)
                return false;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box == null) return;

        Gizmos.color = Color.green;
        Bounds b = box.bounds;
        Gizmos.DrawWireCube(b.center, new Vector3(b.size.x - edgePadding * 2f, b.size.y - edgePadding * 2f, 0f));
    }
}
