using System.Collections;
using UnityEngine;

public class ShooterEnemyController : BaseEnemyController
{
    [SerializeField] float fireInterval = 1f;
    [SerializeField] GameObject bulletPrefab;

    private Coroutine fireCoroutine;

    protected override void Start()
    {
        base.Start();
        fireCoroutine = StartCoroutine(FireBulletRoutine());
    }

    protected override void UpdateEnemyBehavior(float distance, Vector2 playerPos)
    {
        Vector3 playerPos3 = new Vector3(playerPos.x, playerPos.y, 0f);

        if (distance < serchRadius)
        {
            RotateToTarget(playerPos3);
        }
        // 範囲外: 回転停止（弾は FireBulletRoutine で撃ち続ける）
    }

    protected override void UpdateWhenHijacked()
    {
        // 乗っ取り中も弾を撃ち続ける（FireBulletRoutine が動いている）
        // 親の向き（プレイヤーの向き）で撃つため、transform.up は既に正しい
    }

    IEnumerator FireBulletRoutine()
    {
        while (true)
        {
            if (bulletPrefab != null)
            {
                FireBullet();
            }
            yield return new WaitForSeconds(fireInterval);
        }
    }

    void FireBullet()
    {
        Vector3 spawnPos = transform.position + (Vector3)(Vector2)transform.up * 0.5f;
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, transform.rotation);
        var bullet = bulletObj.GetComponent<BulletController>();
        if (bullet != null)
        {
            bullet.Fire((Vector2)transform.up);
        }
    }

    public override void StopTracking()
    {
        base.StopTracking(); // StopAllCoroutines で fireCoroutine も停止する
        // 乗っ取り中も弾を撃ち続けるため、発射コルーチンを再開
        fireCoroutine = StartCoroutine(FireBulletRoutine());
    }
}
