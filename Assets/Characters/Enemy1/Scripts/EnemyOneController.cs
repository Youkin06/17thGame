using System.Collections;
using System.Collections.Generic;
// using System.Numerics;
using UnityEngine;

public class EnemyOneController : MonoBehaviour
{
    [SerializeField]float serchRadius = 5.0f;
    [SerializeField]float moveSpeed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        
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
            MoveToPlayer(playerPos);//プレイヤーの位置まで移動
        }
    }

    //引数1から引数2までの距離を測るメソッド
    float CheckDistance(Vector2 thisPos,Vector2 playerPos)
    {
        float distance = Vector2.Distance(thisPos,playerPos);
        return distance;
    }

    //スクリプトのオブジェクトの位置を引数の位置まで移動するメソッド
    void MoveToPlayer(Vector2 destination)
    {
        this.transform.position = Vector2.MoveTowards(this.transform.position, destination, moveSpeed*Time.deltaTime);
    }

    //衝突判定
    void OnCollisionEnter2D(Collision2D collision2D){
        if(collision2D.gameObject.tag == "Player"){//プレイヤーと衝突したら
            Destroy(this.gameObject);//このオブジェクト自身を消す
        }
    }
}
