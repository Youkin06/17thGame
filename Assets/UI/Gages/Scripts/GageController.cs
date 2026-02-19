using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GageController : MonoBehaviour
{
    [SerializeField] private RectTransform[] circles;

    void Start()
    {
        CacheCircleChildren();
    }

    /// <summary>
    /// 子オブジェクトをHierarchy上の順序（上から順）で取得して保持する。
    /// </summary>
    private void CacheCircleChildren()
    {
        int childCount = transform.childCount;
        circles = new RectTransform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            circles[i] = transform.GetChild(i).GetComponent<RectTransform>();
        }
    }

    public RectTransform[] GetCircles()
    {
        return circles;
    }
}
