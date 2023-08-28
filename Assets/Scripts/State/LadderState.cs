using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LadderState : BaseState
{
    public LadderState( PlayerController pController, Inputs inputManager) : base(pController,inputManager,stateName: StateName.LADDER)
    {
        Debug.Log("LadderState 생성");
    }

    public override void OnEnterState()
    {
        pController._speed = pController.MoveSpeed;
    }

    public override void OnUpdateState()
    {

        pController._verticalVelocity = 0; // 사다리에서 내려가거나 점프할 때, 수직 가속이 높아지는 것을 막음

        if (inputManager.move != Vector2.zero)
        {
            Vector3 value = inputManager.move * Time.deltaTime * pController._speed;
            pController._controller.Move(value);
        }

        if( !pController.OnLadder )
        {
            pController.stateMachine.ChangeState(StateName.WALK); // IDLE
        }

        if (inputManager.jump) {
            pController.stateMachine.ChangeState(StateName.JUMP);
        }
    }


    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {}
}