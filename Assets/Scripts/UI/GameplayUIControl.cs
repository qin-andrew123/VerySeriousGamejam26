using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

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

public class ActorPortrait
{
    public Image ActorImage;
    public bool IsCurrentTurn;
    public int ActorIndex;

    public ActorPortrait(Image actorImage, bool isCurrentTurn, int actorIndex)
    {
        ActorImage = actorImage;
        IsCurrentTurn = isCurrentTurn;
        ActorIndex = actorIndex;
    }
}

public class ActorScore
{
    public VisualElement ScoreBoxRoot;
    public Image ActorImage;
    public TextElement ActorTextElement;
    public int ActorScoreValue;
    public int ActorIndex;
    public bool ElementDoneAnimating = false;
    public ActorScore(VisualElement scoreBoxRoot, Image actorImage, TextElement actorTextElement, int actorScoreValue, int actorIndex)
    {
        ScoreBoxRoot = scoreBoxRoot;
        ActorImage = actorImage;
        ActorTextElement = actorTextElement;
        ActorScoreValue = actorScoreValue;
        ActorIndex = actorIndex;
    }
}

public class GameplayUIControl : MonoBehaviour
{
    [SerializeField] private float _roundFadeTime = 1.0f;
    [SerializeField] private float _turnDelay = 1.0f;
    [SerializeField] private Texture2D _interactableSprite;
    [SerializeField] private Texture2D _uninteractableSprite;
    [SerializeField] private Vector2 _offsetForMouseTexture = new Vector2(3, 3);

    private VisualElement m_rootUI;

    // Round Layout
    private VisualElement _roundRoot;
    private VisualElement _turnContainer;
    private List<ActorPortrait> _turnPortraits = new List<ActorPortrait>();
    private Dictionary<string, int> _validPortraitToIndex = new Dictionary<string, int>();
    private TextElement _roundInfo;
    private FadeElement _roundFade;

    // Score layout
    private VisualElement _scoreContainer;
    private List<ActorScore> _actorScores = new List<ActorScore>();
    private Dictionary<string, int> _validActorScoreToIndex = new Dictionary<string, int>();
    private Coroutine _scoreCoroutine = null;

    // Remote Layout
    private VisualElement _remoteRoot;
    private List<GameplayButton> _gameplayButtons = new List<GameplayButton>();
    private Dictionary<string, int> _validButtonsToIndex = new Dictionary<string, int>();

    public void UpdateScoreForActor(int actorIndex, int score)
    {
        ActorScore actor = _actorScores[actorIndex];
        actor.ActorScoreValue = score;
        actor.ActorTextElement.text = $": {actor.ActorScoreValue}";
        if (_scoreCoroutine == null)
        {
            _scoreCoroutine = StartCoroutine(StartScoreBoxMove(actor));
        }
        else
        {
            StopCoroutine(_scoreCoroutine);
            _scoreCoroutine = null;
        }
    }

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

    #region Remote Control
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

        for (int i = 0; i <_gameplayButtons.Count; ++i)
        {
            MarkButtonAsUnavailable(i);
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
    #endregion

    #region Initialization
    private void InitializeButtonInfo(string buttonName)
    {
        Button action = m_rootUI.Q<Button>(buttonName);
        Assert.IsNotNull(action);
        action.RegisterCallback<MouseUpEvent>(HandleMouseUp);

        int currentIndex = _gameplayButtons.Count;
        _gameplayButtons.Add(new GameplayButton(action, false, currentIndex));
        _validButtonsToIndex.Add(buttonName, currentIndex);
    }

    private void InitializePortraitInfo(string portraitName)
    {
        Image portrait = _turnContainer.Q<Image>(portraitName);
        Assert.IsNotNull(portrait);

        int currentIndex = _turnPortraits.Count;
        _turnPortraits.Add(new ActorPortrait(portrait, false, currentIndex));
        _validPortraitToIndex.Add(portraitName, currentIndex);
    }

    private void InitializeScoreInfo(string actorScoreName)
    {
        VisualElement scoreRoot = _scoreContainer.Q<VisualElement>(actorScoreName);
        Assert.IsNotNull(scoreRoot);

        // Grab portrait and text element from the uxml child
        Image portrait = scoreRoot.Q<Image>("Portrait");
        TextElement score = scoreRoot.Q<TextElement>("Score");
        score.text = $":";
        int currentIndex = _actorScores.Count;
        _actorScores.Add(new ActorScore(scoreRoot, portrait, score, 0, currentIndex));
        _validActorScoreToIndex.Add(actorScoreName, currentIndex);
    }


    private void InitializeRoundInfo()
    {
        _roundRoot = m_rootUI.Q<VisualElement>("TurnRoot");
        _roundInfo = m_rootUI.Q<TextElement>("RoundNum");
        _roundFade = new FadeElement(this, _roundInfo, _roundFadeTime);
        _roundFade.OnBannerComplete += OnRoundBannerComplete;

        _turnContainer = m_rootUI.Q<VisualElement>("TurnContainer");
        InitializePortraitInfo("PlayerPortrait");
        InitializePortraitInfo("NPC1Portrait");
        InitializePortraitInfo("NPC2Portrait");
        InitializePortraitInfo("NPC3Portrait");
        InitializePortraitInfo("NPC4Portrait");
        InitializePortraitInfo("NPC5Portrait");

        _scoreContainer = m_rootUI.Q<VisualElement>("ScoreRoot");
        InitializeScoreInfo("PlayerScore");
        InitializeScoreInfo("NPC1Score");
        InitializeScoreInfo("NPC2Score");
        InitializeScoreInfo("NPC3Score");
        InitializeScoreInfo("NPC4Score");
        InitializeScoreInfo("NPC5Score");

        _remoteRoot = m_rootUI.Q<VisualElement>("RemoteRoot");
        _remoteRoot.RegisterCallback<MouseEnterEvent>(HandleMouseEnter, TrickleDown.TrickleDown);
        _remoteRoot.RegisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);

        InitializeButtonInfo("ActionOne");
        InitializeButtonInfo("ActionTwo");
        InitializeButtonInfo("ActionThree");
        InitializeButtonInfo("ActionFour");
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

        _remoteRoot.UnregisterCallback<MouseEnterEvent>(HandleMouseEnter, TrickleDown.TrickleDown);
        _remoteRoot.UnregisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);


        foreach (var button in _gameplayButtons)
        {
            button.ActionButton.UnregisterCallback<MouseUpEvent>(HandleMouseUp);
        }
    }

    #endregion

    #region Game Information
    private IEnumerator StartScoreBoxMove(ActorScore scoreElement)
    {
        _scoreContainer.AddToClassList("scorePanel--active");
        yield return new WaitForSecondsRealtime(0.5f);

        // Inner score element movement
        scoreElement.ScoreBoxRoot.AddToClassList("scorePanel--active");
        yield return new WaitForSecondsRealtime(0.5f);
        scoreElement.ScoreBoxRoot.RemoveFromClassList("scorePanel--active");

        // Outer score box movement
        yield return new WaitForSecondsRealtime(3.0f);
        _scoreContainer.RemoveFromClassList("scorePanel--active");
    }


    private void OnRoundBannerComplete()
    {
        TurnManager.Instance.RoundUIAnimationComplete = true;
    }

    private IEnumerator OnTurnAnimationComplete()
    {
        yield return new WaitForSecondsRealtime(_turnDelay);
        TurnManager.Instance.TurnUIAnimationComplete = true;
    }

    private void UpdateRoundInformation(int roundNumber)
    {
        _roundInfo.text = $"Round {roundNumber}";
        _roundFade.Show();
    }

    private void UpdateTurnInformation(TurnOrder turn)
    {
        foreach (ActorPortrait actor in _turnPortraits)
        {
            if ((int)turn == actor.ActorIndex)
            {
                actor.ActorImage.AddToClassList("portrait--active");
                actor.IsCurrentTurn = true;
            }
            else
            {
                actor.ActorImage.RemoveFromClassList("portrait--active");
                actor.IsCurrentTurn = false;
            }
        }

        StartCoroutine(OnTurnAnimationComplete());
    }

    #endregion
}
