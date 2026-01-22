using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingController : MonoBehaviour
{
    [SerializeField] private GameObject settingContainer;
    [SerializeField] private GameObject backObj;
    [SerializeField] private GameObject resetObj;
    [SerializeField] private GameObject muteObj;
    [SerializeField] private GameObject displayObj;
    [SerializeField] private GameObject fixedObj;
    [SerializeField] private GameObject vibrationObj;

    // Start is called before the first frame update
    void Start()
    {
        settingContainer = GameObject.Find("SettingScrollView");

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
