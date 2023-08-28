using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashState : BaseState
{
    bool isTimer = false;
    // TODO: dashPOWER 입력하기 in script

    public float dashPower = 60;
    public float dashRollTime = 0.2f; // 대시 앞구르기 모션 시간.
    public float dashTetanyTime = 0.1f;      // 대시 후, 경직시간 
    public float dashCoolTime = 0.2f;
    public Inputs _input;
    public GameObject _mainCamera;
    float moveSpeed = 7f;
    float targetSpeed;
    
    public DashState( PlayerController pController, Inputs inputManager) : base(pController,inputManager, StateName.DASH)
    {
        Debug.Log("DashState 생성");
        _input      = pController.GetInputs();
        _mainCamera = pController.GetMainCamera();
    }


    public override void OnEnterState()
    {
        pController.dashCounter = 0;
        pController.isDashing = true;
        Timer.CreateTimer(pController.gameObject, dashRollTime, DashRollTimer);
        
        Vector3 dashDirection = (pController.transform.forward).normalized; // TODO 계산 필요. 경사면 등

        float minimumDash = dashPower * Time.deltaTime;
        float addDash     = pController.getSpeed()    * Time.deltaTime;

        Vector3 verticalDash = new Vector3(0.0f, pController.getVerticalVelocity(), 0.0f) * Time.deltaTime;
        
        pController._controller.Move( dashDirection * (minimumDash + addDash) + verticalDash); // 이걸 playerController에서 call 하고 param을 바꾸는 방향
    }

    public override void OnUpdateState()
    {

        //Vector3 value = inputManager.move * Time.deltaTime * pController._speed;
        //pController._controller.Move(value);
        
        // move the player
        //Vector3 targetDirection = Quaternion.Euler(0.0f, pController._targetRotation, 0.0f) * Vector3.forward;
        //pController._controller.Move(targetDirection.normalized * (pController._speed * Time.deltaTime) +
                                    //new Vector3(0.0f, pController._verticalVelocity, 0.0f) * Time.deltaTime);
                           
                                    
        targetSpeed = moveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3( pController._controller.velocity.x, 0.0f, pController._controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

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
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
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
        pController.isDashing    = false; // 이게 왜 실행 안됨?
        pController.isDashTetany = true;

        //Debug.Log( "DashRollTimer : pController.isDashing " + pController.isDashing);

        Timer.CreateTimer(pController.gameObject, dashRollTime, DashTetanyTimer);
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
        isTimer = false;
    }
}
