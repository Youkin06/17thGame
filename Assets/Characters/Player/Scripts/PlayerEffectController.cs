using System.Collections;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerController playerController;

    [Header("エフェクト親オブジェクト")]
    [Tooltip("突進（Dashing）中に再生したいパーティクルの親")]
    [SerializeField] private GameObject playerDashEffectRoot;
    
    [Tooltip("加速・最高速（Accelerating / MaxSpeed）中に再生したいパーティクルの親")]
    [SerializeField] private GameObject playerEffectRoot;

    private ParticleSystem[] dashParticles;
    private ParticleSystem[] moveParticles;
    private PlayerMoveState lastState;

    void Start()
    {
        // PlayerControllerの自動取得
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        // 指定されたルート以下にあるすべてのParticleSystemを配列に格納
        if (playerDashEffectRoot != null)
        {
            dashParticles = playerDashEffectRoot.GetComponentsInChildren<ParticleSystem>();
        }
        
        if (playerEffectRoot != null)
        {
            moveParticles = playerEffectRoot.GetComponentsInChildren<ParticleSystem>();
        }

        // 状態の初期化
        if (playerController != null)
        {
            lastState = playerController.currentState;
            // 起動時の状態に合わせてエフェクトを適用（重要：これで出ない問題を防止）
            UpdateEffects(lastState);
        }
        else
        {
            // コントローラーがない場合は念のためすべて停止
            StopAllParticles();
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // 状態が変化した瞬間にだけエフェクトを更新する（1フレームのみ実行）
        if (playerController.currentState != lastState)
        {
            UpdateEffects(playerController.currentState);
            lastState = playerController.currentState;
        }
    }

    private void UpdateEffects(PlayerMoveState currentState)
    {
        // 1. 突進中（Dashing）の判定
        bool isDashing = (currentState == PlayerMoveState.Dashing);
        ToggleParticles(dashParticles, isDashing);

        // 2. 移動中（Accelerating または MaxSpeed）の判定
        // Idle以外の移動状態で再生
        bool isMoving = (currentState == PlayerMoveState.Accelerating || currentState == PlayerMoveState.MaxSpeed);
        ToggleParticles(moveParticles, isMoving);
    }

    private void ToggleParticles(ParticleSystem[] particles, bool shouldPlay)
    {
        if (particles == null) return;

        foreach (var p in particles)
        {
            if (shouldPlay)
            {
                // 子オブジェクトのパーティクルも含めて一斉に再生
                if (!p.isPlaying) 
                {
                    p.Play(true);
                }
            }
            else
            {
                // 子オブジェクトも含めて停止。
                // StopEmittingAndClear：停止した瞬間に画面上の粒子も消去する
                // StopEmitting：放出だけ止めて、画面上の粒子は寿命まで残す（自然な余韻）
                if (p.isPlaying)
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    private void StopAllParticles()
    {
        ToggleParticles(dashParticles, false);
        ToggleParticles(moveParticles, false);
    }
}