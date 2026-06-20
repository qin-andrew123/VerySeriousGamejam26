using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using System;
public enum RoundState
{
    ROUND_STATE_INVALID = -1,
    ROUND_STATE_START = 0,
    ROUND_STATE_TAKING_TURNS,
    ROUND_STATE_END,
    ROUND_STATE_SIZE
}

public enum TurnState
{
    TURN_STATE_INVALID = -1,
    TURN_STATE_START = 0, // NOTIFY START OF TURN
    TURN_STATE_CHOOSING = 1,
    TURN_STATE_PLAY_ACTION,
    TURN_STATE_END_TURN,
    TURN_STATE_SIZE
}

public enum TurnOrder
{
    TURN_ORDER_INVALID = -1,
    TURN_ORDER_PLAYER = 0,
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
    public static event Action<TurnOrder> OnTurnStartNotify;
    public RoundState CurrentRoundState { get { return m_roundState; } }
    public TurnState CurrentTurnState {  get { return m_turnState; } }
    public int NumRounds { get  { return m_numRounds; } }
    public bool IsPaused { get; set; } = false;
    public bool IsGameOver { get; set; } = false;
    public float RoundDelayTime { get { return m_roundDelayTime; } }
    public float TurnDelayTime { get { return m_turnDelayTime; } }
    private RoundState m_roundState = RoundState.ROUND_STATE_INVALID;
    private TurnState m_turnState = TurnState.TURN_STATE_INVALID;
    private int m_numRounds = 0;
    bool m_actionChosen = false;

    [SerializeField] private float m_roundDelayTime = 1.5f;
    [SerializeField] private float m_turnDelayTime = 1.5f;
    [SerializeField] private AIManager m_aiManager;
    private ActionType m_currTurnAction = ActionType.ACTION_TYPE_INVALID;

    // Called at the end of a turn's given phase
    // Returns true if advances to a new turn, false otherwise
    public bool AdvanceTurnState()
    {
        int advanceTurnValue = ((int)m_turnState + 1) % (int)TurnState.TURN_STATE_SIZE;
        Debug.Log("Turn was: " + m_turnState.ToString() + " | Is now: " + ((TurnState)advanceTurnValue).ToString());
        m_turnState = (TurnState)advanceTurnValue;
        return advanceTurnValue == (int)TurnState.TURN_STATE_START;
    }

    // Called when all turns are finished and to move onto the next round phase (Called at very end)
    // Returns true if advances to a new round, false otherwise
    private bool AdvanceRoundState()
    {
        int advanceRoundValue = ((int)m_roundState + 1) % (int)RoundState.ROUND_STATE_SIZE;
        Debug.Log("Round was: " + m_roundState.ToString() + " | Is now: " + ((RoundState)advanceRoundValue).ToString());
        m_roundState = (RoundState)advanceRoundValue;
        return advanceRoundValue == (int)RoundState.ROUND_STATE_START;
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
            //yield return new WaitUntil(() => m_actionChosen);
            yield return new WaitForSecondsRealtime(m_turnDelayTime);
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

    private IEnumerator WaitOnActionPerformed()
    {
        if (m_currTurnAction == ActionType.ACTION_TYPE_INVALID)
        {
            Debug.LogWarning("Invalid Action passed in");
            yield break;
        }
        yield return new WaitForSecondsRealtime(m_turnDelayTime);

        // TODO AQIN: UpdateBoard Manager with the action
    }
    private IEnumerator UpdateTurnInternal(TurnOrder turn)
    {
        Assert.IsTrue(m_turnState == TurnState.TURN_STATE_START);

        OnTurnStartNotify?.Invoke(turn);

        yield return new WaitForSecondsRealtime(m_turnDelayTime);

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionChosen(turn));

        AdvanceTurnState();
        yield return StartCoroutine(WaitOnActionPerformed());

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
            yield return StartCoroutine(UpdateTurnInternal((TurnOrder)i));
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
    }

    private void Start()
    {
        m_roundState = RoundState.ROUND_STATE_START;
        m_turnState = TurnState.TURN_STATE_START;
        PlayerInput.OnPlayerSelectedAction += OnPlayerSelectedAction;

        StartCoroutine(UpdateGameInternal());
    }

    private void OnDestroy()
    {
        PlayerInput.OnPlayerSelectedAction -= OnPlayerSelectedAction;
    }
}
