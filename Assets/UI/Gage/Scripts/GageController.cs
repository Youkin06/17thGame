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

    // 敵名ログの重複出力防止用
    private BaseEnemyController lastLoggedEnemy;

    // 現在ゲージ再生中の敵を保持（敵が変わったら作り直す）
    private BaseEnemyController activeGaugeEnemy;

    // 円の初期スケール（解除時に戻す用）
    private Vector3[] initialScales;

    // 8個を順番に縮めるDOTweenシーケンス
    private Sequence gaugeSequence;

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
    /// キャッシュ済みの円配列を取得する。(外部スクリプトからのアクセス用)
    /// </summary>
    public RectTransform[] GetCircles()
    {
        return circles;
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
    /// 毎フレーム処理。乗っ取り状態を監視し、ゲージ開始・停止と表示用値の更新を行う。
    /// </summary>
    void FixedUpdate()
    {
        // 乗っ取り中なら、その敵の hijackDuration を取得
        if (TryGetHijackDuration(out float duration))
        {
            currentHijackDuration = duration;

            // 敵が切り替わった時だけ名前ログ
            LogHijackedEnemyNameIfChanged();

            // 必要時のみゲージ再生開始
            StartGaugeIfNeeded(duration);
        }
        else
        {
            // 非乗っ取り時は値クリア
            currentHijackDuration = 0f;
            lastLoggedEnemy = null;

            // 再生停止＆全円を元サイズへ
            StopGaugeAndReset();
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
    /// 乗っ取り対象が変化したタイミングで敵名と持続時間をログ出力する。（デバッグ用　コメントアウト）
    /// </summary>
    /*
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
    */

    /// <summary>
    /// 必要な場合のみゲージ縮小シーケンスを開始する。既に同一敵で再生中なら何もしない。
    /// </summary>
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

    /// <summary>
    /// ゲージ再生を停止し、全Circleを初期スケールへ戻す。
    /// </summary>
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

    /// <summary>
    /// オブジェクト破棄時にTweenを停止し、リークや参照残りを防止する。
    /// </summary>
    void OnDestroy()
    {
        if (gaugeSequence != null)
        {
            gaugeSequence.Kill();
        }
    }
}
