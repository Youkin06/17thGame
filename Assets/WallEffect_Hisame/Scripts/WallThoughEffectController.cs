using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallThoughEffectController : MonoBehaviour
{
    /*
    エフェクト再生するためのクラス
    衝突した際にInstantiateでタイルの位置に生成して使用する(再生はStartで自動)
    生成する際に、衝突した方向に応じて回転を下:z=0、右z=90、上z=180、左z=-90にする
    */

    [SerializeField] float effectDuration = 1f; // エフェクトの再生時間
    private ParticleSystem[] particles;
    
    // Start is called before the first frame update
    void Start()
    {   
        particles = GetComponentsInChildren<ParticleSystem>();
        PlayEffect();
        DeactivateEffect();
    }

    public void PlayEffect()
    {
        foreach(var p in particles) p.Play();
    }

    public void StopEffect()
    {
        foreach(var p in particles) p.Stop();
    }

    public void ActivateEffect()
    {
        foreach(Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        PlayEffect();
        Debug.Log("Activate Effect");
    }

    public void DeactivateEffect()
    {
        foreach(Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }
}
