using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyCam : MonoBehaviour
{
    [SerializeField] private float _speed = 25;
    [SerializeField] private float _sensitivity = 2f;
    [SerializeField] private int _minX = -80;
    [SerializeField] private int _maxX = 80;

    private float _cameraVerticalRotation = 0;

    // Update is called once per frame
    void Update()
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

        // Move the camera left and right
        float horizontal = Input.GetAxis("Horizontal");
        transform.position += transform.right.normalized * horizontal * Time.deltaTime * _speed;

        // Move the camera forward and back
        float vertical = Input.GetAxis("Vertical");
        Vector3 direction = transform.forward;
        direction.y = 0;
        direction.Normalize();
        transform.position += direction * vertical * Time.deltaTime * _speed;

        // Move the camera up and down
        float upDown = Input.GetAxis("Jump");
        transform.position += Vector3.up * upDown * Time.deltaTime * _speed;
    }
}
