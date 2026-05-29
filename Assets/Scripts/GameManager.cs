using System;
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

    public NguoiChoi(MauNguoiChoi m)
    {
        mau = m;
    }
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

    public static event Action<GameState> OnStateChanged;
    public static event Action<MauNguoiChoi> OnTurnChanged;
    public static event Action<List<MauNguoiChoi>> OnGameOver;

    // ----------------------------------------------------------
    //  Turn Manager
    // ----------------------------------------------------------
    [Header("Người chơi")]
    [SerializeField] private int soNguoiChoi = 4;

    [Header("Thiết lập lượt đầu tiên")]
    [SerializeField] private bool chonNgauNhienNguoiDiTruoc = true;
    [SerializeField] private MauNguoiChoi nguoiChoiDiTruocCoDinh = MauNguoiChoi.XanhLa;

    private List<NguoiChoi> danhSachNguoiChoi = new List<NguoiChoi>();
    private List<MauNguoiChoi> bangXepHang = new List<MauNguoiChoi>();

    private int luotHienTai = 0;

    public NguoiChoi NguoiChoiHienTai
    {
        get
        {
            if (danhSachNguoiChoi == null || danhSachNguoiChoi.Count == 0)
                return null;

            luotHienTai = Mathf.Clamp(luotHienTai, 0, danhSachNguoiChoi.Count - 1);
            return danhSachNguoiChoi[luotHienTai];
        }
    }

    private bool duocDiThem = false;
    private int giaTriXucXac = 0;
    public int GiaTriXucXac => giaTriXucXac;

    private List<QuancoMovement> quanCoTheChon = new List<QuancoMovement>();

    // ============================================================
    //  UNITY LIFECYCLE
    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
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
        {
            danhSachNguoiChoi.Add(new NguoiChoi(cacMau[i]));
        }

        Debug.Log($"[GameManager] Đã khởi tạo {danhSachNguoiChoi.Count} người chơi.");
    }

    public void BatDauGame()
    {
        duocDiThem = false;
        giaTriXucXac = 0;

        bangXepHang.Clear();

        foreach (var nc in danhSachNguoiChoi)
        {
            nc.daThang = false;
            nc.soQuanVeDich = 0;
        }

        ChonNguoiDiTruoc();

        DoiTrangThai(GameState.Wait_For_Roll);
    }

    private void ChonNguoiDiTruoc()
    {
        if (danhSachNguoiChoi == null || danhSachNguoiChoi.Count == 0)
        {
            luotHienTai = 0;
            return;
        }

        if (chonNgauNhienNguoiDiTruoc)
        {
            luotHienTai = UnityEngine.Random.Range(0, danhSachNguoiChoi.Count);
        }
        else
        {
            int index = danhSachNguoiChoi.FindIndex(nc => nc.mau == nguoiChoiDiTruocCoDinh);
            luotHienTai = index >= 0 ? index : 0;
        }

        Debug.Log($"[GameManager] Người đi trước: {NguoiChoiHienTai.mau}");
    }

    // ============================================================
    //  STATE MACHINE
    // ============================================================
    private void DoiTrangThai(GameState trangThaiMoi)
    {
        if (currentState == trangThaiMoi) return;

        currentState = trangThaiMoi;

        string tenLuot = NguoiChoiHienTai != null ? NguoiChoiHienTai.mau.ToString() : "---";
        Debug.Log($"[GameManager] State → {trangThaiMoi} | Lượt: {tenLuot}");

        OnStateChanged?.Invoke(trangThaiMoi);
        XuLyKhiVaoTrangThai(trangThaiMoi);
    }

    private void XuLyKhiVaoTrangThai(GameState state)
    {
        switch (state)
        {
            case GameState.Wait_For_Roll:
                XuLy_ChoTung();
                break;

            case GameState.Rolling:
                break;

            case GameState.Wait_For_Selection:
                XuLy_ChoChon();
                break;

            case GameState.Moving:
                break;

            case GameState.Check_Win:
                XuLy_KiemTraThang();
                break;

            case GameState.Game_Over:
                XuLy_KetThuc();
                break;
        }
    }

    // ============================================================
    //  HANDLERS
    // ============================================================
    private void XuLy_ChoTung()
    {
        duocDiThem = false;

        if (NguoiChoiHienTai != null)
        {
            OnTurnChanged?.Invoke(NguoiChoiHienTai.mau);
            Debug.Log($"[GameManager] Đến lượt {NguoiChoiHienTai.mau} – Nhấn tung xúc xắc!");
        }
    }

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

    private void KhiTungXong(int ketQua)
    {
        giaTriXucXac = ketQua;

        Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} tung được: {ketQua}");

        quanCoTheChon = tokenLogic.LayQuanCoTheChon(NguoiChoiHienTai.mau, ketQua);

        if (quanCoTheChon.Count == 0)
        {
            Debug.Log("[GameManager] Bị kẹt! Không có quân nào đi được.");

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
        Debug.Log($"[GameManager] Có {quanCoTheChon.Count} quân có thể đi. Chờ chọn...");

        if (quanCoTheChon.Count == 1)
        {
            ChonQuan(quanCoTheChon[0]);
        }
    }

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

        tokenLogic.ThucHienDiChuyen(quan, giaTriXucXac, KhiDiChuyenXong);
    }

    private void KhiDiChuyenXong(MoveResult ketQua)
    {
        Debug.Log($"[GameManager] Di chuyển xong. Kết quả: {ketQua}");

        if (giaTriXucXac == 1 || giaTriXucXac == 6)
        {
            duocDiThem = true;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} được đi thêm lượt do đổ được {giaTriXucXac}!");
        }
        else
        {
            duocDiThem = false;
        }

        if (ketQua == MoveResult.Reached_Home)
        {
            NguoiChoiHienTai.soQuanVeDich++;
            Debug.Log($"[GameManager] {NguoiChoiHienTai.mau} đã có {NguoiChoiHienTai.soQuanVeDich} quân về đích.");
        }

        DoiTrangThai(GameState.Check_Win);
    }

    private void XuLy_KiemTraThang()
    {
        if (!NguoiChoiHienTai.daThang && tokenLogic.KiemTraThang(NguoiChoiHienTai.mau))
        {
            NguoiChoiHienTai.daThang = true;

            if (!bangXepHang.Contains(NguoiChoiHienTai.mau))
            {
                bangXepHang.Add(NguoiChoiHienTai.mau);
            }

            Debug.Log($"[GameManager] 🎉 Chúc mừng {NguoiChoiHienTai.mau} đã hoàn thành phần thi! Hạng hiện tại: {bangXepHang.Count}");

            int soNguoiDaThang = 0;

            foreach (var nc in danhSachNguoiChoi)
            {
                if (nc.daThang)
                    soNguoiDaThang++;
            }

            if (soNguoiDaThang >= 3)
            {
                DoiTrangThai(GameState.Game_Over);
                return;
            }
            else
            {
                ChuyenLuot();
                return;
            }
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
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSFX();
        }

        foreach (var nc in danhSachNguoiChoi)
        {
            if (!bangXepHang.Contains(nc.mau))
            {
                bangXepHang.Add(nc.mau);
            }
        }

        Debug.Log("[GameManager] 🏁 TRÒ CHƠI HOÀN TOÀN KẾT THÚC! Bảng xếp hạng đã được tạo.");

        OnGameOver?.Invoke(new List<MauNguoiChoi>(bangXepHang));
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

            if (guard > danhSachNguoiChoi.Count)
                break;
        }
        while (danhSachNguoiChoi[next].daThang);

        return next;
    }

    // ============================================================
    //  PUBLIC HELPERS
    // ============================================================
    public List<NguoiChoi> LayDanhSachNguoiChoi() => danhSachNguoiChoi;

    public List<QuancoMovement> LayQuanCoTheChon() => quanCoTheChon;

    public bool LaNguoiChoiHienTai(MauNguoiChoi mau)
    {
        return NguoiChoiHienTai != null && NguoiChoiHienTai.mau == mau;
    }

    public List<MauNguoiChoi> LayBangXepHang()
    {
        return new List<MauNguoiChoi>(bangXepHang);
    }
}