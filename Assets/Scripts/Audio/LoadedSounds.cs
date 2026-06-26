using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;

[CreateAssetMenu(fileName = "LoadedSounds", menuName = "Scriptable Objects/LoadedSounds")]
public class LoadedSounds : ScriptableObject
{
    public List<SoundInformation> OneShots = new List<SoundInformation>();
    public List<SoundInformation> BackgroundNoise = new List<SoundInformation>();
    public List<SoundInformation> Music = new List<SoundInformation>();

    private Dictionary<string, AudioClip> _nameToAudioClip = new Dictionary<string, AudioClip>();

    public void InitializeSoundInfo()
    {
        if (_nameToAudioClip.Count > 0)
        {
            return;
        }

        int oneshotSize = OneShots.Count;
        foreach (SoundInformation soundInfo in OneShots)
        {
            if (!_nameToAudioClip.TryAdd(soundInfo.SoundName, soundInfo.Clip))
            {
                Debug.LogError($"Failed to add clip {soundInfo.SoundName}. Element already exists");
            }
        }

        int backgroundNoiseSize = BackgroundNoise.Count;
        foreach (SoundInformation soundInfo in BackgroundNoise)
        {
            if (!_nameToAudioClip.TryAdd(soundInfo.SoundName, soundInfo.Clip))
            {
                Debug.LogError($"Failed to add clip {soundInfo.SoundName}. Element already exists");
            }
        }

        int musicSize = Music.Count;
        foreach (SoundInformation soundInfo in Music)
        {
            if (!_nameToAudioClip.TryAdd(soundInfo.SoundName, soundInfo.Clip))
            {
                Debug.LogError($"Failed to add clip {soundInfo.SoundName}. Element already exists");
            }
        }
#if UNITY_EDITOR
        Assert.IsTrue(_nameToAudioClip.Count == (oneshotSize + backgroundNoiseSize + musicSize), "We failed to add some sound");
#endif
    }

    public AudioClip TryGetAudioClip(string name)
    {
        if (_nameToAudioClip.TryGetValue(name, out var clip))
        {
            return clip;
        }

        return null;
    }
}
