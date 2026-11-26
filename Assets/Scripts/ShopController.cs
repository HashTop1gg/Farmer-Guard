using System;
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

    [Header("Shop Items")]
    [SerializeField] private Sprite healthIcon;
    [SerializeField] private Sprite soupIcon;
    [SerializeField] private Sprite knifeIcon;
    [SerializeField] private Sprite coinIcon;
    [SerializeField] private int healthBoostCost = 25;
    [SerializeField] private float healthBoostDuration = 30f;
    [SerializeField] private int healthPerSecond = 2;
    [SerializeField] private int knifeBoostCost = 40;
    [SerializeField] private float knifeBoostDuration = 30f;
    [SerializeField] private float knifeDamageMultiplier = 1.5f;

    private Transform shopTransform;
    private Player_Script player;
    private PlayerAttack playerAttack;
    private Fence fence;
    private CurrencyManager currencyManager;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        player = FindAnyObjectByType<Player_Script>();
        if (player != null)
            playerAttack = player.GetComponent<PlayerAttack>();

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

    private void BuyHealthBoost()
    {
        if (player == null || player.IsDead || currencyManager == null) return;

        if (currencyManager.TrySpendCoins(healthBoostCost))
            player.ApplyHealthBoost(healthBoostDuration, healthPerSecond);
    }

    private void BuyKnifeBoost()
    {
        if (playerAttack == null || currencyManager == null) return;

        if (currencyManager.TrySpendCoins(knifeBoostCost))
            playerAttack.ApplyDamageBoost(knifeBoostDuration, knifeDamageMultiplier);
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
        Rect modal = new Rect(Screen.width / 2f - 320f, Screen.height / 2f - 225f, 640f, 450f);
        GUI.Box(modal, "SHOP");
        if (coinIcon != null)
            GUI.DrawTexture(new Rect(modal.x + 520f, modal.y + 14f, 20f, 20f), coinIcon.texture, ScaleMode.ScaleToFit, true);
        GUI.Label(new Rect(modal.x + 544f, modal.y + 12f, 70f, 24f), currentCoins.ToString());

        bool hasPen = sheepPen != null;
        bool penFull = hasPen && sheepPen.IsFull;
        string counter = hasPen ? $"{sheepPen.SheepCount}/{sheepPen.MaxSheep}" : "-";

        DrawPurchaseRow(modal, 54f, healthIcon, "Repair fence", $"+{fenceRepairAmount} fence HP",
            fenceRepairCost, fence != null && currentCoins >= fenceRepairCost, "Buy", TryRepairFence);
        DrawPurchaseRow(modal, 111f, healthIcon, "Heal player", $"+{playerHealAmount} HP immediately",
            playerHealCost, player != null && !player.IsDead && player.CurrentHealth < player.MaxHealth &&
            currentCoins >= playerHealCost, "Buy", TryHealPlayer);
        DrawPurchaseRow(modal, 168f, null, "Buy sheep", $"In pen: {counter}", sheepCost,
            hasPen && !penFull && currentCoins >= sheepCost, penFull ? "Full" : "Buy", BuySheep);
        DrawPurchaseRow(modal, 225f, soupIcon, "Soup", $"+{healthPerSecond} HP/sec for {healthBoostDuration:0} sec",
            healthBoostCost, player != null && !player.IsDead && currentCoins >= healthBoostCost, "Buy", BuyHealthBoost);
        DrawPurchaseRow(modal, 282f, knifeIcon, "Knife", $"+{(knifeDamageMultiplier - 1f) * 100f:0}% attack damage for {knifeBoostDuration:0} sec",
            knifeBoostCost, playerAttack != null && currentCoins >= knifeBoostCost, "Buy", BuyKnifeBoost);

        GUI.enabled = true;

        if (GUI.Button(new Rect(modal.x + modal.width - 140f, modal.y + 396f, 120f, 34f), "Close"))
            Close();

        GUI.backgroundColor = previousBackground;
    }

    private void DrawPurchaseRow(
        Rect modal,
        float yOffset,
        Sprite icon,
        string title,
        string description,
        int cost,
        bool canBuy,
        string buttonLabel,
        Action purchase)
    {
        float rowY = modal.y + yOffset;
        if (icon != null)
            GUI.DrawTexture(new Rect(modal.x + 18f, rowY + 2f, 42f, 42f), icon.texture, ScaleMode.ScaleToFit, true);

        GUI.Label(new Rect(modal.x + 72f, rowY, 315f, 22f), title);
        GUI.Label(new Rect(modal.x + 72f, rowY + 22f, 315f, 22f), description);

        if (coinIcon != null)
            GUI.DrawTexture(new Rect(modal.x + 408f, rowY + 12f, 20f, 20f), coinIcon.texture, ScaleMode.ScaleToFit, true);

        GUI.Label(new Rect(modal.x + 432f, rowY + 10f, 52f, 24f), cost.ToString());
        GUI.enabled = canBuy;
        if (GUI.Button(new Rect(modal.x + 495f, rowY + 5f, 125f, 34f), buttonLabel))
            purchase();

        GUI.enabled = true;
    }
}