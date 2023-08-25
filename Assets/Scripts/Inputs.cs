using UnityEngine;
using UnityEngine.InputSystem;


public class Inputs : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool dash;
    public bool backflip;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    public PlayerController playerController;
    
	public void OnMove(InputValue value)
	{
        move = value.Get<Vector2>();
	}
    
	public void OnLook(InputValue value)
	{
		if(cursorInputForLook)
		{
			look = value.Get<Vector2>();
		}
	}

	public void OnJump(InputValue value)
	{
		jump = value.isPressed;
        playerController.HandlingJump();
	}

	public void OnSprint(InputValue value)
	{
        sprint = value.isPressed;
	}

    public void OnDash(InputValue value)
	{
        dash = value.isPressed;
        if(dash)
        {
            playerController.Dash();
        }
	}
    
    public void OnBackflip(InputValue value)
    {
	    if(value.isPressed)
	    {
		    playerController.wallJumpCounter = 0f;
		    playerController.Backflip();
	    }
    }
    

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}

