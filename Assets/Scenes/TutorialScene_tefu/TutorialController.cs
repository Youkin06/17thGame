using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.AI.Navigation;
using UnityEngine.AI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    [Header("シーン移動などの設定")]
    public Slider slider;
    public GameObject sceneHandler;
    public string ResultSceneName;
    public string GameOverSceneName;

    [Header("HandControllerの設定")]
    public Canvas canvas;
    private GameObject hand;
    private CanvasGroup handCanvasGroup;

    public GameObject player; // プレイヤーオブジェクトを参照
    public AudioClip dashSound; // ダッシュ音を参照
    AudioSource audioSource;
    public Image targetImage;
    public TextMeshProUGUI leaveAndDash;
    public TextMeshProUGUI hijack;
    public TextMeshProUGUI practice;
    

    [Header("参照コンポーネント")]
    private PlayerController playerController; // PlayerControllerへの参照
    private PlayerMoveState previousState; // 前フレームの状態を記録
    public GameObject navMesh;

    [SerializeField] private GameObject enemy1;
    [SerializeField] private GameObject enemy2;
    [SerializeField] private GameObject enemy3;

    //ゲームオーバー処理を1度だけ行うための判定
    private bool isGameEnded = false;
    private bool stopHandController = false; // HandControllerを停止するフラグ
    private bool mapChanged = false; // マップが変更されたかどうかのフラグ
    
    // 新規追加：各チュートリアルの実行フラグ
    private bool hasShownLeaveAndDash = false;
    private bool hasShownHijack = false;
    private bool hasShownPractice = false; // 新規追加
    private bool isLeaveAndDashBlinking = false;
    private bool shouldStopLeaveAndDashBlink = false; // 点滅を停止するフラグ
    
    // 新規追加：CanvasGroup
    private CanvasGroup leaveAndDashCanvasGroup;
    private CanvasGroup hijackCanvasGroup;
    private CanvasGroup targetImageCanvasGroup;
    private CanvasGroup practiceCanvasGroup;
    
    // 新規追加：enemy1~3のtargetImageのCanvasGroup
    private CanvasGroup enemy1TargetImageCanvasGroup;
    private CanvasGroup enemy2TargetImageCanvasGroup;
    private CanvasGroup enemy3TargetImageCanvasGroup;
    
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        hand = canvas.transform.Find("Hand").gameObject;
        
        // PlayerControllerの取得
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                previousState = playerController.currentState;
            }
        }
        
        // CanvasGroupがなければ追加
        handCanvasGroup = hand.GetComponent<CanvasGroup>();
        if (handCanvasGroup == null)
        {
            handCanvasGroup = hand.AddComponent<CanvasGroup>();
        }
        
        // 初期状態を透明に設定
        handCanvasGroup.alpha = 0f;
        
        // 新規追加：LeaveAndDash用のCanvasGroupを取得または追加
        if (leaveAndDash != null)
        {
            leaveAndDashCanvasGroup = leaveAndDash.GetComponent<CanvasGroup>();
            if (leaveAndDashCanvasGroup == null)
            {
                leaveAndDashCanvasGroup = leaveAndDash.gameObject.AddComponent<CanvasGroup>();
            }
            leaveAndDashCanvasGroup.alpha = 0f;
        }
        
        // 新規追加：Hijack用のCanvasGroupを取得または追加
        if (hijack != null)
        {
            hijackCanvasGroup = hijack.GetComponent<CanvasGroup>();
            if (hijackCanvasGroup == null)
            {
                hijackCanvasGroup = hijack.gameObject.AddComponent<CanvasGroup>();
            }
            hijackCanvasGroup.alpha = 0f;
        }
        
        // 新規追加：Practice用のCanvasGroupを取得または追加
        if (practice != null)
        {
            practiceCanvasGroup = practice.GetComponent<CanvasGroup>();
            if (practiceCanvasGroup == null)
            {
                practiceCanvasGroup = practice.gameObject.AddComponent<CanvasGroup>();
            }
            practiceCanvasGroup.alpha = 0f;
        }
        
        // 新規追加：TargetImage用のCanvasGroupを取得または追加
        if (targetImage != null)
        {
            targetImageCanvasGroup = targetImage.GetComponent<CanvasGroup>();
            if (targetImageCanvasGroup == null)
            {
                targetImageCanvasGroup = targetImage.gameObject.AddComponent<CanvasGroup>();
            }
            targetImageCanvasGroup.alpha = 1f; // 通常は表示に設定
        }
        
        // 新規追加：enemy1~3のtargetImageのCanvasGroupを取得または追加
        if (enemy1 != null)
        {
            Image enemy1TargetImage = enemy1.GetComponentInChildren<Image>();
            if (enemy1TargetImage != null)
            {
                enemy1TargetImageCanvasGroup = enemy1TargetImage.GetComponent<CanvasGroup>();
                if (enemy1TargetImageCanvasGroup == null)
                {
                    enemy1TargetImageCanvasGroup = enemy1TargetImage.gameObject.AddComponent<CanvasGroup>();
                }
                enemy1TargetImageCanvasGroup.alpha = 1f;
            }
        }
        
        if (enemy2 != null)
        {
            Image enemy2TargetImage = enemy2.GetComponentInChildren<Image>();
            if (enemy2TargetImage != null)
            {
                enemy2TargetImageCanvasGroup = enemy2TargetImage.GetComponent<CanvasGroup>();
                if (enemy2TargetImageCanvasGroup == null)
                {
                    enemy2TargetImageCanvasGroup = enemy2TargetImage.gameObject.AddComponent<CanvasGroup>();
                }
                enemy2TargetImageCanvasGroup.alpha = 1f;
            }
        }
        
        if (enemy3 != null)
        {
            Image enemy3TargetImage = enemy3.GetComponentInChildren<Image>();
            if (enemy3TargetImage != null)
            {
                enemy3TargetImageCanvasGroup = enemy3TargetImage.GetComponent<CanvasGroup>();
                if (enemy3TargetImageCanvasGroup == null)
                {
                    enemy3TargetImageCanvasGroup = enemy3TargetImage.gameObject.AddComponent<CanvasGroup>();
                }
                enemy3TargetImageCanvasGroup.alpha = 1f;
            }
        }
        
        // HandControllerを開始
        HandControllerLoopAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    // Update is called once per frame
    void Update()
    {
        if(slider.value <= 0 && isGameEnded == false)
        {
            isGameEnded = true;
            OnPlayerDead();
        }

        // Dashingになった瞬間を検知
        if (playerController != null)
        {
            if (previousState != PlayerMoveState.Dashing && playerController.currentState == PlayerMoveState.Dashing)
            {
                // Dashingになった瞬間
                if (audioSource != null && dashSound != null)
                {
                    audioSource.PlayOneShot(dashSound);
                }
                
                // 新規追加：MaxSpeedからDashingに移行したら点滅を停止
                if (previousState == PlayerMoveState.MaxSpeed && isLeaveAndDashBlinking)
                {
                    shouldStopLeaveAndDashBlink = true;
                }
            }
            
            // 新規追加：MaxSpeedになったらLeaveAndDashを点滅開始
            if (!hasShownLeaveAndDash && playerController.currentState == PlayerMoveState.MaxSpeed)
            {
                hasShownLeaveAndDash = true;
                LeaveAndDashBlinkAsync(this.GetCancellationTokenOnDestroy()).Forget();
            }
            
            // 現在の状態を記録
            previousState = playerController.currentState;
        }
        
        // 新規追加：プレイヤーのY座標が48に到達したらHijackチュートリアル開始
        if (!hasShownHijack && player != null && player.transform.position.y >= 48f)
        {
            hasShownHijack = true;
            HijackTutorialAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        // 新規追加：プレイヤーのY座標が74に到達したらPracticeチュートリアル開始
        if (!hasShownPractice && player != null && player.transform.position.y >= 74f)
        {
            hasShownPractice = true;
            enemy1.GetComponent<NavMeshAgent>().enabled = true;
            enemy2.GetComponent<NavMeshAgent>().enabled = true;
            enemy3.GetComponent<NavMeshAgent>().enabled = true;
            PracticeTutorialAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        if (mapChanged)
        {
            navMesh.GetComponent<NavMeshSurface>().BuildNavMesh();
            mapChanged = false;
        }
        else return;
    }
    
    // 新規追加：LeaveAndDashを1秒間隔でフェード点滅
    private async UniTask LeaveAndDashBlinkAsync(CancellationToken cancellationToken)
    {
        if (leaveAndDashCanvasGroup == null) return;
        
        isLeaveAndDashBlinking = true;
        float fadeDuration = 0.5f; // フェードイン/アウトの時間
        
        while (isLeaveAndDashBlinking && !cancellationToken.IsCancellationRequested)
        {
            // フェードイン
            float elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                leaveAndDashCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
                await UniTask.Yield(cancellationToken);
            }
            leaveAndDashCanvasGroup.alpha = 1f;
            
            // 0.5秒待機（表示したまま）
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            
            // フェードアウト
            elapsedTime = 0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                leaveAndDashCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                await UniTask.Yield(cancellationToken);
            }
            leaveAndDashCanvasGroup.alpha = 0f;
            
            // 0.5秒待機（非表示のまま）= 合計1秒間隔
            await UniTask.Delay(500, cancellationToken: cancellationToken);
            
            // 新規追加：停止フラグが立っていたら現在のサイクルで終了
            if (shouldStopLeaveAndDashBlink)
            {
                isLeaveAndDashBlinking = false;
                leaveAndDashCanvasGroup.alpha = 0f; // 確実に非表示にする
                break;
            }
        }
    }
    
    // 新規追加：Hijackチュートリアル（スロー演出付き）
    private async UniTask HijackTutorialAsync(CancellationToken cancellationToken)
    {
        if (hijackCanvasGroup == null) return;
        
        // 時間の進みを遅くする
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;
        
        float fadeDuration = 0.3f; // フェードイン/アウトの時間（realtime）
        float displayDuration = 3.0f; // 表示時間（realtime）
        
        // TargetImageの点滅を開始
        if (targetImageCanvasGroup != null)
        {
            TargetImageBlinkAsync(targetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        }
        
        // Hijackをフェードイン
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            hijackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        hijackCanvasGroup.alpha = 1f;
        
        // 3秒表示
        await UniTask.Delay((int)(displayDuration * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);
        
        // Hijackをフェードアウト
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            hijackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        hijackCanvasGroup.alpha = 0f;
        targetImageCanvasGroup.alpha = 0f; // 確実に非表示にする
        
        // 時間の進みを元に戻す
        Time.timeScale = originalTimeScale;
    }
    
    // 新規追加：Practiceチュートリアル（スロー演出付き）
    private async UniTask PracticeTutorialAsync(CancellationToken cancellationToken)
    {
        if (practiceCanvasGroup == null) return;
        
        // 時間の進みを遅くする
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;
        
        float fadeDuration = 0.3f; // フェードイン/アウトの時間（realtime）
        float displayDuration = 3.0f; // 表示時間（realtime）
        
        // enemy1~3のTargetImageの点滅を開始
        if (enemy1TargetImageCanvasGroup != null)
        {
            TargetImageBlinkAsync(enemy1TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        }
        if (enemy2TargetImageCanvasGroup != null)
        {
            TargetImageBlinkAsync(enemy2TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        }
        if (enemy3TargetImageCanvasGroup != null)
        {
            TargetImageBlinkAsync(enemy3TargetImageCanvasGroup, displayDuration + fadeDuration * 2, cancellationToken).Forget();
        }
        
        // Practiceをフェードイン
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            practiceCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        practiceCanvasGroup.alpha = 1f;
        
        // 3秒表示
        await UniTask.Delay((int)(displayDuration * 1000), ignoreTimeScale: true, cancellationToken: cancellationToken);
        
        // Practiceをフェードアウト
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            practiceCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            await UniTask.Yield(cancellationToken);
        }
        practiceCanvasGroup.alpha = 0f;

        // enemy1~3のTargetImageを確実に非表示にする
        enemy1TargetImageCanvasGroup.alpha = 0f;
        enemy2TargetImageCanvasGroup.alpha = 0f;
        enemy3TargetImageCanvasGroup.alpha = 0f;
        
        // 時間の進みを元に戻す
        Time.timeScale = originalTimeScale;
    }
    
    // 新規追加：TargetImageをフェード点滅（汎用化）
    private async UniTask TargetImageBlinkAsync(CanvasGroup canvasGroup, float duration, CancellationToken cancellationToken)
    {
        if (canvasGroup == null) return;
        
        float originalAlpha = canvasGroup.alpha;
        float blinkSpeed = 0.2f; // 点滅の速さ
        float elapsedTime = 0f;
        
        while (elapsedTime < duration && !cancellationToken.IsCancellationRequested)
        {
            // フェードアウト
            float fadeTime = 0f;
            while (fadeTime < blinkSpeed)
            {
                fadeTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(originalAlpha, 0f, fadeTime / blinkSpeed);
                await UniTask.Yield(cancellationToken);
            }
            
            // フェードイン
            fadeTime = 0f;
            while (fadeTime < blinkSpeed)
            {
                fadeTime += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, originalAlpha, fadeTime / blinkSpeed);
                await UniTask.Yield(cancellationToken);
            }
            
            elapsedTime += blinkSpeed * 2;
        }
        
        // 元のアルファ値に戻す
        canvasGroup.alpha = originalAlpha;
    }

    // HandControllerを繰り返す+フラグ管理
    private async UniTask HandControllerLoopAsync(CancellationToken cancellationToken)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        Vector3 initialPosition = hand.transform.localPosition;
        
        while (!stopHandController)
        {
            // プレイヤーが動いていたら停止
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                break;
            }
            
            // 1秒待機
            await UniTask.Delay(1000, cancellationToken: cancellationToken);
            
            // プレイヤーが動いていたら停止
            if (playerRb != null && playerRb.velocity.magnitude > 0f)
            {
                stopHandController = true;
                handCanvasGroup.alpha = 0f;
                hand.SetActive(false);
                break;
            }
            
            // HandAnimationを実行
            await HandAnimationAsync(playerRb, initialPosition, cancellationToken);
            
            // 既に停止フラグが立っていたら終了
            if (stopHandController)
            {
                break;
            }
            
            // Handを3秒かけて下方向に400ずらす
            await MoveHandDownAsync(playerRb, cancellationToken);
            
            // 既に停止フラグが立っていたら終了
            if (stopHandController)
            {
                break;
            }
        }
    }

    private async UniTask HandAnimationAsync(Rigidbody2D playerRb, Vector3 initialPosition, CancellationToken cancellationToken)
    {
        // 既に停止していたら終了
        if (stopHandController) return;
        
        Vector3 startPosition = initialPosition;
        Vector3 targetPosition = startPosition + new Vector3(0, 400f, 0);
        
        float fadeDuration = 0.5f; // フェードイン/アウトの時間
        float moveDuration = 1.0f; // 移動の時間
        
        // フェードイン
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            // プレイヤーが動いたら中断
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
        
        // 上方向に移動
        elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            // プレイヤーが動いたら中断
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
        
        // フェードアウト
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            // プレイヤーが動いたら中断
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

    // Handを3秒かけて下方向に400ずらす
    private async UniTask MoveHandDownAsync(Rigidbody2D playerRb, CancellationToken cancellationToken)
    {
        // 既に停止していたら終了
        if (stopHandController) return;
        
        Vector3 startPosition = hand.transform.localPosition;
        Vector3 targetPosition = startPosition + new Vector3(0, -400f, 0);
        float moveDuration = 3.0f;
        
        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            // プレイヤーが動いたら中断
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

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れた対象がゴールかつ、エイムアシストではない自分自身のColliderが触れた場合にのみクリア処理
        if (collision.gameObject.tag == "Goal" && GetComponent<Collider2D>().IsTouching(collision))
        {
            OnGoalReached();
        }
    }

    //ゴール到達時の挙動
    public void OnGoalReached()
    {
        Debug.Log("Game Clear/Goal");
        if (sceneHandler != null)
        {
            // sceneHandlerからSceneNavigatorスクリプトを取得
            SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

            // スクリプトが見つかったら関数を実行
            if (navigator != null)
            {
                navigator.LoadScene(ResultSceneName);
            }
            else
            {
                Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
            }
        }
    }

    // プレイヤー死亡時の挙動
    public void OnPlayerDead()
    {
        Debug.Log("Game Over/Dead");
        if (sceneHandler != null)
        {
            // sceneHandlerからSceneNavigatorスクリプトを取得
            SceneNavigator navigator = sceneHandler.GetComponent<SceneNavigator>();

            // スクリプトが見つかったら関数を実行
            if (navigator != null)
            {
                navigator.LoadScene(GameOverSceneName);
            }
            else
            {
                Debug.LogError("sceneHandlerにSceneNavigatorが見つかりません！");
            }
        }
    }
}