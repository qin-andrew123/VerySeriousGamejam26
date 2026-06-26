using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
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
    public bool ShouldAnimate = false;

    public ActorScore(VisualElement scoreBoxRoot, Image actorImage, TextElement actorTextElement, int actorScoreValue, int actorIndex)
    {
        ScoreBoxRoot = scoreBoxRoot;
        ActorImage = actorImage;
        ActorTextElement = actorTextElement;
        ActorScoreValue = actorScoreValue;
        ActorIndex = actorIndex;
    }

    public IEnumerator RunAnimation()
    {
        // Inner score element movement
        ScoreBoxRoot.AddToClassList("scorePanel--active");
        yield return new WaitForSecondsRealtime(1f);
        ScoreBoxRoot.RemoveFromClassList("scorePanel--active");
        ShouldAnimate = false;
    }
}

public class GameplayUIControl : MonoBehaviour
{
    [SerializeField] private float _roundFadeTime = 1.0f;
    [SerializeField] private float _turnDelay = 1.0f;
    [SerializeField] private Texture2D _interactableSprite;
    [SerializeField] private Texture2D _uninteractableSprite;
    [SerializeField] private Vector2 _offsetForMouseTexture = new Vector2(3, 3);
    [SerializeField] private List<Texture2D> _portraitIcons = new List<Texture2D>();
    [SerializeField] private List<Sprite> _tooltipImages = new List<Sprite>();
    [SerializeField] private List<string> _tooltipDescriptions = new List<string>();

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

    // Tooltip Layout
    private VisualElement _tooltipRoot;
    private Image _tooltipImage;
    private TextElement _tooltipText;

    public void UpdateScoreForActor(List<int> scores)
    {
        for(int i = 0; i < scores.Count; ++i)
        {
            ActorScore actor = _actorScores[i];

            actor.ShouldAnimate = actor.ActorScoreValue != scores[i];
            actor.ActorScoreValue = scores[i];
            actor.ActorTextElement.text = $": {actor.ActorScoreValue}";
        }

        if (_scoreCoroutine == null)
        {
            _scoreCoroutine = StartCoroutine(StartScoreBoxMove());
        }

    }

    public void MarkButtonAsAvailable(int index)
    {
        GameplayButton gameplayButton = _gameplayButtons[index];
        gameplayButton.IsAvailable = true;
    }

    public void MarkButtonAsUnavailable(int index)
    {
        GameplayButton gameplayButton = _gameplayButtons[index];
        gameplayButton.IsAvailable = false;
    }

    #region Remote Control
    private void HandleMouseDown(MouseDownEvent evt)
    {
        AudioManager.Instance.PlayAudioOneShot("RemoteButtonPress");
    }

    private void HandleMouseEnter(MouseEnterEvent evt)
    {
        VisualElement target = evt.target as VisualElement;
        if (_validButtonsToIndex.TryGetValue(target.name, out int value))
        {
            _tooltipRoot.style.left = evt.mousePosition.x - 520;
            _tooltipRoot.style.top = evt.mousePosition.y + 20;
            _tooltipRoot.style.display = DisplayStyle.Flex;

            _tooltipText.text = _tooltipDescriptions[value];
            _tooltipImage.sprite = _tooltipImages[value];

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

        _tooltipRoot.style.display = DisplayStyle.None;
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
            else
            {
                return;
            }
        }

        for (int i = 0; i <_gameplayButtons.Count; ++i)
        {
            MarkButtonAsUnavailable(i);
        }
    }

    private void ClampTooltipToScreen()
    {
        Rect tooltipRect = _tooltipRoot.worldBound;
        Rect screenRect = _tooltipRoot.panel.visualTree.worldBound;

        Vector2 offset = Vector2.zero;

        if (tooltipRect.xMax > screenRect.xMax)
            offset.x = screenRect.xMax - tooltipRect.xMax;

        if (tooltipRect.xMin < screenRect.xMin)
            offset.x = screenRect.xMin - tooltipRect.xMin;

        if (tooltipRect.yMax > screenRect.yMax)
            offset.y = screenRect.yMax - tooltipRect.yMax;

        if (tooltipRect.yMin < screenRect.yMin)
            offset.y = screenRect.yMin - tooltipRect.yMin;

        if (offset != Vector2.zero)
        {
            Vector2 current = _tooltipRoot.resolvedStyle.translate;
            _tooltipRoot.style.translate = current + offset;
        }
    }

    private void OnTooltipGeometryChanged(GeometryChangedEvent evt)
    {
        ClampTooltipToScreen();
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
#if UNITY_EDITOR
        Assert.IsNotNull(action);
#endif
        action.RegisterCallback<MouseUpEvent>(HandleMouseUp);
        action.RegisterCallback<MouseDownEvent>(HandleMouseDown, TrickleDown.TrickleDown);

        int currentIndex = _gameplayButtons.Count;
        _gameplayButtons.Add(new GameplayButton(action, false, currentIndex));
        _validButtonsToIndex.Add(buttonName, currentIndex);
    }

    private void InitializePortraitInfo(string portraitName)
    {
        Image portrait = _turnContainer.Q<Image>(portraitName);
#if UNITY_EDITOR
        Assert.IsNotNull(portrait);
#endif
        int currentIndex = _turnPortraits.Count;
        portrait.image = _portraitIcons[currentIndex];
        _turnPortraits.Add(new ActorPortrait(portrait, false, currentIndex));
        _validPortraitToIndex.Add(portraitName, currentIndex);
    }

    private void InitializeScoreInfo(string actorScoreName)
    {
        VisualElement scoreRoot = _scoreContainer.Q<VisualElement>(actorScoreName);
#if UNITY_EDITOR
        Assert.IsNotNull(scoreRoot);
#endif
        // Grab portrait and text element from the uxml child
        Image portrait = scoreRoot.Q<Image>("Portrait");
        TextElement score = scoreRoot.Q<TextElement>("Score");
        score.text = $":";
        int currentIndex = _actorScores.Count;
        portrait.image = _portraitIcons[currentIndex];

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

        _scoreContainer = m_rootUI.Q<VisualElement>("ScoreRoot");
        InitializeScoreInfo("PlayerScore");
        InitializeScoreInfo("NPC1Score");
        InitializeScoreInfo("NPC2Score");
        InitializeScoreInfo("NPC3Score");

        _remoteRoot = m_rootUI.Q<VisualElement>("RemoteRoot");
        _remoteRoot.RegisterCallback<MouseEnterEvent>(HandleMouseEnter, TrickleDown.TrickleDown);
        _remoteRoot.RegisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);

        InitializeButtonInfo("ActionOne");
        InitializeButtonInfo("ActionTwo");
        InitializeButtonInfo("ActionThree");
        InitializeButtonInfo("ActionFour");

        _tooltipRoot = m_rootUI.Q<VisualElement>("TooltipRoot");
        _tooltipImage = _tooltipRoot.Q<Image>("TooltipImage");
        _tooltipText = _tooltipRoot.Q<TextElement>("TooltipText");
        _tooltipRoot.RegisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged, TrickleDown.TrickleDown);
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

        _tooltipRoot.UnregisterCallback<GeometryChangedEvent>(OnTooltipGeometryChanged, TrickleDown.TrickleDown);

        foreach (var button in _gameplayButtons)
        {
            button.ActionButton.UnregisterCallback<MouseUpEvent>(HandleMouseUp);
            button.ActionButton.UnregisterCallback<MouseDownEvent>(HandleMouseDown,TrickleDown.TrickleDown);
        }
    }

    #endregion

    #region Game Information
    private IEnumerator StartScoreBoxMove()
    {
        _scoreContainer.AddToClassList("scorePanel--active");
        yield return new WaitForSecondsRealtime(0.5f);

        List<Coroutine> animations = new List<Coroutine>();
        for (int i = 0; i < _actorScores.Count; ++i)
        {
            ActorScore scoreElement = _actorScores[i];
            if (scoreElement.ShouldAnimate)
            {
                animations.Add(StartCoroutine(scoreElement.RunAnimation()));
            }
        }

        foreach (Coroutine c in animations)
        {
            yield return c;
        }

        // Outer score box movement
        yield return new WaitForSecondsRealtime(0.5f);
        _scoreContainer.RemoveFromClassList("scorePanel--active");
        _scoreCoroutine = null;
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
