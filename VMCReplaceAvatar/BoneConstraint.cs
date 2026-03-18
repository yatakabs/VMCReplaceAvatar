using UnityEngine;

namespace VMCReplaceAvatar
{
    public class BoneConstraint : MonoBehaviour
    {
        public RestPose restPose;
        public Quaternion initialRotation;
        public Transform targetBone;

        private void Start()
        {
            initialRotation = transform.localRotation;
        }

        private void Update()
        {
            if (restPose != null)
            {
                if (restPose.isRestPose)
                    transform.localRotation = initialRotation;
                else
                    transform.localRotation = targetBone.localRotation;
            }
        }
    }
}
