using UnityEngine;

namespace VMCReplaceAvatar
{
    public class SelfScaling : MonoBehaviour
    {
        public bool AvatarSelfScaling = false;
        public Setting Setting;

        private void Start()
        {
            AvatarSelfScaling = Setting.avatarSelfScaling;
        }

        private void Update()
        {
            if (Setting != null)
            {
                if (Setting.avatarSelfScaling != AvatarSelfScaling)
                {
                    AvatarSelfScaling = Setting.avatarSelfScaling;
                }
            }
        }

    }
}
