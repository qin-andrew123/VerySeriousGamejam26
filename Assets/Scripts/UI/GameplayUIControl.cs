using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameplayButton
{
    public Button ActionButton;
    public bool IsAvailable;
    public int ActionIndex;

    public GameplayButton(Button button, bool available, int index)
    {
        ActionButton = button;
        IsAvailable = available;
        ActionIndex = index;
    }
}

public class GameplayUIControl : MonoBehaviour
{
    [SerializeField] private float _roundFadeTime = 1.0f;
    [SerializeField] private float _turnFadeTime = 1.0f;
    [SerializeField] private Texture2D _interactableSprite;
    [SerializeField] private Texture2D _uninteractableSprite;
    [SerializeField] private Vector2 _offsetForMouseTexture = new Vector2(3, 3);

    private VisualElement m_rootUI;

    // Round Layout
    private VisualElement _roundRoot;
    private TextElement _roundInfo;
    private TextElement _turnInfo;
    private FadeElement _roundFade;
    private FadeElement _turnFade;

    private VisualElement _remoteRoot;
    private List<GameplayButton> _gameplayButtons = new List<GameplayButton>();
    private Dictionary<string, int> _validButtonsToIndex = new Dictionary<string, int>();

    public void MarkButtonAsAvailable(int index)
    {
        GameplayButton gameplayButton = _gameplayButtons[index];
        gameplayButton.IsAvailable = true;

        Button button = gameplayButton.ActionButton;
        button.RemoveFromClassList("remotebutton");
        button.AddToClassList("remotebuttonActive");
    }

    public void MarkButtonAsUnavailable(int index)
    {
        GameplayButton gameplayButton = _gameplayButtons[index];
        gameplayButton.IsAvailable = false;

        Button button = gameplayButton.ActionButton;
        button.RemoveFromClassList("remotebuttonActive");
        button.AddToClassList("remotebutton");
    }

    private void HandleMouseEnter(MouseEnterEvent evt)
    {
        VisualElement target = evt.target as VisualElement;
        if (_validButtonsToIndex.TryGetValue(target.name, out int value))
        {
            if (!_gameplayButtons[value].IsAvailable)
            {
                UnityEngine.Cursor.SetCursor(_uninteractableSprite, _offsetForMouseTexture, CursorMode.Auto);
            }
            else
            {
                UnityEngine.Cursor.SetCursor(_interactableSprite, _offsetForMouseTexture, CursorMode.Auto);
            }
        }
    }

    private void HandleMouseLeave(MouseLeaveEvent evt)
    {
        VisualElement target = evt.target as VisualElement;
        if (_validButtonsToIndex.TryGetValue(target.name, out int value))
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    private void HandleMouseUp(MouseUpEvent evt)
    {
        bool mouseLeftPressed = 0 == evt.button;
        if (!mouseLeftPressed)
        {
            return;
        }

        if (!IsClickValid())
        {
            return;
        }

        VisualElement target = evt.target as VisualElement;
        if (_validButtonsToIndex.TryGetValue(target.name, out int value))
        {
            if (_gameplayButtons[value].IsAvailable)
            {
                PlayerActionManager.Instance.SelectAction(value);
            }
        }
    }

    private bool IsClickValid()
    {
        if (!TurnManager.Instance.IsPlayersTurn())
        {
            Debug.Log("Clicked on a button during " + TurnManager.Instance.CurrentTurn.ToString());
            return false;
        }

        if (PlayerInput.Instance.CurrentInputState != InputState.INPUT_STATE_GAMEPLAY)
        {
            return false;
        }

        if (!PlayerInput.Instance.CanInteract)
        {
            return false;
        }

        return true;
    }

    private void InitializeButtonInfo(string buttonName)
    {
        Button action = m_rootUI.Q<Button>(buttonName);
        Assert.IsNotNull(action);
        action.RegisterCallback<MouseUpEvent>(HandleMouseUp);

        int currentIndex = _gameplayButtons.Count;
        _gameplayButtons.Add(new GameplayButton(action, false, currentIndex));
        _validButtonsToIndex.Add(buttonName, currentIndex);
    }

    private void InitializeRoundInfo()
    {
        _roundRoot = m_rootUI.Q<VisualElement>("TurnRoot");
        _roundInfo = m_rootUI.Q<TextElement>("RoundNum");
        _turnInfo = m_rootUI.Q<TextElement>("CurrentTurn");
        _roundFade = new FadeElement(this, _roundInfo, _roundFadeTime);
        _turnFade = new FadeElement(this, _turnInfo, _turnFadeTime);

        _roundFade.OnBannerComplete += OnRoundBannerComplete;
        _turnFade.OnBannerComplete += OnTurnBannerComplete;

        _remoteRoot = m_rootUI.Q<VisualElement>("RemoteRoot");
        _remoteRoot.RegisterCallback<MouseEnterEvent>(HandleMouseEnter, TrickleDown.TrickleDown);
        _remoteRoot.RegisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);

        InitializeButtonInfo("ActionOne");
        InitializeButtonInfo("ActionTwo");
        InitializeButtonInfo("ActionThree");
        InitializeButtonInfo("ActionFour");
    }

    private void OnRoundBannerComplete()
    {
        TurnManager.Instance.RoundUIAnimationComplete = true;
    }

    private void OnTurnBannerComplete()
    {
        TurnManager.Instance.TurnUIAnimationComplete = true;
    }

    private void UpdateRoundInformation(int roundNumber)
    {
        _roundInfo.text = $"Round {roundNumber}";
        _roundFade.Show();
    }

    private void UpdateTurnInformation(TurnOrder turn, bool isTurnSkipped)
    {
        if (isTurnSkipped)
        {
            _turnInfo.text = "Turn Skipped!";
        }
        else
        {
            switch (turn)
            {
                case TurnOrder.TURN_ORDER_PLAYER:
                    _turnInfo.text = "Player's Turn";
                    break;
                case TurnOrder.TURN_ORDER_NPC1:
                    _turnInfo.text = "NPC1's Turn";
                    break;
                case TurnOrder.TURN_ORDER_NPC2:
                    _turnInfo.text = "NPC2's Turn";
                    break;
                case TurnOrder.TURN_ORDER_NPC3:
                    _turnInfo.text = "NPC3's Turn";
                    break;
                case TurnOrder.TURN_ORDER_NPC4:
                    _turnInfo.text = "NPC4's Turn";
                    break;
                case TurnOrder.TURN_ORDER_NPC5:
                    _turnInfo.text = "NPC5's Turn";
                    break;
            }
        }

        _turnFade.Show();
    }

    private void OnEnable()
    {
        m_rootUI = GetComponent<UIDocument>().rootVisualElement;

        InitializeRoundInfo();
        TurnManager.OnRoundStartNotify += UpdateRoundInformation;
        TurnManager.OnTurnStartNotify += UpdateTurnInformation;
    }

    private void OnDisable()
    {
        TurnManager.OnRoundStartNotify -= UpdateRoundInformation;
        TurnManager.OnTurnStartNotify -= UpdateTurnInformation;

        _roundFade.OnBannerComplete -= OnRoundBannerComplete;
        _turnFade.OnBannerComplete -= OnTurnBannerComplete;

        _remoteRoot.UnregisterCallback<MouseEnterEvent>(HandleMouseEnter, TrickleDown.TrickleDown);
        _remoteRoot.UnregisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);


        foreach (var button in _gameplayButtons)
        {
            button.ActionButton.UnregisterCallback<MouseUpEvent>(HandleMouseUp);
        }
    }
}
