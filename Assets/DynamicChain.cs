using UnityEngine;
using System.Collections.Generic;

public class DynamicChain : MonoBehaviour
{
    public IKChain ikChain;
    public List<Transform> chainTransforms = new List<Transform>();

    [Header("Torque settings")]
    public float torqueStrength = 5f;   // proportional gain
    public float dampingStrength = 1f;  // angular velocity damping

    // Cached rigidbodies, populated once links are ready
    List<Rigidbody> rbs = new List<Rigidbody>();
    bool rbsCached = false;

    void TryCacheRigidbodies()
    {
        if (rbsCached || chainTransforms.Count != ikChain.links.Count) return;
        rbs.Clear();
        foreach (Transform t in chainTransforms)
            rbs.Add(t.GetComponent<Rigidbody>());
        rbsCached = true;
    }

    void FixedUpdate()
    {
        TryCacheRigidbodies();
        if (!rbsCached) return;

        for (int i = 0; i < ikChain.links.Count; i++)
        {
            Rigidbody rb = rbs[i];
            Quaternion target = ikChain.links[i].transform.rotation;
            Quaternion current = rb.rotation;

            // Shortest-path rotation error as axis-angle
            Quaternion error = target * Quaternion.Inverse(current);
            error.ToAngleAxis(out float angleDeg, out Vector3 axis);

            // Keep angle in (-180, 180] to avoid spinning the long way round
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