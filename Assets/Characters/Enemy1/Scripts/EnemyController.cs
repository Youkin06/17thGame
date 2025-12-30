using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

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
    
    // 徘徊関連のパラメータ
    [SerializeField] float patrolMinX = -8f;//徘徊範囲X最小値
    [SerializeField] float patrolMaxX = 8f;//徘徊範囲X最大値
    [SerializeField] float patrolMinY = -4f;//徘徊範囲Y最小値
    [SerializeField] float patrolMaxY = 4f;//徘徊範囲Y最大値
    [SerializeField] float patrolWaitTimeMin = 0.5f;//徘徊時の待機時間(最小)
    [SerializeField] float patrolWaitTimeMax = 1.5f;//徘徊時の待機時間(最大)
    [SerializeField] float patrolSpeed = 3f;//徘徊時の移動速度
    [SerializeField] float arrivalDistance = 0.5f;//目的地到着判定の距離
    
    private float moveSpeed;//移動するスピード
    private bool isAttacking;
    private bool isHijacked = false; // 乗っ取り中フラグ
    private bool isPatrolling = false; // 徘徊中フラグ
    private Vector3 patrolTarget; // 現在の徘徊目標地点
    private Rigidbody2D enemyRb; // Rigidbody2Dへの参照
    private Vector3 startPosition; // 初期位置（徘徊の中心点）
    private bool useNavMesh = false; // NavMeshを使用するかどうか

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
        
        // NavMeshAgentがある場合のみ使用
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            useNavMesh = true;
            agent.updateRotation = false;//2DなのでNavMeshAgentの自動回転はオフにする
            agent.updateUpAxis = false;//2DなのでNavMeshAgentの立ち上がりはオフにする
        }
        
        enemyRb = GetComponent<Rigidbody2D>(); // Rigidbody2Dを取得
        
        startPosition = transform.position; // 初期位置を記録
        StartCoroutine(PatrolRoutine()); // 徘徊ルーチンを開始
    }

    // Update is called once per frame
    void Update()
    {
        // 乗っ取り中は処理をスキップ
        if (isHijacked) return;

        GameObject player = GameObject.FindWithTag("Player");//プレイヤーオブジェクトの取得
        if (player == null) return; // プレイヤーが見つからない場合は処理をスキップ
        
        Vector2 thisPos = this.gameObject.transform.position;//オブジェクト自身の位置
        Vector2 playerPos = player.transform.position;//プレイヤーの位置

        float distance = CheckDistance(thisPos, playerPos);//プレイヤーまでの距離を算出

        if (distance < attackRadius)
        {
            // プレイヤー発見：徘徊を停止して攻撃モードへ
            if (isPatrolling)
            {
                StopCoroutine(PatrolRoutine());
                isPatrolling = false;
            }
            
            if (!isAttacking)
            {
                StartCoroutine(AttackToTarget(playerPos));
            }
        }
        else if (distance < serchRadius)//距離が指定距離以下なら
        {
            // プレイヤー発見：徘徊を停止して追尾モードへ
            if (isPatrolling)
            {
                StopCoroutine(PatrolRoutine());
                isPatrolling = false;
            }
            
            if (!isAttacking)
            {
                MoveToTarget(playerPos);
            }
        }
        else
        {
            // プレイヤーが索敵範囲外：徘徊モードへ
            // 徘徊中でない場合のみ開始（重複起動防止）
            if (!isPatrolling && !isAttacking)
            {
                StartCoroutine(PatrolRoutine());
            }
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
        if (useNavMesh)
        {
            // NavMeshAgentが有効で、NavMesh上に配置されている場合のみSetDestinationを呼ぶ
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            {
                return;
            }
            
            RotateToTarget(targetPos);
            agent.speed = moveSpeed;
            agent.destination = targetPos;
        }
        else
        {
            // NavMeshを使わない移動
            RotateToTarget(targetPos);
            transform.position = Vector2.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
        }
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
        
        if (useNavMesh && agent != null)
        {
            agent.enabled = false;//NavMeshAgentを無効に
        }
        
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
        
        if (useNavMesh && agent != null)
        {
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
            }
        }
        
        isAttacking = false;//攻撃中のフラッグをオフ
        Debug.Log("攻撃ループ終了");
    }

    /// <summary>
    /// 徘徊ルーチン：ランダムな位置へ移動 → 待機 → 再移動を繰り返す
    /// </summary>
    IEnumerator PatrolRoutine()
    {
        isPatrolling = true;
        Debug.Log("徘徊ルーチン開始");
        
        while (true)
        {
            // 乗っ取り中や攻撃中は徘徊しない
            if (isHijacked || isAttacking)
            {
                isPatrolling = false;
                yield break;
            }
            
            // ランダムな目的地を生成
            patrolTarget = GetRandomPatrolPoint();
            
            Debug.Log($"徘徊目的地: {patrolTarget}");
            
            if (useNavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
            {
                // NavMeshを使った移動
                agent.speed = patrolSpeed;
                agent.destination = patrolTarget;
                
                // 目的地に到着するまで待機
                while (agent.pathPending || agent.remainingDistance > arrivalDistance)
                {
                    // プレイヤーを発見したら徘徊を中断
                    if (CheckPlayerInRange())
                    {
                        Debug.Log("プレイヤー発見、徘徊中断");
                        isPatrolling = false;
                        yield break;
                    }
                    
                    if (agent.hasPath)
                    {
                        RotateToTarget(patrolTarget);
                    }
                    
                    yield return null;
                }
            }
            else
            {
                // NavMeshを使わない移動
                while (Vector2.Distance(transform.position, patrolTarget) > arrivalDistance)
                {
                    // プレイヤーを発見したら徘徊を中断
                    if (CheckPlayerInRange())
                    {
                        Debug.Log("プレイヤー発見、徘徊中断");
                        isPatrolling = false;
                        yield break;
                    }
                    
                    // 目的地に向かって回転
                    RotateToTarget(patrolTarget);
                    
                    // 目的地に向かって移動
                    transform.position = Vector2.MoveTowards(
                        transform.position, 
                        patrolTarget, 
                        patrolSpeed * Time.deltaTime
                    );
                    
                    yield return null;
                }
            }
            
            Debug.Log("目的地到着、待機開始");
            
            // 到着後、ランダムな時間待機
            float waitDuration = Random.Range(patrolWaitTimeMin, patrolWaitTimeMax);
            Debug.Log($"{waitDuration}秒待機");
            yield return new WaitForSeconds(waitDuration);
            
            Debug.Log("待機終了、次の目的地へ");
        }
    }
    
    /// <summary>
    /// プレイヤーが索敵範囲内にいるかチェック
    /// </summary>
    bool CheckPlayerInRange()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            return distance < serchRadius;
        }
        return false;
    }
    
    /// <summary>
    /// 徘徊範囲内のランダムな地点を取得
    /// </summary>
    Vector3 GetRandomPatrolPoint()
    {
        // 指定範囲内のランダムな座標を生成
        float randomX = Random.Range(patrolMinX, patrolMaxX);
        float randomY = Random.Range(patrolMinY, patrolMaxY);
        Vector3 randomPoint = new Vector3(randomX, randomY, 0);
        
        return randomPoint;
    }

    void StopMove()
    {
        if (useNavMesh && agent != null)
        {
            agent.speed = 0f;
        }
    }

    //プレイヤー追尾範囲をSceneビューに表示
    void OnDrawGizmos()
    {
        //探索範囲の表示
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(this.transform.position, serchRadius);

        //攻撃範囲の表示
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(this.transform.position, attackRadius);

        //徘徊範囲の表示（矩形）
        Gizmos.color = Color.yellow;
        Vector3 bottomLeft = new Vector3(patrolMinX, patrolMinY, 0);
        Vector3 bottomRight = new Vector3(patrolMaxX, patrolMinY, 0);
        Vector3 topRight = new Vector3(patrolMaxX, patrolMaxY, 0);
        Vector3 topLeft = new Vector3(patrolMinX, patrolMaxY, 0);
        
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        //徘徊目標地点の表示
        if (Application.isPlaying && isPatrolling)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(patrolTarget, 0.3f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }

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
        isHijacked = true;
        isPatrolling = false;
        
        if (useNavMesh && agent != null)
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
    }

    /// <summary>
    /// 乗っ取り解除時に呼ばれる
    /// </summary>
    public void ReleaseEnemy()
    {
        isHijacked = false;
        transform.SetParent(null);
        
        if (useNavMesh && agent != null)
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
        
        // 徘徊ルーチンを再開
        StartCoroutine(PatrolRoutine());
    }
}