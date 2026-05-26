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
    XanhLa   = 0,
    Vang     = 1,
    XanhDuong = 2,
    Do       = 3
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
    [SerializeField] private TokenLogic  tokenLogic;

    // ----------------------------------------------------------
    //  State Machine
    // ----------------------------------------------------------
    [Header("Trạng thái hiện tại (Read-Only)")]
    [SerializeField] private GameState currentState = GameState.Idle;
    public GameState CurrentState => currentState;

    // Event để Dev 3 (UI) lắng nghe
    public static event Action<GameState>       OnStateChanged;
    public static event Action<MauNguoiChoi>    OnTurnChanged;
    public static event Action<MauNguoiChoi>    OnGameOver;

    // ----------------------------------------------------------
    //  Turn Manager
    // ----------------------------------------------------------
    [Header("Người chơi")]
    [SerializeField] private int soNguoiChoi = 4;

    private List<NguoiChoi> danhSachNguoiChoi = new List<NguoiChoi>();
    private int luotHienTai = 0;
    public NguoiChoi NguoiChoiHienTai => danhSachNguoiChoi[luotHienTai];

    private bool duocDiThem = false;   // Được đi thêm khi ra 6 hoặc đá quân
    private int  giaTriXucXac = 0;
    public  int  GiaTriXucXac => giaTriXucXac;

    // Danh sách quân hợp lệ sau khi tung xúc xắc
    private List<QuancoMovement> quanCoTheChon = new List<QuancoMovement>();

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        duocDiThem  = false;
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
            case GameState.Wait_For_Roll:      XuLy_ChoTung();        break;
            case GameState.Rolling:            /* DiceManager lo */   break;
            case GameState.Wait_For_Selection: XuLy_ChoChon();        break;
            case GameState.Moving:             /* QuancoMovement lo */ break;
            case GameState.Check_Win:          XuLy_KiemTraThang();   break;
            case GameState.Game_Over:          XuLy_KetThuc();        break;
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
            Debug.Log("[GameManager] Không có quân nào đi được → chuyển lượt.");
            ChuyenLuot();
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

        // LUẬT MỚI: Vừa ra chuồng → được tung thêm ngay lập tức (không tính lượt)
        if (ketQua == MoveResult.ExitedBase)
        {
            duocDiThem = true;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} vừa ra chuồng → tung thêm!");
            DoiTrangThai(GameState.Check_Win);
            return;
        }

        // LUẬT MỚI: Chỉ được đi thêm khi ra số 6 hoặc đá quân đối thủ
        if (ketQua == MoveResult.KickedOpponent || giaTriXucXac == 6)
        {
            duocDiThem = true;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} được đi thêm lượt!");
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
        if (tokenLogic.KiemTraThang(NguoiChoiHienTai.mau))
        {
            NguoiChoiHienTai.daThang = true;
            DoiTrangThai(GameState.Game_Over);
            return;
        }

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
        Debug.Log($"[GameManager] 🎉 {NguoiChoiHienTai.mau} THẮNG!");
        OnGameOver?.Invoke(NguoiChoiHienTai.mau); // Dev 3 hiện popup
    }

    // ============================================================
    //  TURN MANAGER
    // ============================================================
    private void ChuyenLuot()
    {
        luotHienTai = TimNguoiChoiTiepTheo();
        Debug.Log($"[GameManager] Chuyển sang lượt: {NguoiChoiHienTai.mau}");
        DoiTrangThai(GameState.Wait_For_Roll);
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
    public List<NguoiChoi> LayDanhSachNguoiChoi()  => danhSachNguoiChoi;
    public List<QuancoMovement> LayQuanCoTheChon()  => quanCoTheChon;
    public bool LaNguoiChoiHienTai(MauNguoiChoi mau) => NguoiChoiHienTai.mau == mau;
}