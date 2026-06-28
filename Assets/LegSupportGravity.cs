using System.Collections.Generic;
using UnityEngine;

public class LegSupportGravity : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public List<WalkTargetPlacer> legs = new List<WalkTargetPlacer>();

    [Header("Support")]
    [Range(0f, 1f)]
    public float requiredSupportFraction = 0.5f; // fraction of legs that need to be connected before gravity is suppressed

    [Header("Gravity")]
    public float gravityForce = 9.81f;
    public bool scaleWithMissingSupport = true; // ramp gravity in as support drops instead of a hard on/off

    public int ConnectedLegCount { get; private set; }
    public float SupportFraction { get; private set; }
    public bool IsSupported { get; private set; }

    void FixedUpdate()
    {
        UpdateSupportState();

        if (IsSupported)
            return;

        float strength = scaleWithMissingSupport ? 1f - SupportFraction : 1f;
        rb.AddForce(Vector3.down * gravityForce * strength, ForceMode.Acceleration);
    }

    void UpdateSupportState()
    {
        int connected = 0;
        for (int i = 0; i < legs.Count; i++)
        {
            if (legs[i] != null && legs[i].grounded && !legs[i].stepping)
                connected++;
        }

        ConnectedLegCount = connected;
        SupportFraction = legs.Count > 0 ? (float)connected / legs.Count : 0f;
        IsSupported = SupportFraction >= requiredSupportFraction;
    }

    // lets MovementController reuse the same connected-leg data for height correction
    public Vector3 GetAverageGroundPoint(out int connectedLegs)
    {
        int connected = 0;
        Vector3 sum = Vector3.zero;
        Vector3 average = Vector3.zero;
        for (int i = 0; i < legs.Count; i++)
        {
            if (legs[i] != null && legs[i].grounded && !legs[i].stepping)
            {
                Vector3 localPos = transform.InverseTransformPoint(legs[i].targetTransform.position);
                connected++;
                sum += localPos;
            }
        }

        average = sum / connected;

        connectedLegs = connected;
        return average;
    }
}