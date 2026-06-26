using NUnit.Framework;
using System.Collections;
using UnityEngine;

public enum AudioSourceType
{
    AST_INVALID = -1,
    AST_MAIN,
    AST_REPEATING,
    AST_ONESHOT,
    AST_AMBIANCE
}

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get { return m_instance; } }

    [SerializeField] private AudioSource _mainAudioSource;
    [SerializeField] private float _mainVolumeInitial = 1.0f;

    [SerializeField] private AudioSource _repeatingSFXSource;
    [SerializeField] private float _repeatingSFXVolumeInitial = 0.5f;

    [SerializeField] private AudioSource _oneShotSource;
    [SerializeField] private float _oneshotVolumeInitial = 0.5f;

    [SerializeField] private AudioSource _ambianceSource;
    [SerializeField] private float _ambianceVolumeInitial = 0.5f;

    private static AudioManager m_instance;

    [SerializeField] private LoadedSounds _loadedSounds;
    private Coroutine _mainAudioCoroutine = null;
    private Coroutine _ambianceCoroutine = null;

    public void SetAudioSourceVolume(AudioSourceType type, float volume)
    {
        switch(type)
        {
            case AudioSourceType.AST_MAIN:
                _mainAudioSource.volume = volume;
                break;
            case AudioSourceType.AST_REPEATING:
                _repeatingSFXSource.volume = volume;
                break;
            case AudioSourceType.AST_ONESHOT:
                _oneShotSource.volume = volume;
                break;
            case AudioSourceType.AST_AMBIANCE:
                _ambianceSource.volume = volume;
                break;
        }
    }

    public bool IsAudioPlaying()
    {
        return _mainAudioSource.isPlaying;
    }

    public void AdjustAudio(float value)
    {
        _mainAudioSource.volume = Mathf.Clamp01(value);
    }

    public void PlayMoveSFX(MoveType moveType)
    {
        switch (moveType)
        {
            case MoveType.MOVE_TYPE_PLUS_ONE:
                PlayAudioOneShot("PlusOneSFX", _oneshotVolumeInitial);
                break;
            case MoveType.MOVE_TYPE_SKIP:
                PlayAudioOneShot("SkipSFX", _oneshotVolumeInitial);
                break;
            case MoveType.MOVE_TYPE_REVERSE:
                PlayAudioOneShot("ReverseSFX", _oneshotVolumeInitial);
                break;
            case MoveType.MOVE_TYPE_CHAOS:
                PlayAudioOneShot("ChaosSFX", _oneshotVolumeInitial);
                break;
        }
    }

    // For music and sound tracks
    public void PlayMainAudio(string name, bool looping = false)
    {
        _mainAudioSource.volume = PlayerPrefs.GetFloat("Volume");
        Debug.Log("Playing Audioclip at volume " +  _mainAudioSource.volume);

        AudioClip clip = _loadedSounds.TryGetAudioClip(name);
        if (clip == null)
        {
            Debug.LogWarning($"Missing audio clip: {name}. Check that it exists");
            return;
        }

        if (_mainAudioCoroutine != null)
        {
            StopMainAudio();
        }

        _mainAudioSource.clip = clip;
        _mainAudioSource.loop = looping;
        _mainAudioSource.Play();

        _mainAudioCoroutine = StartCoroutine(SelectNextMainTrack());
    }

    public void StopMainAudio()
    {
        _mainAudioSource.Stop();
        StopCoroutine(_mainAudioCoroutine);
        _mainAudioCoroutine = null;
    }

    public void PlayAmbiance(string name)
    {
        AudioClip clip = _loadedSounds.TryGetAudioClip(name);
        if (clip == null)
        {
            Debug.LogWarning($"Missing audio clip: {name}. Check that it exists");
            return;
        }

        if (_ambianceCoroutine != null)
        {
            StopAmbiance();
        }

        _ambianceSource.clip = clip;
        _ambianceSource.loop = true;
        _ambianceSource.Play();

        _ambianceCoroutine = StartCoroutine(SelectNextAmbianceTrack());
    }

    public void StopAmbiance()
    {
        _ambianceSource.Stop();
        StopCoroutine(_ambianceCoroutine);
        _ambianceCoroutine = null;
    }

    public void PlayRepeatingOneShot(string name)
    {
        AudioClip clip = _loadedSounds.TryGetAudioClip(name);
        if (clip == null)
        {
            Debug.LogWarning($"Missing audio clip: {name}. Check that it exists");
            return;
        }

        _repeatingSFXSource.clip = clip;
        _repeatingSFXSource.loop = true;
        _repeatingSFXSource.Play();
    }

    public void StopPlayingRepeatingOneShot()
    {
        _repeatingSFXSource.Stop();
    }

    public void PlayAudioOneShot(string name, float scale = 1.0f)
    {
        _oneShotSource.volume = PlayerPrefs.GetFloat("Volume");
        Debug.Log("Playing Audioclip at volume " + _oneShotSource.volume);

        AudioClip clip = _loadedSounds.TryGetAudioClip(name);
        if (clip == null)
        {
            Debug.LogWarning($"Missing audio clip: {name}. Check that it exists");
            return;
        }
        _oneShotSource.PlayOneShot(clip, scale);
    }

    private IEnumerator SelectNextMainTrack()
    {
        yield return new WaitUntil(() => !_mainAudioSource.isPlaying && _mainAudioSource.time == 0);
        Debug.Log("Track Has Finished!");

        float waitDurationSeconds = Random.Range(5f, 15f);
        yield return new WaitForSecondsRealtime(waitDurationSeconds);
        Debug.Log("Done with Delay! Playing next song");

        int randomMusicIndex = Random.Range(0, _loadedSounds.Music.Count);

        PlayMainAudio(_loadedSounds.Music[randomMusicIndex].SoundName);
    }

    private IEnumerator SelectNextAmbianceTrack()
    {
        yield return new WaitUntil(() => !_ambianceSource.isPlaying && _ambianceSource.time == 0);

        float waitDurationSeconds = Random.Range(5f, 15f);
        yield return new WaitForSecondsRealtime(waitDurationSeconds);

        int randomMusicIndex = Random.Range(0, _loadedSounds.BackgroundNoise.Count);

        PlayAmbiance(_loadedSounds.BackgroundNoise[randomMusicIndex].SoundName);
    }


    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;
        DontDestroyOnLoad(gameObject);

        if (_mainAudioSource == null)
        {
            _mainAudioSource = gameObject.GetComponent<AudioSource>();
        }

        if (_oneShotSource == null)
        {
            Debug.LogWarning("OneShot source was set to null, we're doing expensive lookup");
            GameObject oneShotChild = GameObject.Find("OneShot");
            _oneShotSource = oneShotChild.GetComponent<AudioSource>();
        }

        if (_repeatingSFXSource == null)
        {
            Debug.LogWarning("repeating oneshot source was set to null, we're doing expensive lookup");
            GameObject repeatedChild = GameObject.Find("RepeatedOneShot");
            _repeatingSFXSource = repeatedChild.GetComponent<AudioSource>();
        }

        if (_ambianceSource == null)
        {
            Debug.LogWarning("Ambiance source was set to null, we're doing expensive lookup");
            GameObject ambianceChild = GameObject.Find("Ambiance");
            _ambianceSource = ambianceChild.GetComponent<AudioSource>();
        }

        _mainAudioSource.playOnAwake = false;
        _loadedSounds.InitializeSoundInfo();
#if UNITY_EDITOR
        Assert.IsNotNull(_mainAudioSource);
        Assert.IsNotNull(_repeatingSFXSource);
        Assert.IsNotNull(_oneShotSource);
        Assert.IsNotNull(_ambianceSource);
#endif
        _mainAudioSource.volume = _mainVolumeInitial;
        _repeatingSFXSource.volume = _repeatingSFXVolumeInitial;
        _oneShotSource.volume = _oneshotVolumeInitial;
        _ambianceSource.volume = _ambianceVolumeInitial;
    }
}
