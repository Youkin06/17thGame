using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class UIController : MonoBehaviour
{
    RectTransform rectTransform;

    Vector3 delayLocalPosition;
	Rect delaySafeArea;
    
    // Start is called before the first frame update
    void Awake()
    {
        Transform container = transform.Find("UIContainer");

        if(container != null)
        {
            rectTransform = container.GetComponent<RectTransform>();
            //初期化
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSafeArea();
    }

    //端末の操作可能範囲に合わせて縮尺を調整
    void UpdateSafeArea()
    {
        if (rectTransform == null) return;

        //前のフレームと値が異なるときに調整
        if (delayLocalPosition != rectTransform.localPosition || delaySafeArea != Screen.safeArea)
		{
			SetAnchor();
		}
    }

    void SetAnchor()
	{
		var safeArea = Screen.safeArea;

        //操作可能範囲 / 画面サイズ = 画面内での比率(0-1)
		// 左下
		var anchorMin = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
		// 右上
        var anchorMax = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

		rectTransform.sizeDelta = Vector2.zero;
		rectTransform.anchorMin = anchorMin;
		rectTransform.anchorMax = anchorMax;

		// 前フレームの値を更新
		delaySafeArea = safeArea;
		delayLocalPosition = rectTransform.localPosition;
	}

    void ResetAnchor()
	{
		rectTransform.anchorMin = Vector2.zero;
		rectTransform.anchorMax = Vector2.one;
	}
}
