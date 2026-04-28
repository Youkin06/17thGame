using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class BrokenWallController : MonoBehaviour
{
    private Tilemap _tilemap;
    
    [Header("設定")]
    [SerializeField] private TileBase targetCrystalTile; // 壊したいタイルのアセット
    [SerializeField] private int breakRadius = 1;       // プレイヤーの周囲何マス分をチェックするか
    [Header("エフェクト")]
    [SerializeField] private GameObject wallBrokenEffectPrefab; // 壊れた壁のエフェクトprefab

    void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. 相手がPlayerControllerを持っているか確認
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();

        // 2. プレイヤーの状態チェック（乗っ取り中 ＋ 突進中）
        if (player != null && player.isHijacking && player.currentState == PlayerMoveState.Dashing)
        {
            Debug.Log("条件合致：プレイヤー周辺のタイルをスキャンします");

            // 3. 衝突時のプレイヤーのワールド座標をタイルマップのセル座標に変換
            Vector3Int playerCellPos = _tilemap.WorldToCell(player.transform.position);

            // 4. プレイヤーの周囲（breakRadiusの範囲）をループでチェック
            for (int x = -breakRadius; x <= breakRadius; x++)
            {
                for (int y = -breakRadius; y <= breakRadius; y++)
                {
                    Vector3Int targetPos = new Vector3Int(playerCellPos.x + x, playerCellPos.y + y, playerCellPos.z);

                    // その座標にあるタイルを取得
                    TileBase hitTile = _tilemap.GetTile(targetPos);

                    // 5. 指定したCrystalタイルであれば消去
                    if (hitTile != null && hitTile == targetCrystalTile)
                    {
                        _tilemap.SetTile(targetPos, null);

                        // エフェクト生成&再生
                        Instantiate(wallBrokenEffectPrefab, _tilemap.CellToWorld(targetPos) + _tilemap.tileAnchor, Quaternion.identity);

                        Debug.Log($"タイル破壊成功: {targetPos}");
                    }
                }
            }
        }
    }
}