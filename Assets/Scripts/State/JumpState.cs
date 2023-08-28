using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 수직 상승 힘을 받을 때만, jumpState
// 수직 상승 힘을 받는 순간이 끝나면 idleState
// SuperJump는 JumpState 의 multiplyValue 값의 변경으로 구현
public class JumpState : BaseState
{
    float multiplyValue = 1f;
    float checkJumpTimer;
    
    GameObject _mainCamera;

    float moveSpeed = 7f;
    float targetSpeed;
    
    public JumpState( PlayerController pController, Inputs inputManager) : base(pController, inputManager, stateName: StateName.JUMP)
    {
        Debug.Log("JumpState 생성");
        _mainCamera = pController.GetMainCamera();
    }

    public override void OnEnterState()
    {
        pController.isJumping = true;
    	if(pController.OnLadder)
        {
            pController.OnLadder = false;
            multiplyValue = 1f;
        }
        else if (pController._controller.isGrounded)
        {
            multiplyValue = (pController.canSuperJumpTimer > 0) ? 2f : 1f;
        }
        pController._verticalVelocity = Mathf.Sqrt(pController.JumpHeight * -2f * pController.Gravity * multiplyValue);
        checkJumpTimer = .005f * pController._verticalVelocity;
    }

    public override void OnUpdateState()
    {
        if (checkJumpTimer > 0) checkJumpTimer -= Time.deltaTime;
        if(pController.isJumping && pController._controller.isGrounded && checkJumpTimer <= 0f) // 땅바닥 점프 후
        {
            pController.stateMachine.ChangeState(StateName.WALK); // to IdleState
        }
        targetSpeed = moveSpeed;

        if (inputManager.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3( pController._controller.velocity.x, 0.0f, pController._controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = inputManager.analogMovement ? inputManager.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || 
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            pController._speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * pController.SpeedChangeRate);

            // round speed to 3 decimal places
            pController._speed = Mathf.Round(pController._speed * 1000f) / 1000f;
        }
        else
        {
            pController._speed = targetSpeed;
        }


        // normalise input direction
        Vector3 inputDirection = new Vector3(inputManager.move.x, 0.0f, inputManager.move.y).normalized;

        // if there is a move input rotate player when the player is moving
        if (inputManager.move != Vector2.zero)
        {
            pController._targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float  rotation = Mathf.SmoothDampAngle(pController.transform.eulerAngles.y, pController._targetRotation, ref pController._rotationVelocity, pController.RotationSmoothTime);

            // rotate to face input direction relative to camera position
            pController.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, pController._targetRotation, 0.0f) * Vector3.forward;

        // move the player
        pController._controller.Move(targetDirection.normalized * (pController._speed * Time.deltaTime) +
                            new Vector3(0.0f, pController._verticalVelocity, 0.0f) * Time.deltaTime);
        /*
        if(pController.isJumping ) // 사다리에서 점프 후
        {            
            pController.stateMachine.ChangeState(StateName.WALK); // to IdleState
        }
        */
    }

    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {
        pController.isJumping = false;
        multiplyValue = 1f;
    }
}