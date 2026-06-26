using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using System;

public enum InputState
{
    INPUT_STATE_INVALID = -1,
    INPUT_STATE_GAMEPLAY,
    INPUT_STATE_DIALOGUE,
    INPUT_STATE_MENU
}

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance { get; private set; }
    public InputState CurrentInputState { get { return m_currentInputState; } }
    public bool CanInteract { get { return m_canInteract; } set { m_canInteract = value; } }
    private InputActionMap m_gameplayMap;
    private InputActionMap m_dialogueMap;
    private InputActionMap m_menusMap;

    private InputState m_currentInputState = InputState.INPUT_STATE_GAMEPLAY;

    private bool m_canInteract = false;

    private void ChangeInputState(InputState state)
    {
        switch (state)
        {
            case InputState.INPUT_STATE_GAMEPLAY:
                m_gameplayMap.Enable();
                m_dialogueMap.Disable();
                m_menusMap.Disable();
                break;

            case InputState.INPUT_STATE_DIALOGUE:
                m_gameplayMap.Disable();
                m_dialogueMap.Enable();
                m_menusMap.Disable();
                break;

            case InputState.INPUT_STATE_MENU:
                m_gameplayMap.Disable();
                m_dialogueMap.Disable();
                m_menusMap.Enable();
                break;

            default:
                Debug.LogWarning("Tried to change input state to invalid state! | State: " + state.ToString());
                return;
        }

        m_currentInputState = state;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        m_gameplayMap = InputSystem.actions.FindActionMap("Player");
        m_dialogueMap = InputSystem.actions.FindActionMap("Dialogue");
        m_menusMap = InputSystem.actions.FindActionMap("UI");
#if UNITY_EDITOR
        Assert.IsNotNull(m_gameplayMap);
        Assert.IsNotNull(m_menusMap);
#endif
    }

    private void PollGameplayInput()
    {
        if (!m_gameplayMap.enabled)
        {
            return;
        }

        if (TurnManager.Instance.IsPlayersTurn())
        {
            m_canInteract = true;
        }
    }

    private void PollDialogueInput()
    {
        if (m_dialogueMap == null)
        {
            return;
        }

        if (!m_dialogueMap.enabled)
        {
            return;
        }

        InputAction advanceAction = m_dialogueMap.FindAction("Advance");
        if (advanceAction == null)
        {
            Debug.LogError("Advance Action is null in dialogue map. Make sure to add it in.");
            return;
        }

        if (advanceAction.WasPressedThisFrame())
        {
            // TODO AQIN: Need to make it smarter. If we are not over a button/clickable, just advance dialogue otherwise return
        }
    }

    private void PollMenuInput()
    {
        if (!m_menusMap.enabled)
        {
            return;
        }
    }
    private void PollInput()
    {
        PollGameplayInput();
        PollDialogueInput();
        PollMenuInput();
    }

    private void Update()
    {
        PollInput();
    }
}
