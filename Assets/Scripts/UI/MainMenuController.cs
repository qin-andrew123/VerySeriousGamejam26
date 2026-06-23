using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

/* TODO BRIEZ:
 * Progress bar while loading level
 * Leaderboard population
 */

#region Leaderboard data
[Serializable]
public class LeaderboardEntry
{
    public string PlayerName;
    public int Score;

    public LeaderboardEntry(string playerName, int score)
    {
        PlayerName = playerName;
        Score = score;
    }
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
}
#endregion

public class MainMenuController : MonoBehaviour
{
    // Inspector properties
    [SerializeField] private string FirstLevelName = "LevelOne";
    [SerializeField] private UIDocument SettingsDocument;
    [SerializeField] private int MaxLeaderboardEntries = 15;

    // Main menu UI document
    private UIDocument _uiDocument;

    // Visual elements
    private Button _newGameButton;
    private Button _settingsButton;
    private Button _exitButton;
    private ListView _leaderboardView;

    #region Initialization
    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("Unable to locate Main Menu UIDocument!");
            return;
        }

        // Get visual elements
        _newGameButton = _uiDocument.rootVisualElement.Q("NewGame") as Button;
        _settingsButton = _uiDocument.rootVisualElement.Q("Settings") as Button;
        _exitButton = _uiDocument.rootVisualElement.Q("Exit") as Button;
        _leaderboardView = _uiDocument.rootVisualElement.Q("Leaderboard") as ListView;

        SubscribeButtons();

        // Set up leaderboard
        CreateTestLeaderboardData();
        InitializeLeaderboard();
    }

    private void OnDisable()
    {
        UnsubscribeButtons();
    }

    private void SubscribeButtons()
    {
        if (_newGameButton != null)
            _newGameButton.clicked += OnClickNewGame;
        if (_settingsButton != null)
            _settingsButton.clicked += OnClickSettings;
        if (_exitButton != null)
            _exitButton.clicked += OnExit;
    }

    private void UnsubscribeButtons()
    {
        if (_newGameButton != null)
            _newGameButton.clicked -= OnClickNewGame;
        if (_settingsButton != null)
            _settingsButton.clicked -= OnClickSettings;
        if (_exitButton != null)
            _exitButton.clicked -= OnExit;
    }


    // TODO BRIEZ: Remove later
    private void CreateTestLeaderboardData()
    {
        // Fake data
        LeaderboardEntry entry1 = new("Joe", 5);
        LeaderboardEntry entry2 = new("Harriet", 25);

        LeaderboardData data = new();
        data.Entries.Add(entry1);
        data.Entries.Add(entry2);

        string serializedData = JsonUtility.ToJson(data);

        PlayerPrefs.SetString("Leaderboard", serializedData);
        PlayerPrefs.Save();
    }

    private void InitializeLeaderboard()
    {
        if (_leaderboardView == null)
        {
            Debug.LogError("Leaderboard view is null!");
            return;
        }

        LeaderboardData leaderboardData = new();

        // Get serialized leaderboard data from PlayerPrefs
        if (!PlayerPrefs.HasKey("Leaderboard"))
        {
            Debug.Log("No leaderboard data found!");
            _leaderboardView.style.display = DisplayStyle.None;
            return;
        }
        
        // Deserialize
        string leaderboardJson = PlayerPrefs.GetString("Leaderboard", string.Empty);
        leaderboardData = JsonUtility.FromJson<LeaderboardData>(leaderboardJson);
        foreach (LeaderboardEntry entry in leaderboardData.Entries)
        {
            Debug.Log($"ENTRY Player: {entry.PlayerName} Score: {entry.Score}");
        }

        // Set up ListView
        _leaderboardView.itemsSource = leaderboardData.Entries;
        _leaderboardView.minimumHeight = 300;
        //_leaderboardView.fixedItemHeight = 30.0f;

        // Define creation and binding for items
        _leaderboardView.makeItem = () =>
        {
            return new Label();
        };
        _leaderboardView.bindItem = (VisualElement element, int index) =>
        {
            Label label = element as Label ?? element.Q<Label>();

            if (label != null &&
                index < leaderboardData.Entries.Count)
            {
                LeaderboardEntry entry = leaderboardData.Entries[index];
                label.text = $"{entry.PlayerName} — {entry.Score}";
                label.style.fontSize = 30;
                label.style.height = 50;
                label.style.paddingBottom = 20;
            }
        };

        _leaderboardView.RefreshItems();
        //_leaderboardView.Rebuild();
    }
    #endregion

    #region Button events
    private void OnClickNewGame()
    {
        // Load into first level
        StartCoroutine(LoadFirstLevelAsync());
    }

    private void OnClickSettings()
    {
        // Display settings menu

        if (SettingsDocument == null)
        {
            Debug.LogError("Settings UIDocument not found!");
            return;
        }

        SettingsDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void OnExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator LoadFirstLevelAsync()
    {
        // Hide main menu UI
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(FirstLevelName);
        loadOperation.allowSceneActivation = false;

        while (!loadOperation.isDone)
        {
            if (loadOperation.progress >= 0.9f)
            {
                loadOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
    #endregion
}
