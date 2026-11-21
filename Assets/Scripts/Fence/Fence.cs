using System;
using System.Collections.Generic;
using UnityEngine;


public class Fence : MonoBehaviour
{
    public event Action<Fence, FenceSection> OnSectionDestroyed;
    public event Action<Fence> OnFenceDestroyed;

    [Header("Auto build (необов'язково)")]
    [SerializeField] private FenceSection sectionPrefab;
    [SerializeField] private int sectionCount = 5;
    [Tooltip("Відстань між центрами секцій у локальних координатах")]
    [SerializeField] private Vector2 sectionStep = new Vector2(2f, 0f);

    private readonly List<FenceSection> sections = new List<FenceSection>();
    private int totalMaxHealth;

    public IReadOnlyList<FenceSection> Sections => sections;
    public int AliveCount { get; private set; }
    public bool IsDestroyed => AliveCount <= 0;

    private void Awake()
    {
        CollectSections();
    }

    private void OnDestroy()
    {
        foreach (FenceSection s in sections)
            if (s != null)
                s.OnSectionDestroyed -= HandleSectionDestroyed;
    }
    public void CollectSections()
    {
        foreach (FenceSection s in sections)
            if (s != null)
                s.OnSectionDestroyed -= HandleSectionDestroyed;

        sections.Clear();
        GetComponentsInChildren(true, sections);

        totalMaxHealth = 0;
        foreach (FenceSection s in sections)
        {
            s.OnSectionDestroyed += HandleSectionDestroyed;
            totalMaxHealth += s.MaxHealth;
        }

        AliveCount = sections.Count;
    }

    private void HandleSectionDestroyed(FenceSection section)
    {
        section.OnSectionDestroyed -= HandleSectionDestroyed;
        AliveCount--;
        OnSectionDestroyed?.Invoke(this, section);

        if (AliveCount <= 0)
            OnFenceDestroyed?.Invoke(this);
    }

    public float GetHealthPercent()
    {
        if (totalMaxHealth <= 0) return 0f;

        int current = 0;
        foreach (FenceSection s in sections)
            if (s != null && !s.IsDestroyed)
                current += s.CurrentHealth;

        return (float)current / totalMaxHealth;
    }
    public void RepairAll(int amountPerSection)
    {
        foreach (FenceSection s in sections)
            if (s != null)
                s.Repair(amountPerSection);
    }
    [ContextMenu("Build Sections")]
    private void BuildSections()
    {
        if (sectionPrefab == null)
        {
            Debug.LogWarning("Fence: призначте Section Prefab.", this);
            return;
        }
        ClearChildren();
        for (int i = 0; i < sectionCount; i++)
        {
            FenceSection s = Instantiate(sectionPrefab, transform);
            s.name = $"Section_{i}";
            s.transform.localPosition = sectionStep * i;
        }
        CollectSections();
    }
    [ContextMenu("Clear Sections")]
    private void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
        sections.Clear();
    }
}