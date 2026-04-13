using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class ShaderToggleVisual : MonoBehaviour
{
    [SerializeField] private ShaderToggleStyle style;

    private Graphic targetGraphic;
    private Material runtimeMaterial;
    private Coroutine animationRoutine;

    private void Awake()
    {
        targetGraphic = GetComponent<Graphic>();

        if (targetGraphic.material == null)
        {
            Debug.LogError($"{name}: Graphic に Material が設定されていません。");
            return;
        }

        // このボタン専用の Material を作る
        runtimeMaterial = new Material(targetGraphic.material);
        runtimeMaterial.name = $"{targetGraphic.material.name}_Runtime_{name}";
        targetGraphic.material = runtimeMaterial;
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    public void ApplyState(bool isOn, bool instant = false)
    {
        if (style == null || runtimeMaterial == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        if (instant || style.duration <= 0f)
        {
            ApplyInstant(isOn);
            return;
        }

        animationRoutine = StartCoroutine(AnimateState(isOn));
    }

    private void ApplyInstant(bool isOn)
    {
        foreach (var setting in style.floatProperties)
        {
            int propertyId = Shader.PropertyToID(setting.propertyName);
            float value = isOn ? setting.onValue : setting.offValue;
            runtimeMaterial.SetFloat(propertyId, value);
        }
    }

    private IEnumerator AnimateState(bool isOn)
    {
        float duration = style.duration;
        float elapsed = 0f;

        int count = style.floatProperties.Count;
        float[] startValues = new float[count];
        float[] targetValues = new float[count];
        int[] propertyIds = new int[count];

        for (int i = 0; i < count; i++)
        {
            var setting = style.floatProperties[i];
            propertyIds[i] = Shader.PropertyToID(setting.propertyName);
            startValues[i] = runtimeMaterial.GetFloat(propertyIds[i]);
            targetValues[i] = isOn ? setting.onValue : setting.offValue;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Lerp(startValues[i], targetValues[i], eased);
                runtimeMaterial.SetFloat(propertyIds[i], value);
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            runtimeMaterial.SetFloat(propertyIds[i], targetValues[i]);
        }

        animationRoutine = null;
    }
}
