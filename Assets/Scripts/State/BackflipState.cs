using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackflipState : BaseState
{
    public static readonly float backflipTime = .5f;

    public BackflipState( PlayerController pController, Inputs inputManager) : base(pController,inputManager,stateName: StateName.BACKFLIP)
    {
        //Debug.Log("BackflipState 생성");
    }

    public override void OnEnterState()
    {
        pController.isBackflip      = true;

        pController.wallJumpCounter = 0f;    // canWallJump = false;
        pController.isBackflip      = true;
   
        pController._animator.SetTrigger("Backflip");

        Timer.CreateTimer(pController.gameObject, backflipTime, BackFlipTimer);
    }

    public override void OnUpdateState()
    {
        pController._verticalVelocity = Mathf.Sqrt(pController.JumpHeight * pController.Gravity * .2f);
    }

    void BackFlipTimer()
    {
        pController.stateMachine.ChangeState(StateName.WALK); // to idle
    }


    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {
        pController.isBackflipDown = true;
        pController._animator.SetTrigger("GoToIdle");

        pController.isBackflip = false;
    }

    public override void HandleInputs()
    {
        
    }

}