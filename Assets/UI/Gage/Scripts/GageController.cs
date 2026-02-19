using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GageController : MonoBehaviour
{
    [SerializeField] private RectTransform[] circles;
    [SerializeField] private HijackSystemController hijackSystemController;
    [SerializeField] private float currentHijackDuration;

    void Start()
    {
        CacheCirclesByChildOrder();

        if (hijackSystemController == null)
        {
            hijackSystemController = FindObjectOfType<HijackSystemController>();
        }
    }

    void Update()
    {
        if (TryGetHijackDuration(out float duration))
        {
            currentHijackDuration = duration;
        }
        else
        {
            currentHijackDuration = 0f;
        }
    }

    /// <summary>
    /// 自分の子オブジェクトをHierarchyの並び順で circles に格納する。
    /// </summary>
    private void CacheCirclesByChildOrder()
    {
        int childCount = transform.childCount;
        circles = new RectTransform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            circles[i] = transform.GetChild(i) as RectTransform;
        }
    }

    public RectTransform[] GetCircles()
    {
        return circles;
    }

    /// <summary>
    /// 現在乗っ取っている敵の hijackDuration を取得する。
    /// </summary>
    public bool TryGetHijackDuration(out float duration)
    {
        duration = 0f;

        if (hijackSystemController == null)
        {
            return false;
        }

        BaseEnemyController hijackedEnemy = hijackSystemController.hijackedEnemy;
        if (hijackedEnemy == null || hijackedEnemy.enemyData == null)
        {
            return false;
        }

        duration = hijackedEnemy.enemyData.hijackDuration;
        return true;
    }
}
