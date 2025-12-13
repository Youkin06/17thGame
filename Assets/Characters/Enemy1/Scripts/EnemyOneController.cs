using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;
using UnityEngine.AI;

public class EnemyOneController : MonoBehaviour
{
    [SerializeField]float serchRadius = 5.0f;//移動を始める距離
    [SerializeField]float moveSpeed = 1.0f;//移動するスピード
    [SerializeField]float angleOffset = 270f;//回転の調整(初期の向き)
    NavMeshAgent agent;
    // Start is called before the first frame update
    void Start()
    {
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

        float distance = CheckDistance(thisPos,playerPos);//プレイヤーまでの距離を算出

        if (distance < serchRadius)//距離が指定距離以下なら
        {
            RotateToTarget(playerPos);//プレイヤーの方向を向かせる
            MoveToPlayer(playerPos);//プレイヤーの位置まで移動
        }
        else
        {
            StopMove();
        }
    }

    //引数1から引数2までの距離を測るメソッド
    float CheckDistance(Vector2 thisPos,Vector2 playerPos)
    {
        //2点間の距離を求める
        float distance = Vector2.Distance(thisPos,playerPos);
        return distance;
    }

    //ターゲットに向きを合わせる(2DなのでNavMeshAgentでは動かない)
    void RotateToTarget(Vector3 targetPosition)
    {
        //プレイヤーとこのオブジェクトの座標の距離をベクターで求める
        Vector3 direction = targetPosition - transform.position;
        //x方向の距離とy方向の距離を使って三角関数で間の角度を求める(度数に変換)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // Z軸（2Dの回転軸）を回す
        transform.rotation = Quaternion.Euler(0, 0, angle + angleOffset);
    }

    //スクリプトのオブジェクトの位置を引数の位置まで移動するメソッド
    void MoveToPlayer(Vector3 targetPos)
    {
        // this.transform.position = Vector2.MoveTowards(this.transform.position, destination, moveSpeed*Time.deltaTime);
        agent.speed=moveSpeed;
        agent.destination = targetPos;
    }

    void StopMove()
    {
        agent.speed=0f;
    }

    //プレイヤー追尾範囲をSceneビューに表示
    void OnDrawGizmos()
    {
        //探索範囲の表示
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(this.transform.position, serchRadius);

        //敵の向きの表示(初期は上向き)
        float rayLength =3.0f;
        Gizmos.color = Color.red;
        Vector3 currentPos = this.transform.position;
        Gizmos.DrawRay(currentPos, transform.up * rayLength);
    }

    //衝突判定
    void OnCollisionEnter2D(Collision2D collision2D){
        if(collision2D.gameObject.tag == "Player"){//プレイヤーと衝突したら
            Destroy(this.gameObject);//このオブジェクト自身を消す
        }
    }
}
