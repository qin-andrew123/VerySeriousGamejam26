using System.Data;
using UnityEngine;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource m_audioSource;
    private static AudioManager m_instance;
    public static AudioManager Instance { get { return m_instance; } }

    public void PlayAudio(AudioClip audioClip, bool looping = false)
    {
        m_audioSource.volume = PlayerPrefs.GetFloat("Volume");
        Debug.Log("Playing Audioclip at volume " +  m_audioSource.volume);
        m_audioSource.clip = audioClip;
        m_audioSource.loop = looping;
        m_audioSource.Play();
    }

    public void PlayAudioOneShot(AudioClip audioClip, float scale = 1.0f)
    {
        m_audioSource.volume = PlayerPrefs.GetFloat("Volume");
        Debug.Log("Playing Audioclip at volume " + m_audioSource.volume);
        m_audioSource.clip = audioClip;
        m_audioSource.PlayOneShot(audioClip, scale);
    }

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this;

        m_audioSource.playOnAwake = false;
    }
}
