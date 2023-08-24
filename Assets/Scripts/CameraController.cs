using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{

    private Camera mainCamera;
    public  Transform focus; // player = GameObject.FindGameObjectWithTag("Player").transform;

    private Vector3 zOffset     = new Vector3(0f, 0f, -5f);


    public float sensX;
    public float sensY;

    public Transform orientation;

    float xRotation;
    float yRotation;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
    }


    void LateUpdate()
    {
        transform.position = focus.position + zOffset;

        
    }

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
     
    }

    private void Update() {
        
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation += mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // rotate cam and orientation
        transform.rotation   = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

    }


}
