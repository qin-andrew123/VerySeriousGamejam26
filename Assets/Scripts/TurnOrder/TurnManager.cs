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
    TURN_ORDER_NPC4,
    TURN_ORDER_NPC5,
    TURN_ORDER_SIZE
}
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }
    public static event Action<int> OnRoundStartNotify;
    public static event Action<TurnOrder, bool> OnTurnStartNotify;

    public RoundState CurrentRoundState { get { return m_roundState; } }
    public TurnState CurrentTurnState {  get { return m_turnState; } }
    public TurnOrder CurrentTurn { get { return m_currentTurn; } }
    public int NumRounds { get  { return m_numRounds; } }
    public bool IsPaused { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public float RoundDelayTime { get { return m_roundDelayTime; } }
    public float TurnDelayTime { get { return m_turnDelayTime; } }

    private RoundState m_roundState = RoundState.ROUND_STATE_INVALID;
    private TurnState m_turnState = TurnState.TURN_STATE_INVALID;
    private TurnOrder m_currentTurn = TurnOrder.TURN_ORDER_INVALID;
    private int m_numRounds = 0;
    private bool m_actionChosen = false;
    private bool m_actionComplete = false;
    private bool m_skippingNextTurn = false;

    [SerializeField] private float m_roundDelayTime = 1.5f;
    [SerializeField] private float m_turnDelayTime = 1.5f;
    [SerializeField] private AIManager m_aiManager;
    private ActionType m_currTurnAction = ActionType.ACTION_TYPE_INVALID;
    private List<bool> m_turnSkips;
    public bool IsPlayersTurn()
    {
        return m_currentTurn == TurnOrder.TURN_ORDER_PLAYER;
    }

    public bool IsTurnSkipped(TurnOrder turn)
    {
        int turnToInt = (int)turn;
        return m_turnSkips[turnToInt];
    }

    public void MarkNextTurnAsSkipped(TurnOrder turn)
    {
        int nextTurn = ((int)turn + 1) % (int)TurnOrder.TURN_ORDER_SIZE;
        Assert.IsTrue(nextTurn >= 0 && nextTurn < (int)TurnOrder.TURN_ORDER_SIZE);

        m_turnSkips[nextTurn] = true;
    }

    public void MarkActionComplete()
    {
        m_actionComplete = true;
    }

    public void AdvanceTurnState()
    {
        int advanceTurnValue = ((int)m_turnState + 1) % (int)TurnState.TURN_STATE_SIZE;
        Debug.Log("Turn was: " + m_turnState.ToString() + " | Is now: " + ((TurnState)advanceTurnValue).ToString());
        m_turnState = (TurnState)advanceTurnValue;
    }

    private void AdvanceRoundState()
    {
        int advanceRoundValue = ((int)m_roundState + 1) % (int)RoundState.ROUND_STATE_SIZE;
        Debug.Log("Round was: " + m_roundState.ToString() + " | Is now: " + ((RoundState)advanceRoundValue).ToString());
        m_roundState = (RoundState)advanceRoundValue;
    }

    private void OnPlayerSelectedAction()
    {
        m_actionChosen = true;
        // TODO AQIN: Set action here
    }

    private IEnumerator WaitOnActionChosen(TurnOrder turn)
    {
        m_actionChosen = false;
        m_currTurnAction = ActionType.ACTION_TYPE_INVALID;
        if (turn == TurnOrder.TURN_ORDER_PLAYER)
        {
            yield return new WaitForSecondsRealtime(m_turnDelayTime);
            m_currTurnAction = ActionType.ACTION_TYPE_PASS;
            //yield return new WaitUntil(() => m_actionChosen);
        }
        else if (turn > TurnOrder.TURN_ORDER_PLAYER && turn < TurnOrder.TURN_ORDER_SIZE)
        {
            yield return new WaitForSecondsRealtime(m_turnDelayTime);
            m_currTurnAction = m_aiManager.EvaluateAction(turn);
        }
        else
        {
            Debug.LogError("Asking for a turn from an invalid turn order");
        }
    }

    private IEnumerator WaitOnActionPerformed(TurnOrder turn)
    {
        if (m_currTurnAction == ActionType.ACTION_TYPE_INVALID)
        {
            Debug.LogWarning("Invalid Action passed in");
            yield break;
        }

        if (turn == TurnOrder.TURN_ORDER_PLAYER)
        {
            BoardManager.Instance.PerformBoardMove(MoveType.MOVE_TYPE_PASS);

            //yield return new WaitUntil(() => m_actionChosen);
        }
        else if (turn > TurnOrder.TURN_ORDER_PLAYER && turn < TurnOrder.TURN_ORDER_SIZE)
        {
            m_aiManager.PerformAction(turn, m_currTurnAction);
        }
        else
        {
            Debug.LogError("Asking for a turn from an invalid turn order");
        }

        yield return new WaitUntil(() => m_actionComplete);
    }

    private IEnumerator UpdateTurnInternal(TurnOrder turn)
    {
        Assert.IsTrue(m_turnState == TurnState.TURN_STATE_START);

        bool turnSkipped = IsTurnSkipped(turn);
        OnTurnStartNotify?.Invoke(turn, turnSkipped);        

        yield return new WaitForSecondsRealtime(m_turnDelayTime);
        if (turnSkipped)
        {
            yield break;
        }

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionChosen(turn));

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionPerformed(turn));

        AdvanceTurnState();
        yield return new WaitForSecondsRealtime(m_turnDelayTime);

        AdvanceTurnState();
    }

    private IEnumerator UpdateRoundInternal()
    {
        Assert.IsTrue(m_roundState == RoundState.ROUND_STATE_START);
        ++m_numRounds;
        OnRoundStartNotify?.Invoke(m_numRounds);

        yield return new WaitForSecondsRealtime(m_roundDelayTime);

        AdvanceRoundState(); // taking turns

        for (int i = (int)TurnOrder.TURN_ORDER_PLAYER; i < (int)TurnOrder.TURN_ORDER_SIZE; ++i)
        {
            m_currentTurn = (TurnOrder)i;
            yield return StartCoroutine(UpdateTurnInternal((TurnOrder)i));
            BoardManager.Instance.RotateBoard();
        }

        AdvanceRoundState(); // End

        yield return new WaitForSecondsRealtime(m_roundDelayTime);

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
        m_turnSkips = Enumerable.Repeat(false, skipSize).ToList();
    }

    private void BeginRounds()
    {
        BoardManager.Instance.InitializeBoard();
        StartCoroutine(UpdateGameInternal());
    }

    private void Start()
    {
        m_roundState = RoundState.ROUND_STATE_START;
        m_turnState = TurnState.TURN_STATE_START;
        PlayerInput.OnPlayerSelectedAction += OnPlayerSelectedAction;
        BeginRounds();
    }

    private void OnDestroy()
    {
        PlayerInput.OnPlayerSelectedAction -= OnPlayerSelectedAction;
    }
}
