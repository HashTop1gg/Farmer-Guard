using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
public class FarmMapBuilder : MonoBehaviour
{
    public static readonly Vector2[] ForestWolfSpawnPositions =
    {
        new Vector2(0f, 3.8f),
        new Vector2(7.55f, 0f),
        new Vector2(0f, -3.8f),
        new Vector2(-7.55f, 0f)
    };

    [SerializeField] private Sprite grassTile;
    [SerializeField] private Sprite roundTree;
    [SerializeField] private Sprite pineTree;
    [SerializeField] private Sprite[] bushes;
    [SerializeField] private Sprite[] flowers;
    [SerializeField] private float tileSize = 1.3f;

    private const string GeneratedRootName = "GeneratedFarmMap";
    private const float HalfWidth = 8.25f;
    private const float HalfHeight = 4.5f;
    private const float WallThickness = 0.5f;

    public static bool IsGeneratedMapObstacle(GameObject target)
    {
        if (target == null)
            return false;

        string objectName = target.name;
        return objectName.StartsWith("Wildflower_", StringComparison.Ordinal)
            || objectName.StartsWith("ForestBush_", StringComparison.Ordinal)
            || objectName.StartsWith("NorthTree_", StringComparison.Ordinal)
            || objectName.StartsWith("SouthTree_", StringComparison.Ordinal)
            || objectName.StartsWith("WestTree_", StringComparison.Ordinal)
            || objectName.StartsWith("EastTree_", StringComparison.Ordinal);
    }

    public static void EnsureGeneratedMapObstacleCollider(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null || !IsGeneratedMapObstacle(renderer.gameObject))
            return;

        BoxCollider2D collider = renderer.GetComponent<BoxCollider2D>();
        if (collider == null)
            collider = renderer.gameObject.AddComponent<BoxCollider2D>();

        Bounds bounds = renderer.sprite.bounds;
        if (renderer.gameObject.name.IndexOf("Tree_", StringComparison.Ordinal) >= 0)
        {
            float width = Mathf.Min(0.5f, bounds.size.x * 0.45f);
            float height = Mathf.Min(0.34f, bounds.size.y * 0.25f);
            collider.offset = new Vector2(bounds.center.x, bounds.min.y + height * 0.5f);
            collider.size = new Vector2(width, height);
        }
        else
        {
            collider.offset = bounds.center;
            collider.size = bounds.size;
        }

        collider.enabled = true;
        collider.isTrigger = false;
    }

    private void OnEnable()
    {
        EnsureMapExists();
    }

    private void Start()
    {
        EnsureMapExists();
    }

    [ContextMenu("Rebuild Farm Map")]
    private void RebuildMap()
    {
        Transform existing = transform.Find(GeneratedRootName);
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        BuildMap();
    }

    private void EnsureMapExists()
    {
        if (transform.Find(GeneratedRootName) == null)
            BuildMap();
    }

    private void BuildMap()
    {
        if (grassTile == null)
        {
            Debug.LogError("FarmMapBuilder requires a grass tile sprite.", this);
            return;
        }

        GameObject generated = new GameObject(GeneratedRootName);
        generated.transform.SetParent(transform, false);

        BuildGround(generated.transform);
        BuildForest(generated.transform);
        BuildFlowers(generated.transform);
        BuildBoundaries(generated.transform);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void BuildGround(Transform parent)
    {
        const int columns = 14;
        const int rows = 8;
        float startX = -columns * tileSize * 0.5f;
        float startY = -rows * tileSize * 0.5f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector2 tilePosition = new Vector2(startX + x * tileSize, startY + y * tileSize);
                GameObject tile = new GameObject($"Grass_{x}_{y}");
                tile.transform.SetParent(parent, false);
                tile.transform.position = tilePosition;

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = grassTile;
                renderer.sortingOrder = -20;
                if (IsPath(tilePosition + Vector2.one * (tileSize * 0.5f)))
                    renderer.color = new Color(0.82f, 0.72f, 0.55f);
            }
        }
    }

    private void BuildForest(Transform parent)
    {
        float[] topTreeX = { -7.9f, -6.5f, -5.1f, -3.7f, -2.3f, 2.3f, 3.7f, 5.1f, 6.5f };
        for (int i = 0; i < topTreeX.Length; i++)
        {
            Sprite sprite = i % 2 == 0 ? pineTree : roundTree;
            PlaceTree(parent, new Vector2(topTreeX[i], 3.15f), sprite, $"NorthTree_{i}");
            PlaceTree(parent, new Vector2(topTreeX[i], -4.05f), i % 2 == 0 ? roundTree : pineTree, $"SouthTree_{i}");
        }

        float[] sideTreeY = { -2.7f, -1.35f, 1.35f, 2.7f };
        for (int i = 0; i < sideTreeY.Length; i++)
        {
            PlaceTree(parent, new Vector2(-7.9f, sideTreeY[i]), i % 2 == 0 ? roundTree : pineTree, $"WestTree_{i}");
            PlaceTree(parent, new Vector2(6.6f, sideTreeY[i]), i % 2 == 0 ? pineTree : roundTree, $"EastTree_{i}");
        }

        PlaceBush(parent, new Vector2(-6.2f, -3.6f), 0);
        PlaceBush(parent, new Vector2(-4.7f, 3.25f), 1);
        PlaceBush(parent, new Vector2(4.5f, -3.65f), 1);
        PlaceBush(parent, new Vector2(6.7f, 3.2f), 0);
    }

    private void BuildFlowers(Transform parent)
    {
        Vector2[] flowerPatches =
        {
            new Vector2(-6.3f, -3.1f), new Vector2(-5.6f, -2.8f), new Vector2(-4.3f, -3.4f),
            new Vector2(-3.3f, -2.7f), new Vector2(-6.5f, -1.9f), new Vector2(-4.9f, -1.5f),
            new Vector2(-6.4f, 1.8f), new Vector2(-5.2f, 2.3f), new Vector2(-4f, 2.9f),
            new Vector2(-3.2f, 2.1f), new Vector2(6.1f, -3.1f), new Vector2(5.5f, -2.6f),
            new Vector2(6.3f, -1.7f), new Vector2(6.2f, 1.7f), new Vector2(5.4f, 2.4f),
            new Vector2(4.1f, 2.9f), new Vector2(3.5f, -3.5f), new Vector2(3.3f, 1.8f),
            new Vector2(-2.9f, -3.4f), new Vector2(-2.6f, 3.1f)
        };

        if (flowers == null || flowers.Length == 0)
            return;

        for (int i = 0; i < flowerPatches.Length; i++)
        {
            Sprite sprite = flowers[i % flowers.Length];
            GameObject flower = new GameObject($"Wildflower_{i + 1}");
            flower.transform.SetParent(parent, false);
            flower.transform.position = flowerPatches[i];

            SpriteRenderer renderer = flower.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = Mathf.RoundToInt(-flowerPatches[i].y * 10f) + 2;
        }
    }

    private void BuildBoundaries(Transform parent)
    {
        CreateBoundary(parent, "MapBoundary_West", new Vector2(-HalfWidth, 0f), new Vector2(WallThickness, HalfHeight * 2f));
        CreateBoundary(parent, "MapBoundary_East", new Vector2(HalfWidth, 0f), new Vector2(WallThickness, HalfHeight * 2f));
        CreateBoundary(parent, "MapBoundary_North", new Vector2(0f, HalfHeight), new Vector2(HalfWidth * 2f, WallThickness));
        CreateBoundary(parent, "MapBoundary_South", new Vector2(0f, -HalfHeight), new Vector2(HalfWidth * 2f, WallThickness));
    }

    private void PlaceTree(Transform parent, Vector2 position, Sprite sprite, string objectName)
    {
        if (sprite == null)
            return;

        GameObject tree = new GameObject(objectName);
        tree.transform.SetParent(parent, false);
        tree.transform.position = position;

        SpriteRenderer renderer = tree.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f);
    }

    private void PlaceBush(Transform parent, Vector2 position, int spriteIndex)
    {
        if (bushes == null || spriteIndex >= bushes.Length || bushes[spriteIndex] == null)
            return;

        GameObject bush = new GameObject($"ForestBush_{spriteIndex}_{position.x}_{position.y}");
        bush.transform.SetParent(parent, false);
        bush.transform.position = position;

        SpriteRenderer renderer = bush.AddComponent<SpriteRenderer>();
        renderer.sprite = bushes[spriteIndex];
        renderer.sortingOrder = Mathf.RoundToInt(-position.y * 10f);
    }

    private void CreateBoundary(Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        GameObject boundary = new GameObject(objectName);
        boundary.transform.SetParent(parent, false);
        boundary.transform.position = position;

        BoxCollider2D collider = boundary.AddComponent<BoxCollider2D>();
        collider.size = size;
    }

    private static bool IsPath(Vector2 point)
    {
        return DistanceToSegment(point, new Vector2(0f, -4.5f), new Vector2(0f, 4.5f)) < 0.4f
            || DistanceToSegment(point, new Vector2(-8f, 0f), Vector2.zero) < 0.4f
            || DistanceToSegment(point, Vector2.zero, new Vector2(1.6f, -0.7f)) < 0.4f;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
            return Vector2.Distance(point, start);

        float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
        return Vector2.Distance(point, start + t * segment);
    }
}
