using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class SettingController : MonoBehaviour
{
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
        bgmSlider = settingRoot.transform.Find("BGMSlider").GetComponent<Slider>();
        seSlider = settingRoot.transform.Find("SESlider").GetComponent<Slider>();
        sizeSlider = settingRoot.transform.Find("SizeSlider").GetComponent<Slider>();
        backButton = settingRoot.transform.Find("BackButton").GetComponent<Button>();
        resetButton = settingRoot.transform.Find("ResetButton").GetComponent<Button>();
        resetContainer = settingRoot.transform.Find("ResetContainer").gameObject;
        yesButton = resetContainer.transform.Find("YesButton").GetComponent<Button>();
        noButton = resetContainer.transform.Find("NoButton").GetComponent<Button>();
        muteButton = settingRoot.transform.Find("MuteButton").GetComponent<CheckButton>();
        displayButton = settingRoot.transform.Find("DisplayButton").GetComponent<CheckButton>();
        fixedButton = settingRoot.transform.Find("FixedButton").GetComponent<CheckButton>();
        vibrationButton = settingRoot.transform.Find("VibrationButton").GetComponent<CheckButton>();

        // チェックがつかないボタンにクリックイベントを登録
        backButton.onClick.AddListener(PushBackButton);
        resetButton.onClick.AddListener(PushResetButton);
        yesButton.onClick.AddListener(PushYesButton);
        noButton.onClick.AddListener(PushNoButton);

        //いったん起動
        settingContainer.SetActive(true);

        // オンオフボタンにそれぞれのメソッドを紐付ける
        muteButton.Setup(false, MuteChanged);
        displayButton.Setup(true, DisplayChanged);
        fixedButton.Setup(false, FixedChanged);
        vibrationButton.Setup(false, VibrationChanged);

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
        Debug.Log($"JoyStickサイズ変更: {intValue} -> 倍率 {targetScale}");
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

        // 見た目のリセット
        muteButton.Setup(false, MuteChanged);
        displayButton.Setup(true, DisplayChanged);
        fixedButton.Setup(false, FixedChanged);
        vibrationButton.Setup(false, VibrationChanged);

        // 中身のリセットを直接呼び出す
        MuteChanged(false);
        DisplayChanged(true);
        FixedChanged(false);
        VibrationChanged(false);

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