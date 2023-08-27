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
    public Inputs inputManager;
    [Header("Player")]
    public float SlowWalkSpeed = 1.0f;
    public float MoveSpeed     = 7.0f;
    public float SprintSpeed   = 10.0f;

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
    private float _speed;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;
    
    //player wall jump
    public bool isWalled = false;
    public bool isWallSliding = false;
    public float wallJumpCounter;
    private float wallJumpTime = 0.35f;
    private Vector3 wallJumpVector;
    
    private PlayerInput _playerInput;
    public CharacterController _controller;
    private Inputs _input;
    private GameObject _mainCamera;
    public Animator _animator;
    public TrailRenderer[] Tyremarks;
    [SerializeField] private GameObject dashParticle = default;
    [SerializeField] private GameObject slashParticle = default;
    [SerializeField] private GameObject boxParticle = default;
    
    private const float _threshold = 0.01f;
    private float dontMoveRotationTimer = 0f;

    public float coyoteTimer = 0f;
    public bool canJumpBuffer = false;
    public float warpTimer = 0f;
    
    #region 사다리
    [Tooltip("사다리 콜라이더와 접촉 시 true")]
    [SerializeField] private bool _touchLadder;
    
    [Tooltip("사다리 매달린 상태 여부")]
    [SerializeField] private bool _onladder;

    public bool OnLadder
    {
        get => _onladder;
        set
        {
            _onladder = value;
            //_lastTouchObject.GetComponent<Ladder>().Attach = _onladder;
        }
    }

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
    [SerializeField] bool  isDashing;
    [SerializeField] bool  isDashTetany;
    [SerializeField] bool  isDashCool;

    [SerializeField] int   dashCounter = 1;
    
    [SerializeField] float dashPower;
    [SerializeField] float dashForwardRollTime; // 대시 앞구르기 모션 시간.
    [SerializeField] float dashTetanyTime;      // 대시 후, 경직시간 
    [SerializeField] float dashCoolTime;

    private WaitForSeconds DASH_FORWARD_ROLL_TIME;
    private WaitForSeconds DASH_TETANY_TIME;
    #endregion
    
    #region Backflip
    [Header("Backflip")]
    [SerializeField] bool isBackflip;
    [SerializeField] bool isBackflipDown;

    private float backflipTime = .5f;
    private float canSuperJumpTimer = 0f;
    
    #endregion
    
    #region Attack

    [Header("Attack")] 
    [SerializeField] private bool isAttack;
    public int comboCount;
    [SerializeField] private float lastClickedTime = 0f;
    [SerializeField] private float maxComboDelay = 1.0f;
    public float nextFireTime = 0f;
    private float ComboRecentlyChangedTimer = 0f;
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
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        _controller  = GetComponent<CharacterController>();
        _input       = GetComponent<Inputs>();
        _playerInput = GetComponent<PlayerInput>();
        

        DASH_FORWARD_ROLL_TIME = new WaitForSeconds(dashForwardRollTime);
        DASH_TETANY_TIME       = new WaitForSeconds(dashTetanyTime);

        isSliding = false;
    }

    private void Update()
    {
        SetGravity();
        if (canSuperJumpTimer > 0) canSuperJumpTimer -= Time.deltaTime;
        if (wallJumpCounter > 0)  // isWallJump-ing
        {
            _controller.Move(wallJumpVector.normalized * (Time.deltaTime * 15.0f) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        } 
        else if (isBackflipDown)
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
        else if (warpTimer <= 0f)
        {
            Move();
        }
        if (warpTimer > 0f) warpTimer -= Time.deltaTime;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, 1f, LayerMask.GetMask("WallLayer")))
        {
            isWalled = true;
        }
        else
        {
            isWalled = false;
        }
        
        if (isWalled && !_controller.isGrounded && _verticalVelocity < 0.0f)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
        if (wallJumpCounter > 0) wallJumpCounter -= Time.deltaTime;
        if (isWallSliding)
        {
            _verticalVelocity = -2f;
        }

        if (Time.time - lastClickedTime > maxComboDelay && isAttack)
        {
            isAttack = false;
            comboCount = 0;
            _animator.SetTrigger("GoToIdle");
        }

        if (_controller.isGrounded) isAttackGrounded = true;
        
        ComboRecentlyChangedTimer -= Time.deltaTime;
        if (dontMoveRotationTimer > 0f) dontMoveRotationTimer -= Time.deltaTime;
        CheckEmit();
        HandlingCoyoteTime();
    }
    private void LateUpdate()
    {
        CameraRotation();
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

    private void Move()
    {
        // set target speed based on move speed, sprint speed and if sprint is pressed
        float targetSpeed = MoveSpeed;

        if( _input.slowWalk ){ targetSpeed = SlowWalkSpeed; }
        if( _input.sprint   ){ targetSpeed = SprintSpeed;   } // 같이 누르면 sprint speed


        if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        // accelerate or decelerate to target speed
        if (!OnLadder && 
            (currentHorizontalSpeed < targetSpeed - speedOffset ||
             currentHorizontalSpeed > targetSpeed + speedOffset))
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }
        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        if (_input.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation  = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);

            // 카메라 방향으로 플레이어 회전
            if (dontMoveRotationTimer <= 0) transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }

        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;


        MoveSliding(); 
        
        // move the player
        if (!CheckLadder())
        {
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }
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
    
    private void SetGravity()
    {
        if (_controller.isGrounded)
        {
            // isGrounded일때 무한히 속도 낮춰지는 것을 방지
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            if(dashCounter == 0){
                dashCounter = 1;  // 대쉬는 공중에서 한번만 가능. 땅에 닿은 후에 충전됨. 최대충전횟수 1회.
            }

        }
        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
        
    }
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (inputManager.jump && isWallSliding)
        {
            if (!_controller.isGrounded && hit.normal.y < 0.1f) 
            {
                Debug.DrawRay(hit.point, hit.normal, Color.red, 1.25f);
                wallJumpCounter = wallJumpTime;
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
    }

    IEnumerator TurnOnBackflipDown()
    {
        yield return new WaitForSeconds(.2f);
        isBackflipDown = true;
    }
    
    public void HandlingJump()
    {
    	if(OnLadder)
        {
            OnLadder = false;
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }
        else if (_controller.isGrounded || (coyoteTimer > 0f && _controller.velocity.y < 0.0f))
        {
            #region Jump

            float multiplyValue = (canSuperJumpTimer > 0) ? 2f : 1f;
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity * multiplyValue);
            
            #endregion
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
        bool isAvailableDash = !isDashing && !isDashTetany && !isDashCool && (dashCounter > 0) && !isAttack &&
                               isAttackGrounded;

        if(isAvailableDash)
        {
            wallJumpCounter = 0f;  // wall jump cancel
            
            //create particle
            GameObject particle = Instantiate(dashParticle, transform.position, _mainCamera.transform.rotation);
            particle.transform.parent = _mainCamera.transform;
            ParticleSystem particlesys = particle.GetComponent<ParticleSystem>();
            particlesys.Play();
            
            StartCoroutine(DashCo());
        }
    }

    IEnumerator DashCo()
    {
        dashCounter = 0;
        isDashing = true;
        Vector3 dashDirection = (transform.forward).normalized; // TODO 계산 필요. 경사면 등

        // 최소한의 대시거리 + 현재이동거리에 비례한 추가거리
        float minimumDash = dashPower * Time.deltaTime;
        float addDash     = _speed    * Time.deltaTime;

        Vector3 verticalDash = new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime;
        
        _controller.Move( dashDirection * (minimumDash + addDash) + verticalDash);
        

        yield return DASH_FORWARD_ROLL_TIME; // 앞구르기 모션 시간
        isDashing = false;
        
        isDashTetany = true;
        yield return DASH_TETANY_TIME; // 대시 후 경직 시간
        isDashTetany = false;
        // TODO: playerState = move

        isDashCool = true;
        StartCoroutine(DashCoolTimeCO());
    }

    IEnumerator DashCoolTimeCO()
    {
        float timer = 0;
        while(timer < dashCoolTime)
        {
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        isDashCool = false;
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
            isBackflip = true;        // TODO: playerState = backflip
            _verticalVelocity = Mathf.Sqrt(JumpHeight * Gravity * .2f);
            StartCoroutine(BackflipCO());
        }
    }
    
    IEnumerator BackflipCO()
    {
        _animator.SetTrigger("Backflip");
        yield return new WaitForSeconds(backflipTime);
        _animator.SetTrigger("GoToIdle");
        isBackflip = false;
        isBackflipDown = true;
    }
    
    #endregion
    
    #region Attack

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
    
    
    public void Attack()
    {
        bool canAttack =
            (!isWalled && !isWallSliding && !isBackflip && !isBackflipDown);

        if(canAttack)
        {
            
            //카메라가 보는 방향으로 변경
            Quaternion newRotation = _mainCamera.transform.rotation;
            newRotation.x = 0.0f;
            newRotation.z = 0.0f;
            transform.rotation = newRotation;
            _targetRotation = Mathf.Atan2(newRotation.x, newRotation.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            
            isAttackGrounded = false;
            isAttack = true;
            wallJumpCounter = 0f;
            lastClickedTime = Time.time;
            nextFireTime = lastClickedTime + 0.5f;
            comboCount++;
            ComboRecentlyChangedTimer = .2f;
            
            if (comboCount == 1)
            {
                //ComboRecentlyChangedTimer = .3f;
                CreateParticle(180.0f);
                _animator.SetTrigger("AttackTrigger1");
            }
            else if (comboCount == 2 && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
                _animator.GetCurrentAnimatorStateInfo(0).IsName("Attack1"))
            {
                //ComboRecentlyChangedTimer = .4f;
                CreateParticle(45.0f);
                _animator.SetTrigger("AttackTrigger2");
            }
            else if (comboCount == 3 && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.4f &&
                _animator.GetCurrentAnimatorStateInfo(0).IsName("Attack2"))
            {
                //ComboRecentlyChangedTimer = .53f;
                CreateParticle(110.0f);
                _animator.SetTrigger("AttackTrigger3");
            }
        }
    }
    
    #endregion
    
    
    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle >  360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    /// <summary>
    /// 사다리 액션 체크 및 수행
    /// 사다리 액션 수행 시, 기본 Move 를 수행하지 않음
    /// </summary>
    /// <returns>사다리 액션 여부 반환</returns>
    private bool CheckLadder()
    {
        if (_touchLadder)
        {
            // 사다리에 붙고
            if (_input.move == Vector2.up)
            {
                // [TODO] 사다리를 바라봐야한다면, 바라보는 대상 카메라 -> 사다리 변경 필요
                OnLadder = true;
            }
            
            // 붙었으면 이동
            if (OnLadder)
            {
                _verticalVelocity = 0; // 사다리에서 내려가거나 점프할 때, 수직 가속이 높아지는 것을 막음
                if (_input.move != default)
                {
                    Vector3 value = _input.move * Time.deltaTime * _speed;
                    //transform.Translate(value);
                    _controller.Move(value);
                }
            }
        }

        return OnLadder;
    }
    

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Ladder")
        {
            _touchLadder = true;
            _lastTouchObject = other.gameObject;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Ladder")
        {
            _touchLadder = false;
            OnLadder = false;
        }
    }

    #region SkidMark

    private void CheckEmit()
    {
        if ((isDashing) || _speed == SprintSpeed)
        {
            startEmmiter();
        }
        else
        {
            stopEmmiter();
        }
    }
    
    private void startEmmiter()
    {
        foreach (TrailRenderer T in Tyremarks)
        {
            T.emitting = true;
        }
    }
    
    private void stopEmmiter()
    {
        foreach (TrailRenderer T in Tyremarks)
        {
            T.emitting = false;
        }
    }

    #endregion
    
    
    #region ResetCamera
    
    public void ResetCamera()
    {
        Vector3 playerDirection = transform.forward;
        float targetYaw = Mathf.Atan2(playerDirection.x, playerDirection.z) * Mathf.Rad2Deg;
        _cinemachineTargetYaw = targetYaw;
        _cinemachineTargetPitch = 0.0f;
        dontMoveRotationTimer = .2f;
    }
    
    #endregion
}
