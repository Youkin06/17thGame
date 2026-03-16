using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// タイルマップ上の壊せる壁を管理するスクリプト。
/// Tilemap オブジェクトにアタッチして使用する。
/// 
/// 【使い方】
/// 1. Grid > Tilemap オブジェクトを作成
/// 2. Tilemap に TilemapCollider2D を追加
/// 3. このスクリプトを Tilemap オブジェクトにアタッチ
/// 4. brokenWallTile に壊せる壁用のタイルアセットを設定
/// 5. タイルパレットから壊せる壁タイルをペイント配置
/// </summary>
[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapCollider2D))]
public class BrokenWallTilemap : MonoBehaviour
{
    [Header("壊せる壁タイル設定")]
    [Tooltip("壊せる壁として扱うタイルアセット（これに一致するタイルのみ破壊対象）")]
    [SerializeField] private TileBase brokenWallTile;

    [Header("破壊エフェクト（オプション）")]
    [Tooltip("破壊時に生成するエフェクトプレハブ（未設定でもOK）")]
    [SerializeField] private GameObject destroyEffectPrefab;

    [Tooltip("破壊時に再生するSE（未設定でもOK）")]
    [SerializeField] private AudioClip destroySE;

    private Tilemap tilemap;

    void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // プレイヤー以外は無視
        if (!collision.gameObject.CompareTag("Player")) return;

        // プレイヤーのコンポーネントを取得
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        HijackSystemController hijackSystem = collision.gameObject.GetComponent<HijackSystemController>();

        if (playerController == null || hijackSystem == null) return;

        // ★ 壊す条件：乗っ取り状態（IsHijacking）かつ突進中（Dashing）
        if (playerController.currentState != PlayerMoveState.Dashing || !hijackSystem.IsHijacking())
        {
            return;
        }

        // 衝突ポイントからタイル座標を計算して破壊
        DestroyTilesAtContactPoints(collision);
    }

    /// <summary>
    /// 衝突ポイントからタイル座標を特定し、壊せるタイルであれば除去する
    /// </summary>
    private void DestroyTilesAtContactPoints(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // 衝突ポイントのワールド座標をタイルのセル座標に変換
            // 接触法線の逆方向に少しオフセットして、壁側のタイルを正確に取得
            Vector3 hitPosition = contact.point + contact.normal * -0.1f;
            Vector3Int cellPosition = tilemap.WorldToCell(hitPosition);

            // 該当セルのタイルを取得
            TileBase tile = tilemap.GetTile(cellPosition);

            // 壊せるタイルかチェック
            if (tile != null && (brokenWallTile == null || tile == brokenWallTile))
            {
                Debug.Log($"タイルマップの壊せる壁を破壊: セル座標 {cellPosition}");

                // エフェクト生成（設定されている場合）
                if (destroyEffectPrefab != null)
                {
                    Vector3 tileWorldPos = tilemap.GetCellCenterWorld(cellPosition);
                    Instantiate(destroyEffectPrefab, tileWorldPos, Quaternion.identity);
                }

                // SE再生（設定されている場合）
                if (destroySE != null)
                {
                    AudioSource.PlayClipAtPoint(destroySE, contact.point);
                }

                // タイルを除去
                tilemap.SetTile(cellPosition, null);
            }
        }
    }
}
