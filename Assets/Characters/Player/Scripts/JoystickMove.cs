using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum PlayerMoveState
{
    Idle,           // 停止中
    Accelerating,   // 加速中（最高速度未到達）
    MaxSpeed,       // 最高速度到達（入力あり）
    Dashing         // 突進中（入力なし、最高速度維持）
}

public class JoystickMove : MonoBehaviour
{
    public float acceleration = 5f; // 加速力
    public float maxSpeed = 10f; // 最大速度
    public float turnSpeed = 10f; // 旋回速度（値が大きいほどキビキビ曲がる）

    public float dashMultiplier = 1.5f;

    private PlayerMoveState currentState = PlayerMoveState.Idle;
    private PlayerMoveState previousState = PlayerMoveState.Idle;
    private float defaultDashDuration = 0.2f;
    private float dashDuration = 0f;
    public DynamicJoystick dynamicJoystick;
    public Rigidbody2D rb;

    void FixedUpdate()
    {
        Vector2 input = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);
        bool hasInput = input.sqrMagnitude > 0.01f;

        // 状態遷移の処理
        UpdateState(hasInput);

        // 状態に応じた処理
        switch (currentState)
        {
            case PlayerMoveState.Idle:
                HandleIdle();
                break;

            case PlayerMoveState.Accelerating:
                HandleAccelerating(input);
                break;

            case PlayerMoveState.MaxSpeed:
                HandleMaxSpeed(input);
                break;

            case PlayerMoveState.Dashing:
                HandleDashing();
                break;
        }
    }

    private void UpdateState(bool hasInput)
    {
        previousState = currentState;

        switch (currentState)
        {
            case PlayerMoveState.Idle:
                if (hasInput)
                {
                    currentState = PlayerMoveState.Accelerating;
                }
                break;

            case PlayerMoveState.Accelerating:
                if (!hasInput)
                {
                    currentState = PlayerMoveState.Idle;
                }
                break;

            case PlayerMoveState.MaxSpeed:
                if (!hasInput)
                {
                    currentState = PlayerMoveState.Dashing;
                }
                break;

            case PlayerMoveState.Dashing:
                dashDuration -= Time.fixedDeltaTime;
                if (dashDuration <= 0)
                {
                    currentState = PlayerMoveState.Idle;
                }
                // 突進中は操作不能のため、入力による状態遷移を無視
                break;
        }

        // 状態が変わったときにログを出力
        if (previousState != currentState)
        {
            Debug.Log($"状態遷移: {previousState} -> {currentState}");
        }
    }

    private void HandleIdle()
    {
        rb.drag = 7.5f;
        rb.angularDrag = 7.5f;
    }

    private void HandleAccelerating(Vector2 input)
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
        UpdateRotation(newDir);

        // 最高速度到達チェック
        if (nextSpeed >= maxSpeed)
        {
            previousState = currentState;
            currentState = PlayerMoveState.MaxSpeed;
            dashDuration = defaultDashDuration;
            Debug.Log($"状態遷移: {previousState} -> {currentState} (最高速度に到達)");
        }
    }

    private void HandleMaxSpeed(Vector2 input)
    {
        rb.drag = 0.5f;

        // 1. 入力方向（行きたい方向）
        Vector2 targetDir = input.normalized;

        // 2. 現在の速度（大きさ）を取得
        float currentSpeed = rb.velocity.magnitude;

        // 3. 最高速度を維持
        float nextSpeed = maxSpeed;

        // 4. 「向き」だけを滑らかに変える
        Vector2 currentDir = rb.velocity.normalized;
        if (currentDir == Vector2.zero) currentDir = targetDir;

        Vector2 newDir = Vector3.RotateTowards(currentDir, targetDir, turnSpeed * Time.fixedDeltaTime, 0f);
        newDir.Normalize();

        // 5. 新しい向き × 最高速度 を適用
        rb.velocity = newDir * nextSpeed;

        // 6. 見た目（画像の回転）
        UpdateRotation(newDir);
    }

    private void HandleDashing()
    {
        rb.drag = 0.5f;

        // 突進中は最高速度を維持
        if (rb.velocity.magnitude > 0.01f)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed * dashMultiplier;
        }
    }

    private void UpdateRotation(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Euler(0, 0, angle),
            720 * Time.deltaTime
        );
    }

    public Vector2 GetJoystickInput(DynamicJoystick dynamicJoystick)
    {
        Vector2 direction = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);
        return direction.normalized;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Enemy" && currentState == PlayerMoveState.Dashing)
        {
            Debug.Log("ダッシュ状態でEnemyに衝突した");
        }
    }
}