using Newtonsoft.Json;
using System.Collections.Generic;

namespace VMCReplaceAvatar
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Config
    {
        [JsonProperty] public bool alwaysDisplayGUI = true;
        [JsonProperty] public bool avatarSelfScaling = false;
        [JsonProperty] public List<VRMAvatarMeshSetting> vrmAvatarMeshSettings = new List<VRMAvatarMeshSetting>();
    }

    public class VRMAvatarMeshSetting
    {
        [JsonProperty] public string avatarName;
        [JsonProperty] public List<MeshSetting> meshSettings = new List<MeshSetting>();
    }

    public class MeshSetting
    {
        [JsonProperty] public string meshName;
        [JsonProperty] public bool isSync = false;
    }
}
