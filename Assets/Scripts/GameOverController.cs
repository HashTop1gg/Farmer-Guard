using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    public static bool IsGameOver { get; private set; }

    [Tooltip("якщо ув≥мкнено Ч поразка настаЇ п≥сл€ руйнуванн€ першоњ секц≥њ. якщо вимкнено Ч коли зламають увесь паркан.")]
    [SerializeField] private bool loseOnFirstBrokenSection = true;

    private string reason = "";
    private Player_Script player;
    private Fence fence;
    private WaveSpawner spawner;
    private ShopController shop;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeSystems()
    {
        if (FindAnyObjectByType<GameOverController>() != null) return;

        GameObject systems = new GameObject("Game Systems");
        systems.AddComponent<GameOverController>();
    }

    private void Awake()
    {
        IsGameOver = false;

        spawner = GetComponent<WaveSpawner>() ?? FindAnyObjectByType<WaveSpawner>();
        shop = GetComponent<ShopController>() ?? FindAnyObjectByType<ShopController>();
        player = FindAnyObjectByType<Player_Script>();
        fence = FindAnyObjectByType<Fence>();

        if (player != null)
            player.OnPlayerDied += HandlePlayerDied;

        if (fence != null)
        {
            fence.OnFenceDestroyed += HandleFenceDestroyed;
            fence.OnSectionDestroyed += HandleSectionDestroyed;
        }
    }

    private void OnDestroy()
    {
        IsGameOver = false;

        if (player != null)
            player.OnPlayerDied -= HandlePlayerDied;

        if (fence != null)
        {
            fence.OnFenceDestroyed -= HandleFenceDestroyed;
            fence.OnSectionDestroyed -= HandleSectionDestroyed;
        }
    }

    private void HandlePlayerDied()
    {
        Finish("Player died");
    }

    private void HandleSectionDestroyed(Fence targetFence, FenceSection section)
    {
        if (loseOnFirstBrokenSection)
            Finish("The fence was breached");
    }

    private void HandleFenceDestroyed(Fence destroyedFence)
    {
        Finish("The fence was destroyed");
    }

    private void Finish(string gameOverReason)
    {
        if (IsGameOver) return;

        IsGameOver = true;
        reason = gameOverReason;
        spawner?.StopWaves();

        if (shop != null && shop.IsOpen)
            shop.Close();

        Time.timeScale = 0f;
    }

    private void OnGUI()
    {
        if (!IsGameOver) return;

        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), "");
        GUIStyle title = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };

        GUIStyle subtitle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20
        };

        GUI.Label(new Rect(Screen.width / 2f - 250f, Screen.height / 2f - 100f, 500f, 60f),
            "GAME OVER", title);
        GUI.Label(new Rect(Screen.width / 2f - 250f, Screen.height / 2f - 35f, 500f, 40f),
            reason, subtitle);

        if (GUI.Button(new Rect(Screen.width / 2f - 100f, Screen.height / 2f + 30f, 200f, 50f),
            "Restart"))
        {
            Time.timeScale = 1f;
            IsGameOver = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}