using UnityEngine;

namespace FlatShadowCaster
{
    [DisallowMultipleComponent]
    public class FlatShadowCaster : MonoBehaviour
    {
        [Header("Shadow Settings")]
        [SerializeField] private Color32 shadowColor = new Color32(0, 0, 0, 128);
        // World-space offset of the shadow camera relative to targetCamera.
        // For "left-top to right-bottom", typical values would be ( +X, -Y ).
        [SerializeField] private Vector2 shadowDistance = new Vector2(0.5f, -0.5f);
        [SerializeField, Range(0.1f, 1f)] private float shadowResolution = 0.5f;
        [SerializeField] private int maxRenderTextureSize = 1024;
        [SerializeField] private FilterMode shadowFilterMode = FilterMode.Bilinear;
        [SerializeField] private string shadowSortingLayerName;
        [SerializeField] private int shadowOrderInLayer;

        [Header("Target Settings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LayerMask targetLayer;

        // Distance from camera along forward direction where the projected quad is placed.
        [SerializeField] private float shadowSpriteDistanceFromCamera = 1f;
        [SerializeField] private string shadowSpriteShaderName = "FlatShadowCaster/ShadowSprite";

        private Camera shadowCamera;
        private RenderTexture shadowTexture;
        private GameObject shadowSpriteObject;
        private SpriteRenderer shadowSpriteRenderer;
        private Material shadowSpriteSharedMaterial;
        // UnityではMonoBehaviourのフィールド初期化時にネイティブ生成系を呼べないため、
        // MaterialPropertyBlockはAwake/Startで生成する。
        private MaterialPropertyBlock propertyBlock;

        private Sprite whiteSprite;
        private int lastTexW = -1;
        private int lastTexH = -1;

        private static readonly int TexturePropId = Shader.PropertyToID("_Texture");
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");

        void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        void Start()
        {
            if (targetCamera == null)
            {
                Debug.LogError($"{nameof(FlatShadowCaster)}: targetCamera is null.");
                enabled = false;
                return;
            }

            var shader = Shader.Find(shadowSpriteShaderName);
            if (shader == null)
            {
                Debug.LogError($"{nameof(FlatShadowCaster)}: Shadow sprite shader not found: {shadowSpriteShaderName}");
                enabled = false;
                return;
            }

            shadowCamera = new GameObject("FlatShadowCaster_shadowCamera").AddComponent<Camera>();
            shadowCamera.CopyFrom(targetCamera);
            shadowCamera.clearFlags = CameraClearFlags.SolidColor;
            shadowCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            shadowCamera.cullingMask = targetLayer;
            shadowCamera.enabled = false;

            EnsureRenderTexture();
            shadowCamera.targetTexture = shadowTexture;

            shadowSpriteObject = new GameObject("FlatShadowCaster_shadowSprite");
            shadowSpriteObject.transform.SetParent(null, false);

            shadowSpriteRenderer = shadowSpriteObject.AddComponent<SpriteRenderer>();
            shadowSpriteRenderer.sortingLayerName = shadowSortingLayerName;
            shadowSpriteRenderer.sortingOrder = shadowOrderInLayer;

            if (whiteSprite == null)
            {
                // 1x1 white sprite used as a UV plane for the render texture.
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            }
            shadowSpriteRenderer.sprite = whiteSprite;

            shadowSpriteSharedMaterial = new Material(shader);
            shadowSpriteRenderer.sharedMaterial = shadowSpriteSharedMaterial;

            propertyBlock.SetColor(ColorPropId, shadowColor);
            propertyBlock.SetTexture(TexturePropId, shadowTexture);
            shadowSpriteRenderer.SetPropertyBlock(propertyBlock);
        }

        void LateUpdate()
        {
            if (shadowCamera == null || shadowSpriteRenderer == null || targetCamera == null) return;

            // Cinemachine(Lens) でorthographicSize/aspectが毎フレーム変わる場合があるため、
            // RenderTexture用のshadowCameraも毎フレーム同期する。
            shadowCamera.orthographic = targetCamera.orthographic;
            shadowCamera.orthographicSize = targetCamera.orthographicSize;
            shadowCamera.aspect = targetCamera.aspect;

            // World-fixed shadow direction:
            // move shadowCamera by world-space offset, then rotate it the same as the target camera.
            Vector3 shadowCamWorldPos = targetCamera.transform.position + new Vector3(shadowDistance.x, shadowDistance.y, 0f);
            shadowCamera.transform.SetPositionAndRotation(shadowCamWorldPos, targetCamera.transform.rotation);

            EnsureRenderTexture();
            shadowCamera.targetTexture = shadowTexture;

            // Place the projected quad in front of the target camera.
            shadowSpriteObject.transform.SetPositionAndRotation(
                targetCamera.transform.position + targetCamera.transform.forward * shadowSpriteDistanceFromCamera,
                targetCamera.transform.rotation
            );

            // Fit the quad to the orthographic view so the RT is fully visible.
            float viewHeight = targetCamera.orthographicSize * 2f;
            float viewWidth = viewHeight * targetCamera.aspect;

            Vector2 spriteSize = shadowSpriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) spriteSize = Vector2.one;

            shadowSpriteObject.transform.localScale = new Vector3(viewWidth / spriteSize.x, viewHeight / spriteSize.y, 1f);

            shadowCamera.Render();

            // Push render output into the shadow shader.
            propertyBlock.SetTexture(TexturePropId, shadowTexture);
            propertyBlock.SetColor(ColorPropId, shadowColor);
            shadowSpriteRenderer.SetPropertyBlock(propertyBlock);
        }

        private void EnsureRenderTexture()
        {
            if (shadowCamera == null) return;

            int desiredW = Mathf.RoundToInt(Screen.width * shadowResolution);
            int desiredH = Mathf.RoundToInt(Screen.height * shadowResolution);
            desiredW = Mathf.Max(2, desiredW);
            desiredH = Mathf.Max(2, desiredH);

            if (maxRenderTextureSize > 0)
            {
                int currentMax = Mathf.Max(desiredW, desiredH);
                if (currentMax > maxRenderTextureSize)
                {
                    float scale = (float)maxRenderTextureSize / currentMax;
                    desiredW = Mathf.Max(2, Mathf.RoundToInt(desiredW * scale));
                    desiredH = Mathf.Max(2, Mathf.RoundToInt(desiredH * scale));
                }
            }

            if (shadowTexture != null && desiredW == lastTexW && desiredH == lastTexH) return;

            if (shadowTexture != null)
            {
                shadowTexture.Release();
                Destroy(shadowTexture);
                shadowTexture = null;
            }

            shadowTexture = new RenderTexture(desiredW, desiredH, 24, RenderTextureFormat.ARGB32);
            shadowTexture.filterMode = shadowFilterMode;
            shadowTexture.wrapMode = TextureWrapMode.Clamp;
            shadowTexture.Create();

            lastTexW = desiredW;
            lastTexH = desiredH;
        }

        void OnDestroy()
        {
            if (shadowCamera != null)
            {
                Destroy(shadowCamera.gameObject);
                shadowCamera = null;
            }

            if (shadowSpriteObject != null)
            {
                Destroy(shadowSpriteObject);
                shadowSpriteObject = null;
            }

            if (shadowTexture != null)
            {
                shadowTexture.Release();
                Destroy(shadowTexture);
                shadowTexture = null;
            }

            if (shadowSpriteSharedMaterial != null)
            {
                Destroy(shadowSpriteSharedMaterial);
                shadowSpriteSharedMaterial = null;
            }
        }
    }
}

