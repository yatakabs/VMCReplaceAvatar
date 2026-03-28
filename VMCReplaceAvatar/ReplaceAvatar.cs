using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using VMC;
using VMCMod;
using VRM;

namespace VMCReplaceAvatar
{
    [VMCPlugin(
    Name: "VMC Replace Avatar",
    Version: "0.1.1",
    Author: "snow",
    Description: "VRMを別のアバターモデルで置き換えるMod",
    AuthorURL: "https://twitter.com/snow_mil",
    PluginURL: "https://github.com/Snow1226/VMCReplaceAvatar")]
    public class ReplaceAvatar : MonoBehaviour
    {
        private Config _config;
        private VRMAvatarMeshSetting _currentAvatarMeshSetting;
        private GameObject _vrmModel = null;
        private GameObject _vrmArmature = null;
        private GameObject _vrmPose = null;

        private bool _restPose = false;

        private GameObject _avatarModel = null;
        private GameObject _rootObject = null;
        private string _avatarFileName;

        private GameObject _scaleSyncTarget = null;

        private string _currentModelName = string.Empty;
        private const int AvatarLayer = 3;

        private Vector2 _scrollPosition = Vector2.zero;
        private Vector2 _skinnedMeshScrollPosition = Vector2.zero;
        private int _selectedSkinnedMeshToggle = 0;

        private ScaleSync _rootSync = null;
        private PositionConstraintScaleSync _avatarRootConstraintScaleSync = null;

        private SelfScaling _selfScaling;
        private GameObject _floorObject;
        private bool _floorDispay = false;

        private Array _boneArray = Enum.GetValues(typeof(HumanBodyBones));

        private FloorOffset _floorOffset;
        private string _address;
        private int _port;

        private bool _displayUI;

        private void Awake()
        {
            LoadConfig();
            VMCEvents.OnModelLoaded += OnModelLoaded;
            VMCEvents.AfterApplyMotion += AfterApplyMotion;

            _selfScaling = new GameObject("AvatarSelfScaling").AddComponent<SelfScaling>();
            _selfScaling.config = _config;

            _floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _floorObject.transform.position = Vector3.zero;
            _floorObject.transform.rotation = Quaternion.identity;
            _floorObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            _floorObject.SetActive(_floorDispay);

            LoadBurstLibrary();

            _floorOffset = new GameObject("FloorOffset").AddComponent<FloorOffset>();
            _floorOffset.Config = _config;
            _address = _config.FloorOffsetSenderAddress;
            _port = _config.FloorOffsetPort;

            _displayUI = _config.DisplayUIatStartup;
        }

        private void OnDestroy()
        {
            VMCEvents.OnModelLoaded -= OnModelLoaded;
            VMCEvents.AfterApplyMotion -= AfterApplyMotion;
            SaveConfig();
        }

        private void LoadBurstLibrary()
        {
            var burstLibraryPath = Path.Combine(Application.dataPath, "Plugins/x86_64/MagicaCloth2.burst.dll");
            Debug.Log($"Burst Library Path : {burstLibraryPath}");
            if (File.Exists(burstLibraryPath))
            {
                if (!Unity.Burst.BurstRuntime.LoadAdditionalLibrary(burstLibraryPath))
                {
                    Debug.LogWarning("MagicaCloth2.burst.dll load failed.");
                }
                else
                {
                    Debug.LogWarning("MagicaCloth2.burst.dll loaded.");

                }
            }
            else
            {
                Debug.LogWarning("MagicaCloth2.burst.dll not found.");
            }
        }

        private void LoadConfig()
        {
            string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            if (File.Exists(Path.Combine(dllDirectory, "VMCReplaceAvatar.json")))
                _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(dllDirectory, "VMCReplaceAvatar.json")));
            else
            {
                _config = new Config();
                File.WriteAllText(Path.Combine(dllDirectory, "VMCReplaceAvatar.json"), JsonConvert.SerializeObject(_config, Formatting.Indented));
            }
        }

        private void SaveConfig()
        {
            var avatarMeshSetting = _config.vrmAvatarMeshSettings.FindIndex(x => x.avatarName == _currentModelName);
            if (avatarMeshSetting >= 0)
            {
                _config.vrmAvatarMeshSettings[avatarMeshSetting] = _currentAvatarMeshSetting;
            }
            else
            {
                _config.vrmAvatarMeshSettings.Add(_currentAvatarMeshSetting);
            }

            string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            File.WriteAllText(Path.Combine(dllDirectory, "VMCReplaceAvatar.json"), JsonConvert.SerializeObject(_config, Formatting.Indented));

            //BlendShape Sync
            SetBlendshapeSync();
        }

        [OnSetting]
        public void OnSetting()
        {
            _displayUI = true;
        }

        private void SetBlendshapeSync()
        {
            if (_vrmModel == null || _avatarModel == null) return;

            _avatarModel.GetComponentsInChildren<BlendShapeSync>(true).ToList().ForEach(x => DestroyImmediate(x));
            Renderer[] newRenderers = _avatarModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in newRenderers)
            {
                if (_currentAvatarMeshSetting.meshSettings.Find(x => x.meshName == renderer.gameObject.name)?.isSync == true)
                {
                    BlendShapeSync sync = renderer.gameObject.AddComponent<BlendShapeSync>();
                    sync.sourceRenderer = _vrmModel.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.gameObject.name == renderer.gameObject.name);
                }
            }
        }

        private void AfterApplyMotion(GameObject currentModel)
        {
            Animator vrmAnimator = currentModel.GetComponent<Animator>();

            //中間ArmatureのPoseをVRMモデルのPoseに合わせる
            if (_vrmPose != null)
            {
                //Root Constraint
                _vrmPose.transform.localPosition = currentModel.transform.localPosition;
                _vrmPose.transform.localRotation = currentModel.transform.localRotation;

                Animator initialPoseAnimator = _vrmPose.GetComponent<Animator>();

                //ArmatureConstraint
                initialPoseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.transform.localPosition = vrmAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.transform.localPosition;
                initialPoseAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.transform.localRotation = vrmAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.transform.localRotation;

                if (vrmAnimator != null && initialPoseAnimator != null)
                {
                    foreach (var bone in _boneArray)
                    {
                        if ((HumanBodyBones)bone == HumanBodyBones.LastBone)
                            continue;

                        var vrmBone = vrmAnimator.GetBoneTransform((HumanBodyBones)bone);
                        var initialPoseBone = initialPoseAnimator.GetBoneTransform((HumanBodyBones)bone);

                        if (!_restPose)
                        {

                            if (vrmBone != null && initialPoseBone != null)
                            {
                                initialPoseBone.localPosition = vrmBone.localPosition;
                                initialPoseBone.localRotation = vrmBone.localRotation;
                            }
                        }
                        else
                        {
                            var initialTransform = initialPoseBone.gameObject.GetComponent<InitialTransform>();
                            initialPoseBone.position = initialTransform.initialPosition;
                            initialPoseBone.rotation = initialTransform.initialRotation;
                        }
                    }
                }
            }

            if(_avatarModel != null && _vrmPose != null)
            {
                Animator avatarAnimator = _avatarModel.GetComponent<Animator>();
                Animator poseAnimator = _vrmPose.GetComponent<Animator>();

                if (avatarAnimator != null && poseAnimator != null)
                {
                    foreach (var bone in _boneArray)
                    {
                        if ((HumanBodyBones)bone == HumanBodyBones.LastBone)
                            continue;

                        var avatarBone = avatarAnimator.GetBoneTransform((HumanBodyBones)bone);
                        var poseBone = poseAnimator.GetBoneTransform((HumanBodyBones)bone);

                        if (avatarBone != null && poseBone != null)
                        {
                            var boneConstraint = avatarBone.gameObject.GetComponent<BoneConstraint>();
                            if (boneConstraint != null)
                            {
                                var worldDelta = boneConstraint.sourceTransform.transform.rotation * Quaternion.Inverse(boneConstraint.sourceTransform.initialRotation);
                                avatarBone.rotation = worldDelta * boneConstraint.worldRotationAtRest;
                            }
                        }
                    }

                }
            }
        }

        private void LateUpdate()
        {
            if(_vrmModel != null && _scaleSyncTarget != null)
                if(_scaleSyncTarget.transform.localScale != Vector3.zero)
                    AfterApplyMotion(_vrmModel);
        }

        private void OnModelLoaded(GameObject currentModel)
        {
            if(_vrmPose != null)
                Destroy(_vrmPose);

            if (_scaleSyncTarget == null)
                _scaleSyncTarget = GameObject.Find("HandTrackerRoot");

            _vrmModel = currentModel;
            _currentModelName = currentModel.GetComponent<VRMMeta>().Meta.Title;

            //VRM初期ポーズを複製
            Animator vrmAnimator = _vrmModel.GetComponent<Animator>();
            _vrmArmature = vrmAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;

            _vrmPose = InstantiateArmature(currentModel, "VRM Initial Pose");

            Animator initialPoseAnimator = _vrmPose.GetComponent<Animator>();
            Array boneArray = Enum.GetValues(typeof(HumanBodyBones));

            //InitialTransform
            foreach (var bone in boneArray)
            {
                if ((HumanBodyBones)bone == HumanBodyBones.LastBone)
                    continue;

                var initialBone = initialPoseAnimator.GetBoneTransform((HumanBodyBones)bone);

                if (initialBone != null)
                {
                    var constraint = initialBone.gameObject.AddComponent<InitialTransform>();
                    constraint.initialPosition = initialBone.position;
                    constraint.initialRotation = initialBone.rotation;
                }
            }

            var avatarMeshSetting = _config.vrmAvatarMeshSettings.Find(x => x.avatarName == _currentModelName);
            if (avatarMeshSetting != null)
                _currentAvatarMeshSetting = avatarMeshSetting;
            else
            {
                _currentAvatarMeshSetting = new VRMAvatarMeshSetting() { avatarName = _currentModelName };
                Renderer[] renderers = currentModel.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    MeshSetting meshSetting = new MeshSetting()
                    {
                        meshName = renderer.gameObject.name,
                        isSync = false
                    };
                    _currentAvatarMeshSetting.meshSettings.Add(meshSetting);
                }
                _config.vrmAvatarMeshSettings.Add(_currentAvatarMeshSetting);
                SaveConfig();
            }
        }

        private GameObject InstantiateArmature(GameObject armatureObject,string objectName)
        {
            var retObj = Instantiate(armatureObject, armatureObject.transform.position, armatureObject.transform.rotation);
            retObj.name = objectName;

            Renderer[] renderers = retObj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if(renderer != null)
                    Destroy(renderer.gameObject);
            }
            var comps = retObj.GetComponentsInChildren<Behaviour>(true);
            var exclusionList = new string[] { "Animator" };
            foreach (var comp in comps)
            {
                try
                {
                    if (comp != null)
                    {
                        foreach (string d in exclusionList)
                        {
                            if (!comp.GetType().Name.ToLower().Contains(d.ToLower()))
                                GameObject.Destroy(comp);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Component Destroy Error : {comp.gameObject.name} - {comp.GetType().Name} / {ex.Message}");
                }
            }
            return retObj;
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                _config.avatarSelfScaling = !_config.avatarSelfScaling;
                SaveConfig();
            }
            if (Input.GetKeyDown(KeyCode.F12))
            {
                Renderer[] vrmRenderers = _vrmModel.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in vrmRenderers)
                {
                    renderer.enabled = !renderer.enabled;
                }
            }
            if(Input.GetKeyDown(KeyCode.Tab))
            {
                _displayUI = !_displayUI;
            }
        }

        private void LoadAvatar()
        {
            var avatarPath = WindowsDialogs.OpenFileDialog("Select Avatar", ".avatar");

            if (avatarPath != null)
            {
                _avatarFileName = Path.GetFileNameWithoutExtension(avatarPath);

                if (_scaleSyncTarget == null)
                    _scaleSyncTarget = GameObject.Find("HandTrackerRoot");

                if (_rootObject != null)
                    Destroy(_rootObject);

                _rootObject = new GameObject("LoadAvatarRoot");
                _rootObject.transform.position = Vector3.zero;
                _rootObject.transform.rotation = Quaternion.identity;
                _rootObject.transform.localScale = Vector3.one;
                _rootSync = _rootObject.AddComponent<ScaleSync>();
                _rootSync.config = _config;
                _rootSync.TargetTransform = GameObject.Find("HandTrackerRoot").transform;

                string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

                AssetBundle asset = AssetBundle.LoadFromFile(avatarPath);
                asset.GetAllAssetNames().ToList().ForEach(x => Debug.Log(x));

                GameObject avatar = asset.LoadAsset<GameObject>(asset.GetAllAssetNames()[0]);
                asset.Unload(false);
                if (avatar != null && _vrmPose != null && _vrmModel != null)
                {
                    if (_avatarModel != null)
                        Destroy(_avatarModel);

                    _avatarModel = Instantiate(avatar, _vrmPose.transform.localPosition, _vrmPose.transform.localRotation);
                    _avatarModel.transform.localScale = _vrmPose.transform.localScale;

                    _avatarModel.transform.SetParent(_rootObject.transform);

                    _avatarRootConstraintScaleSync = _avatarModel.AddComponent<PositionConstraintScaleSync>();
                    _avatarRootConstraintScaleSync.TargetConstraintObject = _vrmPose;
                    _avatarRootConstraintScaleSync.TargetScaleReferenceObject = _scaleSyncTarget;
                    _avatarRootConstraintScaleSync.config = _config;
                    _avatarRootConstraintScaleSync.IsLocal = true;

                    var armature = _avatarModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.AddComponent<PositionConstraintScaleSync>();
                    armature.TargetConstraintObject = _vrmModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;
                    armature.TargetScaleReferenceObject = _scaleSyncTarget;
                    armature.IsLocal = true;

                    Renderer[] newRenderers = _avatarModel.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in newRenderers)
                    {
                        renderer.gameObject.layer = AvatarLayer;
                        if (_currentAvatarMeshSetting.meshSettings.Find(x => x.meshName == renderer.gameObject.name)?.isSync == true)
                        {
                            SetBlendshapeSync();
                        }
                    }

                    //床面調整
                    if (_vrmArmature != null)
                    {
                        var avatarFloorOffset = _config.avatarFloorOffsets.Find(x => x.avatarName == _avatarFileName);
                        if (avatarFloorOffset != null)
                        {
                            _vrmArmature.transform.localPosition = new Vector3(_vrmArmature.transform.localPosition.x, avatarFloorOffset.offset, _vrmArmature.transform.localPosition.z);
                        }
                        else
                        {
                            var floorHeight = GetFloorHeight(_avatarModel);
                            AvatarFloorOffset avatarOffset = new AvatarFloorOffset()
                            {
                                avatarName = _avatarFileName,
                                offset = floorHeight
                            };
                            _config.avatarFloorOffsets.Add(avatarOffset);
                            _vrmArmature.transform.localPosition = new Vector3(_vrmArmature.transform.localPosition.x, -floorHeight, _vrmArmature.transform.localPosition.z);
                        }
                    }

                    //VRM Mesh非表示
                    Renderer[] vrmRenderers = _vrmModel.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in vrmRenderers)
                    {
                        renderer.enabled = false;
                    }

                    Animator avatarAnimator = _avatarModel.GetComponent<Animator>();
                    Animator poseAnimator = _vrmPose.GetComponent<Animator>();

                    _restPose = true;

                    if (avatarAnimator != null && poseAnimator != null)
                    {

                        foreach (var bone in _boneArray)
                        {
                            if ((HumanBodyBones)bone == HumanBodyBones.LastBone)
                                continue;

                            var poseBone = poseAnimator.GetBoneTransform((HumanBodyBones)bone);
                            var avatarBone = avatarAnimator.GetBoneTransform((HumanBodyBones)bone);

                            if (poseBone != null && avatarBone != null)
                            {
                                var initialTrans = poseBone.gameObject.GetComponent<InitialTransform>();
                                if(initialTrans != null)
                                {
                                    poseBone.position = initialTrans.initialPosition;
                                    poseBone.rotation = initialTrans.initialRotation;

                                    var rot = avatarBone.gameObject.AddComponent<BoneConstraint>();
                                    rot.sourceTransform = initialTrans;
                                    rot.worldRotationAtRest = avatarBone.rotation;
                                }

                                if ((HumanBodyBones)bone == HumanBodyBones.Hips)
                                {
                                    //Hips高さ合わせ
                                    avatarBone.position = new Vector3(avatarBone.position.x, poseBone.position.y, avatarBone.position.z);

                                    var pos = avatarBone.gameObject.AddComponent<PositionConstraintScaleSync>();
                                    pos.TargetConstraintObject = poseBone.gameObject;
                                    pos.TargetScaleReferenceObject = _scaleSyncTarget;
                                }
                            }
                        }
                    }
                    _restPose = false;
                }
            }

        }

        private float GetFloorHeight(GameObject avatar)
        {
            float floorHeight = float.MaxValue;
            SkinnedMeshRenderer[] skinnedMeshRenderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var renderer in skinnedMeshRenderers)
            {
                Vector3[] vertices = renderer.sharedMesh.vertices;
                List<Vector3> worldVertices = new List<Vector3>();
                foreach (var vertex in vertices)
                    worldVertices.Add(renderer.gameObject.transform.TransformPoint(vertex));
                var min = worldVertices.Min(m => m.y);
                if (min < floorHeight)
                    floorHeight = min;
            }
            return floorHeight;
        }

        private void OnGUI()
        {
            if (_vrmModel != null && _vrmArmature != null && _displayUI)
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(240), GUILayout.Height(600)))
                    {
                        _config.avatarSelfScaling = GUILayout.Toggle(_config.avatarSelfScaling, "Avatar Self Scaling");

                        if (GUILayout.Button("\nAvatar Change\n"))
                        {
                            LoadAvatar();
                        }

                        GUILayout.Space(10);

                        var floor = GUILayout.Toggle(_floorDispay, "Display Dummy Floor");
                        if (floor != _floorDispay)
                        {
                            _floorDispay = floor;
                            _floorObject.SetActive(_floorDispay);
                        }

                        GUILayout.Label($"Height Adjust Offset : {_vrmArmature.transform.localPosition.y.ToString("0.###")}");

                        float offset = _vrmArmature.transform.localPosition.y;

                        using (new GUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button("<<"))
                                offset = offset - 0.01f;
                            if (GUILayout.Button("<"))
                                offset = offset - 0.001f;

                            if (GUILayout.Button(">"))
                                offset = offset + 0.001f;
                            if (GUILayout.Button(">>"))
                                offset = offset + 0.01f;
                        }
                        offset = GUILayout.HorizontalSlider(offset, -0.5f, 0.5f);

                        offset = Mathf.Clamp(offset, -0.5f, 0.5f);
                        if (offset != _vrmArmature.transform.localPosition.y)
                            _vrmArmature.transform.localPosition = new Vector3(_vrmArmature.transform.localPosition.x, offset, _vrmArmature.transform.localPosition.z);
                        if (_avatarModel != null)
                        {
                            GUILayout.Space(5);
                            using (new GUILayout.VerticalScope(GUI.skin.box))
                            {
                                var meshes = _avatarModel.GetComponentsInChildren<SkinnedMeshRenderer>(true);

                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.Label("Select footwear");
                                    if (GUILayout.Button("Adjust"))
                                    {
                                        var offsetValue = GetFloorHeight(meshes[_selectedSkinnedMeshToggle].gameObject);
                                        offsetValue = Mathf.Clamp(offsetValue, -0.5f, 0.5f);

                                        Debug.Log($"Selected Mesh : {meshes[_selectedSkinnedMeshToggle].gameObject.name} / Offset Value : {offsetValue}");
                                        _vrmArmature.transform.localPosition = new Vector3(_vrmArmature.transform.localPosition.x, -offsetValue, _vrmArmature.transform.localPosition.z);
                                    }
                                }
                                _skinnedMeshScrollPosition = GUILayout.BeginScrollView(_skinnedMeshScrollPosition, GUILayout.Height(150));
                                var toggleCount = 0;
                                foreach (var mesh in meshes)
                                {
                                    if (GUILayout.Toggle(_selectedSkinnedMeshToggle == toggleCount, mesh.name))
                                        _selectedSkinnedMeshToggle = toggleCount;
                                    toggleCount++;
                                }
                                GUILayout.EndScrollView();
                            }
                        }
                        if(_avatarFileName != null)
                            _config.avatarFloorOffsets.Find(x => x.avatarName == _avatarFileName).offset = offset;

                        GUILayout.Space(10);

                        using (new GUILayout.VerticalScope(GUI.skin.box))
                        {
                            GUILayout.Label("BlendShape Sync Mesh");
                            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(_avatarModel != null ? 150 : 300));
                            foreach (var meshSetting in _currentAvatarMeshSetting.meshSettings)
                            {
                                bool isSync = GUILayout.Toggle(meshSetting.isSync, meshSetting.meshName);
                                if (isSync != meshSetting.isSync)
                                {
                                    meshSetting.isSync = isSync;
                                    SaveConfig();
                                }
                            }
                            GUILayout.EndScrollView();
                        }

                        GUILayout.Space(10);

                        using (new GUILayout.VerticalScope())
                        {
                            GUILayout.Label("Floor Offset Sender");
                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.Label("Address");
                                GUILayout.Label("Port");
                                GUILayout.Label(" ");
                            }
                            using (new GUILayout.HorizontalScope())
                            {
                                _address = GUILayout.TextField(_address);
                                _port = int.Parse(GUILayout.TextField(_port.ToString()));
                                if (GUILayout.Button("Apply"))
                                {
                                    if(_address != _config.FloorOffsetSenderAddress || _port != _config.FloorOffsetPort)
                                    {
                                        _floorOffset.RemoveTask(_config.FloorOffsetPort);

                                        _config.FloorOffsetSenderAddress = _address;
                                        _config.FloorOffsetPort = _port;

                                        _floorOffset.AddSendTask();

                                    }
                                }
                            }
                        }
                        GUILayout.Space(10);
                        using (new GUILayout.HorizontalScope())
                        {
                            _config.DisplayUIatStartup = GUILayout.Toggle(_config.DisplayUIatStartup, "Display UI at Startup");
                            if (GUILayout.Button("x", GUILayout.Width(60))){
                                _displayUI = false;
                            }
                        }
                    }
                }
                GUILayout.EndArea();
            }
        }
    }
}
