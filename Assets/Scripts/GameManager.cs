using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    // public PlayerController playerController;

    // StateContext stateContext; // 이게 맞나??

    // private IState _idleState, _walkState, _jumpState;
    // private IState _dashState;

    // void Start()
    // {
    //     stateContext = new StateContext(playerController);

    //     _idleState = gameObject.AddComponent<IdleState>();
    //     _idleState = gameObject.AddComponent<WalkState>();
    //     _jumpState = gameObject.AddComponent<JumpState>();
    //     _dashState = gameObject.AddComponent<DashState>();

    //     stateContext.Transition(_idleState);
    // }

    // void Update()
    // {
        
    // }




    // public void IdleS()
    // {
    //     stateContext.Transition(_idleState);
    // }
    // public void JumpS()
    // {
    //     stateContext.Transition(_jumpState);
    // }

    // public void WalkS()
    // {
    //     stateContext.Transition(_walkState);
    // }
    // public void DashS(PlayerController playerController_)
    // {
    //     stateContext.Transition(_dashState);
    // }

    // public void DashS()
    // {
    //     stateContext.Transition();
    // }
}
