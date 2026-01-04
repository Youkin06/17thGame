using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("参照コンポーネント")]
    [SerializeField] private CinemachineVirtualCamera virtualCam;
    private CinemachineFramingTransposer transposer;
    [SerializeField] private JoystickMove joystickMove;
    private Rigidbody2D rb;

    [Header("ダッシュ調整")]
    [SerializeField] private GameObject mainCamera;
    private Vector2 currentCamPos;
    private Vector2 targetCamPos;
    private float camVelocity;

    [Header("ズーム調整")]
    public float maxZoomSize = 10f;
    public float minZoomSize = 5.0f;
    public float zoomStartSpeed = 2.0f;
    public AnimationCurve zoomCurve;
    public float zoomSmoothTime = 0.5f;
    private float currentLOS;   //LOS = Lens Ortho Size
    private float targetLOS;
    private float zoomVelocity; // SmoothDamp用の参照変

    [Header("カメラ追従調整")]
    private Transform cameraTarget; // カメラが追従するダミーのターゲット
    private enum CameraFollowState
    {
        Normal,     // 通常追従
        Locked,     // 位置固定（ダッシュ中〜待機中）
        Rejoining   // 再合流中
    }
    private CameraFollowState cameraFollowState = CameraFollowState.Normal;
    private float cameraWaitTimer = 0f;
    private float cameraRejoinTimer = 0f;
    private float cameraRejoinDuration = 0f;
    private Vector3 cameraRejoinStartPos;
    private float cameraRejoinStartLOS; // 再合流開始時のズームサイズ

    void Start()
    {
        transposer = virtualCam.GetCinemachineComponent<CinemachineFramingTransposer>();
        rb = GetComponent<Rigidbody2D>();
        if (virtualCam != null)
        {
            currentLOS = virtualCam.m_Lens.OrthographicSize;

            // ダミーターゲットの作成
            GameObject targetObj = new GameObject("PlayerCameraTarget");
            cameraTarget = targetObj.transform;
            cameraTarget.position = transform.position;
            
            // VirtualCameraのFollowをダミーターゲットに変更
            virtualCam.Follow = cameraTarget;
        }
    }

    void OnValidate()
    {
        // 必須コンポーネントが割り当てられていない場合は処理しない
        if (joystickMove == null) return;

        // カーブが未設定の場合は初期化
        if (zoomCurve == null || zoomCurve.length < 2)
        {
            zoomCurve = AnimationCurve.Linear(0, minZoomSize, joystickMove.playerMaxSpeed, maxZoomSize);
        }
        else
        {
            // 既存のキーを取得
            Keyframe[] keys = zoomCurve.keys;

            // 最初と最後のキーを現在の設定値に合わせて更新
            // 最初のキー: (0, minZoomSize)
            keys[0].time = 0;
            keys[0].value = minZoomSize;

            // 最後のキー: (playerMaxSpeed, maxZoomSize)
            keys[keys.Length - 1].time = joystickMove.playerMaxSpeed;
            keys[keys.Length - 1].value = maxZoomSize;

            // 更新したキーを適用
            zoomCurve.keys = keys;
        }
    }

    void FixedUpdate()
    {
        
    }

    void Update()
    {
        if (virtualCam == null || joystickMove == null) return;
        
        // ターゲットのズームサイズを計算（適用はLateUpdateで状態に合わせて行う）
        CalculateTargetLOS();

        if (joystickMove.currentState == PlayerMoveState.Dashing)
        {
            
            if (joystickMove.currentState != PlayerMoveState.Dashing)
            {
                
            }
        }

    }

    void CalculateTargetLOS()
    {
        float currentVelocity = rb.velocity.magnitude;

        if (currentVelocity <= zoomStartSpeed)
        {
            targetLOS = minZoomSize;
        }
        else
        {
            // カーブから直接目標サイズを取得（横軸＝速度、縦軸＝サイズ）
            targetLOS = zoomCurve.Evaluate(currentVelocity);
        }
    }

    void LateUpdate()
    {
        if (cameraTarget == null || joystickMove == null) return;

        switch (cameraFollowState)
        {
            case CameraFollowState.Normal:
                // 通常時はプレイヤーの位置に同期
                cameraTarget.position = transform.position;

                // 通常時はSmoothDampでズーム適用
                currentLOS = Mathf.SmoothDamp(currentLOS, targetLOS, ref zoomVelocity, zoomSmoothTime);
                virtualCam.m_Lens.OrthographicSize = currentLOS;

                // ダッシュ開始を検知してLocked状態へ
                if (joystickMove.currentState == PlayerMoveState.Dashing)
                {
                    ChangeCameraState(CameraFollowState.Locked);
                }
                break;

            case CameraFollowState.Locked:
                // 位置は更新しない（固定）

                // 状態監視
                if (joystickMove.currentState == PlayerMoveState.Dashing)
                {
                    // ダッシュ中はタイマーリセット
                    cameraWaitTimer = 0f;
                }
                else if (joystickMove.currentState == PlayerMoveState.Idle)
                {
                    // アイドル状態になったら0.5秒待つ
                    cameraWaitTimer += Time.deltaTime;
                    if (cameraWaitTimer >= 0.5f)
                    {
                        StartRejoining();
                    }
                }
                else if (joystickMove.currentState == PlayerMoveState.Accelerating || joystickMove.currentState == PlayerMoveState.MaxSpeed)
                {
                    // 0.5秒待たずに再合流開始
                    StartRejoining();
                }
                break;

            case CameraFollowState.Rejoining:
                // プレイヤーまで徐々に近づける
                cameraRejoinTimer += Time.deltaTime;
                float t = cameraRejoinTimer / cameraRejoinDuration;
                
                // Ease In Out (SmoothStep) - ユーザー要望により復活
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 位置の補間
                cameraTarget.position = Vector3.Lerp(cameraRejoinStartPos, transform.position, smoothT);

                // ズームの補間
                currentLOS = Mathf.Lerp(cameraRejoinStartLOS, targetLOS, smoothT);
                virtualCam.m_Lens.OrthographicSize = currentLOS;

                // 到着判定
                if (t >= 1.0f)
                {
                    ChangeCameraState(CameraFollowState.Normal);
                }
                
                // 再合流中に再度ダッシュしたらまた止める
                if (joystickMove.currentState == PlayerMoveState.Dashing)
                {
                    ChangeCameraState(CameraFollowState.Locked);
                }
                break;
        }
    }

    private void ChangeCameraState(CameraFollowState newState)
    {
        cameraFollowState = newState;
        if (newState == CameraFollowState.Locked)
        {
            cameraWaitTimer = 0f;
        }
    }

    private void StartRejoining()
    {
        ChangeCameraState(CameraFollowState.Rejoining);
        cameraRejoinStartPos = cameraTarget.position;
        cameraRejoinStartLOS = currentLOS; // 現在のズーム値を保存
        cameraRejoinTimer = 0f;

        // 近づけるスピードは playerMaxSpeed + 1
        float catchUpSpeed = joystickMove.playerMaxSpeed + 1f;
        float distance = Vector3.Distance(cameraRejoinStartPos, transform.position);
        
        // 時間 = 距離 / 速さ
        if (catchUpSpeed > 0)
        {
            cameraRejoinDuration = distance / catchUpSpeed;
        }
        else
        {
            cameraRejoinDuration = 0f; // 即時
        }
    }
}
