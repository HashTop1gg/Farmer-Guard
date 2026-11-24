using UnityEngine;
using UnityEngine.InputSystem;

public class ShopController : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("Fence Repair")]
    [SerializeField] private int fenceRepairAmount = 25;
    [SerializeField] private int fenceRepairCost = 15;

    [Header("Player Heal")]
    [SerializeField] private int playerHealAmount = 30;
    [SerializeField] private int playerHealCost = 20;

    [Header("Sheep")]
    [SerializeField] private SheepPen sheepPen;
    [SerializeField] private int sheepCost = 50;

    private Transform shopTransform;
    private Player_Script player;
    private Fence fence;
    private CurrencyManager currencyManager;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        player = FindAnyObjectByType<Player_Script>();
        fence = FindAnyObjectByType<Fence>();
        currencyManager = FindAnyObjectByType<CurrencyManager>();

        if (sheepPen == null)
            sheepPen = FindAnyObjectByType<SheepPen>();

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
            bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            if (Game_Input.Instance.WasInteractPressedThisFrame() || escapePressed)
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

    private void TryRepairFence()
    {
        if (fence == null || currencyManager == null) return;

        if (currencyManager.TrySpendCoins(fenceRepairCost))
        {
            fence.RepairAll(fenceRepairAmount);
        }
    }

    private void TryHealPlayer()
    {
        if (player == null || currencyManager == null) return;

        if (currencyManager.TrySpendCoins(playerHealCost))
        {
            player.Heal(playerHealAmount);
        }
    }

    private void BuySheep()
    {
        if (sheepPen == null || sheepPen.IsFull || currencyManager == null) return;

        if (currencyManager.TrySpendCoins(sheepCost))
        {
            sheepPen.TrySpawnSheep();
        }
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

        int currentCoins = currencyManager != null ? currencyManager.CurrentCoins : 0;

        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.98f);
        Rect modal = new Rect(Screen.width / 2f - 230f, Screen.height / 2f - 150f, 460f, 300f);
        GUI.Box(modal, "SHOP");

        GUI.Label(new Rect(modal.x + 20f, modal.y + 50f, modal.width - 40f, 30f),
            $"Repair fence: +{fenceRepairAmount} health ({fenceRepairCost} coins)");
        GUI.enabled = fence != null && currentCoins >= fenceRepairCost;
        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 48f, 140f, 34f), $"Repair ({fenceRepairCost})"))
            TryRepairFence();

        GUI.Label(new Rect(modal.x + 20f, modal.y + 105f, modal.width - 40f, 30f),
            $"Heal player: +{playerHealAmount} health ({playerHealCost} coins)");
        GUI.enabled = player != null && currentCoins >= playerHealCost;
        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 103f, 140f, 34f), $"Heal ({playerHealCost})"))
            TryHealPlayer();

        bool hasPen = sheepPen != null;
        bool penFull = hasPen && sheepPen.IsFull;
        string counter = hasPen ? $"{sheepPen.SheepCount}/{sheepPen.MaxSheep}" : "-";

        GUI.Label(new Rect(modal.x + 20f, modal.y + 160f, modal.width - 40f, 30f),
            $"Buy sheep: {counter} ({sheepCost} coins)");

        GUI.enabled = hasPen && !penFull && currentCoins >= sheepCost;
        string sheepBtnText = penFull ? "Pen full" : $"Buy ({sheepCost})";
        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 158f, 140f, 34f), sheepBtnText))
            BuySheep();

        GUI.enabled = true;

        if (GUI.Button(new Rect(modal.x + 300f, modal.y + 240f, 140f, 34f), "Close"))
            Close();

        GUI.backgroundColor = previousBackground;
    }
}