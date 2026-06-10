using UnityEngine;

public class DirectedTarget : MonoBehaviour
{
    public Transform Target;
    public float Speed = 5f;
    public float MaxSpeed = 10f;
    public float Acceleration = 20f;
    public float SlowRadius = 3f;     // start slowing down
    public float StopRadius = 0.2f;   // stop completely

    private Rigidbody rb;

    private void Awake()
    {
        rb = Target.GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 toTarget = transform.position - Target.position;
        float distance = toTarget.magnitude;

        if (distance < StopRadius)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 direction = toTarget / distance;

        // Scale speed based on distance (arrival behavior)
        float targetSpeed = Speed;

        if (distance < SlowRadius)
        {
            targetSpeed = Speed * (distance / SlowRadius);
        }

        Vector3 desiredVelocity = direction * targetSpeed;

        Vector3 velocityChange = desiredVelocity - rb.linearVelocity;

        Vector3 force = Vector3.ClampMagnitude(velocityChange * Acceleration, Acceleration);

        rb.AddForce(force, ForceMode.Acceleration);

        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, MaxSpeed);

        // Smooth rotation toward movement direction
        Vector3 vel = rb.linearVelocity;
        if (vel.sqrMagnitude > 0.005f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(vel);
            rb.MoveRotation(targetRotation);
        }
    }
}