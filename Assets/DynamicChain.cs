using System.Collections.Generic;
using UnityEngine;

public class DynamicChain : MonoBehaviour
{
    public IKChain ikChain;
    public List<Transform> chainTransforms = new List<Transform>();

    [Header("Torque settings")]
    public float torqueStrength = 5f;   // proportional gain
    public float dampingStrength = 1f;  // angular velocity damping

    // Cached rigidbodies, populated once links are ready
    List<Rigidbody> rbs = new List<Rigidbody>();

    public bool initialized = false;

    public void HydrateRigidbodyList()
    {
        for (int i = 0; i < chainTransforms.Count; i++)
        {
            rbs.Add(chainTransforms[i].GetComponent<Rigidbody>());
        }
        initialized = true;
    }

    void FixedUpdate()
    {
        if (initialized)
            for (int i = 0; i < ikChain.links.Count; i++)
            {
                Rigidbody rb = rbs[i];
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