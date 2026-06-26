using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundInformation", menuName = "Scriptable Objects/SoundInformation")]
public class SoundInformation : ScriptableObject
{
    public AudioClip Clip;
    public string SoundName;
}
