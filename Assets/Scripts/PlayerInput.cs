using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

public enum InputState
{
    INPUT_STATE_INVALID = -1,
    INPUT_STATE_GAMEPLAY = 0,
    INPUT_STATE_DIALOGUE = 1,
}
public class PlayerInput : MonoBehaviour
{
    private InputActionMap m_gameplayMap;
    private InputActionMap m_dialogueMap;
    private InputState m_currentInputState = InputState.INPUT_STATE_GAMEPLAY;
    private Vector2 m_velocity = Vector2.zero;
    private float m_interactCooldown = 0.5f;
    private float m_interactTimer = 0.0f;
    private bool m_canInteract = false;
    private void ChangeInputState(InputState state)
    {
        switch (state)
        {
            case InputState.INPUT_STATE_GAMEPLAY:
                m_gameplayMap.Enable();
                m_dialogueMap.Disable();
                break;

            case InputState.INPUT_STATE_DIALOGUE:
                m_gameplayMap.Disable();
                m_dialogueMap.Enable();
                break;

            default:
                Debug.LogWarning("Tried to change input state to invalid state! | State: " + state.ToString());
                return;
        }

        m_currentInputState = state;
    }

    private void Awake()
    {
        m_gameplayMap = InputSystem.actions.FindActionMap("Gameplay");
        m_dialogueMap = InputSystem.actions.FindActionMap("Dialogue");
        Assert.IsNotNull(m_gameplayMap);
        Assert.IsNotNull(m_dialogueMap);
    }
    private void OnDestroy()
    {
    }

    private void PollGameplayInput()
    {
        if (!m_gameplayMap.enabled)
        {
            return;
        }

        InputAction moveAction = m_gameplayMap.FindAction("Move");
        if (moveAction == null)
        {
            Debug.LogError("Move Action is null in gameplay map. Make sure to add it in.");
            return;
        }

        if (moveAction.IsPressed())
        {
            m_velocity = moveAction.ReadValue<Vector2>().normalized;
        }
    }

    private void PollDialogueInput()
    {
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

    private void PollInput()
    {
        if (m_interactTimer <= 0)
        {
            m_canInteract = true;
        }
        else
        {
            m_interactTimer -= Time.deltaTime;
        }
        PollGameplayInput();
        PollDialogueInput();
    }

    private void Update()
    {
        PollInput();
    }
}
