using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Coin Settings")]
    [SerializeField] private int startingCoins = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text withdrawPanelBalanceText;

    private const string COIN_KEY = "PlayerCoins";

    private int currentCoins;

    public int CurrentCoins => currentCoins;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
            currentCoins = startingCoins;
            SaveCoins();
        }
    }

    // =========================================================
    // ADD COINS
    // =========================================================

    public void AddCoins(int amount)
    {
        Debug.Log($"AddCoins() called with amount = {amount}");

        if (amount <= 0)
        {
            Debug.LogWarning(
                $"CoinManager: Invalid coin amount: {amount}"
            );

            return;
        }

        currentCoins += amount;

        Debug.Log(
            $"Coins added successfully. New total = {currentCoins}"
        );

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
            Debug.LogWarning(
                "CoinManager: Spend amount must be greater than 0."
            );

            return false;
        }

        if (currentCoins < amount)
        {
            Debug.Log(
                $"Not enough coins. Required = {amount}, " +
                $"Available = {currentCoins}"
            );

            return false;
        }

        currentCoins -= amount;

        SaveCoins();
        UpdateCoinUI();

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
    // SET COINS
    // =========================================================

    public void SetCoins(int amount)
    {
        currentCoins = Mathf.Max(0, amount);

        SaveCoins();
        UpdateCoinUI();
    }

    // =========================================================
    // UI
    // =========================================================

    private void UpdateCoinUI()
    {
        if (coinText == null)
        {
            Debug.LogError(
                "CoinManager: Coin Text is NOT assigned!"
            );

            return;
        }

        coinText.text =": "+ currentCoins.ToString();
        withdrawPanelBalanceText.text= currentCoins.ToString();

        Debug.Log(
            $"Coin UI updated: {coinText.text}"
        );
    }

    // =========================================================
    // SAVE
    // =========================================================

    private void SaveCoins()
    {
        PlayerPrefs.SetInt(COIN_KEY, currentCoins);
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

    [ContextMenu("Reset Coins")]
    private void ResetCoins()
    {
        PlayerPrefs.DeleteKey(COIN_KEY);

        currentCoins = startingCoins;

        SaveCoins();
        UpdateCoinUI();

        Debug.Log(
            $"Coins reset to {startingCoins}"
        );
    }
}