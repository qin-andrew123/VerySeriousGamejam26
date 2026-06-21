using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
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
    MOVE_TYPE_INVALID = -2,
    MOVE_TYPE_PASS = -1,
    MOVE_TYPE_PLUS_ONE,
    MOVE_TYPE_SKIP,
    MOVE_TYPE_REVERSE,
    MOVE_TYPE_CHAOS,
    MOVE_TYPE_SIZE
}
public class AIEvaluator : MonoBehaviour
{
    [SerializeField] private TurnOrder m_AIType;
    [SerializeField] private TurnOrder m_opponent = TurnOrder.TURN_ORDER_INVALID;

    public ActionType GetAction()
    {
        return EvaluateBestAction();
    }

    public MoveType GetMoveType(ActionType actionType)
    {
        switch (m_AIType)
        {
            case TurnOrder.TURN_ORDER_NPC1:
                return GetNPC1Move(actionType);
            case TurnOrder.TURN_ORDER_NPC2:
                return GetNPC2Move(actionType);
            case TurnOrder.TURN_ORDER_NPC3:
                return GetNPC3Move(actionType);
            case TurnOrder.TURN_ORDER_NPC4:
                return GetNPC4Move(actionType);
            case TurnOrder.TURN_ORDER_NPC5:
                return GetNPC5Move(actionType);
            default:
                Debug.LogError("AIType is invalid");
                return MoveType.MOVE_TYPE_INVALID;
        }
    }

    // Default Tries Plus One, Fallback is Reverse
    private ActionType EvaluationNPC1()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // Check positions based on simulation Currently, there is only one but 
        List<Vector3> currentPositions = BoardManager.Instance.GetCurrentPositions();
        List<Vector3> predictedPositions = BoardManager.Instance.SimulateRotateBoard(2, BoardManager.Instance.TurningClockwise);
        for (int i = 0; i < predictedPositions.Count; ++i)
        {
            float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
            float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

            if (distSqrToPredicted < distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_DEFAULT;
            }
            else if (distSqrToPredicted >= distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_FALLBACK;
            }
        }

        if (bestAction == ActionType.ACTION_TYPE_FALLBACK)
        {
            predictedPositions.Clear();
            predictedPositions = BoardManager.Instance.SimulateRotateBoard(1, !BoardManager.Instance.TurningClockwise);

            for (int i = 0; i < predictedPositions.Count; ++i)
            {
                float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
                float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

                if (distSqrToPredicted >= distSqrToCurrent && Random.Range(0.0f, 1.0f) < 0.2f)
                {
                    bestAction = ActionType.ACTION_TYPE_PASS;
                }
            }
        }

        return bestAction;
    }

    // Default Tries Plus One, Fallback is Reverse
    private MoveType GetNPC1Move(ActionType actionType)
    {
        if (actionType == ActionType.ACTION_TYPE_DEFAULT)
        {
            return MoveType.MOVE_TYPE_PLUS_ONE;
        }
        else if (actionType == ActionType.ACTION_TYPE_FALLBACK)
        {
            return MoveType.MOVE_TYPE_REVERSE;
        }
        else
        {
            return MoveType.MOVE_TYPE_PASS;
        }
    }

    // Default Tries Reverse Fallback is plus one
    private ActionType EvaluationNPC2()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // Check positions based on simulation Currently, there is only one but 
        List<Vector3> currentPositions = BoardManager.Instance.GetCurrentPositions();
        List<Vector3> predictedPositions = BoardManager.Instance.SimulateRotateBoard(1, !BoardManager.Instance.TurningClockwise);
        for (int i = 0; i < predictedPositions.Count; ++i)
        {
            float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
            float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

            if (distSqrToPredicted < distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_DEFAULT;
            }
            else if (distSqrToPredicted >= distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_FALLBACK;
            }
        }

        if (bestAction == ActionType.ACTION_TYPE_FALLBACK)
        {
            predictedPositions.Clear();
            predictedPositions = BoardManager.Instance.SimulateRotateBoard(2, BoardManager.Instance.TurningClockwise);

            for (int i = 0; i < predictedPositions.Count; ++i)
            {
                float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
                float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

                if (distSqrToPredicted >= distSqrToCurrent && Random.Range(0.0f, 1.0f) < 0.2f)
                {
                    bestAction = ActionType.ACTION_TYPE_PASS;
                }
            }
        }

        return bestAction;
    }

    // Default Tries Reverse Fallback is plus one
    private MoveType GetNPC2Move(ActionType actionType)
    {
        if (actionType == ActionType.ACTION_TYPE_DEFAULT)
        {
            return MoveType.MOVE_TYPE_REVERSE;
        }
        else if (actionType == ActionType.ACTION_TYPE_FALLBACK)
        {
            return MoveType.MOVE_TYPE_PLUS_ONE;
        }
        return MoveType.MOVE_TYPE_PASS;
    }

    // Default Action is Skip 30% chance | +1 Otherwise. Fallback is Reverse
    private ActionType EvaluationNPC3()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // Check positions based on simulation Currently, there is only one but 
        List<Vector3> currentPositions = BoardManager.Instance.GetCurrentPositions();
        List<Vector3> predictedPositions = BoardManager.Instance.SimulateRotateBoard(2, BoardManager.Instance.TurningClockwise);
        for (int i = 0; i < predictedPositions.Count; ++i)
        {
            float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
            float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

            if (distSqrToPredicted < distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_DEFAULT;
            }
            else if (distSqrToPredicted >= distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_FALLBACK;
            }
        }

        if (bestAction == ActionType.ACTION_TYPE_FALLBACK)
        {
            predictedPositions.Clear();
            predictedPositions = BoardManager.Instance.SimulateRotateBoard(1, !BoardManager.Instance.TurningClockwise);

            for (int i = 0; i < predictedPositions.Count; ++i)
            {
                float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
                float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

                if (distSqrToPredicted >= distSqrToCurrent && Random.Range(0.0f, 1.0f) < 0.2f)
                {
                    bestAction = ActionType.ACTION_TYPE_PASS;
                }
            }
        }

        return bestAction;
    }

    // Default Action is Skip 30% chance | +1 Otherwise. Fallback is Reverse
    private MoveType GetNPC3Move(ActionType actionType)
    {
        if (actionType == ActionType.ACTION_TYPE_DEFAULT)
        {
            if (Random.Range(0f, 1f) <= 0.3f)
            {
                return MoveType.MOVE_TYPE_SKIP;
            }
            else
            {
                return MoveType.MOVE_TYPE_PLUS_ONE;
            }
        }
        else if (actionType == ActionType.ACTION_TYPE_FALLBACK)
        {
            return MoveType.MOVE_TYPE_REVERSE;
        }

        return MoveType.MOVE_TYPE_PASS;
    }

    // Default is Skip 30% Chance | Reverse otherwise. Fallback is Plus One
    private ActionType EvaluationNPC4()
    {
        ActionType bestAction = ActionType.ACTION_TYPE_DEFAULT;
        float heuristic = 1.0f;

        if (m_opponent == TurnOrder.TURN_ORDER_INVALID)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        // Check positions based on simulation Currently, there is only one but 
        List<Vector3> currentPositions = BoardManager.Instance.GetCurrentPositions();
        List<Vector3> predictedPositions = BoardManager.Instance.SimulateRotateBoard(1, !BoardManager.Instance.TurningClockwise);
        for (int i = 0; i < predictedPositions.Count; ++i)
        {
            float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
            float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

            if (distSqrToPredicted < distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_DEFAULT;
            }
            else if (distSqrToPredicted >= distSqrToCurrent)
            {
                bestAction = ActionType.ACTION_TYPE_FALLBACK;
            }
        }

        if (bestAction == ActionType.ACTION_TYPE_FALLBACK)
        {
            predictedPositions.Clear();
            predictedPositions = BoardManager.Instance.SimulateRotateBoard(2, BoardManager.Instance.TurningClockwise);

            for (int i = 0; i < predictedPositions.Count; ++i)
            {
                float distSqrToCurrent = Vector3.SqrMagnitude(currentPositions[i] - gameObject.transform.position);
                float distSqrToPredicted = Vector3.SqrMagnitude(predictedPositions[i] - gameObject.transform.position);

                if (distSqrToPredicted >= distSqrToCurrent && Random.Range(0.0f, 1.0f) < 0.2f)
                {
                    bestAction = ActionType.ACTION_TYPE_PASS;
                }
            }
        }

        return bestAction;
    }

    // Default is Skip 30% Chance | Reverse otherwise. Fallback is Plus One
    private MoveType GetNPC4Move(ActionType actionType)
    {
        if (actionType == ActionType.ACTION_TYPE_DEFAULT)
        {
            if (Random.Range(0f, 1f) <= 0.3f)
            {
                return MoveType.MOVE_TYPE_SKIP;
            }
            else
            {
                return MoveType.MOVE_TYPE_REVERSE;
            }
        }
        else if (actionType == ActionType.ACTION_TYPE_FALLBACK)
        {
            return MoveType.MOVE_TYPE_PLUS_ONE;
        }

        return MoveType.MOVE_TYPE_PASS;
    }

    // Special 5% chance for chaos so 5% chance to do something otherwise pass.
    private ActionType EvaluationNPC5()
    {
        if (Random.Range(0f, 1f) < 0.03f)
        {
            return ActionType.ACTION_TYPE_DEFAULT;
        }

        return ActionType.ACTION_TYPE_PASS;
    }

    // Special 5% chance for chaos so 5% chance to do something otherwise pass.
    private MoveType GetNPC5Move(ActionType actionType)
    {
        if (actionType == ActionType.ACTION_TYPE_DEFAULT)
        {
            return MoveType.MOVE_TYPE_CHAOS;
        }

        return MoveType.MOVE_TYPE_PASS;
    }

    private ActionType EvaluateBestAction()
    {
        switch (m_AIType)
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
}
