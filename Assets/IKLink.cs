using UnityEngine;

public class IKLink : MonoBehaviour
{
    public Transform directionPole;
    public Transform rotationPole;
    public float maxAngle = 45f;
    public float maxTwist = 45f; //Enforced from root and up
    public float PositionSpringModifier = 1.0f;

}
public enum IKRotationMode
{
    RotateTowardsPole,
    MimicPoleRotationLocally,
    ZLocked
}