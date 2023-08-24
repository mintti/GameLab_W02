using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MJi
{
    public class CameraController : MonoBehaviour
    {
        public Transform target;                                           // Player's reference.
        public Vector3 pivotOffset = new Vector3(0.0f, 1.7f,  0.0f);       // Offset to repoint the camera.
        public Vector3 camOffset   = new Vector3(0.0f, 0.0f, -3.0f);
        
        // Speed of camera responsiveness.
        public float horizontalAimingSpeed = 6f;                           // Horizontal turn speed.
        public float verticalAimingSpeed = 6f;                                // This transform.
        public float targetMaxVerticalAngle = 30f;                               // Camera max clamp angle. 
        public float minVerticalAngle = -60f;     
        
        private Vector3 offset;
        private float rotationX = 0.0f;
        private float inputH;
        private float inputV;

        private float angleH;
        private float angleV;
        
        private void Start()
        {
            offset = transform.position - target.position;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            angleH = target.eulerAngles.y;
                
            var cam = transform;
            cam.rotation = Quaternion.identity;
            cam.position = target.position + Quaternion.identity * pivotOffset + Quaternion.identity * camOffset;
        }


        private void Update()
        {
            angleH += Mathf.Clamp(inputH, -1, 1) * horizontalAimingSpeed;
            angleV += Mathf.Clamp(inputV, -1, 1) * verticalAimingSpeed;
            
            angleV = Mathf.Clamp(angleV, minVerticalAngle, targetMaxVerticalAngle);

            var cam = transform;
                
            Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);

            Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
            cam.rotation = aimRotation;
            cam.position = target.position + camYRotation * pivotOffset + aimRotation * camOffset;
            
        }

        // private void Update()
        // {
        //     // Calculate the desired camera position based on the target's position
        //     Vector3 desiredPosition = target.position + offset;
        //
        //     // Smoothly move the camera towards the desired position
        //     transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
        //
        //     // Get mouse input for rotation
        //
        //     // Rotate the camera around the target horizontally
        //     rotationX += mouseX * rotationSpeed;
        //     Quaternion rotation = Quaternion.Euler(60, rotationX, 0);
        //
        //     // Apply the rotation to the camera
        //     transform.rotation = rotation;
        // }

        public void MoveMouse(InputAction.CallbackContext context)
        {
            var vector = context.ReadValue<Vector2>();
            
            inputH = vector.x;
            inputV = vector.y;
            
            
            Debug.Log(vector);
        }
    }
}
