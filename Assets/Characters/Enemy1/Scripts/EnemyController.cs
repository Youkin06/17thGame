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
    private float moveSpeed;//移動するスピード
    private bool isAttacking;

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
    }

    // Update is called once per frame
    void Update()
    {
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
        RotateToTarget(targetPos);
        // this.transform.position = Vector2.MoveTowards(this.transform.position, destination, moveSpeed*Time.deltaTime);
        agent.speed = moveSpeed;
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
        agent.enabled = true;//NavMeshAgentを有効に
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

    //衝突判定
    void OnCollisionEnter2D(Collision2D collision2D)
    {
        if (collision2D.gameObject.tag == "Player")
        {//プレイヤーと衝突したら
            Debug.Log("プレイヤーと衝突");
            Destroy(this.gameObject);//このオブジェクト自身を消す
        }
    }
}
