using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelAudioHandler : MonoBehaviour
{
    [SerializeField] private string _audioToPlay = "MenuMusic";
    [SerializeField]
    [Tooltip("The name of the scene this script is in")]
    private string _levelName;
    private void HandleLevelLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _levelName)
        {
            AudioManager.Instance.PlayMainAudio(_audioToPlay);
            AudioManager.Instance.PlayAmbiance("Ambiance");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleLevelLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleLevelLoaded;
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMainAudio(_audioToPlay);
        }
    }
}
