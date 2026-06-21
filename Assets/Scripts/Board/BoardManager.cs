using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;
    [SerializeField] private float m_angleOfRotation = 60.0f;
    [SerializeField] private float m_rotationDuration = 1.0f;
    [SerializeField] private float m_delayTime = 0.5f;
    [SerializeField] private List<Transform> m_foodSpawnPoint;
    [SerializeField] private List<GameObject> m_itemSpawnPrefabs;
    private List<GameObject> _currentlyAvailableItems = new List<GameObject>();
    private int m_numRotations = 1;
    private bool m_isRotating = false;

    // TODO AQIN: make this into like Item tag + position
    public List<Vector3> GetCurrentPositions()
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            result.Add(_currentlyAvailableItems[i].transform.position);
        }

        return result;
    }

    // TODO AQIN: make this into like Item tag + position
    public List<Vector3> SimulateRotateBoard(int numRotations, bool isClockwise)
    {
        List<Vector3> result = new List<Vector3>();
        float totalAngle = m_angleOfRotation * numRotations * (isClockwise ? -1f : 1f);

        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            Vector3 predicted = CalculateItemLocation(gameObject.transform, _currentlyAvailableItems[i].transform, endRot); ;
            Debug.Log($"Simulated Board Rotation Result {predicted}");
            result.Add(predicted);
        }

        return result;
    }

    public void InitializeBoard()
    {
        // TODO AQIN: More advanced calculations: based on rotations but for now just randomly pick a spot
        int randomSpawnIndex = Random.Range(0, m_foodSpawnPoint.Count);
        int randomItemIndex = Random.Range(0, m_itemSpawnPrefabs.Count);

        GameObject go = Instantiate(m_itemSpawnPrefabs[randomItemIndex], m_foodSpawnPoint[randomSpawnIndex]);
        _currentlyAvailableItems.Add(go);
    }

    public void RotateBoardByAmount(int numTurns)
    {
        m_numRotations += numTurns;
        RotateBoard();
    }

    public void RotateBoard()
    {
        if (m_isRotating)
        {
            return;
        }
        StartCoroutine(RotateBoardCoroutine());
    }

    public void RandomizeRotation()
    {
        int numRotations = Random.Range(0, 100);
        Debug.Log($"Rotating {numRotations} times");
        RotateBoardByAmount(numRotations);
    }

    public void PerformBoardMove(MoveType moveType)
    {
        if (moveType == MoveType.MOVE_TYPE_PLUS_ONE)
        {
            RotateBoardByAmount(1);
        }
        else if (moveType == MoveType.MOVE_TYPE_REVERSE)
        {
            TurningClockwise = !TurningClockwise;
        }
        else if (moveType == MoveType.MOVE_TYPE_CHAOS)
        {
            RandomizeRotation();
        }
        StartCoroutine(ActionCompleteDelay());
    }

    private Vector3 CalculateItemLocation(Transform parent, Transform obj, Quaternion simulatedParentRotation)
    {
        Vector3 localOffset = Vector3.zero;
        Quaternion localRot = Quaternion.identity;

        var chain = new List<Transform>();
        Transform t = obj;
        while (t != parent)
        {
            chain.Add(t);
            t = t.parent;

            if (chain.Count > 3)
            {
                Debug.LogError("Item could not be calculated because it is not a parent of the child");
                return Vector3.zero;
            }
        }

        chain.Reverse();

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        foreach (var node in chain)
        {
            pos = rot * node.localPosition + pos;
            rot = rot * node.localRotation;
        }

        return parent.position + simulatedParentRotation * pos;
    }

    private IEnumerator RotateBoardCoroutine()
    {
        m_isRotating = true;

        float totalAngle = m_angleOfRotation * m_numRotations * (TurningClockwise ? -1f : 1f);
        
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);
        float elapsed = 0f;
        while (elapsed < m_rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / m_rotationDuration);
            float currentAngle = Mathf.Lerp(0f, totalAngle, t);
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, currentAngle);
            yield return null;
        }

        transform.rotation = endRot;
        yield return new WaitForSeconds(0.3f);
        m_isRotating = false;
        m_numRotations = 1;
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
