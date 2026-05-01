using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// アタッチしたオブジェクトを Awake で Instantiate して影コピーを自動生成するコンポーネント。
/// Player など任意のオブジェクトに1つ付けるだけで動作する汎用スクリプト。
/// </summary>
[DisallowMultipleComponent]
public class PlayerShadowBoneCopy : MonoBehaviour
{
    // =========================
    // Follow Settings
    // =========================
    [Header("Follow Settings")]
    // 影のワールド固定オフセット（右下方向が標準）。
    [SerializeField]
    private Vector3 worldOffset = new Vector3(0.4f, -0.4f, 0f);

    // true のとき localScale も本体からコピーする。
    [SerializeField]
    private bool copyLocalScale;

    // 非アクティブなボーンも対応表の対象に含める。
    [SerializeField]
    private bool includeInactiveBones = true;

    // =========================
    // Shadow Visuals
    // =========================
    [Header("Shadow Visuals")]
    // 影に適用する基本色（黒半透明）。
    [SerializeField]
    private Color shadowColor = new Color(0f, 0f, 0f, 0.45f);

    // 本体より背面に置くための sortingOrder 差分。
    [SerializeField]
    private int sortingOrderOffset = -10;

    // true の場合 Sorting Layer 名も強制上書きする。
    [SerializeField]
    private bool overrideSortingLayer;

    // overrideSortingLayer 有効時に使う Sorting Layer 名。
    [SerializeField]
    private string shadowSortingLayerName = "Default";

    // =========================
    // Disable Options
    // =========================
    [Header("Disable Options")]
    // 影側で名前指定で SetActive(false) するオブジェクト名リスト（判定・エフェクト系）。
    [SerializeField]
    private string[] disableObjectNames =
    {
        "hitbox",
        "PlayerDashEffect",
        "PlayerMoveEffect",
        "StickEffect",
        "eye",
        "PlayerTailTargetObject",
        "TargetCanvas"
    };

    // 影側で無効化するゲームロジック系スクリプトの型名リスト。
    [SerializeField]
    private string[] disableScriptTypeNames =
    {
        "PlayerController",
        "HijackSystemController",
        "CameraController",
        "BaseEnemyController",
        "DynamicJoystick",
    };

    // -----------------------------------------------
    // 内部状態
    // -----------------------------------------------

    // 本体ボーンと影ボーンの同期ペア（インデックス一致で LateUpdate コピー）。
    private readonly List<Transform> sourceBones = new List<Transform>();
    private readonly List<Transform> shadowBones = new List<Transform>();

    // Instantiate で生成した影ルート。
    private Transform shadowRoot;

    // disableObjectNames に含まれるノードは影生成時に意図的にオフにしているため active をミラーしない。
    private HashSet<string> _skipActiveMirrorNames;

    // Instantiate 中の再帰呼び出しを防ぐフラグ。
    // Instantiate(gameObject) するとコピー側の Awake も同フレームで呼ばれるため、
    // static フラグで「影生成中かどうか」を共有し二重 Instantiate を遮断する。
    private static bool _isCreatingShadow;

    // -----------------------------------------------
    // Unity ライフサイクル
    // -----------------------------------------------

    private void Awake()
    {
        // 影コピー側の Awake 呼び出しは無視する（CreateShadow 内で後処理する）。
        if (_isCreatingShadow)
        {
            return;
        }

        CreateShadow();
    }

    private void LateUpdate()
    {
        if (shadowRoot == null)
        {
            return;
        }

        // 影ルートを本体位置 + 固定オフセットに追従させる。
        shadowRoot.SetPositionAndRotation(transform.position + worldOffset, transform.rotation);

        // 各ボーンペアの localPosition / localRotation を複製する。
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

            // 本体の activeSelf を影に反映（Animator 無効の影でも乗っ取りなどの見た目切替に追従する）。
            if (_skipActiveMirrorNames == null || !_skipActiveMirrorNames.Contains(source.name))
            {
                bool activeSelf = source.gameObject.activeSelf;
                if (shadow.gameObject.activeSelf != activeSelf)
                {
                    shadow.gameObject.SetActive(activeSelf);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // 本体が破棄されたら影も合わせて削除する。
        if (shadowRoot != null)
        {
            Destroy(shadowRoot.gameObject);
        }
    }

    // -----------------------------------------------
    // 影生成ロジック
    // -----------------------------------------------

    private void CreateShadow()
    {
        // フラグを立ててから Instantiate する。
        // Instantiate 内でコピー側の Awake が即座に呼ばれるが、
        // フラグが true なので再帰せず早期 return する。
        _isCreatingShadow = true;
        GameObject shadowGO;
        try
        {
            shadowGO = Instantiate(
                gameObject,
                transform.position + worldOffset,
                transform.rotation
            );
        }
        finally
        {
            _isCreatingShadow = false;
        }

        _skipActiveMirrorNames =
            disableObjectNames != null && disableObjectNames.Length > 0
                ? new HashSet<string>(disableObjectNames)
                : null;

        shadowGO.name = gameObject.name + "_Shadow";
        shadowRoot = shadowGO.transform;

        // 影 GameObject 上のこのコンポーネントは不要なので削除する。
        PlayerShadowBoneCopy shadowScript = shadowGO.GetComponent<PlayerShadowBoneCopy>();
        if (shadowScript != null)
        {
            Destroy(shadowScript);
        }

        // Instantiate が階層内の Transform 参照を自動リマップするため、
        // SpriteSkin の boneTransforms は既に影ボーンを指した状態になる。
        // 手動リマップは不要。

        // 不要コンポーネントを無効化する。
        DisableUnnecessaryComponents(shadowGO);

        // 指定オブジェクトを非アクティブにする。
        DisableExcludedObjects(shadowGO);

        // 影の見た目（色・描画順）を適用する。
        ApplyShadowVisuals(shadowGO);

        // ボーン対応表を構築する。
        BuildBoneMapping();
    }

    /// <summary>
    /// 影には不要なコンポーネントを無効化する。
    /// SpriteSkin と SpriteRenderer は維持する。
    /// </summary>
    private void DisableUnnecessaryComponents(GameObject shadowGO)
    {
        // Animator を無効化（二重アニメーション防止）。
        foreach (Animator anim in shadowGO.GetComponentsInChildren<Animator>(true))
        {
            anim.enabled = false;
        }

        // NavMeshAgent を無効化（経路探索・移動不要）。
        foreach (NavMeshAgent agent in shadowGO.GetComponentsInChildren<NavMeshAgent>(true))
        {
            agent.enabled = false;
        }

                foreach (EnemyController enemycontroller in shadowGO.GetComponentsInChildren<EnemyController>(true))
        {
            enemycontroller.enabled = false;
        }

        // Collider2D 全種を無効化（当たり判定不要）。
        foreach (Collider2D col in shadowGO.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }

        // Rigidbody2D を無効化（物理演算不要）。
        foreach (Rigidbody2D rb in shadowGO.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.simulated = false;
        }

        // ゲームロジック系スクリプトと Rig/IK 系を無効化する。
        HashSet<string> blockList = new HashSet<string>(disableScriptTypeNames);
        foreach (Behaviour behaviour in shadowGO.GetComponentsInChildren<Behaviour>(true))
        {
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;

            // ゲームロジック系。
            if (blockList.Contains(typeName))
            {
                behaviour.enabled = false;
                continue;
            }

            // Rig / IK Constraint 系。
            bool isRigType =
                typeName == "Rig"
                || typeName == "RigBuilder"
                || typeName.EndsWith("Constraint")
                || typeName.Contains("IKConstraint");

            if (isRigType)
            {
                behaviour.enabled = false;
            }
        }
    }

    /// <summary>
    /// 名前リストに一致する子オブジェクトを非アクティブにする。
    /// </summary>
    private void DisableExcludedObjects(GameObject shadowGO)
    {
        if (disableObjectNames == null || disableObjectNames.Length == 0)
        {
            return;
        }

        HashSet<string> excludedNames = new HashSet<string>(disableObjectNames);
        foreach (Transform child in shadowGO.GetComponentsInChildren<Transform>(true))
        {
            if (child == shadowRoot)
            {
                continue;
            }

            if (excludedNames.Contains(child.name))
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 影側 SpriteRenderer に影色・sortingOrder を適用する。
    /// </summary>
    private void ApplyShadowVisuals(GameObject shadowGO)
    {
        foreach (SpriteRenderer sr in shadowGO.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.color = shadowColor;
            sr.sortingOrder += sortingOrderOffset;
            if (overrideSortingLayer && !string.IsNullOrEmpty(shadowSortingLayerName))
            {
                sr.sortingLayerName = shadowSortingLayerName;
            }
        }
    }

    // -----------------------------------------------
    // ボーン対応表
    // -----------------------------------------------

    /// <summary>
    /// 本体と影の相対パス一致でボーンペアを構築する。
    /// LateUpdate はこのリストだけを参照して高速同期する。
    /// </summary>
    [ContextMenu("Rebuild Bone Mapping")]
    public void BuildBoneMapping()
    {
        sourceBones.Clear();
        shadowBones.Clear();

        if (shadowRoot == null)
        {
            Debug.LogError($"{nameof(PlayerShadowBoneCopy)}: shadowRoot が未設定です。", this);
            return;
        }

        // 影側ボーンの相対パス辞書を作成する。
        Dictionary<string, Transform> shadowMap = BuildRelativePathMap(
            shadowRoot,
            includeInactiveBones
        );

        // 本体配下のボーンを走査して一致するペアを登録する。
        int unmatchedCount = 0;
        foreach (Transform sourceChild in GetComponentsInChildren<Transform>(includeInactiveBones))
        {
            if (sourceChild == transform)
            {
                continue;
            }

            string relativePath = GetRelativePath(transform, sourceChild);
            if (shadowMap.TryGetValue(relativePath, out Transform shadowChild))
            {
                sourceBones.Add(sourceChild);
                shadowBones.Add(shadowChild);
            }
            else
            {
                unmatchedCount++;
            }
        }

        if (sourceBones.Count == 0)
        {
            Debug.LogError(
                $"{nameof(PlayerShadowBoneCopy)}: ボーン対応が1件も見つかりませんでした。",
                this
            );
            return;
        }

        if (unmatchedCount > 0)
        {
            Debug.LogWarning(
                $"{nameof(PlayerShadowBoneCopy)}: matched={sourceBones.Count}, unmatched={unmatchedCount}",
                this
            );
        }
        Debug.Log($"[ShadowBoneMapping] source={sourceBones.Count} shadow={shadowBones.Count}", this);

    }

    // -----------------------------------------------
    // ユーティリティ
    // -----------------------------------------------

    private static Dictionary<string, Transform> BuildRelativePathMap(
        Transform root,
        bool includeInactive
    )
    {
        Dictionary<string, Transform> map = new Dictionary<string, Transform>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(includeInactive))
        {
            if (t == root)
            {
                continue;
            }

            string path = GetRelativePath(root, t);
            if (!map.ContainsKey(path))
            {
                map.Add(path, t);
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
