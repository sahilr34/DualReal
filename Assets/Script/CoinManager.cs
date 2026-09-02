using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Coin Settings")]
    [SerializeField] private int startingCoins = 0;

    // Player can withdraw coins only in multiples of 50.
    public const int WITHDRAW_MULTIPLE = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text withdrawPanelBalanceText;

    private const string COIN_KEY = "PlayerCoins";

    private int currentCoins;

    public int CurrentCoins => currentCoins;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //AddCoins(223);
        LoadCoins();
        UpdateCoinUI();

        Debug.Log($"CoinManager initialized. Current Coins = {currentCoins}");
    }

    // =========================================================
    // LOAD
    // =========================================================

    private void LoadCoins()
    {
        if (PlayerPrefs.HasKey(COIN_KEY))
        {
            currentCoins = PlayerPrefs.GetInt(COIN_KEY);
        }
        else
        {
            currentCoins = Mathf.Max(0, startingCoins);

            SaveCoins();
        }

        // Safety check.
        currentCoins = Mathf.Max(0, currentCoins);
    }

    // =========================================================
    // ADD COINS
    // =========================================================

    public void AddCoins(int amount)
    {
        Debug.Log($"AddCoins() called with amount = {amount}");

        if (amount <= 0)
        {
            Debug.LogWarning( $"CoinManager: Invalid coin amount: {amount}");

            return;
        }

        currentCoins += amount;

        Debug.Log($"Coins added successfully. New total = {currentCoins}");

        SaveCoins();
        UpdateCoinUI();
    }

    // =========================================================
    // SPEND COINS
    // =========================================================

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning("CoinManager: Spend amount must be greater than 0.");

            return false;
        }

        if (currentCoins < amount)
        {
            Debug.Log(
                $"Not enough coins. " +
                $"Required = {amount}, " +
                $"Available = {currentCoins}"
            );

            return false;
        }

        currentCoins -= amount;

        SaveCoins();
        UpdateCoinUI();

        Debug.Log(
            $"Spent {amount} coins. " +
            $"Remaining coins = {currentCoins}"
        );

        return true;
    }

    // =========================================================
    // GET COINS
    // =========================================================

    public int GetCoins()
    {
        return currentCoins;
    }

    // =========================================================
    // GET WITHDRAWABLE COINS
    // =========================================================

    public int GetWithdrawableCoins()
    {
        /*
         * Returns the largest multiple of 50
         * that the player can currently withdraw.
         *
         * Examples:
         *
         * 13  -> 0
         * 49  -> 0
         * 50  -> 50
         * 63  -> 50
         * 100 -> 100
         * 150 -> 150
         * 499 -> 450
         * 500 -> 500
         * 563 -> 550
         */

        if (currentCoins < WITHDRAW_MULTIPLE)
        {
            return 0;
        }

        return
            (currentCoins / WITHDRAW_MULTIPLE)
            * WITHDRAW_MULTIPLE;
    }

    // =========================================================
    // CAN WITHDRAW
    // =========================================================

    public bool CanWithdraw()
    {
        return currentCoins >= WITHDRAW_MULTIPLE;
    }

    // =========================================================
    // WITHDRAW COINS
    // =========================================================

    public bool WithdrawCoins()
    {
        int withdrawableCoins = GetWithdrawableCoins();

        if (withdrawableCoins < WITHDRAW_MULTIPLE)
        {
            Debug.LogWarning(
                $"Cannot withdraw. " +
                $"Minimum withdrawal is {WITHDRAW_MULTIPLE} coins."
            );

            return false;
        }

        return SpendCoins(withdrawableCoins);
    }

    // =========================================================
    // SET COINS
    // =========================================================

    public void SetCoins(int amount)
    {
        currentCoins = Mathf.Max(0, amount);

        SaveCoins();
        UpdateCoinUI();

        Debug.Log(
            $"Coins set to {currentCoins}"
        );
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateCoinUI()
    {
        if (coinText != null)
        {
            coinText.text =
                ": " + currentCoins.ToString();
        }
        else
        {
            Debug.LogWarning(
                "CoinManager: Coin Text is NOT assigned!"
            );
        }

        if (withdrawPanelBalanceText != null)
        {
            withdrawPanelBalanceText.text =
                currentCoins.ToString();
        }
        else
        {
            Debug.LogWarning(
                "CoinManager: Withdraw Panel Balance Text " +
                "is NOT assigned!"
            );
        }

        Debug.Log(
            $"Coin UI updated. Current Coins = {currentCoins}"
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(
            COIN_KEY,
            currentCoins
        );

        PlayerPrefs.Save();
    }

    // =========================================================
    // TESTING
    // =========================================================

    [ContextMenu("Add 100 Coins")]
    private void Add100Coins()
    {
        AddCoins(100);
    }

    [ContextMenu("Add 50 Coins")]
    private void Add50Coins()
    {
        AddCoins(50);
    }

    [ContextMenu("Reset Coins")]
    private void ResetCoins()
    {
        PlayerPrefs.DeleteKey(COIN_KEY);

        currentCoins =
            Mathf.Max(0, startingCoins);

        SaveCoins();
        UpdateCoinUI();

        Debug.Log(
            $"Coins reset to {startingCoins}"
        );
    }

    [ContextMenu("Log Withdrawable Coins")]
    private void LogWithdrawableCoins()
    {
        Debug.Log(
            $"Current Coins = {currentCoins} | " +
            $"Withdrawable Coins = {GetWithdrawableCoins()} | " +
            $"Remaining = {currentCoins - GetWithdrawableCoins()}"
        );
    }
}