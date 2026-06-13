using System.Collections.Generic;
using UnityEngine;

public class DynamicChainManual : MonoBehaviour
{
    public IKChain ikChain;
    public List<Rigidbody> dynamicChainLinks = new List<Rigidbody>();

    [Header("Torque settings")]
    public float torqueStrength = 50f;   // proportional gain
    public float dampingStrength = 1f;  // angular velocity damping

    private void Update()
    {
        for (int i = 0; i < ikChain.links.Count; i++)
        {
            Rigidbody rb = dynamicChainLinks[i];
            Quaternion rawTarget = ikChain.links[i].transform.rotation;

            // Extract forward from the IK target and rebuild rotation using
            // only yaw and pitch, with world up as the reference.
            // This completely ignores Z/roll, derived purely from the forward vector.
            Vector3 forward = rawTarget * Vector3.forward;
            Quaternion target;

            Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, forward);
            if (projectedUp.sqrMagnitude > 0.001f)
                target = Quaternion.LookRotation(forward, projectedUp.normalized);
            else
                target = Quaternion.LookRotation(forward, Vector3.forward);

            Quaternion current = rb.rotation;

            if (Quaternion.Dot(target, current) < 0f)
                target = new Quaternion(-target.x, -target.y, -target.z, -target.w);

            Quaternion error = target * Quaternion.Inverse(current);
            error.ToAngleAxis(out float angleDeg, out Vector3 axis);

            if (angleDeg > 180f) angleDeg -= 360f;

            if (axis.sqrMagnitude > 0.001f)
            {
                Vector3 torque = axis.normalized * (angleDeg * Mathf.Deg2Rad * torqueStrength);
                torque -= rb.angularVelocity * dampingStrength;
                rb.AddTorque(torque, ForceMode.Force);
            }
        }
    }



}
