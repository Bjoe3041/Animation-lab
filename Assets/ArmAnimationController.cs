using UnityEngine;

public class ArmAnimationController : MonoBehaviour
{
    [Header("Targets")]
    public Transform animationEndpointTarget;
    public Transform walkingTarget;

    [Header("Position Offsets (local space)")]
    public Vector3 spreadOutArmOffset;
    public Vector3 groupArmOffset;

    [Header("Movement")]
    public Vector3 movementDirection;

    [Header("Transition Speed")]
    public float spreadGroupSmoothTime = 0.2f;

    [Header("Swim Settings")]
    public float swimDriftRadius = 0.3f;
    public float swimFrequency = 1.2f;
    [Tooltip("Per-arm phase offset so arms don't all move in sync.")]
    public float swimPhaseOffset = 0f;

    [Header("Walk Settings")]
    public bool isLeadPairArm = true;
    public float stepHeight = 0.25f;
    public float stepSmoothTime = 0.12f;
    [Tooltip("Distance the body travels before triggering a new step.")]
    public float stepDistanceThreshold = 0.4f;
    [Tooltip("Minimum seconds between steps regardless of distance.")]
    public float stepCooldown = 0.25f;
    [Tooltip("How far ahead of current position to plant the foot.")]
    public float stepDirectionLead = 0.35f;
    public LayerMask groundMask;

    // ── Private state ────────────────────────────────────────────────────────

    Vector3 _smoothDampVelocity;
    Vector3 _walkStepVelocity;

    // The world-space position the foot is currently smoothing toward.
    Vector3 _currentStepTarget;
    // Body position recorded when the last step was planted.
    Vector3 _bodyPositionAtLastStep;
    float _lastStepTime;
    bool _stepTargetInitialized;

    // ── Public methods ────────────────────────────────────────────────────────

    public void SpreadOutArm()
    {
        Vector3 worldTarget = transform.TransformPoint(spreadOutArmOffset);
        animationEndpointTarget.position = Vector3.SmoothDamp(
            animationEndpointTarget.position,
            worldTarget,
            ref _smoothDampVelocity,
            spreadGroupSmoothTime);
    }

    public void GroupArm()
    {
        Vector3 worldTarget = transform.TransformPoint(groupArmOffset);
        animationEndpointTarget.position = Vector3.SmoothDamp(
            animationEndpointTarget.position,
            worldTarget,
            ref _smoothDampVelocity,
            spreadGroupSmoothTime);
    }

    public void DuringSwim()
    {
        // Independent sine waves per axis + individual phase so arms drift individually.
        float t = Time.time * swimFrequency + swimPhaseOffset;
        Vector3 drift = new Vector3(
            Mathf.Sin(t) * swimDriftRadius,
            Mathf.Sin(t * 1.3f + 1f) * swimDriftRadius * 0.5f,
            Mathf.Cos(t * 0.9f + 2f) * swimDriftRadius
        );

        Vector3 worldBase = transform.TransformPoint(groupArmOffset);
        animationEndpointTarget.position = worldBase + drift;
    }

    public void DuringWalk()
    {
        if (!_stepTargetInitialized)
        {
            _currentStepTarget = GetGroundTarget();
            _bodyPositionAtLastStep = transform.position;
            _stepTargetInitialized = true;
        }

        // Lead-pair arms step first; trail-pair arms wait one half-cycle.
        bool pairReady = isLeadPairArm
            ? true
            : (Time.time - _lastStepTime) > stepCooldown * 0.5f;

        float distanceDrifted = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(_bodyPositionAtLastStep.x, 0, _bodyPositionAtLastStep.z));

        bool cooldownExpired = (Time.time - _lastStepTime) > stepCooldown;
        bool distanceTrigger = distanceDrifted > stepDistanceThreshold;

        if (pairReady && cooldownExpired && distanceTrigger)
        {
            _currentStepTarget = GetGroundTarget();
            _bodyPositionAtLastStep = transform.position;
            _lastStepTime = Time.time;
        }

        // Smooth the walking target toward the planted step position.
        walkingTarget.position = Vector3.SmoothDamp(
            walkingTarget.position,
            _currentStepTarget,
            ref _walkStepVelocity,
            stepSmoothTime);

        // Drive the IK endpoint to match the walking target.
        animationEndpointTarget.position = walkingTarget.position;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    Vector3 GetGroundTarget()
    {
        // Look for ground beneath the arm's natural reach point,
        // offset forward by movement direction for anticipatory stepping.
        Vector3 castOrigin = transform.TransformPoint(groupArmOffset)
                           + movementDirection.normalized * stepDirectionLead
                           + Vector3.up * 2f;

        if (Physics.Raycast(castOrigin, Vector3.down, out RaycastHit hit, 10f, groundMask))
        {
            return hit.point + Vector3.up * stepHeight;
        }

        // Fallback: use the current arm position elevated by step height.
        return transform.TransformPoint(groupArmOffset) + Vector3.up * stepHeight;
    }
}