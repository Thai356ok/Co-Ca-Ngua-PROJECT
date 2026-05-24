using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ENUM: Kết quả một nước đi – dùng chung với GameManager
// ============================================================
public enum MoveResult
{
    Normal,          // Di chuyển bình thường
    KickedOpponent,  // Đá được quân đối thủ về chuồng
    Reached_Home,    // Quân về đích thành công
}

// ============================================================
//  TOKENLOGIC: Rule Engine – giao tiếp trực tiếp với QuancoMovement (Dev 1)
// ============================================================
public class TokenLogic : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Constants – phải khớp với Dev 1
    // ----------------------------------------------------------
    private const int TONG_O_VONG_NGOAI  = 56; // Dev 1 dùng % 56
    private const int SO_BUOC_VAO_CHUONG = 55; // Dev 1: soBuocDaDi == 55 thì rẽ vào chuồng
    private const int SO_BAC_CHUONG      = 6;  // 6 bậc chuồng đích
    private const int SO_QUAN_MOI_MAU    = 4;
    private const int SO_QUAN_PHAI_VE    = 4;

    // Ô an toàn trên vòng tròn chung (trùng với vị trí xuất phát 4 màu + 4 ô giữa)
    private static readonly HashSet<int> OAnToan = new HashSet<int> { 0, 8, 14, 22, 28, 36, 42, 50 };

    // ----------------------------------------------------------
    //  Quản lý tất cả quân – key là MauNguoiChoi
    //  Lấy QuancoMovement từ Scene bằng tag hoặc kéo thả
    // ----------------------------------------------------------
    [Header("Kéo thả 4 nhóm quân vào đây (theo thứ tự XanhLa/Vang/XanhDuong/Do)")]
    [SerializeField] private List<QuancoMovement> quanXanhLa   = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanVang      = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanXanhDuong = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanDo        = new List<QuancoMovement>();

    // ============================================================
    //  RULE 1: Lấy danh sách quân CÓ THỂ ĐI với giá trị xúc xắc
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

    /// <summary>Kiểm tra một quân có hợp lệ để đi không.</summary>
    private bool CoTheChon(QuancoMovement quan, int xucXac)
    {
        // --- Đang trong chuồng chờ (chưa ra quân) ---
        if (quan.viTriHienTai == -1)
            return xucXac == 6; // Chỉ ra được khi ra 6

        // --- Đã về đích hoàn toàn ---
        if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
            return false;

        // --- Đang trong đường chuồng đích ---
        if (quan.dangOChuongDich)
        {
            // Không được vượt quá số bậc chuồng
            return (quan.bacChuongHienTai + xucXac) <= SO_BAC_CHUONG;
        }

        // --- Đang trên vòng tròn chung ---
        // Kiểm tra có vừa đủ bước vào chuồng không (không vượt)
        int buocConLaiDenCua = SO_BUOC_VAO_CHUONG - quan.soBuocDaDi;
        if (xucXac > buocConLaiDenCua)
        {
            // Bước vào đường chuồng – kiểm tra không vượt 6 bậc
            int buocTrongChuong = xucXac - buocConLaiDenCua;
            return buocTrongChuong <= SO_BAC_CHUONG;
        }

        return true; // Đi bình thường trên vòng ngoài
    }

    // ============================================================
    //  THỰC HIỆN DI CHUYỂN – Gọi BatDauDiChuyen của Dev 1
    //  Chờ flag dangDiChuyen = false rồi mới gọi callback
    // ============================================================
    public void ThucHienDiChuyen(QuancoMovement quan, int xucXac, Action<MoveResult> onComplete)
    {
        StartCoroutine(ChoDiChuyenRoiKiemTra(quan, xucXac, onComplete));
    }

    private IEnumerator ChoDiChuyenRoiKiemTra(QuancoMovement quan, int xucXac, Action<MoveResult> onComplete)
    {
        // Gọi Dev 1 di chuyển
        quan.BatDauDiChuyen(xucXac);

        // Chờ cho đến khi Dev 1 báo xong (dangDiChuyen = false)
        yield return new WaitUntil(() => !quan.dangDiChuyen);

        // --- Kiểm tra kết quả sau khi đến nơi ---
        MoveResult ketQua = MoveResult.Normal;

        if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
        {
            // Về đích hoàn toàn
            ketQua = MoveResult.Reached_Home;
            Debug.Log($"[TokenLogic] 🏠 Quân {quan.mauCuaToi} về đích!");
        }
        else if (!quan.dangOChuongDich)
        {
            // Kiểm tra có đá quân đối thủ không
            ketQua = KiemTraDaQuan(quan);
        }

        onComplete?.Invoke(ketQua);
    }

    // ============================================================
    //  RULE 2: Kiểm tra đá quân đối thủ tại ô hiện tại
    // ============================================================
    private MoveResult KiemTraDaQuan(QuancoMovement quanVuaMo)
    {
        int viTriHienTai = quanVuaMo.viTriHienTai;

        // Ô an toàn → không đá được
        if (OAnToan.Contains(viTriHienTai))
            return MoveResult.Normal;

        // Duyệt tất cả quân màu khác
        foreach (MauNguoiChoi mau in Enum.GetValues(typeof(MauNguoiChoi)))
        {
            // Bỏ qua cùng màu
            if (mau == (MauNguoiChoi)(int)quanVuaMo.mauCuaToi) continue;

            foreach (var quanDich in LayQuanTheoMau(mau))
            {
                if (quanDich.viTriHienTai == viTriHienTai && !quanDich.dangOChuongDich)
                {
                    // Đá quân về chuồng
                    quanDich.viTriHienTai    = -1;
                    quanDich.soBuocDaDi      = 0;
                    quanDich.dangOChuongDich = false;
                    quanDich.bacChuongHienTai = 0;
                    // Đưa quân về vị trí vật lý chuồng ban đầu
                    quanDich.transform.position = LayViTriChuongBanDau(quanDich);

                    Debug.Log($"[TokenLogic] 💥 {quanVuaMo.mauCuaToi} đá quân {quanDich.mauCuaToi} về chuồng!");
                    return MoveResult.KickedOpponent;
                }
            }
        }

        return MoveResult.Normal;
    }

    // ============================================================
    //  RULE 3: Kiểm tra điều kiện THẮNG
    // ============================================================
    public bool KiemTraThang(MauNguoiChoi mau)
    {
        int soQuanVeDich = 0;
        foreach (var quan in LayQuanTheoMau(mau))
        {
            if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
                soQuanVeDich++;
        }
        return soQuanVeDich >= SO_QUAN_PHAI_VE;
    }

    // ============================================================
    //  HELPERS
    // ============================================================

    /// <summary>Map MauNguoiChoi → danh sách QuancoMovement tương ứng.</summary>
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

    /// <summary>Lấy vị trí vật lý chuồng ban đầu để reset quân bị đá.</summary>
    private Vector3 LayViTriChuongBanDau(QuancoMovement quan)
    {
        // TODO: Dev 1 / BoardManager cung cấp vị trí chuồng ban đầu của từng quân
        // Tạm thời dùng vị trí hiện tại (sẽ update khi có data từ Dev 1)
        Debug.LogWarning($"[TokenLogic] Cần Dev 1 cung cấp tọa độ chuồng ban đầu cho {quan.mauCuaToi}!");
        return quan.transform.position;
    }

    /// <summary>Lấy số quân đã về đích của một màu.</summary>
    public int DemQuanVeDich(MauNguoiChoi mau)
    {
        int count = 0;
        foreach (var quan in LayQuanTheoMau(mau))
            if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
                count++;
        return count;
    }
}