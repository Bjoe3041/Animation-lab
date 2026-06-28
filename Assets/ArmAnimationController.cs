using UnityEngine;

public class ArmAnimationController : MonoBehaviour
{
    [Header("Targets")]
    public Transform chainTarget;
    public Vector3 targetedPoint;
    public float targetingSpeed;

    [Header("Position Offsets (local space)")]
    public Vector3 spreadOutArmOffset;
    public Vector3 groupArmOffset;
    public Vector3 walkOffset;

    public Vector3 movementDirection, currentStepTargetPoint, stepTargetArea;
    public LayerMask groundMask;
    public AnimationCurve legLiftCurve;
    bool Stepping = false;
    public float maxFootDistanceFromBody;
    public float stepSpeed, stepProgress;

    private void FixedUpdate()
    {
        chainTarget.transform.position = Vector3.Lerp(chainTarget.transform.position, targetedPoint, targetingSpeed);    
    }

    public void SpreadOutArm()
    {
        targetedPoint = transform.TransformPoint(spreadOutArmOffset);
    }

    public void GroupArm()
    {
        targetedPoint = transform.TransformPoint(groupArmOffset);
    }

    public void DuringSwim() //called repeatedly when Swim state is active from OctopusAnimationController
    {
        Vector3 drift = new Vector3(Random.Range(-1,2), Random.Range(-1, 2), Random.Range(-1, 2));

        Vector3 worldBase = transform.TransformPoint(groupArmOffset);
        chainTarget.position = worldBase + drift * 20f * Time.deltaTime;
    }

    public void DuringWalk() //called repeatedly when Walk state is active from OctopusAnimationController
    {
        //When chainTarget is far away from transform
        //Wide capsule raycast for ground, at walkoffset + random vector3 + movementDirection.
        //Aim towards transform.down, since the creature can walk on walls and roofs.
        //When hit move targetedPoint to hit point over many iterations, and lift the targetedPoint by a value specified by an animation curve, which takes the "progress" of the movement towards the new assigned position, from 0 to 1.
        //The lift should be applied in the transform.up direction, which makes the creature "lift" its foot when it moves it.

        Debug.DrawRay(transform.position, transform.up*10, Color.red);

        if(Vector3.Distance(transform.position, targetedPoint) > maxFootDistanceFromBody)
            BeginStep();

        if (Stepping)
            ContinueStep();

                                   
        void BeginStep()
        {
            Stepping = true;
            currentStepTargetPoint = transform.TransformPoint(stepTargetArea) + movementDirection;
        }

        void ContinueStep()
        {
            stepProgress += Time.deltaTime * stepSpeed;
            Vector3 upwardsOffset = transform.up * legLiftCurve.Evaluate(stepProgress);
            Vector3 horizontalOffset = Vector3.Lerp(targetedPoint, currentStepTargetPoint, stepProgress);

            targetedPoint = horizontalOffset + upwardsOffset; //Later change to raycast down from this point

            if (stepProgress >= 1)
                EndStep();
        }

        void EndStep()
        {
            Stepping = false;
            stepProgress = 0;
        }

    }
}