using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;



public class StateMachine
{

    public BaseState CurrentState {get; private set;}
    private Dictionary<StateName, BaseState> states = new Dictionary<StateName, BaseState>();

    public StateMachine(StateName stateName, BaseState state)
    {
        Debug.Log("InitStateMachine");

        AddState(stateName, state);
        CurrentState = GetState(stateName);
    }

    public void AddState(StateName stateName, BaseState state)
    {
        Debug.Log("AddState :" + stateName);

        if(!states.ContainsKey(stateName))
        {
            states.Add(stateName, state);
        }
    }

    public BaseState GetState(StateName stateName)
    {
        if(states.TryGetValue(stateName, out BaseState state))
            return state;
        return null;

    }

    public void DeleteState(StateName removeStateName)
    {
        if (states.ContainsKey(removeStateName))
        {
            states.Remove(removeStateName);
        }
    }

    public void ChangeState(StateName nextStateName)
    {
        Debug.Log("ChangeState from (" + CurrentState + ")  to (" + nextStateName + ")");

        CurrentState?.OnExitState();
        if( states.TryGetValue(nextStateName, out BaseState newState ))
        {
            CurrentState = newState;
        }
        CurrentState?.OnEnterState();
    }


    public void UpdateState()
    {
        CurrentState?.OnUpdateState();
    }

    public void FixedUpdateState()
    {
        CurrentState?.OnFixedUpdateState();
    }
}

public abstract class BaseState
{
    protected PlayerController controller{ get; private set; }

    public BaseState (PlayerController controller)
    {
        this.controller = controller;
    }

    public abstract void OnEnterState();
    public abstract void OnUpdateState();
    public abstract void OnFixedUpdateState();
    public abstract void OnExitState();

}

public enum StateName{
    WALK = 100,
    DASH,
    ATTACK
}

public class WalkState : BaseState
{
    public const float CONVERT_UNIT_VALUE = 0.01f;
    public const float DEFAULT_CONVERT_MOVESPEED = 3f;

    Inputs _input;
    GameObject _mainCamera;

    float moveSpeed = 7f;
    float targetSpeed;

    public WalkState( PlayerController controller) : base(controller)
    {
        Debug.Log("WalkState 생성");
        
        _input      = controller.GetInputs();
        _mainCamera = controller.GetMainCamera();
    }


    public override void OnEnterState()
    {}

    public override void OnUpdateState()
    {
        targetSpeed = moveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3( controller._controller.velocity.x, 0.0f, controller._controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            controller._speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * controller.SpeedChangeRate);

            // round speed to 3 decimal places
            controller._speed = Mathf.Round(controller._speed * 1000f) / 1000f;
        }
        else
        {
            controller._speed = targetSpeed;
        }


        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            controller._targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float  rotation = Mathf.SmoothDampAngle(controller.transform.eulerAngles.y, controller._targetRotation, ref controller._rotationVelocity, controller.RotationSmoothTime);

            // rotate to face input direction relative to camera position
            controller.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, controller._targetRotation, 0.0f) * Vector3.forward;

        // move the player
        controller._controller.Move(targetDirection.normalized * (controller._speed * Time.deltaTime) +
                            new Vector3(0.0f, controller._verticalVelocity, 0.0f) * Time.deltaTime);

    }

    public override void OnFixedUpdateState()
    {

    }
    public override void OnExitState()
    {
        
    }

}




public class DashState : BaseState
{
    bool isTimer = false;
    // TODO: dashPOWER 입력하기 in script

    public float dashPower = 10;
    public float dashRollTime = 0.2f; // 대시 앞구르기 모션 시간.
    public float dashTetanyTime = 0.1f;      // 대시 후, 경직시간 
    public float dashCoolTime = 0.1f;

    public DashState( PlayerController controller) : base(controller)
    {
        Debug.Log("DashState 생성");
    }


    public override void OnEnterState()
    {

    }

    public override void OnUpdateState()
    {
        Vector3 dashDirection = (controller.transform.forward).normalized; // TODO 계산 필요. 경사면 등

        float minimumDash = dashPower * Time.deltaTime;
        float addDash     = controller.getSpeed()    * Time.deltaTime;

        Vector3 verticalDash = new Vector3(0.0f, controller.getVerticalVelocity(), 0.0f) * Time.deltaTime;
        
        controller._controller.Move( dashDirection * (minimumDash + addDash) + verticalDash);
        
        // todo: timer 생성을 once로 끝내야함
        // 아니면 

        if(!isTimer){
            isTimer = true;

            controller.wallJumpCounter = 0f;  // wall jump cancel
            controller.dashCounter = 0;
            controller.isDashing = true;

            Timer.CreateTimer(controller.gameObject, dashRollTime, DashRollTimer);
        }
    
    }

    void DashRollTimer()
    {
        controller.isDashing    = false; // 이게 왜 실행 안됨?
        controller.isDashTetany = true;

        //Debug.Log( "DashRollTimer : controller.isDashing " + controller.isDashing);

        Timer.CreateTimer(controller.gameObject, dashRollTime, DashTetanyTimer);
    }

    void DashTetanyTimer()
    {
        controller.isDashTetany = false;

        controller.stateMachine.ChangeState(StateName.WALK);

        controller.isDashCool = true;
        Timer.CreateTimer(controller.gameObject, dashCoolTime, DashCoolTimer); // dashCoolTime 후에 DashCoolTimer() 실행
    }


    void DashCoolTimer()
    {
        controller.isDashCool = false;
        isTimer = false;
    }


    public override void OnFixedUpdateState()
    {

    }
    public override void OnExitState()
    {
        
    }
}
