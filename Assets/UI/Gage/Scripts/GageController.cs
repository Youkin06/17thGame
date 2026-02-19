using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GageController : MonoBehaviour
{
    [SerializeField] private RectTransform[] circles;
    [SerializeField] private HijackSystemController hijackSystemController;
    [SerializeField] private float currentHijackDuration;

    private BaseEnemyController lastLoggedEnemy;
    private BaseEnemyController activeGaugeEnemy;
    private Vector3[] initialScales;
    private Sequence gaugeSequence;

    void Start()
    {
        CacheCirclesByChildOrder();
        CacheInitialScales();

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
            LogHijackedEnemyNameIfChanged();
            StartGaugeIfNeeded(duration);
        }
        else
        {
            currentHijackDuration = 0f;
            lastLoggedEnemy = null;
            StopGaugeAndReset();
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

    private void CacheInitialScales()
    {
        initialScales = new Vector3[circles.Length];
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null)
            {
                initialScales[i] = circles[i].localScale;
            }
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

    private void LogHijackedEnemyNameIfChanged()
    {
        BaseEnemyController currentEnemy = hijackSystemController.hijackedEnemy;
        if (currentEnemy == null || currentEnemy == lastLoggedEnemy)
        {
            return;
        }

        Debug.Log($"Hijacked Enemy: {currentEnemy.gameObject.name}" + $" (Duration: {currentEnemy.enemyData.hijackDuration}s)");
        lastLoggedEnemy = currentEnemy;
    }

    private void StartGaugeIfNeeded(float hijackDuration)
    {
        BaseEnemyController currentEnemy = hijackSystemController.hijackedEnemy;
        if (currentEnemy == null || circles == null || circles.Length == 0)
        {
            return;
        }
        if (currentEnemy == activeGaugeEnemy && gaugeSequence != null && gaugeSequence.IsActive())
        {
            return;
        }

        StopGaugeAndReset();
        activeGaugeEnemy = currentEnemy;

        float perCircleDuration = Mathf.Max(0.01f, hijackDuration) / circles.Length;
        gaugeSequence = DOTween.Sequence();

        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] == null)
            {
                continue;
            }

            gaugeSequence.Append(circles[i].DOScale(Vector3.zero, perCircleDuration).SetEase(Ease.Linear));
        }
    }

    private void StopGaugeAndReset()
    {
        if (gaugeSequence != null)
        {
            gaugeSequence.Kill();
            gaugeSequence = null;
        }

        if (circles != null && initialScales != null)
        {
            for (int i = 0; i < circles.Length; i++)
            {
                if (circles[i] != null && i < initialScales.Length)
                {
                    circles[i].localScale = initialScales[i];
                }
            }
        }

        activeGaugeEnemy = null;
    }

    void OnDestroy()
    {
        if (gaugeSequence != null)
        {
            gaugeSequence.Kill();
        }
    }
}
