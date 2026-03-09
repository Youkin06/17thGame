using System.Collections;
using System.Collections.Generic;
// DOTweenの型（Sequence, DOScale など）を使う
using DG.Tweening;
using UnityEngine;

// 円形ゲージ制御クラス
public class GageController : MonoBehaviour
{
    // ゲージの円（子オブジェクト）を格納する配列
    [SerializeField] private RectTransform[] circles;

    // 乗っ取り状態を持つシステム参照
    [SerializeField] private HijackSystemController hijackSystemController;

    // 現在の乗っ取り時間（確認用）
    [SerializeField] private float currentHijackDuration;
    [SerializeField] private float fastFillTotalDuration = 0.4f;
    [SerializeField] private float fullShownPauseDuration = 0.1f;

    // 現在ゲージ再生中の敵
    private BaseEnemyController activeGaugeEnemy;
    private Vector3[] initialScales;
    private Sequence gaugeSequence;
    private bool isGaugeVisible;
    private bool hasSetVisibility;

    /// <summary>
    /// 初期化処理。円参照のキャッシュと初期スケール保存、依存参照の自動取得を行う。
    /// </summary>
    void Start()
    {
        // 子オブジェクトを上から順に circles に入れる
        CacheCirclesByChildOrder();

        // 初期スケールを記録
        CacheInitialScales();

        // Inspector未設定ならシーンから自動探索
        if (hijackSystemController == null)
        {
            hijackSystemController = FindObjectOfType<HijackSystemController>();
        }

        SetGaugeVisible(false);
        SetAllCirclesToInitialScale();
    }

    /// <summary>
    /// 自分の子をHierarchy順で circles に格納
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

    /// <summary>
    /// circles の初期スケールを保存し、リセット時に復元できるようにする。
    /// </summary>
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

    

    /// <summary>
    /// 毎フレーム処理。乗っ取り状態を監視し、ゲージの表示切り替えと再生制御を行う。
    /// </summary>
    void FixedUpdate()
    {
        // 乗っ取り中なら、その敵の hijackDuration を取得
        if (TryGetHijackDuration(out float duration))
        {
            currentHijackDuration = duration;
            PlayFillThenShrinkIfNeeded(duration);
        }
        else
        {
            currentHijackDuration = 0f;
            StopGaugeAndHide();
        }
    }

    

    /// <summary>
    /// 乗っ取り中の敵から hijackDuration を取得
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

    /// <summary>
    /// 乗っ取り開始時に、Circle0から順に高速で拡大→一時停止→順次縮小を再生する。
    /// </summary>
    private void PlayFillThenShrinkIfNeeded(float hijackDuration)
    {
        BaseEnemyController currentEnemy = hijackSystemController.hijackedEnemy;
        if (currentEnemy == null || circles == null || circles.Length == 0)
        {
            return;
        }
        if (currentEnemy == activeGaugeEnemy)
        {
            return;
        }

        StopGaugeAnimation();
        SetGaugeVisible(true);
        SetAllCirclesToZeroScale();

        activeGaugeEnemy = currentEnemy;
        float fillPerCircleDuration = Mathf.Max(0.01f, fastFillTotalDuration) / circles.Length;
        float shrinkTotalDuration = Mathf.Max(0.01f, hijackDuration - fastFillTotalDuration - fullShownPauseDuration);
        float shrinkPerCircleDuration = shrinkTotalDuration / circles.Length;
        gaugeSequence = DOTween.Sequence();

        // 高速で順番に拡大
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] == null)
            {
                continue;
            }

            gaugeSequence.Append(circles[i].DOScale(initialScales[i], fillPerCircleDuration).SetEase(Ease.Linear));
        }

        // 全表示状態で一時停止
        gaugeSequence.AppendInterval(Mathf.Max(0f, fullShownPauseDuration));

        // 1つずつ順番に縮小（前の仕様）
        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] == null)
            {
                continue;
            }

            gaugeSequence.Append(circles[i].DOScale(Vector3.zero, shrinkPerCircleDuration).SetEase(Ease.Linear));
        }
    }

    /// <summary>
    /// 非乗っ取り時にゲージを停止し、表示を消す。
    /// </summary>
    private void StopGaugeAndHide()
    {
        StopGaugeAnimation();
        SetGaugeVisible(false);
        SetAllCirclesToInitialScale();
        activeGaugeEnemy = null;
    }

    /// <summary>
    /// 再生中のゲージTweenを停止する。
    /// </summary>
    private void StopGaugeAnimation()
    {
        if (gaugeSequence == null)
        {
            return;
        }

        gaugeSequence.Kill();
        gaugeSequence = null;
    }

    /// <summary>
    /// 全てのCircleを初期スケールへ戻す。
    /// </summary>
    private void SetAllCirclesToInitialScale()
    {
        if (circles == null || initialScales == null)
        {
            return;
        }

        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null && i < initialScales.Length)
            {
                circles[i].localScale = initialScales[i];
            }
        }
    }

    /// <summary>
    /// 全てのCircleをゼロスケールに設定する。
    /// </summary>
    private void SetAllCirclesToZeroScale()
    {
        if (circles == null)
        {
            return;
        }

        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null)
            {
                circles[i].localScale = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// ゲージの表示/非表示を切り替える。
    /// </summary>
    private void SetGaugeVisible(bool visible)
    {
        if (circles == null)
        {
            return;
        }
        if (hasSetVisibility && isGaugeVisible == visible)
        {
            return;
        }

        for (int i = 0; i < circles.Length; i++)
        {
            if (circles[i] != null)
            {
                circles[i].gameObject.SetActive(visible);
            }
        }

        isGaugeVisible = visible;
        hasSetVisibility = true;
    }

    /// <summary>
    /// オブジェクト破棄時にTweenを停止し、リークや参照残りを防止する。
    /// </summary>
    void OnDestroy()
    {
        StopGaugeAnimation();
    }
}
