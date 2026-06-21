using System.Collections;
using UnityEngine;

public enum ActionType
{
    ACTION_TYPE_INVALID = -1,
    ACTION_TYPE_DEFAULT = 0,
    ACTION_TYPE_FALLBACK = 1,
    ACTION_TYPE_PASS
}

public enum MoveType
{
    MOVE_TYPE_INVALID = -1,
    MOVE_TYPE_PASS,
    MOVE_TYPE_SKIP,
    MOVE_TYPE_PLUS_ONE,
    MOVE_TYPE_REVERSE,
    MOVE_TYPE_CHAOS
}
public class AIEvaluator : MonoBehaviour
{
    [SerializeField] private TurnOrder m_AIType;
    [SerializeField] private TurnOrder m_opponent = TurnOrder.TURN_ORDER_INVALID;

    private ActionType EvaluationNPC1()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // NPC 1 tries to default plus one

        return bestAction;
    }
    private ActionType EvaluationNPC2()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // NPC 1 tries to default plus one

        return bestAction;

    }
    private ActionType EvaluationNPC3()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // NPC 1 tries to default plus one

        return bestAction;

    }
    private ActionType EvaluationNPC4()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // NPC 1 tries to default plus one

        return bestAction;

    }
    private ActionType EvaluationNPC5()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // NPC 1 tries to default plus one

        return bestAction;

    }
    private ActionType EvaluateBestAction()
    {
        switch(m_AIType)
        {
            case TurnOrder.TURN_ORDER_NPC1:
                return EvaluationNPC1();
            case TurnOrder.TURN_ORDER_NPC2:
                return EvaluationNPC2();
            case TurnOrder.TURN_ORDER_NPC3:
                return EvaluationNPC3();
            case TurnOrder.TURN_ORDER_NPC4:
                return EvaluationNPC4();
            case TurnOrder.TURN_ORDER_NPC5:
                return EvaluationNPC5();
            default:
                Debug.LogError("AIType is invalid");
                return ActionType.ACTION_TYPE_INVALID;
        }
    }

    public ActionType GetAction()
    {
        return EvaluateBestAction();
    }

    public MoveType GetMoveType(ActionType actionType)
    {
        switch(m_AIType)
        {
            case TurnOrder.TURN_ORDER_NPC1:
                {
                    return MoveType.MOVE_TYPE_INVALID;
                }
            case TurnOrder.TURN_ORDER_NPC2:
                {
                    return MoveType.MOVE_TYPE_INVALID;
                }
            case TurnOrder.TURN_ORDER_NPC3:
                {
                    return MoveType.MOVE_TYPE_INVALID;
                }
            case TurnOrder.TURN_ORDER_NPC4:
                {
                    return MoveType.MOVE_TYPE_INVALID;
                }
            case TurnOrder.TURN_ORDER_NPC5:
                {
                    return MoveType.MOVE_TYPE_INVALID;
                }
            default:
                Debug.LogError("AIType is invalid");
                return MoveType.MOVE_TYPE_INVALID;

        }
    }
}
