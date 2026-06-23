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
    private ScrollView _leaderboardView;

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
        _leaderboardView = _uiDocument.rootVisualElement.Q("Leaderboard") as ScrollView;

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


    // TODO BRIEZ: Test data for leaderboard! Remove later
    private void CreateTestLeaderboardData()
    {
        // Fake data
        LeaderboardEntry entry1 = new("Joe", 5);
        LeaderboardEntry entry2 = new("Harriet", 25);
        LeaderboardEntry entry3 = new("Leroy", 18);
        LeaderboardEntry entry4 = new("Credence", 24);
        LeaderboardEntry entry5 = new("Grenouille", 24);
        LeaderboardEntry entry6 = new("Minerva", 1);
        LeaderboardEntry entry7 = new("Wilhelm", -18);

        LeaderboardData data = new();
        data.Entries.Add(entry1);
        data.Entries.Add(entry2);
        data.Entries.Add(entry3);
        data.Entries.Add(entry4);
        data.Entries.Add(entry5);
        data.Entries.Add(entry6);
        data.Entries.Add(entry7);

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
        
        // Deserialize and order by score
        string leaderboardJson = PlayerPrefs.GetString("Leaderboard", string.Empty);
        leaderboardData = JsonUtility.FromJson<LeaderboardData>(leaderboardJson);
        leaderboardData.Entries =
            leaderboardData.Entries.OrderByDescending(entry => entry.Score).ToList();

        // Populate
        /*
         * Note/todo briez: Is this the ideal way to make a leaderboard? Of course not.
         * But ListView is acting up and we're on a bit of a schedule.
         * Feel free to optimise if the chance arises, lord knows this code
         * will almost certainly never see the light of day again.
         */
        int leaderboardDataCount = leaderboardData.Entries.Count;
        int visualElementIndex = 0;
        foreach (VisualElement entry in _leaderboardView.Children())
        {
            // Hide extra entries if not enough leaderboard data
            if (visualElementIndex > leaderboardDataCount - 1)
            {
                entry.style.display = DisplayStyle.None;
                continue;
            }

            // Find text labels
            Label playerNameLabel = entry.Q("PlayerName") as Label;
            Label scoreLabel = entry.Q("Score") as Label;
            if (playerNameLabel == null || scoreLabel == null)
            {
                Debug.LogError($"Missing labels for leaderboard entry {entry.name!}");
                continue;
            }

            // Display values
            playerNameLabel.text = leaderboardData.Entries[visualElementIndex].PlayerName;
            scoreLabel.text = $"{leaderboardData.Entries[visualElementIndex].Score}";

            ++visualElementIndex;
        }
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
