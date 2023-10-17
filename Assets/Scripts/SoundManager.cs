using System.Collections.Generic;
using UnityEngine;

public enum SoundType
{
    TypeSelect,
    TypeMove,
    TypePop,
    TypeGameOver,
    TypeNoMatch
};

public class SoundManager : MonoBehaviour
{
    public List<AudioClip> clips;
    public static SoundManager Instance;
    AudioSource Source;
    public AudioSource bgm; // link this with a bgm prefab, add tag "BGM", loop, don't play on awake

    private void Awake()
    {
        Instance = this;
        Source = GetComponent<AudioSource>();
        GameObject currentBGM = GameObject.FindGameObjectWithTag("BGM");
        if (currentBGM == null)
        {
            AudioSource spawned = Instantiate(bgm);
            spawned.Play();
            DontDestroyOnLoad(spawned);
        }
    }

    public void PlaySound(SoundType clipType)
    {
        Source.PlayOneShot(clips[(int)clipType]);
    }
}