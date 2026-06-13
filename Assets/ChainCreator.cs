using UnityEngine;

public class ChainCreator : MonoBehaviour
{
    public IKChain chain;
    public GameObject ChainLink, ParentObject;
    public Vector3 startOffset;
    public int amount;
    public float length;
    public AnimationCurve lengthModifierCurve;

    public Transform directionPole, rotationPole;

    void Start()
    {
        float yPosition = 0;
        for (int i = 0; i < amount; i++)
        {
            yPosition += length * lengthModifierCurve.Evaluate((float)i / (float)amount);
            Vector3 worldPos = transform.position + transform.TransformDirection(startOffset) + transform.up * yPosition;
            GameObject g = Instantiate(ChainLink, worldPos, transform.rotation, ParentObject.transform);

            g.GetComponent<IKLink>().directionPole = directionPole;
            g.GetComponent<IKLink>().rotationPole = rotationPole;
            chain.links.Add(g.GetComponent<IKLink>());

        }

        chain.InitialiseChain();

    }

   
}
