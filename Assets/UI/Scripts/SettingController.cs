using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    public GameObject joyStick;
    [SerializeField] private GameObject settingContainer;
    [SerializeField] private GameObject bgmObj;
    [SerializeField] private GameObject bgmAudioObj;
    [SerializeField] private GameObject seObj;
    [SerializeField] private GameObject backObj;
    [SerializeField] private GameObject resetObj;
    [SerializeField] private GameObject muteObj;
    [SerializeField] private GameObject displayObj;
    [SerializeField] private GameObject fixedObj;
    [SerializeField] private GameObject sizeObj;

    [SerializeField] private GameObject vibrationObj;

    // Start is called before the first frame update
    void Start()
    {
        joyStick = GameObject.Find("Dynamic Joystick");

        settingContainer = GameObject.Find("SettingScrollView");

        bgmObj = GameObject.Find("BGMSlider");
        Slider bgmSlider = bgmObj.GetComponent<Slider>();
        bgmAudioObj = GameObject.Find("BGMAudioSource");
        AudioSource bgmAudioSource = bgmAudioObj.GetComponent<AudioSource>();

        seObj = GameObject.Find("SESlider");
        Slider seSlider = seObj.GetComponent<Slider>();

        sizeObj = GameObject.Find("SizeSlider");
        Slider sizeSlider = sizeObj.GetComponent<Slider>();

        backObj = GameObject.Find("BackButton");
        Button backButton = backObj.GetComponent<Button>();
        backButton.onClick.AddListener(PushBackButton);

        resetObj = GameObject.Find("ResetButton");
        Button resetButton = resetObj.GetComponent<Button>();


        //オンオフボタン
        muteObj = GameObject.Find("MuteButton");
        muteObj.GetComponent<CheckButton>().Setup(false,MuteChanged);

        displayObj = GameObject.Find("DisplayButton");
        displayObj.GetComponent<CheckButton>().Setup(true,DisplayChanged);

        fixedObj = GameObject.Find("FixedButton");
        fixedObj.GetComponent<CheckButton>().Setup(false,FixedChanged);

        vibrationObj = GameObject.Find("VibrationButton");
        vibrationObj.GetComponent<CheckButton>().Setup(false,VibrationChanged);

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
