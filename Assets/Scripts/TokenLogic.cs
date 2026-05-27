using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MoveResult
{
    Normal,          // Di chuyển bình thường
    KickedOpponent,  // Đá được quân đối thủ về chuồng
    Reached_Home,    // Quân về đích thành công
}

public class TokenLogic : MonoBehaviour
{
    // ----------------------------------------------------------
    //  Constants
    // ----------------------------------------------------------
    private const int TONG_O_VONG_NGOAI = 56;
    private const int SO_BUOC_VAO_CHUONG = 55; // Về lại 55: Đi 55 bước tới cửa, bước 56 là quẹo vô bậc 1
    private const int SO_BAC_CHUONG = 6;

    [Header("Kéo thả 4 nhóm quân vào đây")]
    [SerializeField] private List<QuancoMovement> quanXanhLa = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanVang = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanXanhDuong = new List<QuancoMovement>();
    [SerializeField] private List<QuancoMovement> quanDo = new List<QuancoMovement>();

    // ============================================================
    //  RULE 1: TÌM QUÂN HỢP LỆ ĐỂ ĐI
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
        // --- 1. ĐANG TRONG CHUỒNG CHỜ ---
        if (quan.viTriHienTai == -1)
        {
            if (xucXac == 1 || xucXac == 6)
            {
                int viTriXuatPhat = LayViTriXuatPhat((MauNguoiChoi)(int)quan.mauCuaToi);
                QuancoMovement quanTaiCua = TimQuanCoTaiViTriChung(viTriXuatPhat);

                if (quanTaiCua != null && quanTaiCua.mauCuaToi == (QuancoMovement.MauSac)(int)quan.mauCuaToi)
                {
                    return false; // Bị kẹt ở cửa
                }
                return true;
            }
            return false;
        }

        // --- 2. ĐÃ LÊN ĐẾN BẬC 6 (MAX) ---
        if (quan.dangOChuongDich && quan.bacChuongHienTai >= SO_BAC_CHUONG)
            return false;

        // --- 3. KIỂM TRA ĐIỀU KIỆN LÊN CHUỒNG VÀ DI CHUYỂN TRONG CHUỒNG ---
        int buocConLaiDenCua = SO_BUOC_VAO_CHUONG - quan.soBuocDaDi;

        if (!quan.dangOChuongDich && buocConLaiDenCua > 0)
        {
            // Chưa tới cửa chuồng mà dư xúc xắc -> Kẹt
            if (xucXac > buocConLaiDenCua) return false;
        }
        else
        {
            // Đang đứng CỬA CHUỒNG hoặc ĐÃ VÀO CHUỒNG
            int bacDichDen = quan.bacChuongHienTai + xucXac;

            if (bacDichDen > SO_BAC_CHUONG) return false; // Vượt quá bậc 6

            // SỬA LỖI Ở ĐÂY: Dò radar từ vị trí hiện tại lên vị trí đích xem có đồng đội chặn không
            for (int i = quan.bacChuongHienTai + 1; i <= bacDichDen; i++)
            {
                if (KiemTraBacChuongCoNguoiChua((MauNguoiChoi)(int)quan.mauCuaToi, i))
                {
                    return false; // Có người cản đường hoặc ô đích đã bị chiếm -> Kẹt!
                }
            }
            return true; // Đường thông hè thoáng
        }

        // --- 4. CHECK CẢN ĐƯỜNG BÊN NGOÀI VÒNG CHUNG ---
        int viTriAo = quan.viTriHienTai;
        int soBuocQuet = xucXac;

        for (int i = 1; i <= soBuocQuet; i++)
        {
            viTriAo = (viTriAo + 1) % TONG_O_VONG_NGOAI;
            bool laODich = (i == xucXac);

            QuancoMovement quanCanDuong = TimQuanCoTaiViTriChung(viTriAo);

            if (quanCanDuong != null)
            {
                if (!laODich)
                    return false;
                else if (quanCanDuong.mauCuaToi == quan.mauCuaToi)
                    return false;
            }
        }
        return true;
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
        quan.BatDauDiChuyen(xucXac);
        yield return new WaitUntil(() => !quan.dangDiChuyen);

        MoveResult ketQua = MoveResult.Normal;

        if (quan.dangOChuongDich)
        {
            ketQua = MoveResult.Reached_Home; // Lên bất kỳ bậc chuồng nào cũng tính là đã về an toàn
        }
        else
        {
            ketQua = KiemTraDaQuan(quan);
        }

        onComplete?.Invoke(ketQua);
    }

    // ============================================================
    //  ĐÁ QUÂN ĐỐI THỦ
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
                    quanDich.viTriHienTai = -1;
                    quanDich.soBuocDaDi = 0;
                    quanDich.dangOChuongDich = false;
                    quanDich.bacChuongHienTai = 0;
                    quanDich.transform.position = LayViTriChuongBanDau(quanDich);

                    Debug.Log($"[TokenLogic] 💥 {quanVuaMo.mauCuaToi} đá bay {quanDich.mauCuaToi} về chuồng!");
                    return MoveResult.KickedOpponent;
                }
            }
        }
        return MoveResult.Normal;
    }

    // ============================================================
    //  LOGIC KIỂM TRA THẮNG CHUẨN VN (Xếp đủ 6-5-4-3)
    // ============================================================
    public bool KiemTraThang(MauNguoiChoi mau)
    {
        int soQuanTrongChuong = 0;
        int tongSoBac = 0;

        foreach (var quan in LayQuanTheoMau(mau))
        {
            if (quan.dangOChuongDich)
            {
                soQuanTrongChuong++;
                tongSoBac += quan.bacChuongHienTai;
            }
        }

        // SỬA LẠI ĐIỀU KIỆN THẮNG: Đủ 4 quân về chuồng VÀ tổng các bậc bằng đúng 18 (6 + 5 + 4 + 3)
        if (soQuanTrongChuong == 4 && tongSoBac == 18)
        {
            return true;
        }

        return false;
    }

    // ============================================================
    //  HELPERS & RADAR
    // ============================================================
    public List<QuancoMovement> LayQuanTheoMau(MauNguoiChoi mau)
    {
        switch (mau)
        {
            case MauNguoiChoi.XanhLa: return quanXanhLa;
            case MauNguoiChoi.Vang: return quanVang;
            case MauNguoiChoi.XanhDuong: return quanXanhDuong;
            case MauNguoiChoi.Do: return quanDo;
            default: return new List<QuancoMovement>();
        }
    }

    private QuancoMovement TimQuanCoTaiViTriChung(int viTriKiemTra)
    {
        foreach (MauNguoiChoi mau in Enum.GetValues(typeof(MauNguoiChoi)))
        {
            foreach (var quan in LayQuanTheoMau(mau))
            {
                if (quan.viTriHienTai == viTriKiemTra && !quan.dangOChuongDich && quan.viTriHienTai != -1)
                {
                    return quan;
                }
            }
        }
        return null;
    }

    // RADAR MỚI: Quét xem một bậc chuồng cụ thể đã có quân nhà đứng chưa
    private bool KiemTraBacChuongCoNguoiChua(MauNguoiChoi mau, int bacKiemTra)
    {
        foreach (var quan in LayQuanTheoMau(mau))
        {
            if (quan.dangOChuongDich && quan.bacChuongHienTai == bacKiemTra)
            {
                return true; // Có người rồi!
            }
        }
        return false;
    }

    private int LayViTriXuatPhat(MauNguoiChoi mau)
    {
        switch (mau)
        {
            case MauNguoiChoi.XanhLa: return 0;
            case MauNguoiChoi.Vang: return 14;
            case MauNguoiChoi.XanhDuong: return 28;
            case MauNguoiChoi.Do: return 42;
            default: return 0;
        }
    }

    private Vector3 LayViTriChuongBanDau(QuancoMovement quan)
    {
        return quan.toaDoChuongBanDau;
    }
}