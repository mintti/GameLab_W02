using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class AttackState : BaseState
{
    public AttackState( PlayerController pController, Inputs inputManager) : base(pController,inputManager,stateName: StateName.ATTACK)
    {
        Debug.Log("AttackState 생성");
    }

    public override void OnEnterState()
    {
        //카메라가 보는 방향으로 변경
        Quaternion newRotation = pController._mainCamera.transform.rotation;
        newRotation.x = 0.0f;
        newRotation.z = 0.0f;
        pController.transform.rotation = newRotation;
        pController._targetRotation    = Mathf.Atan2(newRotation.x, newRotation.z) * Mathf.Rad2Deg + pController._mainCamera.transform.eulerAngles.y;

        pController.isAttackGrounded = false;
        pController.isAttack         = true;
        pController.wallJumpCounter  = 0f; //canWallJump = false;
        pController.lastClickedTime  = Time.time;
        pController.nextFireTime     = pController.lastClickedTime + 0.5f;
        pController.comboCount++;
        pController.ComboRecentlyChangedTimer = .2f;
        
        if (pController.comboCount == 1)
        {
            pController.CreateParticle(180.0f);
            pController.CreateParticleCollider();
            pController._animator.SetTrigger("AttackTrigger1");
            Timer.CreateTimer(pController.gameObject, .5f, ComboTimer);
        }
        else if (pController.comboCount == 2 && pController._animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
            pController._animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
        {
            pController.CreateParticle(45.0f);
            pController.CreateParticleCollider();
            pController._animator.SetTrigger("AttackTrigger2");
            Timer.CreateTimer(pController.gameObject, .5f, ComboTimer);
        }
        else if (pController.comboCount == 3 && pController._animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
            pController._animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"))
        {
            pController.CreateParticle(110.0f);
            pController.CreateParticleCollider();
            pController._animator.SetTrigger("AttackTrigger3");
            Timer.CreateTimer(pController.gameObject, .5f, ComboTimer);
        }
        
    }

    void ComboTimer()
    {
        pController.stateMachine.ChangeState(StateName.WALK); // IDLE
    }
    
    public override void OnUpdateState()
    {}

    public override void OnFixedUpdateState()
    {}

    public override void OnExitState()
    {}
}