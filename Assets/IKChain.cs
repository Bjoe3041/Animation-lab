using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IKChain : MonoBehaviour
{
    [Header("Chain")]
    public  List<IKLink> links;

    public Transform target;

    [Tooltip("Fixed anchor for the root position. If null, links[0].transform is used.")]
    public Transform root;

    [Header("Solver")]
    [Range(1, 64)]
    [Tooltip("Maximum FABRIK iterations per frame.")]
    public int maxIterations = 10;

    [Tooltip("Stop iterating when the tip is within this distance of the target.")]
    public float tolerance = 0.001f;

    [Tooltip("How strongly the direction poles pull each link (0 = off, 1 = full).")]
    [Range(0f, 1f)]
    public float poleWeight = 0.3f;

    // Cached bone lengths between consecutive links.
    private float[] _lengths;
    public float TotalLength { get => (_lengths == null ? 0: _lengths.Sum()); }

    [Header("Info")]
    public bool TargetInReach = false;

    public IKRotationMode rotationMode;

    private void Update()
    {
        if (!IsValid()) return;
        SolveIK();
    }

    public void InitialiseChain()
    {
        if (links == null || links.Count < 2) return;

        int count = links.Count;
        _lengths = new float[count - 1];

        for (int i = 0; i < count - 1; i++)
        {
            _lengths[i] = Vector3.Distance(links[i].transform.position,
                                             links[i + 1].transform.position);
        }

        Debug.Log("Arm length is: " + TotalLength);
    } 

    private void SolveIK()
    {
        int count = links.Count;
        Vector3 rootPos = root != null ? root.position : links[0].transform.position;
        Vector3 targetPos = target.position;

        // Nudge toward direction poles before solving
        for (int i = 1; i < count; i++)
        {
            Vector3 dir = (links[i].directionPole.position - links[i].transform.position).normalized;
            links[i].transform.position += dir * _lengths[i - 1];
        }

        float distToTarget = Vector3.Distance(links[0].transform.position, targetPos);
        TargetInReach = distToTarget <= TotalLength;

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // Pole nudge
            for (int i = 1; i < count; i++)
            {
                Vector3 dir = (links[i].directionPole.position - links[i].transform.position).normalized * 0.1f;
                links[i].transform.position += dir;
            }

            // Forward pass: tip → root, with angle constraints
            links[count - 1].transform.position = targetPos;
            for (int i = count - 2; i >= 0; i--)
            {
                Vector3 dir = (links[i].transform.position - links[i + 1].transform.position).normalized;

                // Clamp dir against links[i+1]'s max angle relative to its parent
                if (i + 2 < count)
                {
                    Vector3 parentDir = (links[i + 1].transform.position - links[i + 2].transform.position).normalized;
                    dir = ClampDirection(dir, parentDir, links[i + 1].maxAngle);
                }

                links[i].transform.position = links[i + 1].transform.position + dir * _lengths[i];
            }

            // Backward pass: root → tip, with angle constraints
            links[0].transform.position = rootPos;
            for (int i = 1; i < count; i++)
            {
                Vector3 dir = (links[i].transform.position - links[i - 1].transform.position).normalized;

                // Clamp dir against links[i-1]'s direction (the parent bone direction)
                if (i >= 2)
                {
                    Vector3 parentDir = (links[i - 1].transform.position - links[i - 2].transform.position).normalized;
                    dir = ClampDirection(dir, parentDir, links[i - 1].maxAngle);
                }

                links[i].transform.position = links[i - 1].transform.position + dir * _lengths[i - 1];
            }

            if (Vector3.Distance(links[count - 1].transform.position, targetPos) < tolerance)
                break;
        }

        //Rotation
        for (int i = 0; i < count; i++) //Assign rotation
        {
            if(rotationMode == IKRotationMode.RotateTowardsPole)
            {
                if (i < count - 1)
                {
                    Vector3 forward = (links[i + 1].transform.position - links[i].transform.position).normalized;
                    Vector3 up = (links[i].rotationPole.transform.position - links[i].transform.position).normalized;

                    if (Vector3.Cross(forward, up).sqrMagnitude < 0.0001f)
                        up = Vector3.right;

                    links[i].transform.rotation = Quaternion.LookRotation(forward, up);
                }
                else
                {
                    if (count >= 2)
                        links[i].transform.rotation = links[i - 1].transform.rotation;
                }
            }
            if (rotationMode == IKRotationMode.MimicPoleRotationLocally)
            {
                Vector3 carriedUp = links[0].rotationPole.transform.rotation * Vector3.up;

                for (int j = 0; j < count; j++)
                {
                    Vector3 forward = j < count - 1
                        ? (links[j + 1].transform.position - links[j].transform.position).normalized
                        : (j >= 1 ? (links[j].transform.position - links[j - 1].transform.position).normalized : Vector3.forward);

                    Vector3 poleUp = links[j].rotationPole.transform.rotation * Vector3.up;
                    Vector3 projectedPoleUp = Vector3.ProjectOnPlane(poleUp, forward);
                    Vector3 projectedCarriedUp = Vector3.ProjectOnPlane(carriedUp, forward);

                    Vector3 chosenUp;
                    float poleConfidence = projectedPoleUp.magnitude;
                    float carriedConfidence = projectedCarriedUp.magnitude;

                    if (poleConfidence > 0.8f)
                    {
                        chosenUp = projectedPoleUp.normalized;
                        carriedUp = chosenUp;
                    }
                    else if (carriedConfidence > 0.001f)
                    {
                        // Pole up is near-singular, use the carried up from previous links instead
                        chosenUp = projectedCarriedUp.normalized;
                        // Don't update carriedUp here, keep carrying the last good value
                    }
                    else
                    {
                        // Both are degenerate, keep whatever we had
                        chosenUp = carriedUp;
                    }

                    links[j].transform.rotation = Quaternion.LookRotation(forward, chosenUp);
                }
            }
            if (rotationMode == IKRotationMode.ZLocked)
            {
                for (int j = 0; j < count; j++)
                {
                    Vector3 forward = j < count - 1
                        ? (links[j + 1].transform.position - links[j].transform.position).normalized
                        : (j >= 1 ? (links[j].transform.position - links[j - 1].transform.position).normalized : Vector3.forward);

                    Vector3 projectedUp = Vector3.ProjectOnPlane(Vector3.up, forward);

                    Quaternion rotation;
                    if (projectedUp.sqrMagnitude > 0.001f)
                        rotation = Quaternion.LookRotation(forward, projectedUp.normalized);
                    else
                        rotation = Quaternion.LookRotation(forward, Vector3.forward);

                    // Strip Z rotation entirely
                    Vector3 euler = rotation.eulerAngles;
                    euler.z = 0f;
                    links[j].transform.rotation = Quaternion.Euler(euler);
                }
            }
        }
    }

    // Clamps `dir` to within `maxAngle` degrees of `reference`.
    private Vector3 ClampDirection(Vector3 dir, Vector3 reference, float maxAngle)
    {
        float angle = Vector3.Angle(reference, dir);
        if (angle <= maxAngle) return dir;
        return Vector3.Slerp(reference, dir, maxAngle / angle).normalized;
    }

    private bool IsValid()
    {
        if (links == null || links.Count < 2 || target == null) return false;
        if (_lengths == null || _lengths.Length != links.Count - 1)
            InitialiseChain();
        return true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (links == null) return;

        for (int i = 0; i < links.Count; i++)
        {
            if (links[i] == null) continue;
            Vector3 pos = links[i].transform.position;

            // Bone line
            if (i < links.Count - 1 && links[i + 1] != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(pos, links[i + 1].transform.position);
            }

            // Joint sphere
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, 0.04f);

            // Direction pole
            if (links[i].directionPole != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(pos, links[i].directionPole.position - links[i].transform.position);
            }

            // Rotation pole
            if (links[i].rotationPole != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(pos, links[i].rotationPole.position - links[i].transform.position);
            }
        }

        // Target
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.06f);
        }
    }
#endif
}