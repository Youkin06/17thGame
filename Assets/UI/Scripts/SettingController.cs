using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

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
    private CheckButton muteButton;
    private CheckButton displayButton;
    private CheckButton fixedButton;
    private CheckButton vibrationButton;

    // Start is called before the first frame update
    void Start()
    {
        bgmAudioSource = GameObject.Find("BGMAudioSource").GetComponent<AudioSource>();
        bgmSlider = settingRoot.transform.Find("BGMSlider").GetComponent<Slider>();
        seSlider = settingRoot.transform.Find("SESlider").GetComponent<Slider>();
        sizeSlider = settingRoot.transform.Find("SizeSlider").GetComponent<Slider>();
        backButton = settingRoot.transform.Find("BackButton").GetComponent<Button>();
        muteButton = settingRoot.transform.Find("MuteButton").GetComponent<CheckButton>();
        displayButton = settingRoot.transform.Find("DisplayButton").GetComponent<CheckButton>();
        fixedButton = settingRoot.transform.Find("FixedButton").GetComponent<CheckButton>();
        vibrationButton = settingRoot.transform.Find("VibrationButton").GetComponent<CheckButton>();

        // 戻るボタンにクリックイベントを登録
        backButton.onClick.AddListener(PushBackButton);

        //いったん起動
        settingContainer.SetActive(true);

        // オンオフボタンにそれぞれのメソッドを紐付ける
        muteButton.Setup(false, MuteChanged);
        displayButton.Setup(true, DisplayChanged);
        fixedButton.Setup(false, FixedChanged);
        vibrationButton.Setup(false, VibrationChanged);

        //非表示にしておく
        settingContainer.SetActive(false);

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
