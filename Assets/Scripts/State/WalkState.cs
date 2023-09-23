using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// == Idle State
public class WalkState : BaseState
{
    public const float CONVERT_UNIT_VALUE = 0.01f;
    public const float DEFAULT_CONVERT_MOVESPEED = 3f;

    GameObject _mainCamera;

    float SlowWalkSpeed = 1.0f;
    float SprintSpeed   = 10.0f;
    float moveSpeed     = 7f;

    float targetSpeed;

    public WalkState( PlayerController pController, Inputs inputManager) : base(pController, inputManager, StateName.WALK)
    {
        //Debug.Log("WalkState 생성");
        
        _mainCamera = pController.GetMainCamera();
    }


    public override void OnEnterState()
    {}

    public override void OnUpdateState()
    {
        targetSpeed = moveSpeed;

        if( inputManager.slowWalk ){ targetSpeed = SlowWalkSpeed; }
        if( inputManager.sprint   ){ targetSpeed = SprintSpeed;   } // 같이 누르면 sprint speed


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


        if (pController.warpTimer <= 0f) // 워프 안되는 현상 방지
        {
            pController._controller.Move(targetDirection.normalized * (pController._speed * Time.deltaTime) +
                    new Vector3(0.0f, pController._verticalVelocity, 0.0f) * Time.deltaTime);
        }


        if (pController._touchLadder)
        {
            // 사다리에 붙고
            Debug.Log(inputManager.move);
            if (inputManager.move.y > 0)  // 화살표Up 누르는 순간
            {
                // [TODO] 사다리를 바라봐야한다면, 바라보는 대상 카메라 -> 사다리 변경 필요
                pController.OnLadder = true; // state 상태 진입
                if (!pController.isExitLadder) pController.stateMachine.ChangeState(StateName.LADDER); // walk state에서 전환
            }
        }

         if ( targetSpeed == SprintSpeed)
         {
            startEmmiter();
         }else{
            stopEmmiter();
         }

    }

    public override void OnFixedUpdateState()
    {

    }
    public override void OnExitState()
    {
        
    }

    public override void HandleInputs()
    {

    }


    private void startEmmiter()
    {
        foreach (TrailRenderer T in pController.Tyremarks)
        {
            T.emitting = true;
        }
    }
    private void stopEmmiter()
    {
        foreach (TrailRenderer T in pController.Tyremarks)
        {
            T.emitting = false;
        }
    }

}