using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseState
{
    float dashPower      = 60;    // TODO: dashPOWER 입력하기 in script
    float dashRollTime   = 0.2f;  // 대시 앞구르기 모션 시간.
    float dashTetanyTime = 0.1f;  // 대시 후, 경직시간.  현재 안 쓰는중
    float dashCoolTime   = 0.2f;

    float moveSpeed      = 7f;
    float targetSpeed;
    
    GameObject _mainCamera;

    public DashState( PlayerController pController, Inputs inputManager) : base(pController, inputManager, StateName.DASH)
    {
        Debug.Log("DashState 생성");
        _mainCamera = pController.GetMainCamera();
    }


    public override void OnEnterState()
    {
        pController.dashCounter = 0;
        pController.isDashing = true;
        
        Vector3 dashDirection = (pController.transform.forward).normalized; // TODO 계산 필요. 경사면 등
        float   minimumDash   = dashPower * Time.deltaTime;
        float   addDash       = pController._speed * Time.deltaTime;
        Vector3 verticalDash  = new Vector3(0.0f, pController._verticalVelocity, 0.0f) * Time.deltaTime;
        
        pController._controller.Move( dashDirection * (minimumDash + addDash) + verticalDash); // 이걸 playerController에서 call 하고 param을 바꾸는 방향


        Timer.CreateTimer(pController.gameObject, dashRollTime, DashRollTimer);
    }

    public override void OnUpdateState()
    {
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

    }

    void DashRollTimer()
    {
        pController.isDashing    = false;
        pController.isDashTetany = true;

        Timer.CreateTimer(pController.gameObject, dashTetanyTime, DashTetanyTimer);
    }

    void DashTetanyTimer()
    {
        pController.isDashTetany = false;

        if (pController.stateMachine.CurrentState.stateName == StateName.DASH)
        {
            pController.stateMachine.ChangeState(StateName.WALK);
        }
    }

    public override void OnFixedUpdateState() {}
    public override void OnExitState()
    {
        pController.isDashCool = true;
        Timer.CreateTimer(pController.gameObject, dashCoolTime, DashCoolTimer); // dashCoolTime 후에 DashCoolTimer() 실행   
    }
    void DashCoolTimer()
    {
        pController.isDashCool = false;
    }
}
