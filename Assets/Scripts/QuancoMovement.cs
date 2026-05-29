using System.Collections;
using UnityEngine;

public class QuancoMovement : MonoBehaviour
{
    public enum MauSac { XanhLa, Vang, XanhDuong, Do }
    public MauSac mauCuaToi;

    [Header("Trạng thái di chuyển")]
    public int viTriHienTai = -1;
    public int soBuocDaDi = 0;
    public bool dangOChuongDich = false;
    public int bacChuongHienTai = 0;

    public bool dangDiChuyen = false;

    [Header("Tọa độ vật lý")]
    public Vector3 toaDoChuongBanDau;

    private void Start()
    {
        toaDoChuongBanDau = transform.position;
    }

    public void BatDauDiChuyen(int soBuoc)
    {
        if (dangDiChuyen) return;
        StartCoroutine(DiChuyenMuot(soBuoc));
    }

    IEnumerator DiChuyenMuot(int soBuoc)
    {
        dangDiChuyen = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayTokenMoveSFX();
        }

        if (viTriHienTai == -1)
        {
            soBuoc = 1;
        }

        for (int i = 0; i < soBuoc; i++)
        {
            Vector3 diemDich = Vector3.zero;

            // ĐÚNG 55 BƯỚC MỚI ĐƯỢC VÀO CHUỒNG ĐÍCH
            if (soBuocDaDi == 55 || dangOChuongDich)
            {
                dangOChuongDich = true;
                bacChuongHienTai++;
                diemDich = LayToaDoChuongDich(bacChuongHienTai);
            }
            else
            {
                if (viTriHienTai == -1)
                {
                    viTriHienTai = LayViTriXuatPhat();
                }
                else
                {
                    viTriHienTai = (viTriHienTai + 1) % 56;
                    soBuocDaDi++;
                }

                diemDich = MapManager.Instance.vongTronChung[viTriHienTai].position;
            }

            float tocDo = 8f;
            while (Vector3.Distance(transform.position, diemDich) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, diemDich, tocDo * Time.deltaTime);
                yield return null;
            }
            transform.position = diemDich; // Chốt hạ vị trí
        }

        dangDiChuyen = false;
        Debug.Log($"[Dev 1] Quân {mauCuaToi} đã chạy xong.");
    }

    public int LayViTriXuatPhat()
    {
        switch (mauCuaToi)
        {
            case MauSac.XanhLa: return 0;
            case MauSac.Vang: return 14;
            case MauSac.XanhDuong: return 28;
            case MauSac.Do: return 42;
            default: return 0;
        }
    }

    Vector3 LayToaDoChuongDich(int bac)
    {
        int indexMANG = bac - 1;
        switch (mauCuaToi)
        {
            case MauSac.XanhLa: return MapManager.Instance.chuongXanhLa[indexMANG].position;
            case MauSac.Vang: return MapManager.Instance.chuongVang[indexMANG].position;
            case MauSac.XanhDuong: return MapManager.Instance.chuongXanhDuong[indexMANG].position;
            case MauSac.Do: return MapManager.Instance.chuongDo[indexMANG].position;
            default: return Vector3.zero;
        }
    }
}