using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerShadowBoneCopy : MonoBehaviour
{
    // =========================
    // Root References
    // =========================
    // 本体プレイヤー側のルート。ここを基準にボーン姿勢を参照する。
    [Header("Root References")]
    [SerializeField] private Transform sourceRoot;
    // 影側ルート。通常はこのコンポーネントが付いている playerShadow 自身。
    [SerializeField] private Transform shadowRoot;

    // =========================
    // Follow Settings
    // =========================
    // 影のワールド固定オフセット（右下方向に置く想定）。
    [Header("Follow Settings")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0.4f, -0.4f, 0f);
    // true のとき localScale も本体からコピーする。
    [SerializeField] private bool copyLocalScale;
    // 非アクティブなボーンも対応表作成対象に含める。
    [SerializeField] private bool includeInactiveBones = true;
    // 何らかの理由で対応表が空のとき、実行中に再構築を試みる。
    [SerializeField] private bool autoRebuildWhenEmpty = true;

    // =========================
    // Shadow Setup (Optional)
    // =========================
    // 影側の Animator を無効化して二重アニメーションを防ぐ。
    [Header("Shadow Setup (Optional)")]
    [SerializeField] private bool disableShadowAnimator = true;
    // 影側の Rig / Constraint を無効化してIK二重計算を防ぐ。
    [SerializeField] private bool disableShadowRigAndConstraints = true;
    // Start時に影色・描画順設定を自動適用する。
    [SerializeField] private bool applyShadowVisualsOnStart = true;
    // 影に適用する基本色（黒半透明）。
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.45f);
    // 本体より背面に置くための sortingOrder 差分。
    [SerializeField] private int sortingOrderOffset = -10;
    // true の場合、Sorting Layer 名も強制上書きする。
    [SerializeField] private bool overrideSortingLayer;
    // overrideSortingLayer 有効時に使う Sorting Layer 名。
    [SerializeField] private string sortingLayerName = "Default";
    // 影側で無効化したいオブジェクト名リスト（判定・エフェクト等）。
    [SerializeField] private string[] disableObjectNames =
    {
        "hitbox",
        "PlayerDashEffect",
        "PlayerMoveEffect",
        "StickEffect",
        "eye",
        "PlayerTailTargetObject"
    };

    // 本体ボーンと影ボーンの対応ペア（インデックス一致で同期）。
    private readonly List<Transform> sourceBones = new List<Transform>();
    private readonly List<Transform> shadowBones = new List<Transform>();

    private void Reset()
    {
        shadowRoot = transform;
    }

    private void Start()
    {
        if (shadowRoot == null)
        {
            shadowRoot = transform;
        }

        BuildBoneMapping();

        if (disableShadowAnimator)
        {
            DisableAnimatorInShadow();
        }

        if (disableShadowRigAndConstraints)
        {
            DisableRigAndConstraintComponents();
        }

        if (applyShadowVisualsOnStart)
        {
            ApplyShadowVisuals();
            DisableExcludedObjects();
        }
    }

    private void LateUpdate()
    {
        if (sourceRoot == null || shadowRoot == null)
        {
            return;
        }

        if (autoRebuildWhenEmpty && sourceBones.Count == 0)
        {
            BuildBoneMapping();
        }

        shadowRoot.SetPositionAndRotation(sourceRoot.position + worldOffset, sourceRoot.rotation);

        int pairCount = Mathf.Min(sourceBones.Count, shadowBones.Count);
        for (int i = 0; i < pairCount; i++)
        {
            Transform source = sourceBones[i];
            Transform shadow = shadowBones[i];
            if (source == null || shadow == null)
            {
                continue;
            }

            shadow.localPosition = source.localPosition;
            shadow.localRotation = source.localRotation;
            if (copyLocalScale)
            {
                shadow.localScale = source.localScale;
            }
        }
    }

    [ContextMenu("Build Bone Mapping")]
    public void BuildBoneMapping()
    {
        sourceBones.Clear();
        shadowBones.Clear();

        if (sourceRoot == null || shadowRoot == null)
        {
            Debug.LogError($"{nameof(PlayerShadowBoneCopy)}: sourceRoot or shadowRoot is not assigned.", this);
            return;
        }

        Dictionary<string, Transform> sourceMap = BuildRelativePathMap(sourceRoot, includeInactiveBones);
        Transform[] shadowTransforms = shadowRoot.GetComponentsInChildren<Transform>(includeInactiveBones);

        int unmatchedCount = 0;
        for (int i = 0; i < shadowTransforms.Length; i++)
        {
            Transform shadow = shadowTransforms[i];
            if (shadow == shadowRoot)
            {
                continue;
            }

            string relativePath = GetRelativePath(shadowRoot, shadow);
            if (sourceMap.TryGetValue(relativePath, out Transform source))
            {
                sourceBones.Add(source);
                shadowBones.Add(shadow);
            }
            else
            {
                unmatchedCount++;
            }
        }

        if (sourceBones.Count == 0)
        {
            Debug.LogError($"{nameof(PlayerShadowBoneCopy)}: no matched bone paths found.", this);
            return;
        }

        if (unmatchedCount > 0)
        {
            Debug.LogWarning(
                $"{nameof(PlayerShadowBoneCopy)}: matched={sourceBones.Count}, unmatchedShadowBones={unmatchedCount}.",
                this
            );
        }
    }

    [ContextMenu("Apply Shadow Visuals")]
    public void ApplyShadowVisuals()
    {
        if (shadowRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = shadowRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            renderer.color = shadowColor;
            renderer.sortingOrder += sortingOrderOffset;
            if (overrideSortingLayer && !string.IsNullOrEmpty(sortingLayerName))
            {
                renderer.sortingLayerName = sortingLayerName;
            }
        }
    }

    [ContextMenu("Disable Excluded Shadow Objects")]
    public void DisableExcludedObjects()
    {
        if (shadowRoot == null || disableObjectNames == null || disableObjectNames.Length == 0)
        {
            return;
        }

        HashSet<string> excludedNames = new HashSet<string>(disableObjectNames);
        Transform[] allTransforms = shadowRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform item = allTransforms[i];
            if (item == shadowRoot)
            {
                continue;
            }

            if (excludedNames.Contains(item.name))
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    private void DisableAnimatorInShadow()
    {
        if (shadowRoot == null)
        {
            return;
        }

        Animator[] animators = shadowRoot.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }
    }

    private void DisableRigAndConstraintComponents()
    {
        if (shadowRoot == null)
        {
            return;
        }

        Behaviour[] behaviours = shadowRoot.GetComponentsInChildren<Behaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            Behaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            bool isRigComponent =
                typeName == "Rig" ||
                typeName == "RigBuilder" ||
                typeName.EndsWith("Constraint") ||
                typeName.Contains("IKConstraint");

            if (isRigComponent)
            {
                behaviour.enabled = false;
            }
        }
    }

    private static Dictionary<string, Transform> BuildRelativePathMap(Transform root, bool includeInactive)
    {
        Dictionary<string, Transform> map = new Dictionary<string, Transform>();
        Transform[] transforms = root.GetComponentsInChildren<Transform>(includeInactive);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform item = transforms[i];
            if (item == root)
            {
                continue;
            }

            string relativePath = GetRelativePath(root, item);
            if (!map.ContainsKey(relativePath))
            {
                map.Add(relativePath, item);
            }
        }

        return map;
    }

    private static string GetRelativePath(Transform root, Transform target)
    {
        if (root == null || target == null || target == root)
        {
            return string.Empty;
        }

        Stack<string> names = new Stack<string>();
        Transform current = target;
        while (current != null && current != root)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
