using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public Inputs inputManager;
    [Header("Player")]
    public float MoveSpeed = 7.0f;
    public float SprintSpeed = 10.0f;
    
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;
   
    public float JumpHeight = 1.2f;
    public float Gravity = -15.0f;

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
    private CharacterController _controller;
    private Inputs _input;
    private GameObject _mainCamera;
    
    private const float _threshold = 0.01f;

    /// <summary>
    /// 사다리 콜라이더와 접촉 시 true
    /// </summary>
    private bool _touchLadder;
    
    /// <summary>
    /// 사다리 상태
    /// </summary>
    private bool _onladder;

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
    
#region Dash
    [Header("Dash")]
    [SerializeField] bool  isDashing;
    [SerializeField] bool  isDashTetany;
    [SerializeField] bool  isDashCool;   
    
    [SerializeField] float dashPower;
    [SerializeField] float dashForwardRollTime; // 대시 앞구르기 모션 시간.
    [SerializeField] float dashTetanyTime;      // 대시 후, 경직시간 
    [SerializeField] float dashCoolTime;

    private WaitForSeconds DASH_FORWARD_ROLL_TIME;
    private WaitForSeconds DASH_TETANY_TIME;
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
        rb = GetComponent<Rigidbody>();
        

        DASH_FORWARD_ROLL_TIME = new WaitForSeconds(dashForwardRollTime);
        DASH_TETANY_TIME       = new WaitForSeconds(dashTetanyTime);
    }

    private void Update()
    {
        SetGravity();
        if (wallJumpCounter > 0)
        {
            _controller.Move(wallJumpVector.normalized * (Time.deltaTime * 15.0f) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }
        else
        {
            Move();
        }
        
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
        float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
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

            // rotate to face input direction relative to camera position
            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;
        
        // move the player
        if (!CheckLadder())
        {
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
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
            }
        }
    }

    public void HandlingJump()
    {
    	if(OnLadder)
        {
            OnLadder = false;
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
        }
        else if (_controller.isGrounded)
        {
            #region Jump
            
            _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            
            #endregion
        }
    }

    public void Dash()
    {
        bool isAvailableDash = (!isDashing && !isDashTetany && !isDashCool );

        if(isAvailableDash)
        {
            isDashing = true;        // TODO: playerState = dash 
            StartCoroutine(DashCo());
        }
    }

    IEnumerator DashCo()
    {
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

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle >  360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

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
                _verticalVelocity = 0;
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
}
