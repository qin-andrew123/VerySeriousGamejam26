using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI;

public class PlayerActionManager : MonoBehaviour
{
    public static PlayerActionManager Instance { get; private set; }

    [SerializeField] private const int NUM_ACTIONS_PER_TURN = 2;
    [SerializeField] private GameplayUIControl _gameplayUIControl;
    [SerializeField]
    [Tooltip("Weights for [ Plus 1 | Skip | Reverse | Chaos ]")]
    private List<float> _actionWeights = new List<float>();

    // [ Plus 1 | Skip | Reverse | Chaos ] if the given acton is available
    private List<bool> _availableActions;

    public void SelectAction(int index)
    {
#if UNITY_EDITOR
        Assert.IsTrue(_availableActions[index]);

        Assert.IsTrue(index >= 0 && index < (int)MoveType.MOVE_TYPE_SIZE);
#endif

        Debug.Log("Player Picked Action: " + index);

        PlayerInput.Instance.CanInteract = false;
        TurnManager.Instance.OnPlayerSelectedAction((MoveType)index);
    }
    public void GenerateAvailableActions()
    {
#if UNITY_EDITOR
        // Debugging purposes for random action
        if (false)
        {
            SelectAction(Random.Range(0,4));
            return;
        }
#endif

        PickActionsRandom();
        MarkButtonAsAvailable();
    }

    private void PickActionsRandom()
    {
        for (int i = 0; i < _availableActions.Count; ++i)
        {
            _availableActions[i] = true;
        }
    }

    private void MarkButtonAsAvailable()
    {
        for (int i = 0; i < _availableActions.Count; ++i)
        {
            if (_availableActions[i])
            {
                _gameplayUIControl.MarkButtonAsAvailable(i);
            }
            else
            {
                _gameplayUIControl.MarkButtonAsUnavailable(i);
            }
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

        _availableActions = Enumerable.Repeat(false, 4).ToList();

#if UNITY_EDITOR
        Assert.IsTrue(_actionWeights.Count == (int)MoveType.MOVE_TYPE_SIZE);
        Assert.IsTrue(_availableActions.Count == (int)MoveType.MOVE_TYPE_SIZE);
#endif
    }
}
