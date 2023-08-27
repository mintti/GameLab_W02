using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum StateName{
    WALK = 100,
    DASH,
    JUMP,
    WALLJUMP,
    BACKFLIP,
    ATTACK,
    LADDER
}

public class StateMachine
{

    public BaseState CurrentState {get; private set;}
    private Dictionary<StateName, BaseState> states = new Dictionary<StateName, BaseState>();

    public StateMachine()
    {
        Debug.Log("InitStateMachine");
    }

    public void AddState(StateName stateName, BaseState state)
    {
        Debug.Log("AddState :" + stateName);

        if(!states.ContainsKey(stateName))
        {
            states.Add(stateName, state);
        }

        if(states.Count == 1){  // 처음 추가되는 스테이트가 CurrentState
            CurrentState = GetState(stateName);
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

    protected Inputs inputManager{ get; private set; }

    public StateName stateName{ get; private set; }
    
    public BaseState (PlayerController controller, Inputs inputsManager, StateName stateName)
    {
        this.controller   = controller;
        this.inputManager = inputsManager;
        this.stateName= stateName;
    }

    public abstract void OnEnterState();
    public abstract void OnUpdateState();
    public abstract void OnFixedUpdateState();
    public abstract void OnExitState();

}


// == Idle State
public class WalkState : BaseState
{
    public const float CONVERT_UNIT_VALUE = 0.01f;
    public const float DEFAULT_CONVERT_MOVESPEED = 3f;

    Inputs _input;
    GameObject _mainCamera;

    float moveSpeed = 7f;
    float targetSpeed;

    public WalkState( PlayerController controller, Inputs inputManager) : base(controller,inputManager, StateName.WALK)
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
        if (currentHorizontalSpeed < targetSpeed - speedOffset || 
            currentHorizontalSpeed > targetSpeed + speedOffset)
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
        // controller._controller.Move(targetDirection.normalized * (controller._speed * Time.deltaTime) +
        //                     new Vector3(0.0f, controller._verticalVelocity, 0.0f) * Time.deltaTime);


        if (controller.warpTimer <= 0f)
        {
            controller._controller.Move(targetDirection.normalized * (controller._speed * Time.deltaTime) +
                    new Vector3(0.0f, controller._verticalVelocity, 0.0f) * Time.deltaTime);
        }


        if (controller._touchLadder)
        {
            // 사다리에 붙고
            if (_input.move == Vector2.up)  // 화살표Up 누르는 순간
            {
                // [TODO] 사다리를 바라봐야한다면, 바라보는 대상 카메라 -> 사다리 변경 필요
                controller.OnLadder = true; // state 상태 진입
                if (!controller.isExitLadder) controller.stateMachine.ChangeState(StateName.LADDER); // walk state에서 전환
            }
        }

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

    public float dashPower = 60;
    public float dashRollTime = 0.2f; // 대시 앞구르기 모션 시간.
    public float dashTetanyTime = 0.1f;      // 대시 후, 경직시간 
    public float dashCoolTime = 0.2f;
    public Inputs _input;
    public GameObject _mainCamera;
    float moveSpeed = 7f;
    float targetSpeed;
    
    public DashState( PlayerController controller, Inputs inputManager) : base(controller,inputManager, StateName.DASH)
    {
        Debug.Log("DashState 생성");
        _input      = controller.GetInputs();
        _mainCamera = controller.GetMainCamera();
    }


    public override void OnEnterState()
    {
        controller.dashCounter = 0;
        controller.isDashing = true;
        Timer.CreateTimer(controller.gameObject, dashRollTime, DashRollTimer);
        
        Vector3 dashDirection = (controller.transform.forward).normalized; // TODO 계산 필요. 경사면 등

        float minimumDash = dashPower * Time.deltaTime;
        float addDash     = controller.getSpeed()    * Time.deltaTime;

        Vector3 verticalDash = new Vector3(0.0f, controller.getVerticalVelocity(), 0.0f) * Time.deltaTime;
        
        controller._controller.Move( dashDirection * (minimumDash + addDash) + verticalDash); // 이걸 playerController에서 call 하고 param을 바꾸는 방향
    }

    public override void OnUpdateState()
    {

        //Vector3 value = inputManager.move * Time.deltaTime * controller._speed;
        //controller._controller.Move(value);
        
        // move the player
        //Vector3 targetDirection = Quaternion.Euler(0.0f, controller._targetRotation, 0.0f) * Vector3.forward;
        //controller._controller.Move(targetDirection.normalized * (controller._speed * Time.deltaTime) +
                                    //new Vector3(0.0f, controller._verticalVelocity, 0.0f) * Time.deltaTime);
                           
                                    
        targetSpeed = moveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3( controller._controller.velocity.x, 0.0f, controller._controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || 
            currentHorizontalSpeed > targetSpeed + speedOffset)
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

        if (controller.stateMachine.CurrentState.stateName == StateName.DASH)
        {
            controller.stateMachine.ChangeState(StateName.WALK);
        }
    }

    public override void OnFixedUpdateState() {}
    public override void OnExitState()
    {
        controller.isDashCool = true;
        Timer.CreateTimer(controller.gameObject, dashCoolTime, DashCoolTimer); // dashCoolTime 후에 DashCoolTimer() 실행   
    }
    void DashCoolTimer()
    {
        controller.isDashCool = false;
        isTimer = false;
    }
}


// 수직 상승 힘을 받을 때만, jumpState
// 수직 상승 힘을 받는 순간이 끝나면 idleState
// SuperJump는 JumpState 의 multiplyValue 값의 변경으로 구현
public class JumpState : BaseState
{
    float multiplyValue = 1f;
    float checkJumpTimer;
    
    Inputs _input;
    GameObject _mainCamera;

    float moveSpeed = 7f;
    float targetSpeed;
    
    public JumpState( PlayerController controller, Inputs inputManager) : base(controller,inputManager,stateName: StateName.JUMP)
    {
        Debug.Log("JumpState 생성");
        _input      = controller.GetInputs();
        _mainCamera = controller.GetMainCamera();
    }

    public override void OnEnterState()
    {
        controller.isJumping = true;
    	if(controller.OnLadder)
        {
            controller.OnLadder = false;
            multiplyValue = 1f;
        }
        else if (controller._controller.isGrounded)
        {
            multiplyValue = (controller.canSuperJumpTimer > 0) ? 2f : 1f;
        }
        controller._verticalVelocity = Mathf.Sqrt(controller.JumpHeight * -2f * controller.Gravity * multiplyValue);
        checkJumpTimer = .005f * controller._verticalVelocity;
    }

    public override void OnUpdateState()
    {
        if (checkJumpTimer > 0) checkJumpTimer -= Time.deltaTime;
        if(controller.isJumping && controller._controller.isGrounded && checkJumpTimer <= 0f) // 땅바닥 점프 후
        {
            controller.stateMachine.ChangeState(StateName.WALK); // to IdleState
        }
        targetSpeed = moveSpeed;

        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3( controller._controller.velocity.x, 0.0f, controller._controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || 
            currentHorizontalSpeed > targetSpeed + speedOffset)
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
        /*
        if(controller.isJumping ) // 사다리에서 점프 후
        {            
            controller.stateMachine.ChangeState(StateName.WALK); // to IdleState
        }
        */
    }

    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {
        controller.isJumping = false;
    }
}
public class WallJumpState : BaseState
{
    float wallJumpTime = 0.35f;

    public WallJumpState( PlayerController controller, Inputs inputManager) : base(controller,inputManager,stateName: StateName.WALLJUMP)
    {
        Debug.Log("WallJumpState 생성");
    }


    public override void OnEnterState()
    {
        controller.isWallJumping = true;
    }

    public override void OnUpdateState()
    {
        if (controller.wallJumpCounter > 0)  // isWallJump-ing
        {
            controller._controller.Move(controller.wallJumpVector.normalized * (Time.deltaTime * 15.0f) +
                             new Vector3(0.0f, controller._verticalVelocity, 0.0f) * Time.deltaTime);
        } 

        if (controller.wallJumpCounter > 0) controller.wallJumpCounter -= Time.deltaTime;


        if(controller.wallJumpCounter <= 0){
            controller.stateMachine.ChangeState(StateName.WALK);
        }

        // State 변경
        // 월점프state -> (공중이면)점프state -> 

        if (inputManager.dash)
        {
            controller.wallJumpCounter = 0f;
            controller.stateMachine.ChangeState(StateName.DASH);

        }
        
        
    }


    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {
        controller.isWallJumping = false;
    }
}
public class BackflipState : BaseState
{
    readonly float backflipTime = .5f;

    public BackflipState( PlayerController controller, Inputs inputManager) : base(controller,inputManager,stateName: StateName.BACKFLIP)
    {
        Debug.Log("BackflipState 생성");
    }

    public override void OnEnterState()
    {
        controller.isBackflip = true;

        controller.wallJumpCounter = 0f; // canWallJump = false;
        controller.isBackflip = true;        // TODO: playerState = backflip
   
        controller._animator.SetTrigger("Backflip");

        Timer.CreateTimer(controller.gameObject, backflipTime, BackFlipTimer);
    }

    public override void OnUpdateState()
    {
        controller._verticalVelocity = Mathf.Sqrt(controller.JumpHeight * controller.Gravity * .2f);
    }

    void BackFlipTimer()
    {
        controller.stateMachine.ChangeState(StateName.WALK); // to idle
    }


    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {
        controller.isBackflipDown = true;
        controller._animator.SetTrigger("GoToIdle");

        controller.isBackflip = false;
    }
}
public class AttackState : BaseState
{
    public AttackState( PlayerController controller, Inputs inputManager) : base(controller,inputManager,stateName: StateName.ATTACK)
    {
        Debug.Log("AttackState 생성");
    }

    public override void OnEnterState()
    {
        //카메라가 보는 방향으로 변경
        Quaternion newRotation = controller._mainCamera.transform.rotation;
        newRotation.x = 0.0f;
        newRotation.z = 0.0f;
        controller.transform.rotation = newRotation;
        controller._targetRotation = Mathf.Atan2(newRotation.x, newRotation.z) * Mathf.Rad2Deg + controller._mainCamera.transform.eulerAngles.y;

        controller.isAttackGrounded = false;
        controller.isAttack = true;
        controller.wallJumpCounter = 0f; //canWallJump = false;
        controller.lastClickedTime = Time.time;
        controller.nextFireTime = controller.lastClickedTime + 0.5f;
        controller.comboCount++;
        controller.ComboRecentlyChangedTimer = .2f;
        
        if (controller.comboCount == 1)
        {
            controller.CreateParticle(180.0f);
            controller._animator.SetTrigger("AttackTrigger1");
            Timer.CreateTimer(controller.gameObject, .5f, ComboTimer);
        }
        else if (controller.comboCount == 2 && controller._animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
            controller._animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
        {
            controller.CreateParticle(45.0f);
            controller._animator.SetTrigger("AttackTrigger2");
            Timer.CreateTimer(controller.gameObject, .5f, ComboTimer);
        }
        else if (controller.comboCount == 3 && controller._animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
            controller._animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"))
        {
            controller.CreateParticle(110.0f);
            controller._animator.SetTrigger("AttackTrigger3");
            Timer.CreateTimer(controller.gameObject, .5f, ComboTimer);
        }
        
    }

    void ComboTimer()
    {
        controller.stateMachine.ChangeState(StateName.WALK); // IDLE
    }
    
    public override void OnUpdateState()
    {}

    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {}
}

public class LadderState : BaseState
{
    public LadderState( PlayerController controller, Inputs inputManager) : base(controller,inputManager,stateName: StateName.LADDER)
    {
        Debug.Log("LadderState 생성");
    }

    public override void OnEnterState()
    {

        controller._speed = controller.MoveSpeed;
    }

    public override void OnUpdateState()
    {

        controller._verticalVelocity = 0; // 사다리에서 내려가거나 점프할 때, 수직 가속이 높아지는 것을 막음

        if (inputManager.move != Vector2.zero)
        {
            Vector3 value = inputManager.move * Time.deltaTime * controller._speed;
            controller._controller.Move(value);
        }

        if( !controller.OnLadder )
        {
            controller.stateMachine.ChangeState(StateName.WALK); // IDLE
        }

        if (inputManager.jump) {
            controller.stateMachine.ChangeState(StateName.JUMP);
        }
    }


    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {

    }
}
