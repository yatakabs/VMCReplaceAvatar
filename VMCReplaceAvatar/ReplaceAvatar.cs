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
    Version: "0.0.1",
    Author: "snow",
    Description: "VRMを別のアバターモデルで置き換えるMod",
    AuthorURL: "https://twitter.com/snow_mil",
    PluginURL: "https://github.com/Snow1226")]
    public class ReplaceAvatar : MonoBehaviour
    {
        private Config _config;
        private VRMAvatarMeshSetting _currentAvatarMeshSetting;
        private GameObject _currentVRMModel = null;
        private GameObject _loadedAvatarModel = null;
        private GameObject _vrmInitialPose = null;
        private GameObject _rootObject = null;

        private GameObject _scaleSyncTarget = null;

        private string _currentModelName = string.Empty;
        private const int AvatarLayer = 3;

        private Vector2 _scrollPosition = Vector2.zero;

        private ScaleSync _rootSync = null;
        private PositionConstraintScaleSync _avatarRootConstraintScaleSync = null;

        private SelfScaling _selfScaling;
        private GameObject _floorObject;
        private bool _floorDispay = false;
        private GameObject _vrmArmature;

        private void Awake()
        {
            LoadSetting();
            VMCEvents.OnModelLoaded += OnModelLoaded;

            _selfScaling = new GameObject("AvatarSelfScaling").AddComponent<SelfScaling>();
            _selfScaling.config = _config;

            _floorObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _floorObject.transform.position = Vector3.zero;
            _floorObject.transform.rotation = Quaternion.identity;
            _floorObject.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            _floorObject.SetActive(_floorDispay);
        }

        private void OnDestroy()
        {
            VMCEvents.OnModelLoaded -= OnModelLoaded;
            SaveSetting();
        }


        private void LoadSetting()
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

        private void SaveSetting()
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

        private void SetBlendshapeSync()
        {
            if (_currentVRMModel == null || _loadedAvatarModel == null) return;

            _loadedAvatarModel.GetComponentsInChildren<BlendShapeSync>(true).ToList().ForEach(x => DestroyImmediate(x));
            Renderer[] newRenderers = _loadedAvatarModel.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in newRenderers)
            {
                if (_currentAvatarMeshSetting.meshSettings.Find(x => x.meshName == renderer.gameObject.name)?.isSync == true)
                {
                    BlendShapeSync sync = renderer.gameObject.AddComponent<BlendShapeSync>();
                    sync.sourceRenderer = _currentVRMModel.GetComponentsInChildren<Renderer>(true).FirstOrDefault(x => x.gameObject.name == renderer.gameObject.name);
                }
            }
        }

        private void OnModelLoaded(GameObject currentModel)
        {
            if(_vrmInitialPose != null)
            {
                Destroy(_vrmInitialPose);
                _vrmInitialPose = null;
            }

            if (_scaleSyncTarget == null)
                _scaleSyncTarget = GameObject.Find("HandTrackerRoot");

            if (_rootObject != null)
            {
                Destroy(_rootObject);
                _rootObject = null;
            }

            _currentVRMModel = currentModel;
            _currentModelName = currentModel.GetComponent<VRMMeta>().Meta.Title;

            //VRM初期ポーズを複製
            Animator vrmAnimator = _currentVRMModel.GetComponent<Animator>();
            _vrmArmature = vrmAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;

            _vrmInitialPose = InstantiateArmature(currentModel, "VRM Initial Pose");
            

            Debug.Log("Model Loaded: " + _currentModelName);

            var avatarMeshSetting = _config.vrmAvatarMeshSettings.Find(x => x.avatarName == _currentModelName);

            if (avatarMeshSetting != null)
            {
                _currentAvatarMeshSetting = avatarMeshSetting;
            }
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
                SaveSetting();
            }
        }

        private GameObject InstantiateArmature(GameObject armatureObject,string objectName)
        {
            var retObj = Instantiate(armatureObject, armatureObject.transform.position, armatureObject.transform.rotation);
            retObj.name = objectName;
            Renderer[] renderers = retObj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
                Destroy(renderer.gameObject);

            var comps = retObj.GetComponentsInChildren<Behaviour>(true);
            var exclusionList = new string[] { "Animator" };
            foreach (var comp in comps)
            {
                if(comp != null)
                {
                    foreach (string d in exclusionList)
                    {
                        if (!comp.GetType().Name.ToLower().Contains(d.ToLower()))
                            GameObject.Destroy(comp);
                    }
                }
            }
            return retObj;
        }


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                _config.avatarSelfScaling = !_config.avatarSelfScaling;
                SaveSetting();
            }
        }

        private void LoadAvatar()
        {
            var avatarPath = WindowsDialogs.OpenFileDialog("Select Avatar", ".avatar");

            if (avatarPath != null)
            {
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
                if (avatar != null)
                {
                    if (_loadedAvatarModel != null)
                        Destroy(_loadedAvatarModel);

                    _loadedAvatarModel = Instantiate(avatar, _currentVRMModel.transform.position, _currentVRMModel.transform.rotation);
                    _loadedAvatarModel.transform.localScale = _currentVRMModel.transform.localScale;

                    _loadedAvatarModel.transform.SetParent(_rootObject.transform);

                    _avatarRootConstraintScaleSync = _loadedAvatarModel.AddComponent<PositionConstraintScaleSync>();
                    _avatarRootConstraintScaleSync.TargetConstraintObject = _currentVRMModel;
                    _avatarRootConstraintScaleSync.TargetScaleReferenceObject = _scaleSyncTarget;
                    _avatarRootConstraintScaleSync.config = _config;
                    _avatarRootConstraintScaleSync.IsLocal = false;

                    var armature = _loadedAvatarModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.AddComponent<PositionConstraintScaleSync>();
                    armature.TargetConstraintObject = _currentVRMModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;
                    armature.TargetScaleReferenceObject = _scaleSyncTarget;
                    armature.IsLocal = true;

                    Renderer[] newRenderers = _loadedAvatarModel.GetComponentsInChildren<Renderer>(true);
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
                        var floorHeight = GetFloorHeight(_loadedAvatarModel);
                        _vrmArmature.transform.localPosition = new Vector3(_vrmArmature.transform.localPosition.x, -floorHeight, _vrmArmature.transform.localPosition.z);
                    }

                    //VRM Mesh非表示
                    Renderer[] vrmRenderers = _currentVRMModel.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in vrmRenderers)
                    {
                        renderer.enabled = false;
                    }

                    Animator avatarAnimator = _loadedAvatarModel.GetComponent<Animator>();
                    Animator vrmAnimator = _currentVRMModel.GetComponent<Animator>();
                    Array boneArray = Enum.GetValues(typeof(HumanBodyBones));

                    if (avatarAnimator != null && vrmAnimator != null)
                    {
                        foreach (var bone in boneArray)
                        {
                            if ((HumanBodyBones)bone == HumanBodyBones.LastBone)
                                continue;

                            var vrmBone = vrmAnimator.GetBoneTransform((HumanBodyBones)bone);
                            var avatarBone = avatarAnimator.GetBoneTransform((HumanBodyBones)bone);

                            if (vrmBone != null && avatarBone != null)
                            {
                                //初期回転合わせ ※キャリブレーション後、VRM1ではおかしくなるので要変更
                                avatarBone.localEulerAngles = avatarBone.localEulerAngles + vrmBone.localEulerAngles;

                                var rot = avatarBone.gameObject.AddComponent<RotationConstraint>();
                                rot.weight = 1;
                                rot.AddSource(new ConstraintSource() { sourceTransform = vrmBone, weight = 1 });

                                /*
                                // Activateボタンリフレクション　なぜかオフセットが正常な値にならない。
                                var activate = typeof(RotationConstraint).GetMethod("ActivateAndPreserveOffset", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (activate != null)
                                    activate.Invoke(rot, null);
                                */

                                rot.rotationAtRest = avatarBone.localEulerAngles;
                                rot.rotationOffset = (Quaternion.Inverse(vrmBone.rotation) * avatarBone.rotation).eulerAngles;
                                rot.locked = true;
                                rot.constraintActive = true;
                                rot.enabled = true;

                                if ((HumanBodyBones)bone == HumanBodyBones.Hips)
                                {
                                    //Hips高さ合わせ
                                    avatarBone.position = new Vector3(avatarBone.position.x, vrmBone.position.y, avatarBone.position.z);

                                    var pos = avatarBone.gameObject.AddComponent<PositionConstraintScaleSync>();
                                    pos.TargetConstraintObject = vrmBone.gameObject;
                                    pos.TargetScaleReferenceObject = _scaleSyncTarget;
                                }
                            }
                        }
                    }
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
            if (_currentVRMModel != null && _config.alwaysDisplayGUI)
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(240), GUILayout.Height(400)))
                    {
                        _config.avatarSelfScaling = GUILayout.Toggle(_config.avatarSelfScaling, "Avatar Self Scaling");

                        if (GUILayout.Button("\nAvatar Change\n"))
                        {
                            LoadAvatar();
                        }

                        GUILayout.Space(10);

                        var floor = GUILayout.Toggle(_floorDispay, "Display Dummy Floor");
                        if(floor != _floorDispay)
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


                        GUILayout.Space(10);

                        using (new GUILayout.VerticalScope(GUI.skin.box))
                        {
                            GUILayout.Label("BlendShape Sync Mesh");
                            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
                            foreach (var meshSetting in _currentAvatarMeshSetting.meshSettings)
                            {
                                bool isSync = GUILayout.Toggle(meshSetting.isSync, meshSetting.meshName);
                                if (isSync != meshSetting.isSync)
                                {
                                    meshSetting.isSync = isSync;
                                    SaveSetting();
                                }
                            }
                            GUILayout.EndScrollView();
                        }
                    }
                }
                GUILayout.EndArea();
            }
        }
    }
}
