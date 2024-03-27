using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    [SerializeField] private bool _flying;
    [SerializeField] private float _speed = 5;
    [SerializeField] private float _jumpSpeed = 200;
    [SerializeField] private float _sensitivity = 2f;
    [SerializeField] private int _minX = -80;
    [SerializeField] private int _maxX = 80;

    private Rigidbody _rb;
    private float _cameraVerticalRotation = 0;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        UpdateCamera();

        // Switch movement modes
        if (Input.GetButtonDown("SwitchMode")) _flying = !_flying;
    }

    private void UpdateCamera()
    {
        // Get the Mouse horizontal movement
        float mouseX = Input.GetAxis("Mouse X");
        Vector3 tempRotation = transform.eulerAngles;
        tempRotation.y += mouseX * _sensitivity;

        // Get the Mouse vertical movement
        float mouseY = Input.GetAxis("Mouse Y");
        _cameraVerticalRotation -= mouseY * _sensitivity;
        _cameraVerticalRotation = Mathf.Clamp(_cameraVerticalRotation, _minX, _maxX);
        tempRotation.x = _cameraVerticalRotation;

        transform.eulerAngles = tempRotation;   // Set the changed rotation
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_flying) FlyCam();
        else WalkCam();
    }

    private void FlyCam()
    {
       _rb.isKinematic = true;

        // Move the camera left and right
        float horizontal = Input.GetAxis("Horizontal");
        transform.position += transform.right.normalized * horizontal * Time.fixedDeltaTime * _speed * 2;

        // Move the camera forward and back
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = transform.forward;
        direction.y = 0;
        direction.Normalize();
        transform.position += direction * vertical * Time.fixedDeltaTime * _speed*2;

        // Move the camera up and down
        float upDown = Input.GetAxis("Jump");
        transform.position += Vector3.up * upDown * Time.fixedDeltaTime * _speed*2;
    }

    private bool _canJump = false;
    private void WalkCam()
    {
        _rb.isKinematic = false;

        // Move the camera forward, back, left and right
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = transform.forward * vertical;
        direction.y = 0;
        direction += transform.right.normalized * horizontal;
        direction.Normalize();
        _rb.MovePosition(transform.position + direction * Time.fixedDeltaTime * _speed);

        // Move the camera up and down
        if (_canJump)
        {
            float upDown = Input.GetAxis("Jump");
            if (upDown > 0)
            {
                _rb.AddForce(Vector3.up * _jumpSpeed);
                _canJump = false;
            }
        }
        
    }

    private void OnCollisionEnter(Collision other)
    {
        _canJump = true;
    }
}
