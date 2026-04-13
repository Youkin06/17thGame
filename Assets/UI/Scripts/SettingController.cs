using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class SettingController : MonoBehaviour
{
    private T FindComponentInChildrenByName<T>(Transform root, string objectName) where T : Component
    {
        Transform target = root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == objectName);

        if (target == null)
        {
            Debug.LogError($"{objectName} が見つかりません。探索開始地点: {root.name}");
            return null;
        }

        // まず自身に付いているか確認
        T component = target.GetComponent<T>();

        // なければ子からも探す
        if (component == null)
        {
            component = target.GetComponentInChildren<T>(true);
        }

        if (component == null)
        {
            Debug.LogError($"{objectName} は見つかりましたが、自身および子階層に {typeof(T).Name} が付いていません。");
            return null;
        }

        return component;
    }
    [SerializeField] private GameObject joyStick; 
    [SerializeField] private GameObject settingContainer;
    [Tooltip("Slider, Buttonなどが入っている親オブジェクトを指定")]
    [SerializeField] private GameObject settingRoot;

    private AudioSource bgmAudioSource;
    private Slider bgmSlider;
    private Slider seSlider;
    private Slider sizeSlider;
    private Button backButton;
    private Button resetButton;
    private GameObject resetContainer;
    private Button yesButton;
    private Button noButton;
    private CheckButton muteButton;
    private CheckButton displayButton;
    private CheckButton fixedButton;
    private CheckButton vibrationButton;

    void Start()
    {
        bgmAudioSource = GameObject.Find("BGMAudioSource").GetComponent<AudioSource>();
        if (settingRoot == null)
        {
            Debug.LogError("settingRoot が Inspector で未設定です。");
            return;
        }
        bgmSlider = FindComponentInChildrenByName<Slider>(settingRoot.transform, "BGMSlider");
        seSlider = FindComponentInChildrenByName<Slider>(settingRoot.transform, "SESlider");
        sizeSlider = FindComponentInChildrenByName<Slider>(settingRoot.transform, "SizeSlider");
        backButton = FindComponentInChildrenByName<Button>(settingRoot.transform, "BackButton");
        resetButton = FindComponentInChildrenByName<Button>(settingRoot.transform, "ResetButton");

        Transform resetContainerTransform = settingRoot.transform.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "ResetContainer");
        if (resetContainerTransform == null)
        {
            Debug.LogError($"ResetContainer が見つかりません。探索開始地点: {settingRoot.name}");
            return;
        }
        resetContainer = resetContainerTransform.gameObject;

        yesButton = FindComponentInChildrenByName<Button>(resetContainer.transform, "YesButton");
        noButton = FindComponentInChildrenByName<Button>(resetContainer.transform, "NoButton");
        muteButton = FindComponentInChildrenByName<CheckButton>(settingRoot.transform, "MuteButton");
        displayButton = FindComponentInChildrenByName<CheckButton>(settingRoot.transform, "DisplayButton");
        fixedButton = FindComponentInChildrenByName<CheckButton>(settingRoot.transform, "FixedButton");
        vibrationButton = FindComponentInChildrenByName<CheckButton>(settingRoot.transform, "VibrationButton");

        if (bgmSlider == null ||
            seSlider == null ||
            sizeSlider == null ||
            backButton == null ||
            resetButton == null ||
            resetContainer == null ||
            yesButton == null ||
            noButton == null ||
            muteButton == null ||
            displayButton == null ||
            fixedButton == null ||
            vibrationButton == null)
        {
            Debug.LogError("SettingController の初期化に失敗しました。必要な UI が見つかっていません。");
            return;
        }

        // チェックがつかないボタンにクリックイベントを登録
        backButton.onClick.AddListener(PushBackButton);
        resetButton.onClick.AddListener(PushResetButton);
        yesButton.onClick.AddListener(PushYesButton);
        noButton.onClick.AddListener(PushNoButton);

        //いったん起動
        settingContainer.SetActive(true);

        // オンオフボタンにそれぞれのメソッドを紐付ける
        muteButton.Setup(MuteChanged);
        displayButton.Setup(DisplayChanged);
        fixedButton.Setup(FixedChanged);
        vibrationButton.Setup(VibrationChanged);

        muteButton.SetState(false, notify: true, instantVisual: true);
        displayButton.SetState(true, notify: true, instantVisual: true);
        fixedButton.SetState(false, notify: true, instantVisual: true);
        vibrationButton.SetState(false, notify: true, instantVisual: true);

        //非表示にしておく
        settingContainer.SetActive(false);
        resetContainer.SetActive(false);

        //BGMの初期値
        bgmAudioSource.volume = bgmSlider.value;
        //音量が変わった時だけ変更
        bgmSlider.onValueChanged.AddListener((vol) =>
        {
            bgmAudioSource.volume = vol;
        });

        //Sizeの初期値
        JoyStickSizeChanged(sizeSlider.value);
        //Sizeが変わるときだけ調整
        sizeSlider.onValueChanged.AddListener(JoyStickSizeChanged);
    }

    void SettingContainerOpen()
    {
        settingContainer.SetActive(true);
    }

    void JoyStickSizeChanged(float value)
    {
        float targetScale = 1.0f;
        //誤差防止
        int intValue = Mathf.RoundToInt(value);

        float scaleSmall = 0.75f;
        float scaleMedium = 1.0f;
        float scaleLarge = 1.3f;

        // 値に応じたサイズを選ぶ
        switch (intValue)
        {
            case 0:
                targetScale = scaleSmall;
                break;
            case 1:
                targetScale = scaleMedium;
                break;
            case 2:
                targetScale = scaleLarge;
                break;
        }

        joyStick.transform.localScale = Vector3.one * targetScale;
    }

    void PushBackButton()
    {
        settingContainer.SetActive(false);
    }

    void PushResetButton()
    {
        resetContainer.SetActive(true);
    }

    void PushYesButton()
    {
        // スライダーの値をリセット
        bgmSlider.value = 0.5f;
        seSlider.value = 0.5f;
        sizeSlider.value = 1;

        // 状態と見た目をリセット
        muteButton.SetState(false, notify: true, instantVisual: true);
        displayButton.SetState(true, notify: true, instantVisual: true);
        fixedButton.SetState(false, notify: true, instantVisual: true);
        vibrationButton.SetState(false, notify: true, instantVisual: true);

        resetContainer.SetActive(false);
    }

    void PushNoButton()
    {
        resetContainer.SetActive(false);
    }

    void MuteChanged(bool isOn)
    {
        if (isOn)
        {
            AudioListener.volume = 0;
        }
        else
        {
            AudioListener.volume = 1;
        }
    }

    void DisplayChanged(bool isOn)
    {
        if (isOn)
        {

        }
        else
        {

        }
    }

    void FixedChanged(bool isOn)
    {
        if (isOn)
        {

        }
        else
        {

        }
    }

    void VibrationChanged(bool isOn)
    {
        if (isOn)
        {

        }
        else
        {

        }
    }
}
