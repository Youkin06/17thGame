using UnityEngine;

/// <summary>
/// 壊せる壁のコントローラー。
/// プレイヤーが「乗っ取り状態」かつ「突進中（Dashing）」で衝突した場合のみ壁が壊れる。
/// プレハブとして使用する場合はこのスクリプトを直接アタッチする。
/// タイルマップから使用する場合は BrokenWallTilemap 経由で呼ばれる。
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class BrokenWallController : MonoBehaviour
{
    [Header("破壊エフェクト（オプション）")]
    [Tooltip("破壊時に生成するエフェクトプレハブ（未設定でもOK）")]
    [SerializeField] private GameObject destroyEffectPrefab;

    [Tooltip("破壊時に再生するSE（未設定でもOK）")]
    [SerializeField] private AudioClip destroySE;

    /// <summary>
    /// 壁が破壊されたときに呼ばれるイベント。
    /// BrokenWallTilemap がリスンして、対応するタイルを除去するために使用。
    /// </summary>
    public System.Action OnWallDestroyed;

    private bool isDestroyed = false;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;

        // プレイヤー以外は無視
        if (!collision.gameObject.CompareTag("Player")) return;

        // プレイヤーのコンポーネントを取得
        PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
        HijackSystemController hijackSystem = collision.gameObject.GetComponent<HijackSystemController>();

        if (playerController == null || hijackSystem == null) return;

        // ★ 壊す条件：乗っ取り状態（IsHijacking）かつ突進中（Dashing）
        if (playerController.currentState == PlayerMoveState.Dashing && hijackSystem.IsHijacking())
        {
            DestroyWall();
        }
    }

    /// <summary>
    /// 壁を破壊する処理
    /// </summary>
    private void DestroyWall()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        Debug.Log($"壊せる壁が破壊されました: {gameObject.name}");

        // 破壊エフェクトの生成（設定されている場合）
        if (destroyEffectPrefab != null)
        {
            Instantiate(destroyEffectPrefab, transform.position, Quaternion.identity);
        }

        // 破壊SEの再生（設定されている場合）
        if (destroySE != null)
        {
            AudioSource.PlayClipAtPoint(destroySE, transform.position);
        }

        // イベント通知（タイルマップ連携用）
        OnWallDestroyed?.Invoke();

        // オブジェクトを破壊
        Destroy(gameObject);
    }
}
