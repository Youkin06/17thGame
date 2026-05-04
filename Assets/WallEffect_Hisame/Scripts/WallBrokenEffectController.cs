using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WallBrokenEffectController : MonoBehaviour
{
    [SerializeField] float effectDuration = 1f; // エフェクトの再生時間
    private ParticleSystem _particleSystem;

    // Start is called before the first frame update
    void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();    
        StartCoroutine(BrokenEffect());  
    }

    private void PlayEffect()
    {
        StartCoroutine(BrokenEffect());
    }

    private IEnumerator BrokenEffect()
    {
        _particleSystem.Play();
        yield return new WaitForSeconds(effectDuration);
        Destroy(this.gameObject);
    }
}
