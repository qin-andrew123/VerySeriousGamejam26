using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }
    public bool TurningClockwise { get; private set; } = true;

    [SerializeField] private int _startingLives;
    [SerializeField] private float _angleOfRotation = 60.0f;
    [SerializeField] private float _rotationDuration = 1.0f;
    [SerializeField] private float _delayTime = 0.5f;
    [SerializeField] private List<Transform> _foodSpawnPoint;
    [SerializeField] private List<GameObject> _itemSpawnPrefabs;
    [SerializeField] private List<GameObject> _actorPositions;

    private List<GoalItem> _currentlyAvailableItems = new List<GoalItem>();
    private int m_numRotations = 0;
    private bool m_isRotating = false;
    private bool _isPlayerConstraintSatisfied = false;

    public List<Vector3> GetItemCurrentPositions()
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            result.Add(_currentlyAvailableItems[i].gameObject.transform.position);
        }

        return result;
    }

    public List<Vector3> SimulateRotateBoard(int numRotations, bool isClockwise)
    {
        List<Vector3> result = new List<Vector3>();
        float totalAngle = _angleOfRotation * numRotations * (isClockwise ? -1f : 1f);

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
        int randomItemIndex = Random.Range(0, _itemSpawnPrefabs.Count);

        GameObject go = Instantiate(_itemSpawnPrefabs[randomItemIndex]);
        GoalItem prefabGoalItem = go.GetComponent<GoalItem>();
        Assert.IsNotNull(prefabGoalItem);
        _currentlyAvailableItems.Add(prefabGoalItem);

        for (int i = 0; i < _currentlyAvailableItems.Count; ++i)
        {
            GenerateConstraintsForItem(i);
            DetermineSpawnPointForItem(_currentlyAvailableItems[i]);
        }
        
        int randomSpawnIndex = Random.Range(0, _foodSpawnPoint.Count);
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
    
    private void DetermineSpawnPointForItem(GoalItem item)
    {
        List<TurnOrder> validStartingLocations = new List<TurnOrder>();

        for (int i = 0; i < _foodSpawnPoint.Count; ++i)
        {
            TurnOrder spawnPointAsActor = (TurnOrder)i;
            if (!item.SatisfyingActors.Contains(spawnPointAsActor))
            {
                validStartingLocations.Add(spawnPointAsActor);
            }
        }

        // Same size as validStartingLocations
        List<float> weights = new List<float>();
        float totalWeight = 0.0f;
        for (int i = 0; i < validStartingLocations.Count; ++i)
        {
            int validSpawnIndex = (int)validStartingLocations[i];
            Vector3 spawnPointPosition = _foodSpawnPoint[validSpawnIndex].transform.position;

            float calculatedWeight = 0.0f;
            foreach (TurnOrder turn in item.SatisfyingActors)
            {
                int satisfyingIndex = (int)turn;
                Vector3 satisfyingSpawnPointPosition = _actorPositions[satisfyingIndex].transform.position;
                calculatedWeight += Vector3.SqrMagnitude(satisfyingSpawnPointPosition - spawnPointPosition);
            }

            totalWeight += calculatedWeight;
            weights.Add(calculatedWeight);
        }

        foreach (float f in weights)
        {
            Debug.Log($"Weight: {f}");
        }

        float randValue = Random.Range(0, totalWeight);
        int randIndex = 0;
        for (int i = 0; i < validStartingLocations.Count; ++i)
        {
            if (weights[i] > randValue)
            {
                randIndex = i;
                break;
            }
            randValue -= weights[i];
        }

        item.gameObject.transform.parent = _foodSpawnPoint[randIndex].transform;
        item.gameObject.transform.position = _foodSpawnPoint[randIndex].transform.position;
    }

    private void PickActorConstraint(GoalItem item, int numActorsToPick)
    {
        List<int> validIndicies = new List<int>();
        for (int i = (int)TurnOrder.TURN_ORDER_NPC1; i < (int)TurnOrder.TURN_ORDER_SIZE; ++i)
        {
            validIndicies.Add(i);
        }

        if (!_isPlayerConstraintSatisfied)
        {
            _isPlayerConstraintSatisfied = true;
            item.AddActorAsSatisfying(TurnOrder.TURN_ORDER_PLAYER);
            int playerIndex = (int)TurnOrder.TURN_ORDER_PLAYER;
            validIndicies.RemoveAt(playerIndex);

            --numActorsToPick;
        }

        while (numActorsToPick > 0)
        {
            int randIndex = Random.Range(0, validIndicies.Count);
            TurnOrder randomActor = (TurnOrder)validIndicies[randIndex];
            item.AddActorAsSatisfying(randomActor);
            validIndicies.RemoveAt(randIndex);
            --numActorsToPick;
        }
    }

    private void GenerateConstraintsForItem(int index)
    {
        GoalItem item = _currentlyAvailableItems[index];
        PickActorConstraint(item, 2);
        Assert.IsTrue(_isPlayerConstraintSatisfied, "Validating Items but Player Constraint isn't satisfied yet");
        Assert.IsTrue(item.SatisfyingActors.Count >= 2);
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

        float totalAngle = _angleOfRotation * m_numRotations * (TurningClockwise ? -1f : 1f);
        
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, totalAngle);
        float elapsed = 0f;
        while (elapsed < _rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _rotationDuration);
            float currentAngle = Mathf.Lerp(0f, totalAngle, t);
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, currentAngle);
            yield return null;
        }

        transform.rotation = endRot;
        yield return new WaitForSeconds(0.3f);
        m_isRotating = false;
        m_numRotations = 0;
    }

    private IEnumerator ActionCompleteDelay()
    {
        yield return new WaitForSeconds(_delayTime);
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
