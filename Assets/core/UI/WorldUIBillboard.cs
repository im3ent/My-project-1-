using UnityEngine;


public class WorldUIBillboard : MonoBehaviour
{
    private Canvas myCanvas;

    void Awake() // 用 Awake 比 Start 更早执行
    {
        // 自动赋值 Event Camera
        myCanvas = GetComponent<Canvas>();
        if (myCanvas != null && myCanvas.worldCamera== null)
        {
            myCanvas.worldCamera = Camera.main;
        }
    }

    void LateUpdate()
    {

    }
}