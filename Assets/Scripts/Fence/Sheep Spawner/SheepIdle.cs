using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SheepIdle : MonoBehaviour
{
    [Header("Idle-анімація")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float framesPerSecond = 4f;
    [Range(0f, 0.5f)]
    [Tooltip("Випадкове відхилення швидкості, щоб вівці не рухались синхронно")]
    [SerializeField] private float fpsRandomness = 0.25f;

    [Header("Повороти")]
    [Tooltip("Постав галочку, якщо на оригінальних спрайтах вівця дивиться вправо")]
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField] private Vector2 turnIntervalRange = new Vector2(2f, 6f);

    [Header("Відображення")]
    [Tooltip("Для виду зверху: нижчі вівці малюються поверх вищих")]
    [SerializeField] private bool sortByY = true;

    private SpriteRenderer sr;
    private float frameDuration;
    private float frameTimer;
    private int frameIndex;
    private float turnTimer;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        float fps = framesPerSecond * Random.Range(1f - fpsRandomness, 1f + fpsRandomness);
        frameDuration = 1f / Mathf.Max(0.1f, fps);

        if (idleFrames != null && idleFrames.Length > 0)
        {
            frameIndex = Random.Range(0, idleFrames.Length);
            sr.sprite = idleFrames[frameIndex];
        }

        SetFacing(Random.value > 0.5f);
        turnTimer = Random.Range(turnIntervalRange.x, turnIntervalRange.y);

        if (sortByY)
            sr.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100f);
    }

    private void Update()
    {
        if (idleFrames != null && idleFrames.Length > 1)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex = (frameIndex + 1) % idleFrames.Length;
                sr.sprite = idleFrames[frameIndex];
            }
        }

        turnTimer -= Time.deltaTime;
        if (turnTimer <= 0f)
        {
            SetFacing(Random.value > 0.5f);
            turnTimer = Random.Range(turnIntervalRange.x, turnIntervalRange.y);
        }
    }

    private void SetFacing(bool faceRight)
    {
        sr.flipX = faceRight != spriteFacesRight;
    }
}
