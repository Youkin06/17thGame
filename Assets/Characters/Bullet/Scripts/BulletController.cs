using UnityEngine;

public class BulletController : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] float ignoreTime = 0.1f; // 発射直後の自己ヒット防止

    private Rigidbody2D rb;
    private float spawnTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnTime = Time.time;
        if (rb != null)
        {
            rb.gravityScale = 0f;
        }
    }

    /// <summary>
    /// 発射方向を設定して速度を適用する。ShooterEnemyController から呼ぶ。
    /// </summary>
    public void Fire(Vector2 direction)
    {
        if (rb != null)
        {
            rb.velocity = direction.normalized * speed;
        }
        else
        {
            var rbAdd = GetComponent<Rigidbody2D>();
            if (rbAdd != null)
                rbAdd.velocity = direction.normalized * speed;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (Time.time - spawnTime < ignoreTime) return;

        if (other.CompareTag("Player"))
        {
            // ダメージ処理は別 issue で実装予定のため、ここでは Destroy のみ
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Time.time - spawnTime < ignoreTime) return;
        Destroy(gameObject);
    }
}
