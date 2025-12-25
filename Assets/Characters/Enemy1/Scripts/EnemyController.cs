using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using System.IO;

public class EnemyController : MonoBehaviour
{
    public EnemyData enemyData;
    [SerializeField] float serchRadius = 6.0f;//移動を始める距離
    [SerializeField] float attackRadius = 3.0f;//移動を始める距離
    [SerializeField] float turnSpeed = 180f;//回転速度
    [SerializeField] float angleOffset = 270f;//回転の調整(初期の向き)
    [SerializeField] float dashSpeed = 5.0f;//突進速度
    [SerializeField] float waitTime = 2f;//攻撃するまでの時間
    [SerializeField] float attackDuration = 3f;//突進する時間
    [SerializeField] float attackCoolDown = 3f;//攻撃してから次に攻撃を始めるまでの時間
    private float moveSpeed;//移動するスピード
    private bool isAttacking;
    private bool isHijacked = false; // 乗っ取り中フラグ
    private Rigidbody2D enemyRb; // Rigidbody2Dへの参照

    NavMeshAgent agent;
    // Start is called before the first frame update
    void Start()
    {
        if (enemyData != null)
        {
            Debug.Log($"この敵のタイプは: {enemyData.enemyType} です");
            Debug.Log($"移動スピードは: {enemyData.moveSpeed} です");
            moveSpeed = enemyData.moveSpeed;
        }

        isAttacking = false;
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;//2DなのでNavMeshAgentの自動回転はオフにする
        agent.updateUpAxis = false;//2DなのでNavMeshAgentの立ち上がりはオフにする
        enemyRb = GetComponent<Rigidbody2D>(); // Rigidbody2Dを取得
    }

    // Update is called once per frame
    void Update()
    {
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"EnemyController.Update:42\",\"message\":\"Update開始\",\"data\":{{\"isHijacked\":{isHijacked.ToString().ToLower()},\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")},\"hasParent\":{(transform.parent != null).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
        
        // 乗っ取り中は処理をスキップ
        if (isHijacked) return;

        GameObject player = GameObject.FindWithTag("Player");//プレイヤーオブジェクトの取得
        Vector2 thisPos = this.gameObject.transform.position;//オブジェクト自身の位置
        Vector2 playerPos = player.transform.position;//プレイヤーの位置

        float distance = CheckDistance(thisPos, playerPos);//プレイヤーまでの距離を算出

        if (distance < attackRadius)
        {
            if (!isAttacking)
            {
                StartCoroutine(AttackToTarget(playerPos));
            }
        }
        else if (distance < serchRadius)//距離が指定距離以下なら
        {
            if (!isAttacking)
            {
                // #region agent log
                try {
                    File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                        $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"EnemyController.Update:64\",\"message\":\"MoveToTarget呼び出し前\",\"data\":{{\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")},\"agentNull\":{(agent == null).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
                } catch {}
                // #endregion
                MoveToTarget(playerPos);
            }
        }
        else
        {
            StopMove();
        }
    }

    //引数1から引数2までの距離を測るメソッド
    float CheckDistance(Vector2 thisPos, Vector2 playerPos)
    {
        //2点間の距離を求める
        float distance = Vector2.Distance(thisPos, playerPos);
        return distance;
    }

    //ターゲットに向きを合わせる(2DなのでNavMeshAgentでは動かない)
    bool RotateToTarget(Vector3 targetPosition)
    {
        //ターゲットとこのオブジェクトの座標の距離をベクターで求める
        Vector3 direction = targetPosition - transform.position;
        //x方向の距離とy方向の距離を使って三角関数で間の角度を求める(度数に変換)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Z軸（2Dの回転軸）を回す
        Quaternion targetRotation = Quaternion.Euler(0, 0, angle + angleOffset);//何度回転すればいいかを求める
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);//turnSpeedの速度で目標の角度まで徐々に回転

        return Quaternion.Angle(transform.rotation, targetRotation) > 0.1f;
    }

    //スクリプトのオブジェクトの位置を引数の位置まで移動するメソッド
    void MoveToTarget(Vector3 targetPos)
    {
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"EnemyController.MoveToTarget:96\",\"message\":\"MoveToTarget開始\",\"data\":{{\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")},\"agentNull\":{(agent == null).ToString().ToLower()}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
        
        // NavMeshAgentが有効で、NavMesh上に配置されている場合のみSetDestinationを呼ぶ
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            // #region agent log
            try {
                File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                    $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"EnemyController.MoveToTarget:102\",\"message\":\"SetDestinationをスキップ\",\"data\":{{\"agentNull\":{(agent == null).ToString().ToLower()},\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
            } catch {}
            // #endregion
            return;
        }
        
        RotateToTarget(targetPos);
        // this.transform.position = Vector2.MoveTowards(this.transform.position, destination, moveSpeed*Time.deltaTime);
        agent.speed = moveSpeed;
        
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"A\",\"location\":\"EnemyController.MoveToTarget:115\",\"message\":\"SetDestination直前\",\"data\":{{\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
        
        agent.destination = targetPos;
    }

    //ターゲットまで直線移動するコルーチン
    IEnumerator DashStraightToTarget(Vector3 direction, float speed, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            transform.position += direction * speed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null; // 1フレーム待機
        }
    }

    IEnumerator AttackToTarget(Vector3 targetPos)
    {
        //0.ターゲットを検知したら開始
        Debug.Log("攻撃ループ開始");
        isAttacking = true;//攻撃中のフラッグをオン(重複したコルーチン開始の防止)
        agent.enabled = false;//NavMeshAgentを無効に
        Vector3 attackTargetPos = targetPos;//ターゲットの座標を固定
        Vector3 direction = (attackTargetPos - this.transform.position).normalized;//ターゲットまでの移動方向

        //1.プレイヤーの向きに回転する
        Debug.Log("回転");
        while (RotateToTarget(attackTargetPos))
        {
            yield return null;
        }

        //2.待機
        Debug.Log("待機");
        yield return new WaitForSeconds(waitTime);//待機の秒数まつ

        //3.一定時間突進攻撃
        Debug.Log("突進!!");
        yield return StartCoroutine(DashStraightToTarget(direction, dashSpeed, attackDuration));//突進のコルーチンの開始

        //4.クールタイム
        Debug.Log("クールタイム");
        yield return new WaitForSeconds(attackCoolDown);//クールタイムの秒数まつ
        
        // NavMesh上に最も近い位置を探す
        UnityEngine.AI.NavMeshHit hit;
        Vector3 currentPos = transform.position;
        float searchRadius = 5f; // 検索半径
        
        if (UnityEngine.AI.NavMesh.SamplePosition(currentPos, out hit, searchRadius, UnityEngine.AI.NavMesh.AllAreas))
        {
            // NavMesh上に近い位置が見つかった場合、その位置に移動
            transform.position = hit.position;
            agent.enabled = true;//NavMeshAgentを有効に
            // NavMesh上に強制的に再配置（Warpを使用）
            if (!agent.isOnNavMesh)
            {
                agent.Warp(hit.position);
            }
        }
        else
        {
            // NavMesh上に近い位置が見つからない場合、エージェントを有効化しない
            Debug.LogWarning($"攻撃終了後、NavMesh上に近い位置が見つかりませんでした。現在位置: {currentPos}");
            // エージェントを有効化しないまま、攻撃フラグをオフにする
        }
        
        isAttacking = false;//攻撃中のフラッグをオフ
        Debug.Log("攻撃ループ終了");
    }

    void StopMove()
    {
        agent.speed = 0f;
    }

    //プレイヤー追尾範囲をSceneビューに表示
    void OnDrawGizmos()
    {
        //探索範囲の表示
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(this.transform.position, serchRadius);

        //探索範囲の表示
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(this.transform.position, attackRadius);

        //敵の向きの表示(初期は上向き)
        float rayLength = 3.0f;
        Gizmos.color = Color.red;
        Vector3 currentPos = this.transform.position;
        Gizmos.DrawRay(currentPos, transform.up * rayLength);
    }



    /// <summary>
    /// 追跡動作を停止（乗っ取り時に呼ばれる）
    /// </summary>
    public void StopTracking()
    {
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\",\"location\":\"EnemyController.StopTracking:176\",\"message\":\"StopTracking開始\",\"data\":{{\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
        
        isHijacked = true;
        if (agent != null)
        {
            agent.enabled = false;
        }
        if (enemyRb != null)
        {
            enemyRb.simulated = false; // 物理シミュレーションを無効化
        }
        // Collider2Dを無効化（プレイヤー本体が次の敵に衝突できるようにする）
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
        StopAllCoroutines();
        isAttacking = false;
        
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"B\",\"location\":\"EnemyController.StopTracking:195\",\"message\":\"StopTracking終了\",\"data\":{{\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
    }

    /// <summary>
    /// 乗っ取り解除時に呼ばれる
    /// </summary>
    public void ReleaseEnemy()
    {
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\",\"location\":\"EnemyController.ReleaseEnemy:200\",\"message\":\"ReleaseEnemy開始\",\"data\":{{\"hasParent\":{(transform.parent != null).ToString().ToLower()},\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
        
        isHijacked = false;
        transform.SetParent(null);
        
        if (agent != null)
        {
            agent.enabled = true;
            // NavMesh上に強制的に再配置（Warpを使用）
            if (!agent.isOnNavMesh)
            {
                agent.Warp(transform.position);
            }
        }
        if (enemyRb != null)
        {
            enemyRb.simulated = true; // 物理シミュレーションを再有効化
        }
        // Collider2Dを再有効化
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = true;
        }
        
        // #region agent log
        try {
            File.AppendAllText("/Users/ryoma/Desktop/17thGame/.cursor/debug.log", 
                $"{{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"C\",\"location\":\"EnemyController.ReleaseEnemy:225\",\"message\":\"ReleaseEnemy終了\",\"data\":{{\"hasParent\":{(transform.parent != null).ToString().ToLower()},\"agentEnabled\":{(agent != null ? agent.enabled.ToString().ToLower() : "null")},\"isOnNavMesh\":{(agent != null ? agent.isOnNavMesh.ToString().ToLower() : "null")}}},\"timestamp\":{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}}}\n");
        } catch {}
        // #endregion
    }
}
