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
            GameObject g = Instantiate(ChainLink, startOffset + new Vector3(0, yPosition, 0) + transform.position, Quaternion.identity, ParentObject.transform);
            g.GetComponent<IKLink>().directionPole = directionPole;
            g.GetComponent<IKLink>().rotationPole = rotationPole;
            chain.links.Add(g.GetComponent<IKLink>());
        } 
    }

   
}
