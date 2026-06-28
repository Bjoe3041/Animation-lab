using UnityEngine;

public class WalkTargetPlacer : MonoBehaviour
{
    [Header("References")]
    public Transform targetTransform; // the leg end-effector target this placer moves
    public Rigidbody rb;

    [Header("Ground Detection")]
    public LayerMask groundMask;
    public float maxDistanceToGround;
    public float castRadius = 0.5f;
    public float forceBeginStepDistance, fallbackStepDistance;
    public float maxLeadDistance;
    public Transform groundPoint, groundPoint2, defaultTargetPosition; // groundPoint = next step target, groundPoint2 = debug, straight down
    Vector3 lastGoodGroundHit;

    [Header("Stepping")]
    public float maxStepDistance; // how far targetTransform can drift from groundPoint before a new step starts
    public float stepSpeed = 2f;
    public AnimationCurve liftCurve;

    public Vector3 movementDirection;

    public bool stepping;
    float stepProgress;
    Vector3 stepStartPoint;
    Vector3 stepEndPoint;

    public WalkTargetPlacer pairedWalkTargetPlacer;
    public float equilibriumDistance;
    public float DistanceFromGroundPointToTargetTransform { get => Vector3.Distance(groundPoint.position, targetTransform.position); }

    public bool grounded;
    public bool falling;

    void Update()
    {

        movementDirection = rb.linearVelocity;
        UpdateGroundPoints();

        falling = Vector3.Distance(lastGoodGroundHit, defaultTargetPosition.position + rb.linearVelocity * Mathf.Clamp(rb.linearVelocity.magnitude, 0, maxLeadDistance)) > fallbackStepDistance;

        if (falling)
        {
            targetTransform.position = Vector3.Lerp(targetTransform.position, defaultTargetPosition.position, Time.deltaTime * stepSpeed);
            return;
        }

        if (!stepping && ShouldStartStep())
            BeginStep();

        if (stepping)
            ContinueStep();
    }

    void UpdateGroundPoints()
    {
        if (Physics.Raycast(transform.position, -transform.up, out RaycastHit hit2, maxDistanceToGround, groundMask))
            groundPoint2.position = hit2.point;

        grounded = false;

        Vector3 horizontalDir = Vector3.ProjectOnPlane(movementDirection, transform.up);
        Vector3 castOrigin = transform.position;
        if (horizontalDir.sqrMagnitude > 0.0001f)
            castOrigin += horizontalDir.normalized * Mathf.Clamp(rb.linearVelocity.magnitude,0, maxLeadDistance);

        if (Physics.SphereCast(castOrigin, castRadius, -transform.up, out RaycastHit hit, maxDistanceToGround, groundMask))
        {
            groundPoint.position = hit.point;
            lastGoodGroundHit = hit.point;
            grounded = true;
            //Debug.DrawLine(castOrigin, hit.point, Color.magenta, 10);
        }
    }

    bool ShouldStartStep()
    {
        if (Vector3.Distance(targetTransform.position, lastGoodGroundHit) > forceBeginStepDistance)
            return true;

        //If other foot is far from its groundpoint (and not mid step), we can begin a step.

        if (pairedWalkTargetPlacer.DistanceFromGroundPointToTargetTransform > equilibriumDistance)
        if(pairedWalkTargetPlacer.grounded && !pairedWalkTargetPlacer.stepping)
            return Vector3.Distance(targetTransform.position, groundPoint.position) > maxStepDistance;
        return false;
    }

    void BeginStep()
    {
        stepping = true;
        stepProgress = 0f;
        stepStartPoint = targetTransform.position;
        stepEndPoint = groundPoint.position;
    }

    void ContinueStep()
    {
        stepProgress += Time.deltaTime * stepSpeed;
        float t = Mathf.Clamp01(stepProgress);

        Vector3 horizontal = Vector3.Lerp(stepStartPoint, stepEndPoint, t);
        Vector3 lift = transform.up * liftCurve.Evaluate(t);

        targetTransform.position = horizontal + lift;

        if (stepProgress >= 1f)
            EndStep();
    }

    void EndStep()
    {
        stepping = false;
        stepProgress = 0f;
    }


    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(transform.position, -transform.up * maxDistanceToGround);
    }

}