using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ENUM: Tất cả trạng thái của vòng lặp game
// ============================================================
public enum GameState
{
    Idle,               // Khởi động, chưa bắt đầu
    Wait_For_Roll,      // Chờ người chơi hiện tại tung xúc xắc
    Rolling,            // Đang trong animation tung xúc xắc
    Wait_For_Selection, // Chờ người chơi chọn quân nào để đi
    Moving,             // Quân đang di chuyển (animation)
    Check_Win,          // Kiểm tra điều kiện thắng sau mỗi nước đi
    Game_Over           // Trò chơi kết thúc
}

// ============================================================
//  ENUM: 4 màu người chơi
// ============================================================
public enum PlayerColor
{
    Red = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3
}

// ============================================================
//  CLASS: Dữ liệu của một người chơi
// ============================================================
[Serializable]
public class PlayerData
{
    public PlayerColor color;
    public bool isHuman = true;        // false = AI (mở rộng sau)
    public int tokenFinishedCount = 0; // Số quân đã về đích
    public bool hasWon = false;

    public PlayerData(PlayerColor c, bool human = true)
    {
        color = c;
        isHuman = human;
    }
}

// ============================================================
//  GAMEMANAGER: Bộ não trung tâm của game
// ============================================================
public class GameManager : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Singleton
    // ----------------------------------------------------------
    public static GameManager Instance { get; private set; }

    // ----------------------------------------------------------
    //  Inspector references – kéo thả trong Editor
    // ----------------------------------------------------------
    [Header("References")]
    [SerializeField] private DiceManager diceManager;       // Dev 1 cung cấp
    [SerializeField] private TokenLogic  tokenLogic;        // File kế bên
    // UIManager và BoardManager sẽ được thêm khi Dev khác hoàn thành

    // ----------------------------------------------------------
    //  State Machine
    // ----------------------------------------------------------
    [Header("State (Read-Only in Inspector)")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public GameState CurrentState => currentState;

    // Event để UI / các hệ thống khác lắng nghe khi state thay đổi
    public static event Action<GameState> OnStateChanged;

    // ----------------------------------------------------------
    //  Turn Manager data
    // ----------------------------------------------------------
    [Header("Players")]
    [SerializeField] private int numberOfPlayers = 4;

    private List<PlayerData> players = new List<PlayerData>();
    private int currentPlayerIndex = 0;
    public PlayerData CurrentPlayer => players[currentPlayerIndex];

    // Theo dõi xem lượt này người chơi có được đi thêm không
    private bool grantExtraTurn = false;

    // Xúc xắc lần này ra mấy
    private int lastDiceValue = 0;
    public int LastDiceValue => lastDiceValue;

    // ----------------------------------------------------------
    //  Movable tokens cache (sau khi tung xúc xắc)
    // ----------------------------------------------------------
    private List<Token> movableTokens = new List<Token>();

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================
    private void Awake()
    {
        // Singleton guard
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializePlayers();
        StartGame();
    }

    // ============================================================
    //  KHỞI TẠO
    // ============================================================
    private void InitializePlayers()
    {
        players.Clear();
        PlayerColor[] colors = (PlayerColor[])Enum.GetValues(typeof(PlayerColor));
        for (int i = 0; i < Mathf.Min(numberOfPlayers, colors.Length); i++)
        {
            players.Add(new PlayerData(colors[i], isHuman: true));
        }
        Debug.Log($"[GameManager] Đã khởi tạo {players.Count} người chơi.");
    }

    public void StartGame()
    {
        currentPlayerIndex = 0;
        grantExtraTurn = false;
        ChangeState(GameState.Wait_For_Roll);
    }

    // ============================================================
    //  STATE MACHINE – Đổi trạng thái
    // ============================================================
    private void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"[GameManager] State → {newState} | Lượt: {CurrentPlayer.color}");
        OnStateChanged?.Invoke(newState);

        // Xử lý logic khi VÀO state mới
        OnEnterState(newState);
    }

    private void OnEnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Wait_For_Roll:
                HandleWaitForRoll();
                break;

            case GameState.Rolling:
                // DiceManager sẽ gọi OnDiceRollComplete() khi xong
                break;

            case GameState.Wait_For_Selection:
                HandleWaitForSelection();
                break;

            case GameState.Moving:
                // TokenLogic / animation sẽ gọi OnTokenMovementComplete() khi xong
                break;

            case GameState.Check_Win:
                HandleCheckWin();
                break;

            case GameState.Game_Over:
                HandleGameOver();
                break;
        }
    }

    // ============================================================
    //  HANDLERS cho từng state
    // ============================================================

    /// <summary>Chuẩn bị lượt mới, thông báo UI.</summary>
    private void HandleWaitForRoll()
    {
        grantExtraTurn = false;
        // TODO: UIManager.Instance.ShowDiceButton(true);
        Debug.Log($"[GameManager] Đến lượt {CurrentPlayer.color} – Nhấn tung xúc xắc!");
    }

    /// <summary>Gọi từ nút UI hoặc input handler.</summary>
    public void RequestDiceRoll()
    {
        if (currentState != GameState.Wait_For_Roll)
        {
            Debug.LogWarning("[GameManager] Không thể tung xúc xắc lúc này.");
            return;
        }
        ChangeState(GameState.Rolling);
        diceManager.RollDice(OnDiceRollComplete); // DiceManager gọi callback
    }

    /// <summary>Callback từ DiceManager sau khi animation tung xong.</summary>
    public void OnDiceRollComplete(int diceValue)
    {
        lastDiceValue = diceValue;
        Debug.Log($"[GameManager] {CurrentPlayer.color} tung được: {diceValue}");

        // Tính danh sách quân có thể đi
        movableTokens = tokenLogic.GetMovableTokens(CurrentPlayer.color, diceValue);

        if (movableTokens.Count == 0)
        {
            Debug.Log("[GameManager] Không có quân nào di chuyển được → chuyển lượt.");
            EndTurn();
        }
        else
        {
            ChangeState(GameState.Wait_For_Selection);
        }
    }

    /// <summary>Khi vào Wait_For_Selection: highlight quân hợp lệ.</summary>
    private void HandleWaitForSelection()
    {
        // TODO: UIManager.Instance.HighlightTokens(movableTokens);
        Debug.Log($"[GameManager] Có {movableTokens.Count} quân có thể đi. Chờ chọn...");

        // Nếu chỉ có 1 quân hợp lệ → tự động chọn
        if (movableTokens.Count == 1)
        {
            SelectToken(movableTokens[0]);
        }
    }

    /// <summary>Người chơi bấm vào quân – gọi từ Token.OnClick().</summary>
    public void SelectToken(Token token)
    {
        if (currentState != GameState.Wait_For_Selection)
        {
            Debug.LogWarning("[GameManager] Không thể chọn quân lúc này.");
            return;
        }

        if (!movableTokens.Contains(token))
        {
            Debug.LogWarning("[GameManager] Quân này không hợp lệ để đi.");
            return;
        }

        // TODO: UIManager.Instance.ClearHighlights();
        ChangeState(GameState.Moving);
        tokenLogic.MoveToken(token, lastDiceValue, OnTokenMovementComplete);
    }

    /// <summary>Callback từ TokenLogic/animation khi quân đã đến nơi.</summary>
    public void OnTokenMovementComplete(MoveResult result)
    {
        Debug.Log($"[GameManager] Di chuyển xong. Kết quả: {result}");

        // Kiểm tra có được đi thêm không
        if (result == MoveResult.KickedOpponent || lastDiceValue == 6)
        {
            grantExtraTurn = true;
            Debug.Log($"[GameManager] {CurrentPlayer.color} được đi thêm lượt! (lý do: {result}, xúc xắc: {lastDiceValue})");
        }

        ChangeState(GameState.Check_Win);
    }

    /// <summary>Kiểm tra xem người chơi hiện tại có thắng chưa.</summary>
    private void HandleCheckWin()
    {
        if (tokenLogic.HasPlayerWon(CurrentPlayer.color))
        {
            CurrentPlayer.hasWon = true;
            Debug.Log($"[GameManager] 🎉 {CurrentPlayer.color} đã thắng!");
            ChangeState(GameState.Game_Over);
            return;
        }

        // Chưa thắng → chuyển lượt hoặc đi thêm
        if (grantExtraTurn)
        {
            Debug.Log($"[GameManager] {CurrentPlayer.color} chơi thêm lượt.");
            ChangeState(GameState.Wait_For_Roll);
        }
        else
        {
            EndTurn();
        }
    }

    private void HandleGameOver()
    {
        Debug.Log("[GameManager] GAME OVER!");
        // TODO: UIManager.Instance.ShowGameOverScreen(CurrentPlayer.color);
    }

    // ============================================================
    //  TURN MANAGER – Chuyển lượt
    // ============================================================
    private void EndTurn()
    {
        currentPlayerIndex = GetNextActivePlayerIndex();
        Debug.Log($"[GameManager] Chuyển sang lượt: {CurrentPlayer.color}");
        ChangeState(GameState.Wait_For_Roll);
    }

    /// <summary>Tìm người chơi tiếp theo chưa thắng.</summary>
    private int GetNextActivePlayerIndex()
    {
        int next = currentPlayerIndex;
        int safetyCounter = 0;

        do
        {
            next = (next + 1) % players.Count;
            safetyCounter++;
            if (safetyCounter > players.Count) break; // tránh vòng lặp vô hạn
        }
        while (players[next].hasWon);

        return next;
    }

    // ============================================================
    //  PUBLIC HELPERS
    // ============================================================
    public List<PlayerData> GetAllPlayers() => players;

    public bool IsCurrentPlayer(PlayerColor color) => CurrentPlayer.color == color;
}