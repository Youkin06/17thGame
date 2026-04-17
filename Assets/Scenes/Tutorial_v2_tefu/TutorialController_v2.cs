using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.AI.Navigation;
using UnityEngine.AI;
using TMPro;

public class TutorialController_v2 : MonoBehaviour
{
    [Header("ゲームクリア/オーバーのUI")]
    public GameObject gameClearPopUp;
    public GameObject gameOverPopUp;

    [Header("HandControllerの設定")]
    public Canvas canvas;
    private GameObject hand;
    private CanvasGroup handCanvasGroup;

    public GameObject player;
    public Image targetImage;
    public TextMeshProUGUI leaveAndDash;
    public TextMeshProUGUI hijack;
    public TextMeshProUGUI practice;

    [Header("参照コンポーネント")]
    public Slider slider;
    private PlayerController playerController;
    private PlayerMoveState previousState;
    public GameObject navMesh;

    [SerializeField] private GameObject enemy1;
    [SerializeField] private GameObject enemy2;
    [SerializeField] private GameObject enemy3;

    private bool isGameEnded = false;
    private bool isGoaled = false;
    private bool stopHandController = false;
    private bool mapChanged = false;

    private bool hasShownLeaveAndDash = false;
    private bool hasShownHijack = false;
    private bool hasShownPractice = false;
    private bool isLeaveAndDashBlinking = false;
    private bool shouldStopLeaveAndDashBlink = false;

    private CanvasGroup leaveAndDashCanvasGroup;
    private CanvasGroup hijackCanvasGroup;
    private CanvasGroup targetImageCanvasGroup;
    private CanvasGroup practiceCanvasGroup;

    private CanvasGroup enemy1TargetImageCanvasGroup;
    private CanvasGroup enemy2TargetImageCanvasGroup;
    private CanvasGroup enemy3TargetImageCanvasGroup;

    void Start()
    {
        Time.timeScale = 1;
        if (gameClearPopUp != null) gameClearPopUp.SetActive(false);
        if (gameOverPopUp != null) gameOverPopUp.SetActive(false);

        hand = canvas.transform.Find("Hand").gameObject;

        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
                previousState = playerController.currentState;
        }

        handCanvasGroup = hand.GetComponent<CanvasGroup>();
        if (handCanvasGroup == null)
            handCanvasGroup = hand.AddComponent<CanvasGroup>();
        handCanvasGroup.alpha = 0f;

        if (leaveAndDash != null)
        {
            leaveAndDashCanvasGroup = leaveAndDash.GetComponent<CanvasGroup>();
            if (leaveAndDashCanvasGroup == null)
                leaveAndDashCanvasGroup = leaveAndDash.gameObject.AddComponent<CanvasGroup>();
            leaveAndDashCanvasGroup.alpha = 0f;
        }

        if (hijack != null)
        {
            hijackCanvasGroup = hijack.GetComponent<CanvasGroup>();
            if (hijackCanvasGroup == null)
                hijackCanvasGroup = hijack.gameObject.AddComponent<CanvasGroup>();
            hijackCanvasGroup.alpha = 0f;
        }

        if (practice != null)
        {
            practiceCanvasGroup = practice.GetComponent<CanvasGroup>();
            if (practiceCanvasGroup == null)
                practiceCanvasGroup = practice.gameObject.AddComponent<CanvasGroup>();
            practiceCanvasGroup.alpha = 0f;
        }

        if (targetImage != null)
        {
            targetImageCanvasGroup = targetImage.GetComponent<CanvasGroup>();
            if (targetImageCanvasGroup == null)
                targetImageCanvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
            targetImageCanvasGroup.alpha = 1f;
        }

        if (enemy1 != null)
        {
            Image img = enemy1.GetComponentInChildren<Image>();
            if (img != null)
            {
                enemy1TargetImageCanvasGroup = img.GetComponent<CanvasGroup>();
                if (enemy1TargetImageCanvasGroup == null)
                    enemy1TargetImageCanvasGroup = img.gameObject.AddComponent<CanvasGroup>();
                enemy1TargetImageCanvasGroup.alpha = 1f;
            }
        }

        if (enemy2 != null)
        {
            Image img = enemy2.GetComponentInChildren<Image>();
            if (img != null)
            {
                enemy2TargetImageCanvasGroup = img.GetComponent<CanvasGroup>();
                if (enemy2TargetImageCanvasGroup == null)
                    enemy2TargetImageCanvasGroup = img.gameObject.AddComponent<CanvasGroup>();
                enemy2TargetImageCanvasGroup.alpha = 1f;
            }
        }

        if (enemy3 != null)
        {
            Image img = enemy3.GetComponentInChildren<Image>();
            if (img != null)
            {
                enemy3TargetImageCanvasGroup = img.GetComponent<CanvasGroup>();
                if (enemy3TargetImageCanvasGroup == null)
                    enemy3TargetImageCanvasGroup = img.gameObject.AddComponent<CanvasGroup>();
                enemy3TargetImageCanvasGroup.alpha = 1f;
            }
        }

        HandControllerLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    void Update()
    {
        if (slider != null && slider.value <= 0 && !isGameEnded)
        {
            isGameEnded = true;
            OnPlayerDead();
        }

        if (playerController != null)
        {
            if (previousState != PlayerMoveState.Dashing && playerController.currentState == PlayerMoveState.Dashing)
            {
                if (previousState == PlayerMoveState.MaxSpeed && isLeaveAndDashBlinking)
                    shouldStopLeaveAndDashBlink = true;
            }

            if (!hasShownLeaveAndDash && playerController.currentState == PlayerMoveState.MaxSpeed)
            {
                hasShownLeaveAndDash = true;
                LeaveAndDashBlinkAsync(this.GetCancellationTokenOnDestroy()).Forget();
            }

            previousState = playerController.currentState;
        }

        if (!hasShownHijack && player != null && player.transform.position.y >= 48f)
        {
            hasShownHijack = true;
            HijackTutorialAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        if (!hasShownPractice && player != null && player.transform.position.y >= 74f)
        {
            hasShownPractice = true;
            if (enemy1 != null) enemy1.GetComponent<NavMeshAgent>().enabled = true;
            if (enemy2 != null) enemy2.GetComponent<NavMeshAgent>().enabled = true;
            if (enemy3 != null) enemy3.GetComponent<NavMeshAgent>().enabled = true;
            PracticeTutorialAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        if (mapChanged)
        {
            navMesh.GetComponent<NavMeshSurface>().BuildNavMesh();
            mapChanged = false;
        }
        else return;
    }

    private async UniTask LeaveAndDashBlinkAsync(CancellationToken cancellationToken)
    {
        if (leaveAndDashCanvasGroup == null) return;

        isLeaveAndDashBlinking = true;
        float fadeDuration = 0.5f;

        while (isLeaveAndDashBlinking && !cancellationToken.IsCancellationRequested)
        {
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                leaveAndDashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                await UniTask.Yield(cancellationToken);
            }
            leaveAndDashCanvasGroup.alpha = 1f;

            await UniTask.Delay(500, cancellationToken: cancellationToken);

            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                leaveAndDashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                await UniTask.Yield(cancellationToken);
            }
            leaveAndDashCanvasGroup.alpha = 0f;

            await UniTask.Delay(500, cancellationToken: cancellationToken);

            if (shouldStopLeaveAndDashBlink)
            {
                isLeaveAndDashBlinking = false;
                leaveAndDashCanvasGroup.alpha = 0f;
                break;
            }
        }
    }

    private async UniTask HijackTutorialAsync(CancellationToken cancellationToken)
    {
        if (hijackCanvasGroup == null) return;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;

        float fadeDuration = 0.3f;
        float displayDuration = 3.0f;

        if (targetImageCanvasGroup != null)
            TargetImageBlinkAsync(targetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            hijackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        hijackCanvasGroup.alpha = 1f;

        await UniTask.Delay((int)(displayDuration * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            hijackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        hijackCanvasGroup.alpha = 0f;
        if (targetImageCanvasGroup != null) targetImageCanvasGroup.alpha = 0f;

        Time.timeScale = originalTimeScale;
    }

    private async UniTask PracticeTutorialAsync(CancellationToken cancellationToken)
    {
        if (practiceCanvasGroup == null) return;

        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;

        float fadeDuration = 0.3f;
        float displayDuration = 3.0f;

        if (enemy1TargetImageCanvasGroup != null)
            TargetImageBlinkAsync(enemy1TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        if (enemy2TargetImageCanvasGroup != null)
            TargetImageBlinkAsync(enemy2TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        if (enemy3TargetImageCanvasGroup != null)
            TargetImageBlinkAsync(enemy3TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            practiceCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        practiceCanvasGroup.alpha = 1f;

        await UniTask.Delay((int)(displayDuration * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            practiceCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        practiceCanvasGroup.alpha = 0f;

        if (enemy1TargetImageCanvasGroup != null) enemy1TargetImageCanvasGroup.alpha = 0f;
        if (enemy2TargetImageCanvasGroup != null) enemy2TargetImageCanvasGroup.alpha = 0f;
        if (enemy3TargetImageCanvasGroup != null) enemy3TargetImageCanvasGroup.alpha = 0f;

        Time.timeScale = originalTimeScale;
    }

    private async UniTask TargetImageBlinkAsync(CanvasGroup canvasGroup, float duration, CancellationToken cancellationToken)
    {
        if (canvasGroup == null) return;

        float originalAlpha = canvasGroup.alpha;
        float blinkSpeed = 0.2f;
        float elapsedTime = 0f;

        while (elapsedTime < duration && !cancellationToken.IsCancellationRequested)
        {
            float fadeTime = 0f;
            while (fadeTime < blinkSpeed)
            {
                fadeTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(originalAlpha, 0f, fadeTime / blinkSpeed);
                await UniTask.Yield(cancellationToken);
            }

            fadeTime = 0f;
            while (fadeTime < blinkSpeed)
            {
                fadeTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, fadeTime / blinkSpeed);
                await UniTask.Yield(cancellationToken);
            }

            elapsedTime += blinkSpeed * 2;
        }

        canvasGroup.alpha = originalAlpha;
    }

    private async UniTask HandControllerLoopAsync(CancellationToken cancellationToken)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector3 initialPosition = hand.transform.localPosition;

        while (!stopHandController)
        {
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                break;
            }

            await UniTask.Delay(1000, cancellationToken: cancellationToken);

            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                break;
            }

            await HandAnimationAsync(playerRb, initialPosition, cancellationToken);

            if (stopHandController) break;

            await MoveHandDownAsync(playerRb, cancellationToken);

            if (stopHandController) break;
        }
    }

    private async UniTask HandAnimationAsync(Rigidbody2D playerRb, Vector3 initialPosition, CancellationToken cancellationToken)
    {
        if (stopHandController) return;

        Vector3 startPosition = initialPosition;
        Vector3 targetPosition = startPosition + new Vector3(0, 400f, 0);
        float fadeDuration = 0.5f;
        float moveDuration = 1.0f;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                return;
            }
            elapsedTime += Time.deltaTime;
            handCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        handCanvasGroup.alpha = 1f;

        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                return;
            }
            elapsedTime += Time.deltaTime;
            hand.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            await UniTask.Yield(cancellationToken);
        }
        hand.transform.localPosition = targetPosition;

        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                return;
            }
            elapsedTime += Time.deltaTime;
            handCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        handCanvasGroup.alpha = 0f;
    }

    private async UniTask MoveHandDownAsync(Rigidbody2D playerRb, CancellationToken cancellationToken)
    {
        if (stopHandController) return;

        Vector3 startPosition = hand.transform.localPosition;
        Vector3 targetPosition = startPosition + new Vector3(0, -400f, 0);
        float moveDuration = 3.0f;

        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                return;
            }
            elapsedTime += Time.deltaTime;
            hand.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            await UniTask.Yield(cancellationToken);
        }
        hand.transform.localPosition = targetPosition;
    }

    public void OnGoalReached()
    {
        if (isGoaled) return;
        isGoaled = true;
        if (gameClearPopUp != null) gameClearPopUp.SetActive(true);
        Invoke(nameof(TimeStop), 1f);
    }

    public void OnPlayerDead()
    {
        if (isGameEnded) return;
        isGameEnded = true;
        if (gameOverPopUp != null) gameOverPopUp.SetActive(true);
        Invoke(nameof(TimeStop), 1f);
    }

    private void TimeStop()
    {
        Time.timeScale = 0;
    }
}
