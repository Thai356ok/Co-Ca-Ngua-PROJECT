using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ENUM dùng chung toàn project – khớp với Dev 1 (MauSac)
//  Thứ tự giữ nguyên theo Dev 1: XanhLa=0, Vang=1, XanhDuong=2, Do=3
// ============================================================
public enum MauNguoiChoi
{
    XanhLa = 0,
    Vang = 1,
    XanhDuong = 2,
    Do = 3
}

// ============================================================
//  ENUM: Trạng thái vòng lặp game
// ============================================================
public enum GameState
{
    Idle,
    Wait_For_Roll,
    Rolling,
    Wait_For_Selection,
    Moving,
    Check_Win,
    Game_Over
}

// ============================================================
//  CLASS: Dữ liệu một người chơi
// ============================================================
[Serializable]
public class NguoiChoi
{
    public MauNguoiChoi mau;
    public bool daThang = false;
    public int soQuanVeDich = 0;

    public NguoiChoi(MauNguoiChoi m) { mau = m; }
}

// ============================================================
//  GAMEMANAGER
// ============================================================
public class GameManager : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Singleton
    // ----------------------------------------------------------
    public static GameManager Instance { get; private set; }

    // ----------------------------------------------------------
    //  References – kéo thả trong Inspector
    // ----------------------------------------------------------
    [Header("References (kéo thả vào đây)")]
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private TokenLogic tokenLogic;

    // ----------------------------------------------------------
    //  State Machine
    // ----------------------------------------------------------
    [Header("Trạng thái hiện tại (Read-Only)")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public GameState CurrentState => currentState;

    // Event để Dev 3 (UI) lắng nghe
    public static event Action<GameState> OnStateChanged;
    public static event Action<MauNguoiChoi> OnTurnChanged;
    public static event Action<MauNguoiChoi> OnGameOver;

    // ----------------------------------------------------------
    //  Turn Manager
    // ----------------------------------------------------------
    [Header("Người chơi")]
    [SerializeField] private int soNguoiChoi = 4;

    private List<NguoiChoi> danhSachNguoiChoi = new List<NguoiChoi>();
    private int luotHienTai = 0;
    public NguoiChoi NguoiChoiHienTai => danhSachNguoiChoi[luotHienTai];

    private bool duocDiThem = false;   // Được đi thêm khi ra 6 hoặc đá quân
    private int giaTriXucXac = 0;
    public int GiaTriXucXac => giaTriXucXac;

    // Danh sách quân hợp lệ sau khi tung xúc xắc
    private List<QuancoMovement> quanCoTheChon = new List<QuancoMovement>();

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        KhoiTaoCacNguoiChoi();
        BatDauGame();
    }

    // ============================================================
    //  KHỞI TẠO
    // ============================================================
    private void KhoiTaoCacNguoiChoi()
    {
        danhSachNguoiChoi.Clear();
        MauNguoiChoi[] cacMau = (MauNguoiChoi[])Enum.GetValues(typeof(MauNguoiChoi));
        for (int i = 0; i < Mathf.Min(soNguoiChoi, cacMau.Length); i++)
            danhSachNguoiChoi.Add(new NguoiChoi(cacMau[i]));

        Debug.Log($"[GameManager] Đã khởi tạo {danhSachNguoiChoi.Count} người chơi.");
    }

    public void BatDauGame()
    {
        luotHienTai = 0;
        duocDiThem = false;
        DoiTrangThai(GameState.Wait_For_Roll);
    }

    // ============================================================
    //  STATE MACHINE
    // ============================================================
    private void DoiTrangThai(GameState trangThaiMoi)
    {
        if (currentState == trangThaiMoi) return;
        currentState = trangThaiMoi;
        Debug.Log($"[GameManager] State → {trangThaiMoi} | Lượt: {NguoiChoiHienTai.mau}");
        OnStateChanged?.Invoke(trangThaiMoi);
        XuLyKhiVaoTrangThai(trangThaiMoi);
    }

    private void XuLyKhiVaoTrangThai(GameState state)
    {
        switch (state)
        {
            case GameState.Wait_For_Roll: XuLy_ChoTung(); break;
            case GameState.Rolling:            /* DiceManager lo */   break;
            case GameState.Wait_For_Selection: XuLy_ChoChon(); break;
            case GameState.Moving:             /* QuancoMovement lo */ break;
            case GameState.Check_Win: XuLy_KiemTraThang(); break;
            case GameState.Game_Over: XuLy_KetThuc(); break;
        }
    }

    // ============================================================
    //  HANDLERS
    // ============================================================
    private void XuLy_ChoTung()
    {
        duocDiThem = false;

        OnTurnChanged?.Invoke(NguoiChoiHienTai.mau); // Dev 3 cập nhật UI
        Debug.Log($"[GameManager] Đến lượt {NguoiChoiHienTai.mau} – Nhấn tung xúc xắc!");
    }

    /// Dev 3 gọi hàm này khi người chơi nhấn nút tung.
    public void YeuCauTungXucXac(int ketQuaTuUI)
    {
        if (currentState != GameState.Wait_For_Roll)
        {
            Debug.LogWarning("[GameManager] Chưa đến lúc tung xúc xắc!");
            return;
        }
        DoiTrangThai(GameState.Rolling);
        KhiTungXong(ketQuaTuUI);
    }

    /// <summary>Callback sau khi DiceManager tung xong.</summary>
    private void KhiTungXong(int ketQua)
    {
        giaTriXucXac = ketQua;
        Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} tung được: {ketQua}");

        // Hỏi TokenLogic: quân nào có thể đi?
        quanCoTheChon = tokenLogic.LayQuanCoTheChon(NguoiChoiHienTai.mau, ketQua);

        if (quanCoTheChon.Count == 0)
        {
            Debug.Log("[GameManager] Bị kẹt! Không có quân nào đi được.");

            // LUẬT MỚI: Bị kẹt nhưng đổ ra 1 hoặc 6 thì ĐƯỢC TUNG TIẾP. Nếu số khác thì MẤT LƯỢT.
            if (ketQua == 1 || ketQua == 6)
            {
                Debug.Log($"[GameManager] Kẹt nhưng ra {ketQua} -> Vẫn được tung xúc xắc tiếp!");
                DoiTrangThai(GameState.Wait_For_Roll);
            }
            else
            {
                ChuyenLuot();
            }
        }
        else
        {
            DoiTrangThai(GameState.Wait_For_Selection);
        }
    }

    private void XuLy_ChoChon()
    {
        // Dev 3 sẽ highlight các quân trong quanCoTheChon
        Debug.Log($"[GameManager] Có {quanCoTheChon.Count} quân có thể đi. Chờ chọn...");

        // Tự động chọn nếu chỉ có 1 quân hợp lệ
        if (quanCoTheChon.Count == 1)
            ChonQuan(quanCoTheChon[0]);
    }

    /// <summary>Dev 3 gọi khi người chơi click vào quân.</summary>
    public void ChonQuan(QuancoMovement quan)
    {
        if (currentState != GameState.Wait_For_Selection)
        {
            Debug.LogWarning("[GameManager] Chưa đến lúc chọn quân!");
            return;
        }
        if (!quanCoTheChon.Contains(quan))
        {
            Debug.LogWarning("[GameManager] Quân này không hợp lệ!");
            return;
        }

        DoiTrangThai(GameState.Moving);

        // Gọi Dev 1 di chuyển, sau đó chờ flag dangDiChuyen = false
        tokenLogic.ThucHienDiChuyen(quan, giaTriXucXac, KhiDiChuyenXong);
    }

    /// <summary>TokenLogic gọi callback này khi quân đã đến đích.</summary>
    private void KhiDiChuyenXong(MoveResult ketQua)
    {
        Debug.Log($"[GameManager] Di chuyển xong. Kết quả: {ketQua}");

        // LUẬT MỚI: CHỈ được đi thêm nếu ra 1 hoặc 6. Đá quân địch KHÔNG CÒN được thưởng thêm lượt.
        if (giaTriXucXac == 1 || giaTriXucXac == 6)
        {
            duocDiThem = true;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} được đi thêm lượt do đổ được {giaTriXucXac}!");
        }
        else
        {
            duocDiThem = false; // Đảm bảo reset cờ, tránh bug
        }

        // Cập nhật số quân về đích
        if (ketQua == MoveResult.Reached_Home)
        {
            NguoiChoiHienTai.soQuanVeDich++;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} đã có {NguoiChoiHienTai.soQuanVeDich} quân về đích.");
        }

        DoiTrangThai(GameState.Check_Win);
    }

    private void XuLy_KiemTraThang()
    {
        // 1. Kiểm tra xem người hiện tại có vừa đưa đủ 4 quân về chuồng an toàn không
        if (!NguoiChoiHienTai.daThang && tokenLogic.KiemTraThang(NguoiChoiHienTai.mau))
        {
            NguoiChoiHienTai.daThang = true; // Đánh dấu người này đã hoàn thành phần thi
            Debug.Log($"[GameManager] 🎉 Chúc mừng {NguoiChoiHienTai.mau} đã hoàn thành phần thi!");

            // 2. Đếm xem trên bàn cờ đã có bao nhiêu người về đích
            int soNguoiDaThang = 0;
            foreach (var nc in danhSachNguoiChoi)
            {
                if (nc.daThang) soNguoiDaThang++;
            }

            // 3. Nếu đã có 3 người về đích (tức là chỉ còn 1 người chót bảng) -> MỚI KẾT THÚC GAME
            if (soNguoiDaThang >= 3)
            {
                DoiTrangThai(GameState.Game_Over);
                return;
            }
            else
            {
                // Nếu chưa đủ 3 người về đích, game tiếp tục!
                // Mặc dù người này có thể đổ ra 6, nhưng vì họ đã hoàn thành game nên mất luôn quyền đi thêm lượt.
                // Hàm ChuyenLuot() sẽ tự động nhảy qua đầu người này ở các vòng sau.
                ChuyenLuot();
                return;
            }
        }

        // --- ĐỐI VỚI NHỮNG NGƯỜI CHƯA HOÀN THÀNH ---
        // Xử lý quyền đi thêm lượt bình thường (đổ ra 1 hoặc 6)
        if (duocDiThem)
        {
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} chơi thêm lượt.");
            DoiTrangThai(GameState.Wait_For_Roll);
        }
        else
        {
            ChuyenLuot();
        }
    }

    private void XuLy_KetThuc()
    {
        Debug.Log($"[GameManager] 🏁 TRÒ CHƠI HOÀN TOÀN KẾT THÚC! Đã tìm ra 3 người chiến thắng.");
        // Dev 3 có thể dùng event này để show bảng xếp hạng
        OnGameOver?.Invoke(NguoiChoiHienTai.mau);
    }

    // ============================================================
    //  TURN MANAGER
    // ============================================================
    private void ChuyenLuot()
    {
        // Thực hiện logic chuyển index người chơi cũ
        luotHienTai = TimNguoiChoiTiepTheo();
        Debug.Log($"[GameManager] Chuyển sang lượt: {NguoiChoiHienTai.mau}");
        DoiTrangThai(GameState.Wait_For_Roll);
        // Khi state chuyển sang Wait_For_Roll, hàm XuLy_ChoTung() sẽ được gọi tự động.
    }

    private int TimNguoiChoiTiepTheo()
    {
        int next = luotHienTai;
        int guard = 0;
        do
        {
            next = (next + 1) % danhSachNguoiChoi.Count;
            guard++;
            if (guard > danhSachNguoiChoi.Count) break;
        }
        while (danhSachNguoiChoi[next].daThang);
        return next;
    }

    // ============================================================
    //  PUBLIC HELPERS cho Dev 3, Dev 4
    // ============================================================
    public List<NguoiChoi> LayDanhSachNguoiChoi() => danhSachNguoiChoi;
    public List<QuancoMovement> LayQuanCoTheChon() => quanCoTheChon;
    public bool LaNguoiChoiHienTai(MauNguoiChoi mau) => NguoiChoiHienTai.mau == mau;
}