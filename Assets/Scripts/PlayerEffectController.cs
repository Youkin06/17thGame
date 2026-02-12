using UnityEngine;

public class PlayerEffectController : MonoBehaviour
{
    [Header("制御するパーティクル")]
    [SerializeField]
    private ParticleSystem moveParticle;

    [Header("色調整")]
    [SerializeField]
    private Color particleColor = new Color(0, 1, 1, 0.5f);

    [Tooltip("後ろに噴射する勢い")]
    [SerializeField, Range(0f, 10f)]
    private float backSpeed = 5.0f;

    private void OnValidate()
    {
        UpdateParticleSettings();
    }

    private void Update()
    {
        UpdateParticleSettings();
    }

    private void UpdateParticleSettings()
    {
        if (moveParticle == null)
            return;

        var main = moveParticle.main;
        main.startColor = particleColor;

        var velocity = moveParticle.velocityOverLifetime;

        if (!velocity.enabled)
            velocity.enabled = true;

        velocity.space = ParticleSystemSimulationSpace.Local;

        velocity.z = new ParticleSystem.MinMaxCurve(-backSpeed);
    }
}
