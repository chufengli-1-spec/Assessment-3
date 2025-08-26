using System.Collections;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public AudioClip introBGM; 
    public AudioClip normalStateBGM; 
    public float introPlayTime = 3f; 

    private AudioSource audioSource;

    void Start()
    {
    
        audioSource = GetComponent<AudioSource>(); 
        StartCoroutine(PlayIntroThenNormalBGM());
    }

    IEnumerator PlayIntroThenNormalBGM()
    {
        audioSource.clip = introBGM;
        audioSource.Play();
        yield return new WaitForSeconds(Mathf.Min(introBGM.length, introPlayTime));

        audioSource.clip = normalStateBGM;
        audioSource.loop = true; 
        audioSource.Play();
    }
}