using System.Drawing;
using UnityEngine;

public class RigidbodyFollower : MonoBehaviour
{
    public Rigidbody[] bodies;
    public IKChain chain;
    public Rigidbody rootBody;

    [Header("Position")]
    public float positionSpring = 50f;
    public float positionDamper = 10f;

    [Header("Smoothing")]
    [Range(0f, 1f)]
    public float neighbourSmoothing = 0.3f;
    public float maxAngleBetweenLinks = 45f;

    [Header("Velocity Limits")]
    public float maxVelocity = 10f;
    public bool limitLinear = true;
    public bool limitAngular = true;

    [Header("Angular Joint Limits")]
    [Tooltip("Max swing angle on each axis for non-root links.")]
    public float angularLimit = 30f;
    [Tooltip("Bounciness at the angular limit (0 = no bounce).")]
    public float angularBounciness = 0f;
    [Tooltip("Contact distance for the angular limit.")]
    public float angularContactDistance = 0f;

    private ConfigurableJoint[] _joints;

    private void Start()
    {
        _joints = new ConfigurableJoint[bodies.Length];

        for (int i = 0; i < bodies.Length; i++)
        {
            var joint = bodies[i].gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = i > 0 ? bodies[i - 1] : rootBody;

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.secondaryAxis = new Vector3(0, 0, -1);

            var limit = new SoftJointLimit
            {
                limit = angularLimit,
                bounciness = angularBounciness,
                contactDistance = angularContactDistance
            };

            joint.angularYZLimitSpring = new SoftJointLimitSpring
            {
                spring = 0.2f,
                damper = 1
            };


            if (i == 0)
            {
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                joint.highAngularXLimit = limit;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimit };
                joint.angularYLimit = limit;
                joint.angularZLimit = limit;
            }
            else
            {
                joint.xMotion = ConfigurableJointMotion.Limited;
                joint.yMotion = ConfigurableJointMotion.Limited;
                joint.zMotion = ConfigurableJointMotion.Limited;

                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                
                joint.highAngularXLimit = limit;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -angularLimit };
                joint.angularYLimit = limit;
                joint.angularZLimit = limit;
            }

            if (limitLinear) bodies[i].maxLinearVelocity = maxVelocity;
            if (limitAngular) bodies[i].maxAngularVelocity = maxVelocity;

            _joints[i] = joint;
        }
    }

    private void FixedUpdate()
    {
        int count = Mathf.Min(bodies.Length, chain.links.Count);
        
        for (int i = 0; i < count; i++)
        {
            Vector3 target = SmoothedPosition(i, count);
            target = ClampedPosition(i, target);

            Vector3 toTarget = target - bodies[i].position;
            bodies[i].AddForce((toTarget * positionSpring * chain.links[i].PositionSpringModifier) - bodies[i].linearVelocity * positionDamper);
        }
    }

    private Vector3 SmoothedPosition(int i, int count)
    {
        if (neighbourSmoothing <= 0f)
            return chain.links[i].transform.position;

        Vector3 prev = chain.links[Mathf.Max(i - 1, 0)].transform.position;
        Vector3 next = chain.links[Mathf.Min(i + 1, count - 1)].transform.position;

        Vector3 neighbourAvg = (prev + next) * 0.5f;
        return Vector3.Lerp(chain.links[i].transform.position, neighbourAvg, neighbourSmoothing);
    }

    private Vector3 ClampedPosition(int i, Vector3 target)
    {
        if (i < 2) return target;

        Vector3 prevPrev = bodies[i - 2].position;
        Vector3 prev = bodies[i - 1].position;

        Vector3 incomingDir = (prev - prevPrev).normalized;
        Vector3 desiredDir = (target - prev).normalized;

        float angle = Vector3.Angle(incomingDir, desiredDir);

        if (angle > maxAngleBetweenLinks)
        {
            Vector3 clampedDir = Vector3.Slerp(incomingDir, desiredDir, maxAngleBetweenLinks / angle);
            float dist = Vector3.Distance(prev, target);
            target = prev + clampedDir * dist;
        }

        return target;
    }
}