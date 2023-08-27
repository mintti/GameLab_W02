using UnityEngine;
using UnityEngine.InputSystem;


public class Inputs : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;
    public bool slowWalk;
    public bool dash;
    public bool backflip;
    public bool attack;
    
    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    public PlayerController playerController;
    

    void Start() 
    {

    }

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
        //playerController.HandlingJump();
        //if(playerController.canWallJump) // 왜 true?
        if( !playerController.isWallJumping && playerController.wallJumpCounter > 0 ) // 왜 true?
        {
            playerController.stateMachine.ChangeState(StateName.WALLJUMP);
        }
        else if(!playerController.isJumping && playerController._controller.isGrounded){
            playerController.stateMachine.ChangeState(StateName.JUMP);
        }
	}

	public void OnSprint(InputValue value)
	{
        sprint = value.isPressed;        
	}

    public void OnSlowWalk(InputValue value)
	{
        slowWalk = value.isPressed;
	}

    public void OnDash(InputValue value)
	{
        dash = value.isPressed;
        if(dash)
        {
            //playerController.Dash();
            bool isAvailableDash = !playerController.isDashing && !playerController.isDashTetany && !playerController.isDashCool && (playerController.dashCounter > 0); 
            if(isAvailableDash){
                playerController.stateMachine.ChangeState(StateName.DASH);
            }else{

                //Debug.Log("playerController.isDashing(" +playerController.isDashing + ") playerController.isDashTetany("+playerController.isDashTetany+") " + " playerController.dashCounter("+ playerController.dashCounter+")");
            }

        }
	}
    
    public void OnBackflip(InputValue value)
    {
	    if(value.isPressed)
	    {
		    // playerController.wallJumpCounter = 0f;
		    playerController.Backflip();
            // playerController.stateMachine.ChangeState(StateName.DASH);

	    }
    }
    
    public void OnAttack(InputValue value)
    {
	    attack = value.isPressed;
	    if(attack && Time.time > playerController.nextFireTime && playerController.comboCount < 3)
	    {
		    playerController.Attack();
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

