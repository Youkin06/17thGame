using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))] // 動作にはTilemapCollider2Dが必須です
public class ThroughWallController : MonoBehaviour
{
    [Header("すり抜け設定")]
    [SerializeField] private TileBase throughTile; // 通り抜けられるタイルのアセット
    [SerializeField] private GameObject throughEffect; // 通り抜ける時に再生するエフェクトプレハブ
    
    private GameObject existthroughEffect; // 生成済みのエフェクトオブジェクト
    private WallThoughEffectController wallThoughEffectController; // エフェクト制御用スクリプト

    private Tilemap _tilemap;
    private Collider2D wallCollider; // 自身のCollider2D
    private Collider2D[] playerColliders; // プレイヤーの全コライダー
    private HijackSystemController hijackSystem; // 乗っ取り状態判定用

    void Start()
    {
        // 自分のGameObjectにアタッチされているコンポーネントを取得
        _tilemap = GetComponent<Tilemap>();
        wallCollider = GetComponent<Collider2D>();

        // シーン内のプレイヤーをタグで探して取得
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // プレイヤーに付いている全てのCollider2Dを取得
            playerColliders = player.GetComponents<Collider2D>();

            // 乗っ取り状態を管理しているコンポーネントを取得
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
            if (wallCollider != null && pCollider != null) 
            {
                // isSoul が true なら物理的な衝突を無視（すり抜け）、false なら衝突有効（ぶつかる）
                Physics2D.IgnoreCollision(wallCollider, pCollider, isSoul);
            }
        }
    }

    // =========================================================
    // トリガーイベント
    // =========================================================

    void OnTriggerEnter2D(Collider2D other)
    {
        //Debug.Log("OnTriggerEnter2D: " + other.gameObject.name);
        ProcessEffect(other, "Enter");
    }

    void OnTriggerStay2D(Collider2D other)
    {
        //Debug.Log("OnTriggerStay2D: " + other.gameObject.name);
        ProcessEffect(other, "Stay");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        //Debug.Log("OnTriggerExit2D: " + other.gameObject.name);
        //ProcessEffect(other, "Exit");
    }

    /// <summary>
    /// エフェクトの処理をまとめた共通メソッド
    /// </summary>
    private void ProcessEffect(Collider2D other, string state)
    {

        // プレイヤーである場合のみ処理
        if (other.gameObject.name == "hitbox") // プレイヤーの当たり判定オブジェクトの名前を指定
        {
            //Debug.Log($"ProcessEffect: {state} - Player detected, checking tile...");
            Vector3 playerPos = other.transform.position;
            Vector3Int cellPosition = _tilemap.WorldToCell(playerPos);

            // セル座標を使ってタイルを取得


                if (state == "Enter")
                {
                    // すでにエフェクトが存在する場合は重複生成しない
                    if (existthroughEffect == null)
                    {
                        Vector3 spawnPos = playerPos;
                        spawnPos.z = -1.0f; // 手前に表示

                        // エフェクトの生成
                        existthroughEffect = Instantiate(throughEffect, spawnPos, Quaternion.identity);
                        wallThoughEffectController = existthroughEffect.GetComponent<WallThoughEffectController>();
                    }
                }
                else if (state == "Stay" && existthroughEffect != null)
                {
                    // プレイヤーの位置にエフェクトを追従させる
                    Vector3 movePos = playerPos;
                    movePos.z = -1.0f;
                    existthroughEffect.transform.position = movePos;
                }
                // else if (state == "Exit" && existthroughEffect != null && wallThoughEffectController != null)
                // {
                //     // エフェクトの削除
                //     wallThoughEffectController.DestroyObject();
                //     wallThoughEffectController = null;
                //     existthroughEffect = null;
                // }
            }        
    }
}