using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    [Header("Panel kết thúc game")]
    [SerializeField] private GameObject endGamePanel;

    [Header("Text kết quả")]
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text rank1Text;
    [SerializeField] private TMP_Text rank2Text;
    [SerializeField] private TMP_Text rank3Text;
    [SerializeField] private TMP_Text rank4Text;

    private void Awake()
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += ShowRanking;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= ShowRanking;
    }

    private void ShowRanking(List<MauNguoiChoi> ranking)
    {
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = "🏆 KẾT THÚC VÁN CHƠI";
        }

        SetRankText(rank1Text, 1, ranking);
        SetRankText(rank2Text, 2, ranking);
        SetRankText(rank3Text, 3, ranking);
        SetRankText(rank4Text, 4, ranking);
    }

    private void SetRankText(TMP_Text text, int rank, List<MauNguoiChoi> ranking)
    {
        if (text == null) return;

        if (ranking != null && ranking.Count >= rank)
        {
            text.text = $"Hạng {rank}: {LayTenNguoiChoi(ranking[rank - 1])}";
        }
        else
        {
            text.text = $"Hạng {rank}: ---";
        }
    }

    private string LayTenNguoiChoi(MauNguoiChoi mau)
    {
        switch (mau)
        {
            case MauNguoiChoi.XanhLa:
                return "Xanh Lá";

            case MauNguoiChoi.Vang:
                return "Vàng";

            case MauNguoiChoi.XanhDuong:
                return "Xanh Dương";

            case MauNguoiChoi.Do:
                return "Đỏ";

            default:
                return mau.ToString();
        }
    }

    public void OnPlayAgainClicked()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnMainMenuClicked()
    {
        SceneManager.LoadScene("StartScene");
    }
}