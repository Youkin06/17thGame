using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum SettingTab
{
    Volume,
    Pad,
    Other
}

public class SettingTabController : MonoBehaviour
{
    [Serializable]
    private class TabEntry
    {
        public SettingTab tab;
        public Button button;
        public GameObject content;
        public Graphic tabGraphic;
        public Graphic textGraphic;

        [NonSerialized] public UnityAction clickAction;
    }

    private class RuntimeMaterialTarget
    {
        public Graphic graphic;
        public Material material;
    }

    [SerializeField] private SettingTab initialTab = SettingTab.Volume;
    [SerializeField] private TabEntry[] tabs;
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(1f, 1f, 1f, 0.55f);
    [SerializeField] private bool disableSelectedButton;
    [Header("Tab Material Animation")]
    [SerializeField] private Material tabAnimationMaterial;
    [SerializeField] private ShaderToggleStyle tabAnimationStyle;
    [SerializeField] private Graphic[] materialDirtyTargets;

    private SettingTab activeTab;
    private bool hasActiveTab;
    private Coroutine materialAnimationRoutine;
    private readonly List<RuntimeMaterialTarget> runtimeMaterialTargets = new List<RuntimeMaterialTarget>();

    private void Awake()
    {
        RegisterButtonEvents();
        CreateRuntimeMaterialTargets();
    }

    private void OnEnable()
    {
        if (hasActiveTab)
        {
            ApplyTabState();
        }
        else
        {
            SelectTab(initialTab, animateMaterial: false);
        }
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
        DestroyRuntimeMaterialTargets();
    }

    public void SelectVolumeTab()
    {
        SelectTab(SettingTab.Volume, animateMaterial: true);
    }

    public void SelectPadTab()
    {
        SelectTab(SettingTab.Pad, animateMaterial: true);
    }

    public void SelectOtherTab()
    {
        SelectTab(SettingTab.Other, animateMaterial: true);
    }

    public void SelectTab(SettingTab tab)
    {
        SelectTab(tab, animateMaterial: true);
    }

    private void SelectTab(SettingTab tab, bool animateMaterial)
    {
        activeTab = tab;
        hasActiveTab = true;
        ApplyTabState();

        if (animateMaterial)
        {
            PlayTabMaterialAnimation();
        }
    }

    private void RegisterButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null || entry.button == null)
            {
                continue;
            }

            SettingTab tab = entry.tab;
            entry.clickAction = () => SelectTab(tab, animateMaterial: true);
            entry.button.onClick.AddListener(entry.clickAction);
        }
    }

    private void UnregisterButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null || entry.button == null || entry.clickAction == null)
            {
                continue;
            }

            entry.button.onClick.RemoveListener(entry.clickAction);
            entry.clickAction = null;
        }
    }

    private void ApplyTabState()
    {
        if (tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null)
            {
                continue;
            }

            bool isSelected = hasActiveTab && entry.tab == activeTab;

            if (entry.content != null)
            {
                entry.content.SetActive(isSelected);
            }

            if (entry.button != null)
            {
                entry.button.interactable = !disableSelectedButton || !isSelected;
            }

            if (entry.tabGraphic != null)
            {
                entry.tabGraphic.gameObject.SetActive(isSelected);
            }

            if (entry.textGraphic != null)
            {
                entry.textGraphic.color = isSelected ? activeTextColor : inactiveTextColor;
            }
        }
    }

    private void PlayTabMaterialAnimation()
    {
        if (tabAnimationMaterial == null || tabAnimationStyle == null)
        {
            return;
        }

        CreateRuntimeMaterialTargets();

        if (materialAnimationRoutine != null)
        {
            StopCoroutine(materialAnimationRoutine);
        }

        materialAnimationRoutine = StartCoroutine(AnimateTabMaterial());
    }

    private IEnumerator AnimateTabMaterial()
    {
        float duration = tabAnimationStyle.duration;

        foreach (var setting in tabAnimationStyle.floatProperties)
        {
            int propertyId = Shader.PropertyToID(setting.propertyName);
            SetShaderFloat(propertyId, setting.propertyName, setting.offValue);
        }

        if (duration <= 0f)
        {
            ApplyTabMaterialTargetValues();
            materialAnimationRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            foreach (var setting in tabAnimationStyle.floatProperties)
            {
                int propertyId = Shader.PropertyToID(setting.propertyName);
                float value = Mathf.Lerp(setting.offValue, setting.onValue, eased);
                SetShaderFloat(propertyId, setting.propertyName, value);
            }

            yield return null;
        }

        ApplyTabMaterialTargetValues();
        materialAnimationRoutine = null;
    }

    private void ApplyTabMaterialTargetValues()
    {
        foreach (var setting in tabAnimationStyle.floatProperties)
        {
            int propertyId = Shader.PropertyToID(setting.propertyName);
            SetShaderFloat(propertyId, setting.propertyName, setting.onValue);
        }
    }

    private void CreateRuntimeMaterialTargets()
    {
        if (runtimeMaterialTargets.Count > 0 || tabAnimationMaterial == null)
        {
            return;
        }

        if (materialDirtyTargets != null)
        {
            foreach (Graphic graphic in materialDirtyTargets)
            {
                TryCreateRuntimeMaterialTarget(graphic);
                TryCreateRuntimeMaterialTargetsInChildren(graphic);
            }
        }

        if (runtimeMaterialTargets.Count > 0 || tabs == null)
        {
            return;
        }

        foreach (TabEntry entry in tabs)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.button != null && entry.button.targetGraphic != null)
            {
                TryCreateRuntimeMaterialTarget(entry.button.targetGraphic);
                TryCreateRuntimeMaterialTargetsInChildren(entry.button.targetGraphic);
            }

            if (entry.button != null)
            {
                TryCreateRuntimeMaterialTargetsInChildren(entry.button.transform);
            }

            if (entry.tabGraphic != null)
            {
                TryCreateRuntimeMaterialTarget(entry.tabGraphic);
                TryCreateRuntimeMaterialTargetsInChildren(entry.tabGraphic);
            }
        }
    }

    private void TryCreateRuntimeMaterialTargetsInChildren(Graphic rootGraphic)
    {
        if (rootGraphic == null)
        {
            return;
        }

        TryCreateRuntimeMaterialTargetsInChildren(rootGraphic.transform);
    }

    private void TryCreateRuntimeMaterialTargetsInChildren(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Graphic[] childGraphics = root.GetComponentsInChildren<Graphic>(includeInactive: true);
        foreach (Graphic childGraphic in childGraphics)
        {
            TryCreateRuntimeMaterialTarget(childGraphic);
        }
    }

    private void TryCreateRuntimeMaterialTarget(Graphic graphic)
    {
        if (!IsAnimationMaterialTarget(graphic))
        {
            return;
        }

        foreach (RuntimeMaterialTarget target in runtimeMaterialTargets)
        {
            if (target.graphic == graphic)
            {
                return;
            }
        }

        Material runtimeMaterial = new Material(graphic.material);
        runtimeMaterial.name = $"{graphic.material.name}_Runtime_{graphic.name}";
        graphic.material = runtimeMaterial;

        runtimeMaterialTargets.Add(new RuntimeMaterialTarget
        {
            graphic = graphic,
            material = runtimeMaterial
        });
    }

    private bool IsAnimationMaterialTarget(Graphic graphic)
    {
        if (graphic == null || tabAnimationStyle == null || tabAnimationMaterial == null)
        {
            return false;
        }

        Material material = graphic.material;
        if (material == null || material.shader != tabAnimationMaterial.shader)
        {
            return false;
        }

        return true;
    }

    private void DestroyRuntimeMaterialTargets()
    {
        foreach (RuntimeMaterialTarget target in runtimeMaterialTargets)
        {
            if (target != null && target.material != null)
            {
                Destroy(target.material);
            }
        }

        runtimeMaterialTargets.Clear();
    }

    private void SetShaderFloat(int propertyId, string propertyName, float value)
    {
        if (tabAnimationMaterial.HasProperty(propertyId))
        {
            tabAnimationMaterial.SetFloat(propertyId, value);
        }

        foreach (RuntimeMaterialTarget target in runtimeMaterialTargets)
        {
            if (target == null || target.graphic == null || target.material == null)
            {
                continue;
            }

            if (!target.material.HasProperty(propertyId))
            {
                Debug.LogWarning($"{target.graphic.name}: Material does not have shader property '{propertyName}'. shader={target.material.shader.name}");
                continue;
            }

            target.material.SetFloat(propertyId, value);
            RefreshMaterialTarget(target, propertyId, propertyName, value);
        }
    }

    private void RefreshMaterialTarget(RuntimeMaterialTarget target, int propertyId, string propertyName, float value)
    {
        Graphic graphic = target.graphic;
        if (graphic == null || target.material == null)
        {
            return;
        }

        graphic.material = target.material;
        graphic.SetMaterialDirty();
        graphic.SetVerticesDirty();

        if (!graphic.isActiveAndEnabled)
        {
            return;
        }

        Material renderingMaterial = graphic.materialForRendering;
        if (renderingMaterial == null)
        {
            graphic.canvasRenderer.materialCount = 1;
            graphic.canvasRenderer.SetMaterial(target.material, 0);
            graphic.canvasRenderer.SetTexture(graphic.mainTexture);
            return;
        }

        if (!ReferenceEquals(renderingMaterial, target.material))
        {
            if (renderingMaterial.HasProperty(propertyId))
            {
                renderingMaterial.SetFloat(propertyId, value);
            }
            else
            {
                Debug.LogWarning($"{graphic.name}: Rendering material does not have shader property '{propertyName}'. renderingShader={renderingMaterial.shader.name}");
            }
        }

        graphic.canvasRenderer.materialCount = 1;
        graphic.canvasRenderer.SetMaterial(renderingMaterial, 0);
        graphic.canvasRenderer.SetTexture(graphic.mainTexture);
    }
}
