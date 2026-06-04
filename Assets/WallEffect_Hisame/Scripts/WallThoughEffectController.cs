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
    private ParticleSystem particle_kirakira; // キラキラエフェクト
    private ParticleSystem particle_bubble; // 泡エフェクト
    
    // Start is called before the first frame update
    void Start()
    {   
        particle_kirakira = transform.Find("ThroughWallEffect_kirakira").GetComponent<ParticleSystem>();
        particle_bubble = transform.Find("ThroughWallEffect_bubble").GetComponent<ParticleSystem>();
    }

    /* 壁に入った時に呼ぶメソッド */
    public void EnterEffect()
    {
        ActivateEffect(new GameObject[]{particle_kirakira.gameObject, particle_bubble.gameObject});
        PlayEffect(new ParticleSystem[]{particle_kirakira, particle_bubble});
    }

    /* 壁から出た時に呼ぶメソッド */
    public void ExitEffect()
    {
        StopEffect(new ParticleSystem[]{particle_kirakira});
        DeactivateEffect(new GameObject[]{particle_bubble.gameObject});
    }

    /* エフェクト再生メソッド */
    void PlayEffect(ParticleSystem[] particles)
    {
        foreach(var p in particles) p.Play();
    }
    /* エフェクト停止メソッド */
    void StopEffect(ParticleSystem[] particles)
    {
        foreach(var p in particles) p.Stop();
    }
    /* エフェクト有効化メソッド */
    void ActivateEffect(GameObject[] effects)
    {
        foreach(GameObject effect in effects)
        {
            effect.SetActive(true);
        }
        Debug.Log("Activate Effect");
    }

    /* エフェクト無効化メソッド */
    void DeactivateEffect(GameObject[] effects)
    {
        foreach(GameObject effect in effects)
        {
            effect.SetActive(false);
        }
    }

    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }
}
