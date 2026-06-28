using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    public Rigidbody rb;
    public float speed;

    [Header("Look")]
    public float mouseSensitivity = 3f;
    public float rollSpeed = 90f;
    public float rollCorrectionSpeed = 90f;

    [Header("Height Correction")]
    public LegSupportGravity legSupport; // optional, averages ground height across connected legs
    public float targetHeightAboveGround = 2.5f;
    public float heightRaycastMaxDistance = 5f;
    public LayerMask groundMask;
    public float heightSpring = 50f;
    public float heightDamper = 10f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleLook();
        HandleRoll();
    }

    void FixedUpdate()
    {
        rb.AddForce(GetInputDirHorizontal().normalized * speed);
        HandleHeightCorrection();
    }
    //Include a small correction towards avg leg connection points, to make it seem .
    /*
     Vector3 avg = legSupport.GetAverageGroundPoint(out int connected);
        if (connected == 0)
            return;
     
     */


    void HandleHeightCorrection()
    {
        if (!Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, heightRaycastMaxDistance, groundMask))
            return; // nothing below to correct against, leave gravity/swimming alone

        float currentHeightAboveGround = hit.distance;
        float error = targetHeightAboveGround - currentHeightAboveGround;

        float upVelocity = Vector3.Dot(rb.linearVelocity, transform.up);
        float correctiveForce = error * heightSpring - upVelocity * heightDamper;

        rb.AddForce(transform.up * correctiveForce, ForceMode.Acceleration);
    }

    public Vector3 GetInputDirHorizontal()
    {
        Vector3 returnDir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
            returnDir += transform.forward;
        if (Input.GetKey(KeyCode.A))
            returnDir -= transform.right;
        if (Input.GetKey(KeyCode.S))
            returnDir -= transform.forward;
        if (Input.GetKey(KeyCode.D))
            returnDir += transform.right;
        return returnDir;
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(-mouseY, mouseX, 0f, Space.Self);
    }

    void HandleRoll()
    {
        float rollInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rollInput += 1f;
        if (Input.GetKey(KeyCode.E)) rollInput -= 1f;

        if (rollInput != 0f)
            transform.Rotate(0f, 0f, rollInput * rollSpeed * Time.deltaTime, Space.Self);
        else
            CorrectRoll();
    }

    void CorrectRoll()
    {
        Vector3 levelUp = Vector3.ProjectOnPlane(Vector3.up, transform.forward);
        if (levelUp.sqrMagnitude < 0.0001f)
            return;

        float rollAngle = Vector3.SignedAngle(transform.up, levelUp, transform.forward);
        float step = Mathf.Clamp(rollAngle, -rollCorrectionSpeed * Time.deltaTime, rollCorrectionSpeed * Time.deltaTime);

        transform.Rotate(0f, 0f, step, Space.Self);
    }
}