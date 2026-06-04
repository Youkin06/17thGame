using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class ThroughWallController : MonoBehaviour
{
    [SerializeField] private TileBase throughTile; // 通り抜けられるタイルのアセット
    [SerializeField] private GameObject throughEffect; //通り抜ける時に再生するエフェクトプレハブ
    private WallThoughEffectController wallThoughEffectController; //エフェクト制御用スクリプト

    private Tilemap _tilemap;
    private Collider2D wallCollider; // 当たり判定用のCollider(魂状態で無効)
    private Collider2D triggerCollider; // 通り抜け判定用のCollider
    private Collider2D[] playerColliders; // プレイヤーの全コライダー（複数ある場合への対策）
    private HijackSystemController hijackSystem; // 乗っ取り状態判定用

    void Start()
    {
        _tilemap = transform.parent.GetComponent<Tilemap>();// 親オブジェクトについている本物のTileMap
        Tilemap myTilemap = GetComponent<Tilemap>();//Triggerを使う際のColliderを作るためのTileMap

        if (_tilemap != null)
        {
            // 親の全タイルを自分にコピーする
            CopyTiles(_tilemap, myTilemap);
        }

        wallCollider = transform.parent.GetComponent<Collider2D>();
        triggerCollider = GetComponent<Collider2D>();

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

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 相手がPlayerControllerを持っているか確認
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        // 2. プレイヤーの状態チェック（魂状態）
        if (player != null && !hijackSystem.IsHijacking())
        {
            Debug.Log("魂が壁に触れました");
            // 3. 相手のコライダーの「現在位置」から、自分のコライダー上で一番近い点を計算
            // triggerCollider は自身に付いている isTrigger = true の TilemapCollider2D
            Vector3 hitPoint = triggerCollider.ClosestPoint(other.transform.position);
            Vector3 direction = (hitPoint - other.transform.position).normalized;
            Vector3 checkPos = hitPoint + (direction * 0.1f);

            // 4. その座標をタイルマップの「セル座標(Vector3Int)」に変換
            Vector3Int cellPosition = _tilemap.WorldToCell(checkPos);

            // 5. セル座標を使ってタイルを取得
            TileBase hitTile = _tilemap.GetTile(cellPosition);

            // 6. 指定したthroughWallタイルであればエフェクト再生
            if (hitTile != null && hitTile == throughTile)
            {
                //生成位置の調整
                Vector3 spawnPos = hitPoint;
                spawnPos.z = -1.0f;

                //エフェクトの生成 & 再生開始(生成時に自動再生)
                wallThoughEffectController = other.GetComponentInChildren<WallThoughEffectController>();
                wallThoughEffectController.EnterEffect();
                Debug.Log($"タイル通り抜け成功: {hitPoint}");
                
            }

        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        //エフェクト再生処理を入れる
        // 1. 相手がPlayerControllerを持っているか確認
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        // 2. プレイヤーの状態チェック（魂状態）
        if (player != null && !hijackSystem.IsHijacking())
        {
            // 3. 相手のコライダーの「現在位置」から、自分のコライダー上で一番近い点を計算
            // triggerCollider は自身に付いている isTrigger = true の TilemapCollider2D
            Vector3 hitPoint = triggerCollider.ClosestPoint(other.transform.position);
            Vector3 direction = (hitPoint - other.transform.position).normalized;
            Vector3 checkPos = hitPoint + (direction * 0.1f);

            // 4. その座標をタイルマップの「セル座標(Vector3Int)」に変換
            Vector3Int cellPosition = _tilemap.WorldToCell(checkPos);

            // 5. セル座標を使ってタイルを取得
            TileBase hitTile = _tilemap.GetTile(cellPosition);

            // 6. 指定したthroughTileであればエフェクトを移動
            if (hitTile != null && hitTile == throughTile && wallThoughEffectController != null)
            {
                //エフェクトの停止
                wallThoughEffectController.ExitEffect();
                Debug.Log("Stop Effect");
            }
        }
    }

    //タイル複製用メソッド
    void CopyTiles(Tilemap source, Tilemap target)
    {
        target.ClearAllTiles();
        foreach (var pos in source.cellBounds.allPositionsWithin)
        {
            TileBase tile = source.GetTile(pos);
            if (tile != null) target.SetTile(pos, tile);
        }
    }
}