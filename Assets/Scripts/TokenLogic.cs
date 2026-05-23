using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ENUM: Kết quả sau một nước đi
// ============================================================
public enum MoveResult
{
    Normal,          // Di chuyển bình thường
    KickedOpponent,  // Đá được quân đối thủ về chuồng
    Reached_Home,    // Quân về đích thành công
    Blocked          // (dự phòng) bị chặn – không thể xảy ra ở luật chuẩn
}

// ============================================================
//  CLASS: Dữ liệu một quân cờ
// ============================================================
[Serializable]
public class Token
{
    public PlayerColor ownerColor;
    public int tokenId;          // 0-3 (mỗi người có 4 quân)

    // -1 = đang trong chuồng (chưa ra quân)
    // 0..55 = vị trí trên bàn cờ chính (vòng ngoài, 56 ô)
    // 56..59 = đường về đích riêng (4 ô straight)
    // 60 = đã về đích hoàn toàn
    public int boardPosition = -1;

    public bool IsInBase   => boardPosition == -1;
    public bool IsFinished => boardPosition == 60;
    public bool IsOnBoard  => boardPosition >= 0 && boardPosition < 60;

    // Vị trí bắt đầu (start cell) trên vòng ngoài – mỗi màu khác nhau
    // Quy ước: Red=0, Blue=14, Green=28, Yellow=42
    public static int GetStartPosition(PlayerColor color) => (int)color * 14;

    // Ô an toàn: 4 ô start của từng màu + 4 ô ngay trước cổng về
    private static readonly HashSet<int> SafePositions = new HashSet<int> { 0, 8, 14, 22, 28, 36, 42, 50 };
    public static bool IsSafeCell(int pos) => SafePositions.Contains(pos % 56);

    public Token(PlayerColor owner, int id)
    {
        ownerColor = owner;
        tokenId = id;
        boardPosition = -1;
    }
}

// ============================================================
//  TOKENLOGIC: Rule Engine + Move Execution
// ============================================================
public class TokenLogic : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Board constants
    // ----------------------------------------------------------
    private const int BOARD_LOOP_LENGTH   = 56; // Số ô trên vòng ngoài
    private const int HOME_STRETCH_LENGTH = 4;  // Số ô đường về đích
    private const int FINISHED_POSITION   = 60; // Quân về đích hoàn toàn
    private const int TOKENS_PER_PLAYER   = 4;
    private const int TOKENS_TO_WIN       = 4;  // Phải đưa đủ 4 quân về đích

    // ----------------------------------------------------------
    //  Tất cả quân trên bàn – khởi tạo từ BoardManager hoặc đây
    // ----------------------------------------------------------
    private Dictionary<PlayerColor, List<Token>> allTokens = new Dictionary<PlayerColor, List<Token>>();

    // ============================================================
    //  KHỞI TẠO
    // ============================================================
    private void Awake()
    {
        InitializeAllTokens();
    }

    private void InitializeAllTokens()
    {
        foreach (PlayerColor color in Enum.GetValues(typeof(PlayerColor)))
        {
            var list = new List<Token>();
            for (int i = 0; i < TOKENS_PER_PLAYER; i++)
                list.Add(new Token(color, i));
            allTokens[color] = list;
        }
        Debug.Log("[TokenLogic] Đã khởi tạo tất cả quân cờ.");
    }

    // ============================================================
    //  RULE 1: Lấy danh sách quân CÓ THỂ đi với giá trị xúc xắc
    // ============================================================
    public List<Token> GetMovableTokens(PlayerColor color, int diceValue)
    {
        var movable = new List<Token>();
        var tokens  = allTokens[color];

        foreach (var token in tokens)
        {
            if (CanMoveToken(token, diceValue))
                movable.Add(token);
        }

        return movable;
    }

    /// <summary>Kiểm tra một quân cụ thể có được phép đi không.</summary>
    private bool CanMoveToken(Token token, int diceValue)
    {
        // --- Trường hợp 1: Quân đang trong chuồng ---
        if (token.IsInBase)
        {
            // Chỉ ra được khi đổ ra 6
            return diceValue == 6;
        }

        // --- Trường hợp 2: Quân đã về đích ---
        if (token.IsFinished)
            return false;

        // --- Trường hợp 3: Quân đang trên bàn ---
        int targetPos = CalculateTargetPosition(token, diceValue);

        // Không được vượt quá đích (không thể đi nếu bước quá số ô còn lại)
        if (targetPos < 0)
            return false;

        return true;
    }

    // ============================================================
    //  RULE 2: Tính vị trí đích sau khi đi `steps` bước
    //          Trả về -1 nếu không hợp lệ (vượt đích)
    // ============================================================
    private int CalculateTargetPosition(Token token, int steps)
    {
        int currentPos = token.boardPosition;
        int startPos   = Token.GetStartPosition(token.ownerColor);

        // Tính ô "cổng vào" đường về đích của màu này
        // Mỗi màu: cổng = startPos - 1 (mod 56), tức là 1 ô trước start
        int gatePos = (startPos - 1 + BOARD_LOOP_LENGTH) % BOARD_LOOP_LENGTH;

        // Số bước còn lại để đến cổng (tính theo chiều đi)
        int stepsToGate = (gatePos - currentPos + BOARD_LOOP_LENGTH) % BOARD_LOOP_LENGTH;

        if (steps <= stepsToGate)
        {
            // Chưa vào đường về đích → đi trên vòng ngoài bình thường
            return (currentPos + steps) % BOARD_LOOP_LENGTH;
        }
        else
        {
            // Đang vào / trong đường về đích
            int homeProgress = steps - stepsToGate; // Bước vào đường riêng
            int homePos = BOARD_LOOP_LENGTH + homeProgress; // 56,57,58,59 + 1 = done (60)

            if (homePos > FINISHED_POSITION)
                return -1; // Không được đi (cần đúng số, không được vượt)

            return homePos;
        }
    }

    // ============================================================
    //  THỰC HIỆN DI CHUYỂN – Gọi sau khi người chơi đã chọn quân
    // ============================================================
    public void MoveToken(Token token, int diceValue, Action<MoveResult> onComplete)
    {
        MoveResult result = MoveResult.Normal;

        if (token.IsInBase && diceValue == 6)
        {
            // --- Ra quân ---
            int startPos = Token.GetStartPosition(token.ownerColor);
            token.boardPosition = startPos;
            Debug.Log($"[TokenLogic] {token.ownerColor} Ra quân #{token.tokenId} → ô {startPos}");
            result = CheckCellConflict(token);
        }
        else
        {
            // --- Di chuyển bình thường ---
            int targetPos = CalculateTargetPosition(token, diceValue);
            token.boardPosition = targetPos;
            Debug.Log($"[TokenLogic] {token.ownerColor} Quân #{token.tokenId} → ô {targetPos}");

            if (targetPos == FINISHED_POSITION)
            {
                result = MoveResult.Reached_Home;
                Debug.Log($"[TokenLogic] 🏠 {token.ownerColor} Quân #{token.tokenId} về đích!");
            }
            else
            {
                result = CheckCellConflict(token);
            }
        }

        // TODO: Trigger animation, sau đó gọi callback
        // Hiện tại gọi ngay (sẽ được Dev animation thay thế)
        onComplete?.Invoke(result);
    }

    // ============================================================
    //  RULE 3: Kiểm tra xung đột tại ô đích
    //          Đá quân đối thủ / bị chặn bởi quân cùng màu
    // ============================================================
    private MoveResult CheckCellConflict(Token movedToken)
    {
        int pos = movedToken.boardPosition;

        // Ô an toàn → không đá được, cũng không bị đá
        if (Token.IsSafeCell(pos))
        {
            Debug.Log($"[TokenLogic] Ô {pos} là ô an toàn, không có xung đột.");
            return MoveResult.Normal;
        }

        // Duyệt tất cả quân của các màu khác
        foreach (var kvp in allTokens)
        {
            if (kvp.Key == movedToken.ownerColor) continue;

            foreach (var opponentToken in kvp.Value)
            {
                if (opponentToken.boardPosition == pos && !opponentToken.IsFinished)
                {
                    // Đá quân đối thủ về chuồng
                    opponentToken.boardPosition = -1;
                    Debug.Log($"[TokenLogic] 💥 {movedToken.ownerColor} đá quân {opponentToken.ownerColor} #{opponentToken.tokenId} về chuồng!");
                    return MoveResult.KickedOpponent;
                }
            }
        }

        return MoveResult.Normal;
    }

    // ============================================================
    //  RULE 4: Kiểm tra điều kiện THẮNG
    // ============================================================
    public bool HasPlayerWon(PlayerColor color)
    {
        int finishedCount = 0;
        foreach (var token in allTokens[color])
        {
            if (token.IsFinished) finishedCount++;
        }
        return finishedCount >= TOKENS_TO_WIN;
    }

    // ============================================================
    //  PUBLIC HELPERS – Các hệ thống khác truy vấn
    // ============================================================

    /// <summary>Lấy tất cả quân của một màu.</summary>
    public List<Token> GetTokens(PlayerColor color) => allTokens[color];

    /// <summary>Lấy tất cả quân đang ở một ô cụ thể.</summary>
    public List<Token> GetTokensAtPosition(int position)
    {
        var result = new List<Token>();
        foreach (var list in allTokens.Values)
            foreach (var token in list)
                if (token.boardPosition == position)
                    result.Add(token);
        return result;
    }

    /// <summary>Đếm quân đã về đích của một người chơi.</summary>
    public int GetFinishedTokenCount(PlayerColor color)
    {
        int count = 0;
        foreach (var token in allTokens[color])
            if (token.IsFinished) count++;
        return count;
    }
}