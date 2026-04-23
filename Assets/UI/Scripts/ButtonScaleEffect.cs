using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Transform target;
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float releaseScale = 1.04f;
    [SerializeField] private float pressDuration = 0.08f;
    [SerializeField] private float releaseDuration = 0.1f;
    [SerializeField] private Ease pressEase = Ease.OutQuad;
    [SerializeField] private Ease releaseEase = Ease.OutBack;

    private Button button;
    private Vector3 defaultScale;
    private Sequence sequence;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (target == null)
        {
            target = transform;
        }

        defaultScale = target.localScale;
    }

    private void OnEnable()
    {
        if (target != null)
        {
            target.localScale = defaultScale;
        }
    }

    private void OnDisable()
    {
        KillAnimation();

        if (target != null)
        {
            target.localScale = defaultScale;
        }
    }

    private void OnDestroy()
    {
        KillAnimation();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        KillAnimation();
        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(target.DOScale(defaultScale * pressedScale, pressDuration).SetEase(pressEase));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        PlayReleaseAnimation();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!CanAnimate())
        {
            return;
        }

        PlayReleaseAnimation();
    }

    private void PlayReleaseAnimation()
    {
        KillAnimation();

        sequence = DOTween.Sequence()
            .SetUpdate(true)
            .Append(target.DOScale(defaultScale * releaseScale, releaseDuration).SetEase(releaseEase))
            .Append(target.DOScale(defaultScale, releaseDuration).SetEase(Ease.OutQuad));
    }

    private bool CanAnimate()
    {
        return target != null && button != null && button.interactable;
    }

    private void KillAnimation()
    {
        if (sequence == null)
        {
            return;
        }

        sequence.Kill();
        sequence = null;
    }
}
