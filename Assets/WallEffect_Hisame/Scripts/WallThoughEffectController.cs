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
    }

    private void PlayEffect()
    {
        StartCoroutine(ThroughEffect());
    }

    private IEnumerator ThroughEffect()
    {
        foreach(var p in particles) p.Play();
        yield return new WaitForSeconds(effectDuration);
        Destroy(this.gameObject);
    }
}
