using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class JoystickMove : MonoBehaviour
{
    public float acceleration = 5f; // 加速力
    public float maxSpeed = 10f; // 最大速度
    public float turnSpeed = 10f; // 旋回速度（値が大きいほどキビキビ曲がる）

    public float dashMultiplier = 1.5f;

    private bool isMaxSpeed = false;
    private float defaultDashDuration = 0.2f;
    private float dashDuration = 0f;
    public DynamicJoystick dynamicJoystick;
    public Rigidbody2D rb;

    void FixedUpdate()
    {
        Vector2 input = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);

        if (input.sqrMagnitude > 0.01f)
        {
            rb.drag = 0.5f;

            // 1. 入力方向（行きたい方向）
            Vector2 targetDir = input.normalized;

            // 2. 現在の速度（大きさ）を取得
            float currentSpeed = rb.velocity.magnitude;

            // 3. 速度の「大きさ」だけを計算（方向転換中でも加速させる）
            //    現在の速度に加速分を足す。accelerationは1秒あたりの速度増加量（線形加速）
            float nextSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed);

            // もし停止状態からなら、最低限の動き出し速度を保証（オプション）
            if (nextSpeed < 1f) nextSpeed = 1f;

            // 4. 「向き」だけを滑らかに変える
            //    LerpではなくRotateTowardsを使うことで、ベクトルの長さを保ったまま回転させる
            //    Vector2はVector3として扱えるため、Vector3.RotateTowardsを使用
            Vector2 currentDir = rb.velocity.normalized;
            if (currentDir == Vector2.zero) currentDir = targetDir; // 停止時は入力方向を現在地とする

            Vector2 newDir = Vector3.RotateTowards(currentDir, targetDir, turnSpeed * Time.fixedDeltaTime, 0f);
            newDir.Normalize(); // 念のため正規化

            // 5. 新しい向き × 計算した速度 を適用
            rb.velocity = newDir * nextSpeed;

            // 6. 見た目（画像の回転）
            float angle = Mathf.Atan2(newDir.y, newDir.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0, 0, angle),
                720 * Time.deltaTime
            );
            if (nextSpeed >= maxSpeed)
            {
                isMaxSpeed = true;
                dashDuration = defaultDashDuration;
                Debug.Log("最高速度に到達");

            }
        }
        else
        {
            if (isMaxSpeed&&dashDuration>0)
            {
            
                rb.velocity = rb.velocity.normalized * maxSpeed* dashMultiplier;
                dashDuration-=Time.fixedDeltaTime;
            }
            else{
                dashDuration = defaultDashDuration;
                isMaxSpeed = false;
                rb.drag = 7.5f;
                rb.angularDrag = 7.5f;
            }
        }
    }

    public Vector2 GetJoystickInput(DynamicJoystick dynamicJoystick)
    {
        Vector2 direction = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);
        return direction.normalized;
    }

    public void OnCollisionEnter2D(Collision2D collision){
        if(collision.gameObject.tag == "Enemy" && isMaxSpeed&&dashDuration>0){
            Debug.Log("ダッシュ状態でEnemyに衝突した");
        }
    }
}