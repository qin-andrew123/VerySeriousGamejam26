using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class WinLossController : MonoBehaviour
{
    // Main menu Scene name
    [SerializeField] private string MainMenuName = "MainMenu";

    private UIDocument _rootDocument;
    private VisualElement _winlossRoot;

    // Buttons
    private Button _playAgainButton;
    private Button _mainMenuButton;
    private Button _quitButton;

    // Want
    private Image _wantImage;

    // Leaderboard
    private VisualElement _leaderboardRoot;
    private Label _leaderboardPrompt;
    private TextField _leaderboardEntry;
    private Button _leaderboardSubmit;
    private LeaderboardData _leaderboardData = new();

    private int _newLeaderboardScore = -1;

    public void RaiseGameOverUI(int newScore)
    {
        _rootDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        _newLeaderboardScore = newScore;
    }

    private void OnEnable()
    {
        _rootDocument = GetComponent<UIDocument>();
        _winlossRoot = _rootDocument.rootVisualElement.Q<VisualElement>("WinLossRoot");

        // Hide on start
        _rootDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Buttons
        _playAgainButton = _winlossRoot.Q<Button>("PlayAgain");
        _mainMenuButton = _winlossRoot.Q<Button>("MainMenu");
        _quitButton = _winlossRoot.Q<Button>("Quit");
        _wantImage = _winlossRoot.Q<Image>("Want");

        _playAgainButton.clicked += HandlePlayAgain;
        _mainMenuButton.clicked += HandleMainMenu;
        _quitButton.clicked += HandleQuit;

        // Leaderboard
        _leaderboardRoot = _rootDocument.rootVisualElement.Q<VisualElement>("LeaderboardRoot");
        _leaderboardPrompt = _leaderboardRoot.Q<Label>("Prompt");
        _leaderboardEntry = _leaderboardRoot.Q<TextField>("Entry");
        _leaderboardSubmit = _leaderboardRoot.Q<Button>("Submit");

        _leaderboardSubmit.clicked += HandleSubmit;

         _leaderboardData.Entries = new List<LeaderboardEntry>();
        string leaderboardJson = PlayerPrefs.GetString("Leaderboard", string.Empty);
        if (leaderboardJson != string.Empty)
        {
            // Deserialize existing leaderboard
            _leaderboardData = JsonUtility.FromJson<LeaderboardData>(leaderboardJson);
            _leaderboardData.Entries =
                _leaderboardData.Entries.OrderByDescending(entry => entry.Score).ToList();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from buttons
        _playAgainButton.clicked -= HandlePlayAgain;
        _mainMenuButton.clicked -= HandleMainMenu;
        _quitButton.clicked -= HandleQuit;
        _leaderboardSubmit.clicked -= HandleSubmit;
    }

    private void HandleSubmit()
    {
        string name = _leaderboardEntry.text;

        // Add new entry
        LeaderboardEntry newEntry = new(name, _newLeaderboardScore);
        _leaderboardData.Entries.Add(newEntry);

        // Take top 10 only
        _leaderboardData.Entries =
            _leaderboardData.Entries.OrderByDescending(entry => entry.Score)
            .Take(10).ToList();

        // Re-serialize
        string serializedData = JsonUtility.ToJson(_leaderboardData);

        PlayerPrefs.SetString("Leaderboard", serializedData);
        PlayerPrefs.Save();

        Debug.Log($"Added new leaderboard entry: ({name} — {_newLeaderboardScore})");

        // Hide leaderboard section
        _leaderboardRoot.style.visibility = Visibility.Hidden;
    }

    private void HandlePlayAgain()
    {
        // Restart game
        AudioManager.Instance.PlayAudioOneShot("TryAgain");
        TurnManager.Instance.ResetGame();

        // Hide Win/Loss UI
        _rootDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void HandleMainMenu()
    {
        // Hide Win/Loss UI
        _rootDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Open Main Menu scene
        StartCoroutine(LoadMainMenuAsync());
    }

    private void HandleQuit()
    {
        // Exit application
        Application.Quit();
    }

    private IEnumerator LoadMainMenuAsync()
    {
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(MainMenuName);
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
}
