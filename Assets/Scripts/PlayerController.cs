using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{

    public StateMachine stateMachine {get; private set;}

    public Inputs inputManager;
    [Header("Player")]

    public float MoveSpeed     = 7.0f;
    float SprintSpeed   = 10.0f;

    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;
   
    public float JumpHeight = 1.2f;
    public float Gravity = -80.0f;
    public float MaxGravity = -80.0f;

    [Header("Cinemachine")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;
    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    public float _speed;
    public float _targetRotation = 0.0f;
    public float _rotationVelocity;
    public float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    
    // jump
    public bool isJumping = false;

    //player wall jump
    public bool isWalled = false;
    public bool isWallSliding = false;
    //public bool canWallJump   = false;
    public float wallJumpTime = 0.35f;

    public float wallJumpCounter;

    public bool isWallJumping = false;
    public Vector3 wallJumpVector;
    
    private PlayerInput _playerInput;
    public CharacterController _controller;
    private Inputs _input;
    public GameObject _mainCamera;
    public Animator _animator;
    public TrailRenderer[] Tyremarks;
    [SerializeField] private GameObject dashParticle = default;
    [SerializeField] private GameObject slashParticle = default;
    [SerializeField] private GameObject slashCollider = default;
    [SerializeField] private GameObject boxParticle = default;
    
    private const float _threshold = 0.01f;
    private float dontMoveRotationTimer = 0f;

    public float coyoteTimer = 0f;
    public bool canJumpBuffer = false;
    public float warpTimer = 0f;
    
    #region 사다리
    [Tooltip("사다리 콜라이더와 접촉 시 true")]
    [SerializeField] public bool _touchLadder;

    public bool isExitLadder;
    
    [Tooltip("사다리 매달린 상태 여부")]
    [SerializeField] public bool OnLadder = false;

    public Transform DefaultTarget { get; private set; }

    public GameObject _lastTouchObject;
    #endregion

    #region Sliding
    [Header("Sliding")]
    [SerializeField] bool isSliding = false;            // 슬라이딩 여부
    [SerializeField] float slideMinAngle = 10;          // 슬라이딩 동작하는 최소 각도
    [SerializeField] float slideMaxAngle = 50;          // 슬라이딩 지원하는 최대 각도
    [SerializeField] float slidingSpeed ;               // 슬라이딩 기본 속도
    [SerializeField] private Vector3 _slideDirection;   // 슬라이딩 방향
    private float _slidingMultipleByAngle;              // 슬라이드 속도에 곱할 각도
    
    #endregion
    
    #region Dash
    [Header("Dash")]
    public bool  isDashing;
    public bool  isDashTetany;
    public bool  isDashCool;

    public int   dashCounter = 1;
    
    #endregion
    
    #region Backflip
    [Header("Backflip")]
    public bool isBackflip;
    public bool isBackflipDown;

    public float canSuperJumpTimer = 0f;
    
    #endregion
    
    #region Attack

    [Header("Attack")] 
    public bool isAttack;
    public int comboCount;
    public float lastClickedTime = 0f;
    public float maxComboDelay = 1.0f;
    public float nextFireTime = 0f;
    public float ComboRecentlyChangedTimer = 0f;
    public bool isAttackGrounded = true; //점프 어택 이후 내려찍기 기술 못쓰게 하기 위한 변수
    
    #endregion
    

    private bool IsCurrentDeviceMouse
    {
        get
        {
            return _playerInput.currentControlScheme == "KeyboardMouse";
        }
    }


    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        _controller  = GetComponent<CharacterController>();
        _input       = GetComponent<Inputs>();
        _playerInput = GetComponent<PlayerInput>();
        

        isSliding = false;

        InitStateMachine();
    }

    void FixedUpdate()
    {
        stateMachine.FixedUpdateState();
    }
    void LateUpdate()
    {
        CameraRotation();
    }
    void Update()
    {
        stateMachine.UpdateState();

        SetGravity();
        GroundCheck();

        if (warpTimer                 > 0) warpTimer                 -= Time.deltaTime;
        if (canSuperJumpTimer         > 0) canSuperJumpTimer         -= Time.deltaTime;
        if (ComboRecentlyChangedTimer > 0) ComboRecentlyChangedTimer -= Time.deltaTime;
        if (dontMoveRotationTimer     > 0) dontMoveRotationTimer     -= Time.deltaTime;


        if (isBackflipDown)
        {
            _controller.Move(Vector3.down * (Time.deltaTime * 50.0f));
            if (_controller.isGrounded)
            {
                canSuperJumpTimer = .5f;
                _verticalVelocity = -2.0f;
                _speed = MoveSpeed;
                isBackflipDown = false;
            }
        } else if (isAttack)
        {
            if (ComboRecentlyChangedTimer > 0 && ComboRecentlyChangedTimer < .2f)
            {
                _controller.Move(Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward.normalized * (Time.deltaTime * 10.0f) +
                                 Vector3.down *  Time.deltaTime);
                _verticalVelocity = -5.0f;
            }
            else
            {
                _controller.Move(Vector3.down * (2.0f * Time.deltaTime));
                _verticalVelocity = -10.0f;
            }
        }
        IfAttackClickLateGoIdle();     
        

        CheckIsWalled();
        CheckIsWallSliding();
        
        HandlingCoyoteTime();
    }

    void IfAttackClickLateGoIdle()
    {
        if( Time.time - lastClickedTime > maxComboDelay && isAttack )
        {
            isAttack = false;
            comboCount = 0;
            _animator.SetTrigger("GoToIdle");
        }
    }


    private void CheckIsWalled()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, LayerMask.GetMask("WallLayer"))){
            isWalled = true;
        }
        else{
            isWalled = false;
        }
    }

    private void CheckIsWallSliding()
    {
        if (isWalled && !_controller.isGrounded && _verticalVelocity < 0.0f){
            isWallSliding = true;
        }
        else{
            isWallSliding = false;
        }

        if (isWallSliding)
        {
            _verticalVelocity = -2f;
        }
    }



    private void CameraRotation()
    {
        // if there is an input and camera position is not fixed
        if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
        {
            //Don't multiply mouse input by Time.deltaTime;
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

            _cinemachineTargetYaw   += _input.look.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw   = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    private bool MoveSliding()
    {
        SetSlopeSlideVelocity();
        isSliding = !(_slideDirection == Vector3.zero);
        
        if (isSliding)
        {
            var veloc = _slideDirection * slidingSpeed * Time.deltaTime * _slidingMultipleByAngle;
            _controller.Move(veloc);
        }
        
        return isSliding;
    }

    private void SetSlopeSlideVelocity()
    {
        if(Physics.Raycast(transform.position , Vector3.down, out RaycastHit slopeHit, 2))
        {
            float angle = Vector3.Angle(slopeHit.normal, Vector3.up);
            if (angle >= slideMinAngle)
            {
                _slidingMultipleByAngle = angle <= slideMaxAngle ? (float)Math.Pow((angle / 60), 2f) : 2f ;
                _slideDirection = Vector3.ProjectOnPlane( Vector3.down, slopeHit.normal).normalized;
                return;
            }
        }

        _slideDirection = Vector3.zero;
    }
    
    private void GroundCheck()
    {
        if (_controller.isGrounded)
        {
            if( dashCounter == 0 ){
                dashCounter =  1;
                // 대쉬는 공중에서 한번만 가능. 땅에 닿은 후에 충전됨. 최대충전횟수 1회.
            }

            isAttackGrounded = true;
        }
    }

    private void SetGravity()
    {
        if (_controller.isGrounded)
        {
            // isGrounded일때 무한히 속도 낮춰지는 것을 방지
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }
        }
        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
        
    }
    
    public void Jump()
    {
        Debug.Log("");

        if (!isJumping && _controller.isGrounded || (coyoteTimer > 0f && _controller.velocity.y < 0.0f)){ // 땅바닥에서 점프
            stateMachine.ChangeState(StateName.JUMP);
        }else if(OnLadder) // 사다리에서 점프
        {
            stateMachine.ChangeState(StateName.JUMP);
        }
        else if (_controller.isGrounded || (coyoteTimer > 0f && _controller.velocity.y < 0.0f))
        {
        }
    }
    

    public void HandlingCoyoteTime()
    {
        if (_controller.isGrounded)
        {
            coyoteTimer = 0.2f;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.5f))
        {
            Debug.DrawRay(transform.position, Vector3.down * 0.5f, Color.green);
            canJumpBuffer = true;
        }
        else
        {
            canJumpBuffer = false;
        }
    }

    #region Dash
    public void Dash()
    {
        bool isAvailableDash = !isDashing && !isDashTetany && !isDashCool && (dashCounter > 0) &&
                                !isAttack && isAttackGrounded && 
                                !isBackflip && !isBackflipDown && 
                                !isWallJumping;
        if(isAvailableDash){
            wallJumpCounter = 0f;  // wall jump cancel
            CreateParticleDash();
            stateMachine.ChangeState(StateName.DASH);
        }else{
            //Debug.Log("isDashing(" +isDashing + ") isDashTetany("+isDashTetany+") " + " dashCounter("+ dashCounter+")");
        }   
    }

    void CreateParticleDash()
    {
        GameObject particle = Instantiate(dashParticle, transform.position, _mainCamera.transform.rotation);
        particle.transform.parent = _mainCamera.transform;
        ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
        particlesys.Play();
    }
    #endregion

    #region Backflip
    public void Backflip()
    {
        bool isAvailableBackflip =
            (!_controller.isGrounded && !isWalled && !isWallSliding && !isBackflip && !isBackflipDown && !isAttack && isAttackGrounded);

        if(isAvailableBackflip)
        {
            wallJumpCounter = 0f;
            stateMachine.ChangeState(StateName.BACKFLIP);

        }
    }
    IEnumerator TurnOnBackflipDown()
    {
        yield return new WaitForSeconds(.2f);
        isBackflipDown = true;
    }
    #endregion
    
    #region Attack
    public void Attack()
    {
	    if( Time.time > nextFireTime && comboCount < 3)
	    {
            bool canAttack = !isWalled && !isWallSliding && !isBackflip && !isBackflipDown;

            if(canAttack)
            {
                stateMachine.ChangeState(StateName.ATTACK);
            }
	    }
    }

    public void CreateParticle(float yAng)
    {
        //create particle
        GameObject particle = Instantiate(slashParticle, transform.position, transform.rotation);
        particle.transform.parent = gameObject.transform;
        ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
        float rad = (Mathf.PI / 180) * yAng;
        particlesys.startRotation3D = new Vector3(0.0f, rad, 0.0f);
        particlesys.Play();
    }
    public void CreateParticleCollider()
    {
        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        GameObject _slashCollider = Instantiate(slashCollider, transform.position + targetDirection * 1.5f, transform.rotation);
    }
    #endregion
    
    
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle >  360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (inputManager.jump && isWallSliding) // 이 코드의 정체는??
        { 
            if(OnLadder)
            {
                OnLadder = false;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            }
            else if (!_controller.isGrounded && hit.normal.y < 0.1f)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.red, 1.25f);
  
                stateMachine.ChangeState(StateName.WALLJUMP);
  
                wallJumpCounter = wallJumpTime;
                //canWallJump = true;
                wallJumpVector = hit.normal;
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                
                Vector3 newRotation = transform.eulerAngles + new Vector3(0f, 180f, 0f);
                transform.eulerAngles = newRotation;

            }
        }
        
        if (hit.collider.CompareTag("Box"))
        {
            if (hit.transform.position.y < transform.position.y && isBackflipDown == true)
            {
                //create particle
                GameObject particle = Instantiate(boxParticle, hit.transform.position, hit.transform.rotation);
                ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
                particlesys.Play();
                
                Destroy(hit.gameObject);
                StartCoroutine("TurnOnBackflipDown");
            }
        }
        
        if (hit.collider.CompareTag("Box"))
        {
            if (hit.transform.position.y < transform.position.y && isBackflipDown == true)
            {
                //create particle
                GameObject particle = Instantiate(boxParticle, hit.transform.position, hit.transform.rotation);
                ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
                particlesys.Play();
                
                Destroy(hit.gameObject);
                StartCoroutine("TurnOnBackflipDown");
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Ladder")
        {
            _touchLadder = true;
            stateMachine.ChangeState(StateName.LADDER);
            _lastTouchObject = other.gameObject;
        }

        if (other.tag == "ExitLadder")
        {
            isExitLadder = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ladder")
        {
            _touchLadder = false;
            OnLadder = false;
        }
        
        if (other.tag == "ExitLadder")
        {
            isExitLadder = false;
        }
    }


    public GameObject GetMainCamera()
    {
        if( _mainCamera == null){
            Debug.Log("_mainCamera is null");
            return null;
        }
        return _mainCamera;
    }


    private void InitStateMachine()
    {
        Debug.Log("InitStateMachine");

        stateMachine = new StateMachine();

        stateMachine.AddState(StateName.WALK,     new WalkState(this,inputManager));
        stateMachine.AddState(StateName.DASH,     new DashState(this,inputManager));
        stateMachine.AddState(StateName.JUMP,     new JumpState(this,inputManager));
        stateMachine.AddState(StateName.WALLJUMP, new WallJumpState(this,inputManager));
        stateMachine.AddState(StateName.BACKFLIP, new BackflipState(this,inputManager));
        stateMachine.AddState(StateName.ATTACK,   new AttackState(this,inputManager));
        stateMachine.AddState(StateName.LADDER,   new LadderState(this,inputManager));

    }

    public void ResetCamera()
    {
        Vector3 playerDirection = transform.forward;
        float targetYaw = Mathf.Atan2(playerDirection.x, playerDirection.z) * Mathf.Rad2Deg;
        _cinemachineTargetYaw = targetYaw;
        _cinemachineTargetPitch = 0.0f;
        dontMoveRotationTimer = .2f;
    }
}