using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashSpriteController : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    void Start()
    {
        // アタッチしたオブジェクト自身にSpriteRendererがある場合は自動取得
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // プレイヤーコントローラーが未設定ならシーン内から探す
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
    }

    void Update()
    {
        if (playerController == null || targetSpriteRenderer == null) return;

        // プレイヤーの状態がDashing（突進中）またはMaxSpeed（最高速度到達）ならON、それ以外ならOFF
        if (playerController.currentState == PlayerMoveState.Dashing || playerController.currentState == PlayerMoveState.MaxSpeed)
        {
            targetSpriteRenderer.enabled = true;
        }
        else
        {
            targetSpriteRenderer.enabled = false;
        }
    }
}