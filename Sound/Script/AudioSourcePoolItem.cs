using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(AudioSource))]
public class AudioSourcePoolItem : MonoBehaviour
{
    public static ObjectPool<AudioSource> Pool;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (Pool != null && _audioSource.time >= _audioSource.clip.length)
        {
            Pool.Release(_audioSource);
        }
    }
}
