using UnityEngine;


public abstract class BasePanel : MonoBehaviour
{
    
    public virtual void ShowPanel()
    {
        gameObject.SetActive(true);
        Debug.Log("Đang hiển thị một Panel chung.");
    }

    public virtual void HidePanel()
    {
        gameObject.SetActive(false);
    }
}