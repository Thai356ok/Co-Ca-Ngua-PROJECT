using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuUI : MonoBehaviour
{
    [Header("Panel hướng dẫn")]
    [SerializeField] private GameObject guidePanel;

    [Header("Tên scene gameplay")]
    [SerializeField] private string mainSceneName = "MainScene";

    private void Start()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    public void OnStartClicked()
    {
        SceneManager.LoadScene(mainSceneName);
    }

    public void OnGuideClicked()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(true);
        }
    }

    public void OnCloseGuideClicked()
    {
        if (guidePanel != null)
        {
            guidePanel.SetActive(false);
        }
    }

    public void OnExitClicked()
    {
        Debug.Log("Thoát game");

        Application.Quit();
    }
}