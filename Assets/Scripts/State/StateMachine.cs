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
        Debug.Log("Create StateMachine");
    }

    public void AddState(StateName stateName, BaseState state)
    {
        //Debug.Log("AddState :" + stateName);

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
        //Debug.Log("ChangeState from (" + CurrentState.stateName + ")  to (" + nextStateName + ")");

        UIManager.Instance.UpdateKeyInfo(nextStateName);
        
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