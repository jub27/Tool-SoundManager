using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.AI;
using UnityEngine.Pool;

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    private AudioSource _audioSourcePrefab;
    [SerializeField]
    private AudioMixer _audioMixer;
    [SerializeField]
    private AudioMixerGroup _bgmMixerGroup;
    [SerializeField]
    private AudioMixerGroup _seMixerGroup;

    private Coroutine _bgmChangeCoroutine;

    // Collection checks will throw errors if we try to release an item that is already in the pool.
    public bool collectionChecks = true;
    public int maxPoolSize;
    public int defaultPoolSize;

    private AudioSource _bgmAudioSource = null;
    private const float VOLUME_FADE_TIME = 1.0f;
    private Dictionary<AudioClip, AudioSource> _recentAudioSourceDictionary;

    public static SoundManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
            return;
        }
        AudioSourcePoolItem.Pool = new ObjectPool<AudioSource>(CreatePooledItem, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, true, defaultPoolSize, maxPoolSize);
        _recentAudioSourceDictionary = new Dictionary<AudioClip, AudioSource>();
    }

    private AudioSource CreatePooledItem()
    {
        AudioSource audioSource = Instantiate(_audioSourcePrefab);
        return audioSource;
    }

    // Called when an item is returned to the pool using Release
    private void OnReturnedToPool(AudioSource audioSource)
    {
        if(_recentAudioSourceDictionary[audioSource.clip] == audioSource)
        {
            _recentAudioSourceDictionary[audioSource.clip] = null;
        }
        audioSource.gameObject.SetActive(false);
    }

    private void OnTakeFromPool(AudioSource audioSource)
    {
        audioSource.volume = 1.0f;
        audioSource.clip = null;
        audioSource.loop = false;
        audioSource.gameObject.SetActive(true);
    }

    // If the pool capacity is reached then any items returned will be destroyed.
    // We can control what the destroy behavior does, here we destroy the GameObject.
    private void OnDestroyPoolObject(AudioSource audioSource)
    {
        Destroy(audioSource.gameObject);
    }

    private AudioSource PlayAudioClip(AudioClip audioClip, AudioMixerGroup audioMixerGroup ,bool loop = false)
    {
        AudioSource audioSource = AudioSourcePoolItem.Pool.Get();
        audioSource.outputAudioMixerGroup = audioMixerGroup;
        audioSource.clip = audioClip;
        audioSource.loop = loop;
        audioSource.Play();

        if (_recentAudioSourceDictionary.ContainsKey(audioClip))
        {
            _recentAudioSourceDictionary[audioClip] = audioSource;
        }
        else
        {
            _recentAudioSourceDictionary.Add(audioClip, audioSource);
        }

        return audioSource;
    }

    private IEnumerator ChangeBGM(AudioClip audioClip, bool loop)
    {
        while (_bgmAudioSource != null && _bgmAudioSource.isPlaying && _bgmAudioSource.volume > 0)
        {
            _bgmAudioSource.volume = Mathf.Max(_bgmAudioSource.volume - (Time.deltaTime / VOLUME_FADE_TIME), 0);
            yield return null;
        }
        AudioSourcePoolItem.Pool.Release(_bgmAudioSource);

        _bgmAudioSource = PlayAudioClip(audioClip, _bgmMixerGroup ,loop);
        _bgmAudioSource.volume = 0;

        while (_bgmAudioSource.volume < 1.0f)
        {
            _bgmAudioSource.volume = Mathf.Min(_bgmAudioSource.volume + (Time.deltaTime / VOLUME_FADE_TIME), 1.0f);
            yield return null;
        }
    }

    public void PlayBGM(AudioClip audioClip, bool loop = false)
    {
        if (_bgmAudioSource == null || _bgmAudioSource.isPlaying == false)
        {
            _bgmAudioSource = PlayAudioClip(audioClip, _bgmMixerGroup,loop);
        }
        else
        {
            if (_bgmChangeCoroutine != null)
                StopCoroutine(_bgmChangeCoroutine);
            _bgmChangeCoroutine = StartCoroutine(ChangeBGM(audioClip, loop));
        }
    }

    public void PlaySE(AudioClip audioClip, bool playOverlap)
    {
        if (playOverlap)
        {
            PlayAudioClip(audioClip, _seMixerGroup);
        }
        else
        {
            if (_recentAudioSourceDictionary.ContainsKey(audioClip) && _recentAudioSourceDictionary[audioClip] != null)
            {
                _recentAudioSourceDictionary[audioClip].Play();
            }
            else
            {
                PlayAudioClip(audioClip, _seMixerGroup);
            }
        }
    }
    
    public void SetBgmVolume(float value)
    {
        value = Mathf.Clamp(value, 0, 1.0f);
        value = Mathf.Lerp(-80f, 0, value);
        _audioMixer.SetFloat("Bgm", value);
    }

    public void SetSeVolume(float value)
    {
        value = Mathf.Clamp(value, 0, 1.0f);
        value = Mathf.Lerp(-80f, 0, value);
        _audioMixer.SetFloat("Se", value);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp(value, 0, 1.0f);
        value = Mathf.Lerp(-80f, 0, value);
        _audioMixer.SetFloat("Master", value);
    }
}