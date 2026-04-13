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

    public bool IsReady => style != null && runtimeMaterial != null;

    private void Awake()
    {
        targetGraphic = GetComponent<Graphic>();

        if (targetGraphic.material == null)
        {
            Debug.LogError($"{name}: Graphic に Material が設定されていません。");
            return;
        }

        // ボタンごとに独立して見た目を変えるため、共有 Material ではなく実行時専用 Material を作る。
        // ここまで来ていない場合は、Graphic か Material の参照設定を確認する。
        runtimeMaterial = new Material(targetGraphic.material);
        runtimeMaterial.name = $"{targetGraphic.material.name}_Runtime_{name}";
        targetGraphic.material = runtimeMaterial;
        ApplyDefaultState();
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
            // ボタンを押しても見た目が変わらない場合、まずここで return していないか確認する。
            // style が未設定、または Awake で runtimeMaterial を作れていないとシェーダー値は更新されない。
            Debug.LogWarning($"{name}: ApplyState skipped. style={style}, runtimeMaterial={runtimeMaterial}");
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
            // propertyName はシェーダー側の Float プロパティ名と完全一致している必要がある。
            // 例: "_Progress"。名前が違うと SetFloat しても見た目は変わらない。
            int propertyId = Shader.PropertyToID(setting.propertyName);
            float value = isOn ? setting.onValue : setting.offValue;

            if (!runtimeMaterial.HasProperty(propertyId))
            {
                Debug.LogWarning($"{name}: Material does not have shader property '{setting.propertyName}'. shader={runtimeMaterial.shader.name}");
            }

            SetShaderFloat(propertyId, setting.propertyName, value);
        }

        targetGraphic.SetMaterialDirty();
    }

    private void ApplyDefaultState()
    {
        if (!IsReady)
        {
            return;
        }

        foreach (var setting in style.floatProperties)
        {
            int propertyId = Shader.PropertyToID(setting.propertyName);
            if (runtimeMaterial.HasProperty(propertyId))
            {
                runtimeMaterial.SetFloat(propertyId, setting.offValue);
            }
        }

        targetGraphic.SetMaterialDirty();
    }

    private void SetShaderFloat(int propertyId, string propertyName, float value)
    {
        runtimeMaterial.SetFloat(propertyId, value);

        targetGraphic.material = runtimeMaterial;
        targetGraphic.SetMaterialDirty();

        if (!targetGraphic.isActiveAndEnabled)
        {
            return;
        }

        Material renderingMaterial = targetGraphic.materialForRendering;
        if (renderingMaterial == null || ReferenceEquals(renderingMaterial, runtimeMaterial))
        {
            targetGraphic.canvasRenderer.materialCount = 1;
            targetGraphic.canvasRenderer.SetMaterial(runtimeMaterial, 0);
            targetGraphic.canvasRenderer.SetTexture(targetGraphic.mainTexture);
            return;
        }

        if (!renderingMaterial.HasProperty(propertyId))
        {
            Debug.LogWarning($"{name}: Rendering material does not have shader property '{propertyName}'. renderingShader={renderingMaterial.shader.name}");
            return;
        }

        renderingMaterial.SetFloat(propertyId, value);
        targetGraphic.canvasRenderer.materialCount = 1;
        targetGraphic.canvasRenderer.SetMaterial(renderingMaterial, 0);
        targetGraphic.canvasRenderer.SetTexture(targetGraphic.mainTexture);
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
            // アニメーション開始時点の値から、ON/OFF の目標値へ補間する。
            // 変化が見えない場合は offValue と onValue が同じ値になっていないか確認する。
            propertyIds[i] = Shader.PropertyToID(setting.propertyName);
            startValues[i] = runtimeMaterial.GetFloat(propertyIds[i]);
            targetValues[i] = isOn ? setting.onValue : setting.offValue;

            if (!runtimeMaterial.HasProperty(propertyIds[i]))
            {
                Debug.LogWarning($"{name}: Material does not have shader property '{setting.propertyName}'. shader={runtimeMaterial.shader.name}");
            }

        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            for (int i = 0; i < count; i++)
            {
                float value = Mathf.Lerp(startValues[i], targetValues[i], eased);
                SetShaderFloat(propertyIds[i], style.floatProperties[i].propertyName, value);
            }

            targetGraphic.SetMaterialDirty();
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            SetShaderFloat(propertyIds[i], style.floatProperties[i].propertyName, targetValues[i]);
        }

        targetGraphic.SetMaterialDirty();

        animationRoutine = null;
    }
}
