using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Linq;
public enum RoundState
{
    ROUND_STATE_INVALID = -1,
    ROUND_STATE_START,
    ROUND_STATE_TAKING_TURNS,
    ROUND_STATE_END,
    ROUND_STATE_SIZE
}

public enum TurnState
{
    TURN_STATE_INVALID = -1,
    TURN_STATE_START,
    TURN_STATE_CHOOSING,
    TURN_STATE_PLAY_ACTION,
    TURN_STATE_END_TURN,
    TURN_STATE_SIZE
}

public enum TurnOrder
{
    TURN_ORDER_INVALID = -1,
    TURN_ORDER_PLAYER,
    TURN_ORDER_NPC1,
    TURN_ORDER_NPC2,
    TURN_ORDER_NPC3,
    TURN_ORDER_SIZE
}
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public static event Action<int> OnRoundStartNotify;
    public static event Action<TurnOrder> OnTurnStartNotify;

    public RoundState CurrentRoundState => _roundState;
    public TurnState CurrentTurnState => _turnState;
    public TurnOrder CurrentTurn => _currentTurn;
    public int NumRounds => _numRounds;
    public bool IsPaused { get; set; } = false;
    public bool IsRoundOver { get; set; } = false;
    public bool IsGameOver { get; set; } = false;

    public bool RoundUIAnimationComplete { get; set; } = false;
    public bool TurnUIAnimationComplete { get; set; } = false;

    [SerializeField] private AIManager _aiManager;
    private RoundState _roundState = RoundState.ROUND_STATE_INVALID;
    private TurnState _turnState = TurnState.TURN_STATE_INVALID;
    private TurnOrder _currentTurn = TurnOrder.TURN_ORDER_INVALID;
    private int _numRounds = 0;
    private bool _actionChosen = false;
    private bool _actionComplete = false;

    private ActionType _aiTurnAction = ActionType.ACTION_TYPE_INVALID;
    private MoveType _playerTurnMove = MoveType.MOVE_TYPE_INVALID;
    private List<bool> _turnSkipStatus;

    private Coroutine _roundCoroutine = null;
    public void ResetGame()
    {
        IsGameOver = false;
        IsRoundOver = false;
        _numRounds = 0;
        if (_roundCoroutine != null)
        {
            StopCoroutine(_roundCoroutine);
            _roundCoroutine = null;
        }

        BeginRounds();
    }

    public void OnPlayerSelectedAction(MoveType inputMoveType)
    {
#if UNITY_EDITOR
        Assert.IsTrue(_playerTurnMove == MoveType.MOVE_TYPE_INVALID);
#endif
        _playerTurnMove = inputMoveType;
        _actionChosen = true;
    }

    public bool IsPlayersTurn()
    {
        return _currentTurn == TurnOrder.TURN_ORDER_PLAYER;
    }

    public bool IsTurnSkipped(TurnOrder turn)
    {
        int turnToInt = (int)turn;
        return _turnSkipStatus[turnToInt];
    }

    public void MarkNextTurnAsSkipped(TurnOrder turn)
    {
        int nextTurn = ((int)turn + 1) % (int)TurnOrder.TURN_ORDER_SIZE;
#if UNITY_EDITOR
        Assert.IsTrue(nextTurn >= 0 && nextTurn < (int)TurnOrder.TURN_ORDER_SIZE);
#endif
        _turnSkipStatus[nextTurn] = true;
    }

    public void MarkActionComplete()
    {
        _actionComplete = true;
    }

    public void AdvanceTurnState()
    {
        int advanceTurnValue = ((int)_turnState + 1) % (int)TurnState.TURN_STATE_SIZE;
        Debug.Log("Turn was: " + _turnState.ToString() + " | Is now: " + ((TurnState)advanceTurnValue).ToString());
        _turnState = (TurnState)advanceTurnValue;
    }

    private void AdvanceRoundState()
    {
        int advanceRoundValue = ((int)_roundState + 1) % (int)RoundState.ROUND_STATE_SIZE;
        Debug.Log("Round was: " + _roundState.ToString() + " | Is now: " + ((RoundState)advanceRoundValue).ToString());
        _roundState = (RoundState)advanceRoundValue;
    }

    private IEnumerator WaitOnActionChosen(TurnOrder turn)
    {
        _actionChosen = false;
        _aiTurnAction = ActionType.ACTION_TYPE_INVALID;
        if (turn == TurnOrder.TURN_ORDER_PLAYER)
        {
            PlayerInput.Instance.CanInteract = true;
            _playerTurnMove = MoveType.MOVE_TYPE_INVALID;
            PlayerActionManager.Instance.GenerateAvailableActions();
            yield return new WaitUntil(() => _actionChosen);
        }
        else if (turn > TurnOrder.TURN_ORDER_PLAYER && turn < TurnOrder.TURN_ORDER_SIZE)
        {
            yield return new WaitForSecondsRealtime(0.2f);
            _aiTurnAction = _aiManager.EvaluateAction(turn);
        }
        else
        {
            Debug.LogError("Asking for a turn from an invalid turn order");
        }
    }

    private IEnumerator WaitOnActionPerformed(TurnOrder turn)
    {
        if (turn == TurnOrder.TURN_ORDER_PLAYER)
        {
            if (_playerTurnMove == MoveType.MOVE_TYPE_SKIP)
            {
                MarkNextTurnAsSkipped(turn);
            }
            BoardManager.Instance.PerformBoardMove(_playerTurnMove);
        }
        else if (turn > TurnOrder.TURN_ORDER_PLAYER && turn < TurnOrder.TURN_ORDER_SIZE)
        {
            if (_aiTurnAction == ActionType.ACTION_TYPE_INVALID)
            {
                Debug.LogWarning("Invalid Action passed in");
                yield break;
            }

            _aiManager.PerformAction(turn, _aiTurnAction);
        }
        else
        {
            Debug.LogError("Asking for a turn from an invalid turn order");
        }

        yield return new WaitUntil(() => _actionComplete);
    }

    private void ResetTurnSkips()
    {
        for (int i = 0; i < _turnSkipStatus.Count; ++i)
        {
            _turnSkipStatus[i] = false;
        }
    }

    private IEnumerator UpdateTurnInternal(TurnOrder turn)
    {
#if UNITY_EDITOR
        Assert.IsTrue(_turnState == TurnState.TURN_STATE_START);
#endif
        OnTurnStartNotify?.Invoke(turn);
        bool turnSkipped = IsTurnSkipped(turn);
        yield return new WaitUntil(() => TurnUIAnimationComplete);
        TurnUIAnimationComplete = false;

        if (turnSkipped)
        {
            ResetTurnSkips();
            yield break;
        }

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionChosen(turn));

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionPerformed(turn));

        // Turn End
        AdvanceTurnState();

        yield return new WaitForSecondsRealtime(0.5f);

        AdvanceTurnState();
    }

    private IEnumerator UpdateRoundInternal()
    {
#if UNITY_EDITOR
        Assert.IsTrue(_roundState == RoundState.ROUND_STATE_START);
#endif
        ++_numRounds;

        BoardManager.Instance.InitializeBoard();
        OnRoundStartNotify?.Invoke(_numRounds);
        yield return new WaitUntil(() => RoundUIAnimationComplete);
        RoundUIAnimationComplete = false;

        AdvanceRoundState(); // taking turns

        yield return new WaitForSecondsRealtime(0.5f);

        for (int i = (int)TurnOrder.TURN_ORDER_PLAYER; i < (int)TurnOrder.TURN_ORDER_SIZE; ++i)
        {
            if (IsRoundOver)
            {
                IsRoundOver = false;
                break;
            }

            _currentTurn = (TurnOrder)i;
            yield return StartCoroutine(UpdateTurnInternal((TurnOrder)i));

            // TODO AQIN Yield here for until completion of the UI
            BoardManager.Instance.RotateBoard();
            yield return new WaitUntil(() => !BoardManager.Instance.IsBoardRotating);
        }

        OnTurnStartNotify?.Invoke(TurnOrder.TURN_ORDER_INVALID);
        yield return new WaitUntil(() =>TurnUIAnimationComplete);
        TurnUIAnimationComplete = false;

        AdvanceRoundState(); // End

        yield return new WaitForSecondsRealtime(0.2f);

        AdvanceRoundState();
    }

    private IEnumerator UpdateGameInternal()
    {
        while (!IsGameOver)
        {
            yield return StartCoroutine(UpdateRoundInternal());
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        int skipSize = (int)TurnOrder.TURN_ORDER_SIZE;
        _turnSkipStatus = Enumerable.Repeat(false, skipSize).ToList();
    }

    private void BeginRounds()
    {
        BoardManager.Instance.InitializeBoard();
        _roundCoroutine = StartCoroutine(UpdateGameInternal());
    }

    private void Start()
    {
        _roundState = RoundState.ROUND_STATE_START;
        _turnState = TurnState.TURN_STATE_START;
        BeginRounds();
    }
}
