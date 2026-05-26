using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveResult
{
    Normal,
    KickedOpponent,
    Reached_Home,
    ExitedBase,      // Vừa ra chuồng → được tung thêm
}

public class TokenLogic : MonoBehaviour
{
    private const int TONG_O_VONG_NGOAI  = 56;
    private const int SO_BUOC_VAO_CHUONG = 55;
    private const int SO_BAC_CHUONG      = 6;
    private const int SO_QUAN_PHAI_VE    = 4;

    [Header("Kéo thả 4 nhóm quân vào đây (theo thứ tự XanhLa/Vang/XanhDuong/Do)")]
    [SerializeField] private List<QuancoMovement> quanXanhLa    = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanVang       = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanXanhDuong  = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanDo         = new List<QuancoMovement>();

    // ============================================================
    //  RULE 1: Lấy danh sách quân CÓ THỂ ĐI
    // ============================================================
    public List<QuancoMovement> LayQuanCoTheChon(MauNguoiChoi mau, int xucXac)
    {
        var danhSach = new List<QuancoMovement>();
        foreach (var quan in LayQuanTheoMau(mau))
        {
            if (CoTheChon(quan, xucXac))
                danhSach.Add(quan);
        }
        return danhSach;
    }

    private bool CoTheChon(QuancoMovement quan, int xucXac)
    {
        // --- Đang trong chuồng chờ ---
        // Ra được khi tung 1 hoặc 6
        // Nhưng ô xuất phát không được có quân cùng màu đứng sẵn
        if (quan.viTriHienTai == -1)
        {
            if (xucXac != 1 && xucXac != 6) return false;

            int oXuatPhat = quan.LayViTriXuatPhat();
            // Không ra nếu ô xuất phát đã có quân cùng màu
            MauNguoiChoi mauQuan = (MauNguoiChoi)(int)quan.mauCuaToi;
            foreach (var q in LayQuanTheoMau(mauQuan))
            {
                if (q == quan) continue;
                if (q.viTriHienTai == oXuatPhat && !q.dangOChuongDich)
                    return false;
            }
            return true;
        }

        // --- Đã về đích hoàn toàn ---
        if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
            return false;

        // --- Đang trong đường chuồng đích ---
        if (quan.dangOChuongDich)
            return (quan.bacChuongHienTai + xucXac) <= SO_BAC_CHUONG;

        // --- Đang trên vòng tròn chung ---
        // Kiểm tra không vượt vào chuồng quá 6 bậc
        int buocConLaiDenCua = SO_BUOC_VAO_CHUONG - quan.soBuocDaDi;
        if (xucXac > buocConLaiDenCua)
        {
            int buocTrongChuong = xucXac - buocConLaiDenCua;
            if (buocTrongChuong > SO_BAC_CHUONG) return false;
        }

        // Không được vượt qua quân khác trên đường đi
        if (CoQuanChangDuong(quan, xucXac)) return false;

        // Không đứng cùng ô với quân cùng màu
        if (TrungViTriQuanCungMau(quan, xucXac)) return false;

        return true;
    }

    // ============================================================
    //  Kiểm tra có quân nào chặn trên đường đi (bước trung gian)
    // ============================================================
    private bool CoQuanChangDuong(QuancoMovement quan, int xucXac)
    {
        if (quan.dangOChuongDich) return false;

        int viTriBatDau = quan.viTriHienTai;

        for (int buoc = 1; buoc < xucXac; buoc++)
        {
            int buocDaDiSau = quan.soBuocDaDi + buoc;
            if (buocDaDiSau >= SO_BUOC_VAO_CHUONG) break;

            int viTriGiua = (viTriBatDau + buoc) % TONG_O_VONG_NGOAI;

            foreach (MauNguoiChoi mau in Enum.GetValues(typeof(MauNguoiChoi)))
            {
                foreach (var q in LayQuanTheoMau(mau))
                {
                    if (q == quan) continue;
                    if (q.viTriHienTai == -1) continue;    // trong chuồng chờ, không chặn
                    if (q.dangOChuongDich) continue;       // trong chuồng đích, không chặn

                    if (q.viTriHienTai == viTriGiua)
                        return true;
                }
            }
        }
        return false;
    }

    // ============================================================
    //  Kiểm tra ô đích có quân cùng màu không
    // ============================================================
    private bool TrungViTriQuanCungMau(QuancoMovement quan, int xucXac)
    {
        if (quan.dangOChuongDich) return false;

        int buocDaDiSau = quan.soBuocDaDi + xucXac;
        if (buocDaDiSau >= SO_BUOC_VAO_CHUONG) return false;

        int viTriDich = (quan.viTriHienTai + xucXac) % TONG_O_VONG_NGOAI;
        MauNguoiChoi mauCuaQuan = (MauNguoiChoi)(int)quan.mauCuaToi;

        foreach (var q in LayQuanTheoMau(mauCuaQuan))
        {
            if (q == quan) continue;
            if (q.viTriHienTai == viTriDich && !q.dangOChuongDich)
                return true;
        }
        return false;
    }

    // ============================================================
    //  THỰC HIỆN DI CHUYỂN
    // ============================================================
    public void ThucHienDiChuyen(QuancoMovement quan, int xucXac, Action<MoveResult> onComplete)
    {
        StartCoroutine(ChoDiChuyenRoiKiemTra(quan, xucXac, onComplete));
    }

    private IEnumerator ChoDiChuyenRoiKiemTra(QuancoMovement quan, int xucXac, Action<MoveResult> onComplete)
    {
        bool dangTrongChuongTruoc = (quan.viTriHienTai == -1);

        quan.BatDauDiChuyen(xucXac);
        yield return new WaitUntil(() => !quan.dangDiChuyen);

        MoveResult ketQua = MoveResult.Normal;

        if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
        {
            ketQua = MoveResult.Reached_Home;
            Debug.Log($"[TokenLogic] 🏠 Quân {quan.mauCuaToi} về đích!");
        }
        else if (dangTrongChuongTruoc)
        {
            // Vừa ra chuồng → kiểm tra đá quân tại ô xuất phát trước
            // rồi mới trả về ExitedBase để tung thêm
            MoveResult ketQuaDa = KiemTraDaQuan(quan);
            if (ketQuaDa == MoveResult.KickedOpponent)
            {
                // Đá được quân → vẫn trả ExitedBase để tung thêm
                // (ra chuồng luôn được tung thêm dù có đá hay không)
                Debug.Log($"[TokenLogic] 💥 Xuất chuồng đá quân địch ngay!");
            }
            ketQua = MoveResult.ExitedBase;
            Debug.Log($"[TokenLogic] 🚀 Quân {quan.mauCuaToi} vừa ra chuồng → tung thêm!");
        }
        else if (!quan.dangOChuongDich)
        {
            ketQua = KiemTraDaQuan(quan);
        }

        onComplete?.Invoke(ketQua);
    }

    // ============================================================
    //  RULE 2: Kiểm tra đá quân đối thủ (không có ô an toàn)
    // ============================================================
    private MoveResult KiemTraDaQuan(QuancoMovement quanVuaMo)
    {
        int viTriHienTai = quanVuaMo.viTriHienTai;

        foreach (MauNguoiChoi mau in Enum.GetValues(typeof(MauNguoiChoi)))
        {
            if (mau == (MauNguoiChoi)(int)quanVuaMo.mauCuaToi) continue;

            foreach (var quanDich in LayQuanTheoMau(mau))
            {
                if (quanDich.viTriHienTai == viTriHienTai && !quanDich.dangOChuongDich)
                {
                    // Reset về vị trí vật lý ban đầu trong chuồng
                    quanDich.viTriHienTai     = -1;
                    quanDich.soBuocDaDi       = 0;
                    quanDich.dangOChuongDich  = false;
                    quanDich.bacChuongHienTai = 0;
                    quanDich.transform.position = quanDich.viTriChuongBanDau;

                    Debug.Log($"[TokenLogic] 💥 {quanVuaMo.mauCuaToi} đá quân {quanDich.mauCuaToi} về chuồng!");
                    return MoveResult.KickedOpponent;
                }
            }
        }

        return MoveResult.Normal;
    }

    // ============================================================
    //  RULE 3: Kiểm tra thắng
    // ============================================================
    public bool KiemTraThang(MauNguoiChoi mau)
    {
        int soQuanVeDich = 0;
        foreach (var quan in LayQuanTheoMau(mau))
            if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
                soQuanVeDich++;
        return soQuanVeDich >= SO_QUAN_PHAI_VE;
    }

    // ============================================================
    //  HELPERS
    // ============================================================
    public List<QuancoMovement> LayQuanTheoMau(MauNguoiChoi mau)
    {
        switch (mau)
        {
            case MauNguoiChoi.XanhLa:    return quanXanhLa;
            case MauNguoiChoi.Vang:      return quanVang;
            case MauNguoiChoi.XanhDuong: return quanXanhDuong;
            case MauNguoiChoi.Do:        return quanDo;
            default: return new List<QuancoMovement>();
        }
    }

    public int DemQuanVeDich(MauNguoiChoi mau)
    {
        int count = 0;
        foreach (var quan in LayQuanTheoMau(mau))
            if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
                count++;
        return count;
    }
}