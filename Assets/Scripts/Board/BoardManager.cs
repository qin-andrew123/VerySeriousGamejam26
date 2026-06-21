using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;
    [SerializeField] private float m_angleOfRotation = 60.0f;
    [SerializeField] private float m_delayTime = 1.0f;
    [SerializeField] private List<Transform> m_foodSpawnPoint;

    private int m_numRotations = 1;
    public void RotateBoardByAmount(int numTurns)
    {
        m_numRotations += numTurns;
        StartCoroutine(ActionCompleteDelay());
    }

    public void RotateBoard()
    {
        GameObject go = gameObject;

        if (TurningClockwise && m_angleOfRotation > 0.0f)
        {
            m_angleOfRotation *= -1;
        }

        go.transform.Rotate(new Vector3(0f, 0f, m_angleOfRotation), Space.Self);
    }

    public void ReverseDirection()
    {
        TurningClockwise = !TurningClockwise;
        StartCoroutine(ActionCompleteDelay());
    }

    public void RandomizeRotation()
    {
        StartCoroutine(ActionCompleteDelay());
    }

    public void PerformBoardMove(MoveType moveType)
    {
        if (moveType == MoveType.MOVE_TYPE_PLUS_ONE)
        {
            RotateBoardByAmount(1);
        }
        else if (moveType == MoveType.MOVE_TYPE_REVERSE)
        {
            ReverseDirection();
        }
        else if (moveType == MoveType.MOVE_TYPE_CHAOS)
        {
            RandomizeRotation();
        }
        else if (moveType == MoveType.MOVE_TYPE_PASS)
        {
            StartCoroutine(ActionCompleteDelay());
        }
    }

    private IEnumerator ActionCompleteDelay()
    {
        yield return new WaitForSeconds(m_delayTime);
        TurnManager.Instance.MarkActionComplete();
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
}
