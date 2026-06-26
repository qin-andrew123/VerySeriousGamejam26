using UnityEngine;
using UnityEngine.UIElements;

public class WinLossController : MonoBehaviour
{
    private UIDocument _rootDocument;
    private VisualElement _winlossRoot;
    private TextElement _gameState;
    private Button _playAgainButton;
    private Button _quitButton;

    public void UpdateGameState(string state)
    {
        _winlossRoot.style.display = DisplayStyle.Flex;
        _gameState.text = state;
    }

    private void HandlePlayAgain()
    {
        AudioManager.Instance.PlayAudioOneShot("TryAgain");
        TurnManager.Instance.ResetGame();
        _winlossRoot.style.display = DisplayStyle.None;
    }

    private void HandleQuit()
    {
        Application.Quit();
    }

    private void OnEnable()
    {
        _rootDocument = GetComponent<UIDocument>();
        _winlossRoot = _rootDocument.rootVisualElement.Q<VisualElement>("winlossRoot");
        _gameState = _rootDocument.rootVisualElement.Q<TextElement>("GameState");

        _playAgainButton = _rootDocument.rootVisualElement.Q<Button>("PlayButton");
        _quitButton = _rootDocument.rootVisualElement.Q<Button>("QuitButton");

        _playAgainButton.clicked += HandlePlayAgain;
        _quitButton.clicked += HandleQuit;
    }
    private void OnDisable()
    {
        _playAgainButton.clicked -= HandlePlayAgain;
        _quitButton.clicked -= HandleQuit;
    }
}
