using UnityEngine;
using System.Collections.Generic;

public class OctopusAnimationController : MonoBehaviour
{
    public OctopusAnimationState state;
    public List<ArmAnimationController> arms;

    OctopusAnimationState _previousState;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            state = OctopusAnimationState.Idle;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            state = OctopusAnimationState.Swimming;
        if (Input.GetKeyDown(KeyCode.Alpha3))
            state = OctopusAnimationState.Walking;
        if (Input.GetKeyDown(KeyCode.Alpha4))
            state = OctopusAnimationState.GroupArms;
        if (Input.GetKeyDown(KeyCode.Alpha5))
            state = OctopusAnimationState.SpreadArms;


        if (state != _previousState)
        {
            OnStateEnter(state);
            _previousState = state;
        }

        switch (state)
        {
            case OctopusAnimationState.Idle:
                // Intentionally empty, arms linger at their last position.
                break;

            case OctopusAnimationState.Swimming:
                foreach (var arm in arms)
                    arm.DuringSwim();
                break;

            case OctopusAnimationState.Walking:
                foreach (var arm in arms)
                    arm.DuringWalk();
                break;
        }
    }

    void OnStateEnter(OctopusAnimationState newState)
    {
        switch (newState)
        {
            case OctopusAnimationState.Idle:
                // No transition needed since arms hold their current positions.
                break;

            case OctopusAnimationState.GroupArms:
            case OctopusAnimationState.Swimming:
                foreach (var arm in arms)
                    arm.GroupArm();
                break;
            case OctopusAnimationState.SpreadArms:
            case OctopusAnimationState.Walking:
                foreach (var arm in arms)
                    arm.SpreadOutArm();
                break;
        }
    }
}

public enum OctopusAnimationState
{
    Idle,
    Swimming,
    Walking,
    SpreadArms,
    GroupArms
}