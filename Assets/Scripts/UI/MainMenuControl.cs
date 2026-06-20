using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenuControl : MonoBehaviour
{
    [SerializeField] private string m_firstLevelName = "LevelOne";
    private VisualElement m_rootUI;

    // Main Menu Layout
    private VisualElement m_menuRoot;
    private Button m_playButton;
    private Button m_settingsButton;
    private Button m_quitButton;

    // Loading Bar
    private VisualElement m_loadingRoot;
    private ProgressBar m_progressBar;

    // Settings
    private VisualElement m_settingsRoot;
    private Slider m_volumeSlider;
    private Button m_settingsBackButton;
    private Button m_settingsApplyButton;

    private void InitializeMainMenu()
    {
        m_menuRoot = m_rootUI.Q<VisualElement>("MenuElements");
        m_playButton = m_rootUI.Q<Button>("Play");
        m_settingsButton = m_rootUI.Q<Button>("Settings");
        m_quitButton = m_rootUI.Q<Button>("Quit");

        m_playButton.clicked += OnPlayButtonClicked;
        m_settingsButton.clicked += OnSettingsButtonClicked;
        m_quitButton.clicked += OnQuitButtonClicked;
    }
    private void InitializeLoadingBar()
    {
        m_loadingRoot = m_rootUI.Q<VisualElement>("LoadingRoot");
        m_progressBar = m_rootUI.Q<ProgressBar>("Loading");
    }

    private void InitializeSettingsMenu()
    {
        m_settingsRoot = m_rootUI.Q<VisualElement>("SettingsRoot");
        m_volumeSlider = m_rootUI.Q<Slider>("Volume");
        m_settingsBackButton = m_rootUI.Q<Button>("Settings-Back");
        m_settingsApplyButton = m_rootUI.Q<Button>("Settings-Apply");

        m_volumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("Volume", 1.0f));
        m_settingsBackButton.clicked += OnSettingsBackButtonClicked;
        m_settingsApplyButton.clicked += OnSettingsApplyClicked;
    }
    
    private void OnEnable()
    {
        m_rootUI = GetComponent<UIDocument>().rootVisualElement;

        InitializeMainMenu();
        InitializeLoadingBar();
        InitializeSettingsMenu();
    }

    private void OnDisable()
    {
        m_playButton.clicked -= OnPlayButtonClicked;
        m_settingsButton.clicked -= OnSettingsButtonClicked;
        m_quitButton.clicked -= OnQuitButtonClicked;
        m_settingsBackButton.clicked -= OnSettingsBackButtonClicked;
        m_settingsApplyButton.clicked -= OnSettingsApplyClicked;
    }

    IEnumerator LoadLevelAsync(string levelName)
    {
        m_menuRoot.style.display = DisplayStyle.None;
        m_loadingRoot.style.display = DisplayStyle.Flex;

        float minDuration = 0.5f;
        float elapsed = 0f;

        AsyncOperation op = SceneManager.LoadSceneAsync(levelName);
        op.allowSceneActivation = false;

        while (true)
        {
            elapsed += Time.deltaTime;

            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(elapsed / minDuration);
            float displayProgress = Mathf.Min(realProgress, timeProgress);

            m_progressBar.value = displayProgress * 100f;
            m_progressBar.title = $"Loading: {Mathf.RoundToInt(displayProgress * 100f)}%";

            if (op.progress >= 0.9f && elapsed >= minDuration)
            {
                op.allowSceneActivation = true;
                yield return null; // let the activation frame happen
                break;
            }

            yield return null;
        }
    }

    private void OnPlayButtonClicked()
    {
        StartCoroutine(LoadLevelAsync(m_firstLevelName));
    }

    private void OnSettingsButtonClicked()
    {
        m_menuRoot.style.display = DisplayStyle.None;
        m_settingsRoot.style.display = DisplayStyle.Flex;
    }

    private void OnQuitButtonClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
    private void SaveVolumeLevel()
    {
        Debug.Log("Volume Value: " + m_volumeSlider.value);
        float normalizedValue = m_volumeSlider.value / m_volumeSlider.highValue;
        PlayerPrefs.SetFloat("Volume", normalizedValue);
    }
    private void OnSettingsApplyClicked()
    {
        SaveVolumeLevel();
    }
    private void OnSettingsBackButtonClicked()
    {
        SaveVolumeLevel();
        m_menuRoot.style.display = DisplayStyle.Flex;
        m_settingsRoot.style.display = DisplayStyle.None;
    }
}
