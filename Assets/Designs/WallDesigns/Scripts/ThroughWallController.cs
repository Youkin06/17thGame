using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class ThroughWallController : MonoBehaviour
{
    private Collider2D wallCollider;
    private Collider2D[] playerColliders; // プレイヤーの全コライダー（複数ある場合への対策）
    private HijackSystemController hijackSystem; // 乗っ取り状態判定用

    [Header("設定")]
    [SerializeField] private TileBase targetThroughTile;  //すり抜けるタイルのアセット

    [Header("エフェクト")]
    [SerializeField] private GameObject wallThroughEffctPrefab;

    

    void Start()
    {
        wallCollider = GetComponent<Collider2D>();

        // シーン内のプレイヤーをタグで探して取得
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // プレイヤーに付いている全てのCollider2Dを取得
            // （hitBoxColliderなど、複数の当たり判定が引っかかるのを防ぐため）
            playerColliders = player.GetComponents<Collider2D>();

            // 乗っ取り状態を管理しているコンポーネントを直接取得（PlayerControllerを経由しない）
            hijackSystem = player.GetComponent<HijackSystemController>();
        }
        else
        {
            Debug.LogWarning("SoulWall: Playerタグが付いたオブジェクトが見つかりません！");
        }
    }

    void Update()
    {
        // プレイヤーやコンポーネントが見つかっていない場合は処理しない
        if (playerColliders == null || playerColliders.Length == 0 || hijackSystem == null)
            return;

        // IsHijacking() が false なら「魂状態（乗っ取っていない状態）」
        bool isSoul = !hijackSystem.IsHijacking();

        // プレイヤーの全てのコライダーに対して、衝突無視のオン/オフを設定
        foreach (var pCollider in playerColliders)
        {
            // isSoul が true なら衝突無視（すり抜け）、false なら衝突有効（ぶつかる）
            Physics2D.IgnoreCollision(wallCollider, pCollider, isSoul);
        }
    }

    
}