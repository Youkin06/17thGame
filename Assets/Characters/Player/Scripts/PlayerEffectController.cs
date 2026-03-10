using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [Header("参照設定")]
    private PlayerController playerController;

    [Header("エフェクト親オブジェクト")]
    [Tooltip("突進中に再生したいパーティクルの親")]
    [SerializeField] private GameObject playerDashEffectRoot;
    
    [Tooltip("加速・最高速中に再生したいパーティクルの親")]
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

        // 初期状態はすべて停止
        StopAllParticles();
        
        if (playerController != null)
        {
            lastState = playerController.currentState;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // 状態が変化したときだけエフェクトを更新
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
                if (!p.isPlaying) p.Play();
            }
            else
            {
                if (p.isPlaying) p.Stop();
            }
        }
    }

    private void StopAllParticles()
    {
        ToggleParticles(dashParticles, false);
        ToggleParticles(moveParticles, false);
    }
}