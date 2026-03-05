using UnityEngine;

namespace VMCReplaceAvatar
{
    public class ScaleSync : MonoBehaviour
    {
        public Transform TargetTransform;
        public bool IsSync = true;

        private void Update()
        {
            if (TargetTransform)
            {
                if (IsSync)
                {
                    transform.position = TargetTransform.position;
                    transform.rotation = TargetTransform.rotation;
                    transform.localScale = new Vector3(1 / TargetTransform.localScale.x, 1 / TargetTransform.localScale.y, 1 / TargetTransform.localScale.z);
                }
                else
                {
                    transform.position = TargetTransform.position;
                    transform.rotation = TargetTransform.rotation;
                    transform.localScale = Vector3.one;
                }
            }
        }
    }
}
