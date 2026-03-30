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

public class PlayerController : MonoBehaviour
{
    [Header("当たり判定")]
    public bool bigHitBox;
    [SerializeField] private Collider2D hitBoxCollider;
    [SerializeField] private Collider2D playerCollider;
    [SerializeField] private float aimAssistStrength = 20f;

    [Header("移動設定")]
    public float acceleration = 5f;
    public float playerMaxSpeed = 10f;
    private float maxSpeed;
    public float turnSpeed = 10f;
    public float dashMultiplier = 1.5f;

    [Header("状態管理")]
    private PlayerMoveState previousState = PlayerMoveState.Idle;
    public PlayerMoveState currentState { get; private set; } = PlayerMoveState.Idle;
    private float defaultDashDuration = 0.2f;
    private float dashDuration = 0f;
    public bool isHijacking { get; private set; } = false;

    [Header("参照コンポーネント")]
    public DynamicJoystick dynamicJoystick;
    public Rigidbody2D rb;
    [SerializeField] private HijackSystemController hijackSystemController;
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController baseAnimatorController;
    [SerializeField] private AnimationClip baseHijackClip;
    private AnimatorOverrideController hijackOverrideController;

    private static readonly int AnimIsMoving = Animator.StringToHash("isMoving");
    private static readonly int AnimAttackTrigger = Animator.StringToHash("attackTrigger");
    private static readonly int AnimIsHijacking = Animator.StringToHash("isHijacking");

    private float totalDistanceMoved = 0f;
    private Vector2 lastPosition;

    void Start()
    {
        playerCollider.enabled = true;

        hitBoxCollider.isTrigger = true;
        hitBoxCollider.enabled = true;

        lastPosition = rb.position;
        maxSpeed = playerMaxSpeed;

        if (hijackSystemController == null)
        {
            hijackSystemController = GetComponent<HijackSystemController>();
        }

        // ★ 追加: Animatorの自動取得
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // 乗っ取り用のControllerの初期化
        if (baseAnimatorController == null && animator != null)
            baseAnimatorController = animator.runtimeAnimatorController;

        hijackOverrideController = new AnimatorOverrideController(baseAnimatorController);
    }

    /// <summary>
    /// Dashing状態を終了する（HijackSystemControllerから呼ばれる）
    /// </summary>
    public void EndDashingState()
    {
        dashDuration = 0f;
    }

    /// <summary>
    /// 乗っ取り状態を設定（HijackSystemControllerから呼ばれる）。Animatorにも即時反映する。
    /// </summary>
    public void SetHijacking(bool value)
    {
        isHijacking = value;
        if (animator != null)
            animator.SetBool(AnimIsHijacking, isHijacking);
    }

    /// <summary>
    /// 累積移動距離を取得
    /// </summary>
    public float GetTotalDistanceMoved()
    {
        return totalDistanceMoved;
    }

    /// <summary>
    /// 移動距離をリセット
    /// </summary>
    public void ResetDistance()
    {
        totalDistanceMoved = 0f;
        lastPosition = rb.position;
    }

    /// <summary>
    /// 速度を設定するメソッド（HijackSystemControllerから呼ばれる）
    /// </summary>
    public void SetMaxSpeed(float speed)
    {
        maxSpeed = speed;
    }

    /// <summary>
    /// プレイヤーのデフォルト速度に戻す
    /// </summary>
    public void ResetMaxSpeed()
    {
        maxSpeed = playerMaxSpeed;
    }

    /// <summary>
    /// ダメージ受付の窓口。弾・ダッシュ敵などから呼ばれ、HijackSystemController に委譲する。
    /// </summary>
    public void OnPlayerDamaged()
    {
        if (hijackSystemController.isInvincible)
        {
            return;
        }
        else
        {
            if (hijackSystemController != null)
                hijackSystemController.OnPlayerDamaged();
            hijackSystemController.StartInvincible();
        }
    }

    void FixedUpdate()
    {
        Vector2 input = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);
        bool hasInput = input.sqrMagnitude > 0.01f;

        // 移動距離の計算
        Vector2 currentPosition = rb.position;
        float distanceThisFrame = Vector2.Distance(lastPosition, currentPosition);
        totalDistanceMoved += distanceThisFrame;
        lastPosition = currentPosition;

        // 移動があった場合、UIを更新

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
        // 状態遷移の処理
        UpdateState(hasInput);
    }

    /// <summary>
    /// 状態遷移を一元管理するメソッド
    /// </summary>
    /// <param name="newState">遷移先の状態</param>
    /// <param name="reason">状態遷移の理由（ログ出力用、オプション）</param>
    private void ChangeState(PlayerMoveState newState, string reason = "")
    {
        if (currentState == newState) return; // 同じ状態への遷移は無視

        previousState = currentState;
        currentState = newState;

        // 状態遷移のログ出力
        string logMessage = $"状態遷移: {previousState} -> {currentState}";
        if (!string.IsNullOrEmpty(reason))
        {
            logMessage += $" ({reason})";
        }
        Debug.Log(logMessage);

        UpdateAnimation();
    }

    /// <summary>
    /// 現在のStateに応じてAnimatorパラメータを更新
    /// </summary>
    private void UpdateAnimation()
    {
        if (animator == null) return;
        // 乗っ取り状態は currentState と独立して常に同期
        animator.SetBool(AnimIsHijacking, isHijacking);


        switch (currentState)
        {
            case PlayerMoveState.Idle:
                animator.SetBool(AnimIsMoving, false);
                break;

            case PlayerMoveState.Accelerating:
            case PlayerMoveState.MaxSpeed:
                animator.SetBool(AnimIsMoving, true);
                break;

            case PlayerMoveState.Dashing:
                // ★ 突進中もMoveアニメを維持（Attackは敵衝突時に発火）
                animator.SetBool(AnimIsMoving, true);
                break;
        }
    }

    private void UpdateState(bool hasInput)
    {
        switch (currentState)
        {
            case PlayerMoveState.Idle:
                if (hasInput)
                {
                    ChangeState(PlayerMoveState.Accelerating);
                }
                break;

            case PlayerMoveState.Accelerating:
                if (!hasInput)
                {
                    ChangeState(PlayerMoveState.Idle);
                }
                break;

            case PlayerMoveState.MaxSpeed:
                if (!hasInput)
                {
                    // Shooter 乗っ取り中は突進不可、Dasher または乗っ取り中でなければ突進可能
                    bool canDash = hijackSystemController == null || !hijackSystemController.IsHijacking()
                        || hijackSystemController.GetHijackedEnemyType() == EnemyType.Dasher;
                    if (canDash)
                        ChangeState(PlayerMoveState.Dashing);
                    else
                        ChangeState(PlayerMoveState.Idle);
                }
                break;

            case PlayerMoveState.Dashing:
                dashDuration -= Time.fixedDeltaTime;
                if (dashDuration <= 0)
                {
                    ChangeState(PlayerMoveState.Idle, "突進時間終了");
                }
                // 突進中は操作不能のため、入力による状態遷移を無視
                break;
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
            ChangeState(PlayerMoveState.MaxSpeed, "最高速度に到達");
            dashDuration = defaultDashDuration;
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
        // CompareTagにしました
        // 敵に当たった場合
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (collision.transform.IsChildOf(this.transform))
            {
                return;
            }
            // ダッシュ中（乗っ取り成功）
            if (currentState == PlayerMoveState.Dashing)
            {
                BaseEnemyController enemy = collision.gameObject.GetComponent<BaseEnemyController>();

                if (enemy == null)
                {
                    Debug.Log("ダッシュ状態でEnemy(scriptなし)に衝突した");
                    rb.velocity = Vector2.zero;

                    Vector2 input = new Vector2(dynamicJoystick.Horizontal, dynamicJoystick.Vertical);
                    bool hasInput = input.sqrMagnitude > 0.01f;

                    if (hasInput) ChangeState(PlayerMoveState.Accelerating, "Enemy衝突後、入力あり");
                    else ChangeState(PlayerMoveState.Idle, "Enemy衝突により中断");
                    return;
                }

                if (hijackSystemController != null)
                {
                    // ★ 追加: 憑依開始時にAttackアニメ発火
                    if (animator != null) animator.SetTrigger(AnimAttackTrigger);
                    hijackSystemController.TryHijackEnemy(collision, this);
                    return;
                }
            }

            // ダッシュ中ではない
            else
            {
                Debug.Log("敵本体に接触ダメージ");
                OnPlayerDamaged();
            }
        }

    }

    //乗っ取りの見た目適用
    public void ApplyHijackVisual(EnemyData enemyData)
    {
        if (animator == null || hijackOverrideController == null)
            return;

        if (enemyData == null || enemyData.hijackClip == null)
            return;

        hijackOverrideController[baseHijackClip.name] = enemyData.hijackClip;

        animator.runtimeAnimatorController = hijackOverrideController;
    }

    //乗っ取りの見た目解除
    public void ResetHijackVisual()
    {
        if (animator == null || baseAnimatorController == null)
            return;

        animator.runtimeAnimatorController = baseAnimatorController;
    }


    //##################################################
    private void ApplyAimSuction(Transform target)
    {
        Vector2 currentVelocity = rb.velocity;
        Vector2 currentPos = rb.position;
        Vector2 targetPos = target.position;

        Vector2 vectorToTarget = targetPos - currentPos;

        if (vectorToTarget.sqrMagnitude < 0.0001f || currentVelocity.sqrMagnitude < 0.0001f) return;

        // 【追加】敵が真後ろにいる場合は吸い付かない（事故防止）
        if (Vector2.Dot(currentVelocity.normalized, vectorToTarget.normalized) < 0) return;

        Vector2 velocityParallel = (Vector2)Vector3.Project(currentVelocity, vectorToTarget);
        Vector2 velocityPerpendicular = currentVelocity - velocityParallel;

        // ■ 修正ポイント2：normalized を外す！
        // normalizedすると、0.1mmのズレでも全力で修正してしまい、ガタガタ震えます。
        // 外すことで「ズレが大きいほど強く、小さいほど優しく」なり、ヌルっと吸い付きます。
        Vector2 correctionDir = -velocityPerpendicular; // .normalized を削除

        rb.AddForce(correctionDir * aimAssistStrength);

        Debug.DrawRay(currentPos, vectorToTarget, Color.green);
        Debug.DrawRay(currentPos, correctionDir * 2f, Color.red);
    }

    public void OnTriggerStay2D(Collider2D other)
    {
        if (bigHitBox) return; // bigHitBoxのときは吸い付き無効

        if (currentState == PlayerMoveState.Dashing && other.CompareTag("Enemy"))
        {
            ApplyAimSuction(other.transform);
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (!bigHitBox) return; // bigHitBoxのときだけ判定

        if (other.CompareTag("Enemy") && currentState == PlayerMoveState.Dashing)
        {
            BaseEnemyController enemy = other.GetComponent<BaseEnemyController>();

            // BaseEnemyControllerがない場合
            if (enemy == null)
            {
                // ■ 修正ポイント3：Triggerで「停止処理」はしない！
                // ここで止めてしまうと、「敵（または敵タグの壁）」の判定枠にかすった瞬間に
                // プレイヤーが空中で急停止してしまいます。
                // 物理的な停止は OnCollisionEnter2D（本体の衝突）に任せましょう。
                return;
            }

            if (hijackSystemController != null)
            {
                // ★ 追加: 憑依開始時にAttackアニメ発火
                if (animator != null) animator.SetTrigger(AnimAttackTrigger);
                hijackSystemController.TryHijackEnemy_Trigger(other, this);
            }
        }
    }

}