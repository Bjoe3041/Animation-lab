using System.Drawing;
using UnityEngine;

public class DynamicChainInitializer : MonoBehaviour
{
    public IKChain ikChain;
    public DynamicChain dynamicChain;
    public GameObject dynamicChainlinkObject;
    bool chainLoaded = false;
    public Transform dynamicChainObjectsParent;

    public void InitializeDynamicChain()
    {
        for (int i = 0; i < ikChain.links.Count; i++)
        {
            IKLink link = ikChain.links[i];
            GameObject g = Instantiate(dynamicChainlinkObject, link.transform.position, ikChain.links[0].rotationPole.transform.rotation);
            dynamicChain.chainTransforms.Add(g.transform);
        }

        ConfigurableJoint rootJoint = dynamicChain.chainTransforms[0].gameObject.AddComponent<ConfigurableJoint>(); //Root joint
        rootJoint.connectedBody = ikChain.root.GetComponent<Rigidbody>();
        dynamicChain.chainTransforms[0].transform.rotation = ikChain.links[0].transform.rotation;

        rootJoint.autoConfigureConnectedAnchor = false;
        rootJoint.anchor = Vector3.zero;
        rootJoint.connectedAnchor = Vector3.zero;

        rootJoint.xMotion = ConfigurableJointMotion.Locked;
        rootJoint.yMotion = ConfigurableJointMotion.Locked;
        rootJoint.zMotion = ConfigurableJointMotion.Locked;

        rootJoint.angularXMotion = ConfigurableJointMotion.Free;
        rootJoint.angularYMotion = ConfigurableJointMotion.Free;
        rootJoint.angularZMotion = ConfigurableJointMotion.Free;
        //rootJoint.transform.parent = rootJoint.connectedBody.transform;
        rootJoint.transform.parent = dynamicChainObjectsParent;


        for (int i = 1; i < dynamicChain.chainTransforms.Count; i++)
        {
            Transform current = dynamicChain.chainTransforms[i];
            Transform previous = dynamicChain.chainTransforms[i - 1];

            Rigidbody currentRb = current.GetComponent<Rigidbody>();
            Rigidbody previousRb = previous.GetComponent<Rigidbody>();

            ConfigurableJoint joint = current.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = previousRb;

            // Keep fixed distance
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;


            // Anchor at the current link's position in the previous link's local space
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            Vector3 worldOffset = current.position - previous.position;
            joint.connectedAnchor = Quaternion.Inverse(previous.rotation) * worldOffset;

            joint.transform.parent = dynamicChainObjectsParent;
        }

        dynamicChain.HydrateRigidbodyList();
    }
}
