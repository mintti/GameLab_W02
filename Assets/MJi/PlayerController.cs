using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MJi
{
    public class PlayerController : MonoBehaviour
    {
        Camera mainCamera;

        [SerializeField] Transform playerObj;
        [SerializeField] Camera camera;
        
        Rigidbody rigidbody;
        
        [Header("Default")]
        [SerializeField] float moveSpeed;
        [SerializeField] float turnSpeed;
        Vector3 moveDirection;
        
        [Header("Running")] 
        [SerializeField] float acceleration;
        [SerializeField] float maxSpeed;
        [SerializeField] float deceleration;
         
        [Header("Jumping")]
        [SerializeField] float jumpHeight;
        [SerializeField] float gravity;
        [SerializeField] float duration;


        [Header("Assists")] 
        [SerializeField] float _coyoteTime;


        public void Start()
        {
            mainCamera = Camera.main;
            rigidbody = GetComponent<Rigidbody>();
        }

        public void FixedUpdate()
        {
            if (moveDirection != Vector3.zero)
            {
                MoveThePlayer3();
                TurnThePlayer();
            }

            Physics.gravity = new Vector3(0, -gravity, 0);
        }

        #region Input System
        public void OnMove(InputAction.CallbackContext value)
        {
            Vector3 input = value.ReadValue<Vector3>();

            if (input != null)
            {
                Debug.Log(input);
                moveDirection = new Vector3(input.x, input.y, input.z);
            }
        }

        public void OnJump(InputAction.CallbackContext value)
        {
            if (value.started)
            {
                rigidbody.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            }
        }
        #endregion


        /// <summary>
        /// 월드 좌표 기준으로 이동
        /// 즉시 회전, 즉시 이동
        /// </summary>
        private void MoveThePlayer()
        {
            //transform.rotation = Quaternion.LookRotation(moveDirection);
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 카메라 방향을 기준으로 이동
        /// </summary>
        void MoveThePlayer2()
        {
            Vector3 movement = CameraDirection(moveDirection) * moveSpeed * Time.deltaTime;
            
            // 1: 이동
            rigidbody.MovePosition(transform.position + movement);
            
            // 2: 부드러운 이동
            // Vector3 smoothedDelta = Vector3.MoveTowards(transform.position, movement, Time.fixedDeltaTime * moveSpeed);
            // rigidbody.MovePosition(transform.position + smoothedDelta);
        }

        public void MoveThePlayer3()
        {
            rigidbody.AddForce(CameraDirection(moveDirection).normalized * moveSpeed * Time.deltaTime, ForceMode.Force);
        }
    
        void TurnThePlayer()
        {
            if(moveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion rotation = Quaternion.Slerp(rigidbody.rotation,
                    Quaternion.LookRotation (CameraDirection(moveDirection)),
                    turnSpeed);
            
               playerObj.rotation = rotation;
            }
        }
        
        Vector3 CameraDirection(Vector3 movementDirection)
        {
            var cameraForward = mainCamera.transform.forward;
            var cameraRight = mainCamera.transform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
        
            return cameraForward * movementDirection.z + cameraRight * movementDirection.x; 
   
        }
    }
}
