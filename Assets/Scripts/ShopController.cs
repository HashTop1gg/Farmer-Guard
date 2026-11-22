using UnityEngine;

public class ShopController : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 2.5f;
    [SerializeField] private int fenceRepairAmount = 25;
    [SerializeField] private int playerHealAmount = 30;

    private Transform shopTransform;
    private Player_Script player;
    private Fence fence;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        player = FindAnyObjectByType<Player_Script>();
        fence = FindAnyObjectByType<Fence>();

        GameObject shopObject = gameObject.name == "Shop"
            ? gameObject
            : GameObject.Find("Shop");
        if (shopObject == null && fence != null)
        {
            shopObject = new GameObject("Shop");
            shopObject.transform.position = fence.transform.position + Vector3.left * 3f;
        }

        shopTransform = shopObject != null ? shopObject.transform : null;
    }

    private void Update()
    {
        if (Game_Input.Instance == null || player == null) return;
        if (GameOverController.IsGameOver) return;

        if (isOpen)
        {
            if (Game_Input.Instance.WasInteractPressedThisFrame() || Input.GetKeyDown(KeyCode.Escape))
                Close();
            return;
        }

        if (IsInRange() && Game_Input.Instance.WasInteractPressedThisFrame())
            Open();
    }

    private bool IsInRange()
    {
        if (shopTransform == null || player == null) return false;
        return Vector2.Distance(player.transform.position, shopTransform.position) <= interactionRadius;
    }

    public void Open()
    {
        isOpen = true;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        isOpen = false;
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        if (isOpen)
            Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (IsInRange() && !isOpen)
        {
            Rect prompt = new Rect(Screen.width / 2f - 140f, Screen.height - 80f, 280f, 34f);
            GUI.Box(prompt, "Press E - Open Shop");
        }

        if (!isOpen) return;

        GUI.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.98f);
        Rect modal = new Rect(Screen.width / 2f - 230f, Screen.height / 2f - 150f, 460f, 300f);
        GUI.Box(modal, "SHOP");

        GUI.Label(new Rect(modal.x + 20f, modal.y + 50f, modal.width - 40f, 30f),
            $"Repair fence: +{fenceRepairAmount} health");
        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 48f, 140f, 34f), "Repair"))
            fence?.RepairAll(fenceRepairAmount);

        GUI.Label(new Rect(modal.x + 20f, modal.y + 105f, modal.width - 40f, 30f),
            $"Heal player: +{playerHealAmount} health");
        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 103f, 140f, 34f), "Heal"))
            player?.Heal(playerHealAmount);

        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 240f, 140f, 34f), "Close"))
            Close();
    }
}
