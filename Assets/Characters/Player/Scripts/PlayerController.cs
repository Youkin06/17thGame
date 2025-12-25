using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    [Header("参照コンポーネント")]
    [SerializeField] private CinemachineVirtualCamera virtualCamScript;
    [SerializeField] private JoystickMove joystickMove;
    private Rigidbody2D rb;

    [Header("ズーム調整")]
    [Tooltip("最大ズーム")]
    public float maxZoomSize = 10f;
    [Tooltip("最小ズーム")]
    public float minZoomSize = 5.0f;
    [Tooltip("ズームが開始される速度")]
    public float zoomStartSpeed = 2.0f;
    [Tooltip("速度に対するズーム倍率のカーブ")]
    public AnimationCurve zoomCurve;
    [Tooltip("ズームの変化の滑らかさ (小さいほど速く追従)")]
    public float zoomSmoothTime = 0.5f;
    private float currentLOS;   //LOS = Lens Ortho Size
    private float targetLOS;
    private float zoomVelocity; // SmoothDamp用の参照変

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (virtualCamScript != null)
        {
            currentLOS = virtualCamScript.m_Lens.OrthographicSize;
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
        if (virtualCamScript == null || joystickMove == null) return;
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

        currentLOS = Mathf.SmoothDamp(currentLOS, targetLOS, ref zoomVelocity, zoomSmoothTime);
        virtualCamScript.m_Lens.OrthographicSize = currentLOS;
    }
}
