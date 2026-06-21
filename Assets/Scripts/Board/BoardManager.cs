using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;
    [SerializeField] private float m_angleOfRotation = 60.0f;
    [SerializeField] private float m_rotationDuration = 1.0f;
    [SerializeField] private float m_delayTime = 0.5f;
    [SerializeField] private List<Transform> m_foodSpawnPoint;
    [SerializeField] private List<GameObject> m_itemSpawnPrefabs;
    private int m_numRotations = 1;
    private bool m_isRotating = false;

    public void InitializeBoard()
    {
        // TODO AQIN: More advanced calculations: based on rotations but for now just randomly pick a spot
        int randomSpawnIndex = Random.Range(0, m_foodSpawnPoint.Count);
        int randomItemIndex = Random.Range(0, m_itemSpawnPrefabs.Count);

        GameObject go = Instantiate(m_itemSpawnPrefabs[randomItemIndex], m_foodSpawnPoint[randomSpawnIndex]);
    }

    public void ResetRotationsAmount()
    {
        // By default we just rotate once
        m_numRotations = 1;
    }

    public void RotateBoardByAmount(int numTurns)
    {
        m_numRotations += numTurns;
        RotateBoard();
        StartCoroutine(ActionCompleteDelay());
    }

    public void RotateBoard()
    {
        if (m_isRotating)
        {
            return;
        }

        StartCoroutine(RotateBoardCoroutine());
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

    private IEnumerator RotateBoardCoroutine()
    {
        m_isRotating = true;

        float clampedAngleOfRotation = (float)((m_angleOfRotation * m_numRotations) % 360);
        if (TurningClockwise)
        {
            clampedAngleOfRotation *= -1f;
        }

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, clampedAngleOfRotation);

        float elapsed = 0f;
        while (elapsed < m_rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_rotationDuration);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.rotation = endRot;

        yield return new WaitForSeconds(0.5f);
        m_isRotating = false;
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
