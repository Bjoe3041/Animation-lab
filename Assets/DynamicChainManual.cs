using System.Collections.Generic;
using UnityEngine;

public class DynamicChainManual : MonoBehaviour
{
    public IKChain ikChain;
    public List<Rigidbody> dynamicChainLinks = new List<Rigidbody>();

    [Header("Torque settings")]
    public float torqueStrength = 50f;   // proportional gain
    public float dampingStrength = 1f;  // angular velocity damping

    private void FixedUpdate()
    {
        for (int i = 0; i < ikChain.links.Count; i++)
        {
            Rigidbody rb = dynamicChainLinks[i];
            Quaternion target = ikChain.links[i].transform.rotation;
            Quaternion current = rb.rotation;

            // Shortest-path rotation error as axis-angle
            Quaternion error = target * Quaternion.Inverse(current);
            error.ToAngleAxis(out float angleDeg, out Vector3 axis);

            // Keep angle in -180, 180 to avoid spinning the long way round
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
