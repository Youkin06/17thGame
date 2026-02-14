using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HijackSystemController : MonoBehaviour
{
    [Header("参照コンポーネント")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerUIController playerUIController;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private StageController stageController;

    // 乗っ取り関連
    public BaseEnemyController hijackedEnemy { get; private set; } = null; // 乗っ取った敵への参照（nullチェックで乗っ取り状態を判定）
    private float hijackTimer = 0f; // 乗っ取りタイマー
    
    void Start()
    {
        // 参照が未設定の場合は自動取得を試みる
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (stageController == null)
        {
            stageController = FindObjectOfType<StageController>();
        }
    }

    /// <summary>
    /// プレイヤーがダメージを受けたときの実処理。乗っ取り中なら解放、そうでなければ死亡処理。
    /// </summary>
    public void OnPlayerDamaged()
    {
        if (IsHijacking())
        {
            ReleaseHijackedEnemy();
            return;
        }
        if (stageController != null)
            stageController.OnPlayerDead();
    }
    
    void FixedUpdate()
    {
        // 乗っ取りタイマーの更新
        if (hijackedEnemy != null)
        {
            hijackTimer += Time.fixedDeltaTime;
            if (hijackedEnemy.enemyData != null
                && hijackTimer >= hijackedEnemy.enemyData.hijackDuration)
            {
                ReleaseHijackedEnemy();
            }
            
            if (hijackedEnemy != null)
            {
            // 乗っ取り中は敵の向きをプレイヤーに合わせる（親子関係を利用）
            hijackedEnemy.transform.localRotation = Quaternion.identity;
            }
        }
    }
    
    /// <summary>
    /// 衝突判定から呼ばれる乗っ取り試行メソッド
    /// </summary>
    public void TryHijackEnemy(Collision2D collision, PlayerController controller)
    {
        if (collision.gameObject.tag != "Enemy") return;
        if (controller.currentState != PlayerMoveState.Dashing) return;
        
        BaseEnemyController enemy = collision.gameObject.GetComponent<BaseEnemyController>();
        
        // BaseEnemyControllerがない場合の処理はPlayerControllerで行う
        if (enemy == null)
        {
            return;
        }
        
        // 衝突したオブジェクトが既にプレイヤーの子オブジェクト（乗っ取っている敵）の場合はスキップ
        if (collision.gameObject.transform.parent == controller.transform)
        {
            return;
        }
        
        // 既に同じ敵を乗っ取っている場合は処理をスキップ
        if (hijackedEnemy == enemy)
        {
            return;
        }
        
        // 既に別の敵を乗っ取り中なら前の敵の参照を保存してから解放
        BaseEnemyController previousEnemy = null;
        if (hijackedEnemy != null)
        {
            previousEnemy = hijackedEnemy; // 解放前の参照を保存
            ReleaseHijackedEnemy();
        }
        
        // 解放した直後の敵と同じ敵の場合はスキップ（再乗っ取りを防ぐ）
        if (previousEnemy == enemy)
        {
            return;
        }
        
        // 新しい敵を乗っ取る
        HijackEnemy(enemy);
    }


    public void TryHijackEnemy_Trigger(Collider2D collider, PlayerController controller)
    {
        if (collider.gameObject.tag != "Enemy") return;
        if (controller.currentState != PlayerMoveState.Dashing) return;
        
        BaseEnemyController enemy = collider.gameObject.GetComponent<BaseEnemyController>();
        
        // BaseEnemyControllerがない場合の処理はPlayerControllerで行う
        if (enemy == null)
        {
            return;
        }
        
        // 衝突したオブジェクトが既にプレイヤーの子オブジェクト（乗っ取っている敵）の場合はスキップ
        if (collider.gameObject.transform.parent == controller.transform)
        {
            return;
        }
        
        // 既に同じ敵を乗っ取っている場合は処理をスキップ
        if (hijackedEnemy == enemy)
        {
            return;
        }
        
        // 既に別の敵を乗っ取り中なら前の敵の参照を保存してから解放
        BaseEnemyController previousEnemy = null;
        if (hijackedEnemy != null)
        {
            previousEnemy = hijackedEnemy; // 解放前の参照を保存
            ReleaseHijackedEnemy();
        }
        
        // 解放した直後の敵と同じ敵の場合はスキップ（再乗っ取りを防ぐ）
        if (previousEnemy == enemy)
        {
            return;
        }
        
        // 新しい敵を乗っ取る
        HijackEnemy(enemy);
    }

    
    /// <summary>
    /// 敵を乗っ取る処理
    /// </summary>
    private void HijackEnemy(BaseEnemyController enemy)
    {
        // 1. 敵の追跡動作を停止
        enemy.StopTracking();
        
        // 2. 敵の位置を取得
        Vector2 enemyPosition = enemy.transform.position;
        
        // 3. プレイヤーを敵の中心に移動
        rb.position = enemyPosition;
        
        // 4. 敵をPlayerの子オブジェクトにする
        enemy.transform.SetParent(playerController.transform);
        enemy.transform.localPosition = Vector3.zero; // 相対位置を0に設定
        
        // 5. 参照を保持
        hijackedEnemy = enemy;
        hijackTimer = 0f;
        
        // 6. 最大速度を敵の速度に変更（イベント時に一度だけ設定）
        if (enemy.enemyData != null)
        {
            playerController.SetMaxSpeed(enemy.enemyData.moveSpeed);
        }
        
        // 7. UIを乗っ取りモードに
        if (playerUIController != null)
        {
            playerUIController.isHijacking = true;
            playerUIController.ResetHijackTimer();
        }
        
        // 8. Dashing状態を終了（直接メソッドを呼ぶ）
        if (playerController != null)
        {
            playerController.EndDashingState();
        }
        
        Debug.Log($"敵を乗っ取りました: {enemy.enemyData?.enemyType}");
    }
    
    /// <summary>
    /// 乗っ取り解除処理
    /// </summary>
    public void ReleaseHijackedEnemy()
    {
        if (hijackedEnemy == null) return;
        
        // 1. 敵を解放
        hijackedEnemy.ReleaseEnemy();
        
        // 2. 最大速度をプレイヤーのデフォルト速度に戻す
        playerController.ResetMaxSpeed();
        
        // 3. 参照をクリア
        hijackedEnemy = null;
        hijackTimer = 0f;
        
        // 4. UIを通常モードに
        if (playerUIController != null)
        {
            playerUIController.ResetHijackTimer();
            playerUIController.isHijacking = false;
        }
        
        Debug.Log("敵を解放しました");
    }
    
    /// <summary>
    /// 乗っ取り中かどうかを取得
    /// </summary>
    public bool IsHijacking()
    {
        return hijackedEnemy != null;
    }

    /// <summary>
    /// 乗っ取っている敵の種類を取得（突進可否の判定に使用）
    /// </summary>
    public EnemyType? GetHijackedEnemyType()
    {
        if (hijackedEnemy == null || hijackedEnemy.enemyData == null) return null;
        return hijackedEnemy.enemyData.enemyType;
    }
}

