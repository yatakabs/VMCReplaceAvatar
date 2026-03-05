using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
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
        private Setting _setting;
        private VRMAvatarMeshSetting _currentAvatarMeshSetting;
        private GameObject _currentVRMModel = null;
        private GameObject _loadedAvatarModel = null;
        private GameObject _initialPose = null;
        private GameObject _rootObject = null;
        private string _currentModelName = string.Empty;
        private const int AvatarLayer = 3;

        private Vector2 _scrollPosition = Vector2.zero;

        private PositionConstraintScaleSync _positionConstraintScaleSync = null;
        private ScaleSync _rootSync = null;
        private PositionConstraintScaleSync _avatarRootConstraintScaleSync = null;

        private SelfScaling _selfScaling;

        private void Awake()
        {
            LoadSetting();
            VMCEvents.OnModelLoaded += OnModelLoaded;
            VMCEvents.OnCameraChanged += OnCameraChanged;

            _selfScaling = new GameObject("AvatarSelfScaling").AddComponent<SelfScaling>();
            _selfScaling.Setting = _setting;
        }

        private void OnDestroy()
        {
            VMCEvents.OnModelLoaded -= OnModelLoaded;
            VMCEvents.OnCameraChanged -= OnCameraChanged;

            SaveSetting();
        }

        private void LoadSetting()
        {
            string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            if (File.Exists(Path.Combine(dllDirectory, "VMCAvatarChange.json")))
                _setting = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(Path.Combine(dllDirectory, "VMCAvatarChange.json")));
            else
            {
                _setting = new Setting();
                File.WriteAllText(Path.Combine(dllDirectory, "VMCAvatarChange.json"), JsonConvert.SerializeObject(_setting, Formatting.Indented));
            }
        }

        private void SaveSetting()
        {
            var avatarMeshSetting = _setting.vrmAvatarMeshSettings.FindIndex(x => x.avatarName == _currentModelName);
            if (avatarMeshSetting >= 0)
            {
                _setting.vrmAvatarMeshSettings[avatarMeshSetting] = _currentAvatarMeshSetting;
            }
            else
            {
                _setting.vrmAvatarMeshSettings.Add(_currentAvatarMeshSetting);
            }

            string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;
            File.WriteAllText(Path.Combine(dllDirectory, "VMCAvatarChange.json"), JsonConvert.SerializeObject(_setting, Formatting.Indented));

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
            if (_rootObject != null)
                Destroy(_rootObject);

            _currentVRMModel = currentModel;
            _currentModelName = currentModel.GetComponent<VRMMeta>().Meta.Title;

            //VRM初期ポーズを複製
            Animator vrmAnimator = _currentVRMModel.GetComponent<Animator>();
            var armature = vrmAnimator.GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;
            _initialPose = Instantiate(armature, armature.transform.position, armature.transform.rotation);

            Debug.Log("Model Loaded: " + _currentModelName);

            var avatarMeshSetting = _setting.vrmAvatarMeshSettings.Find(x => x.avatarName == _currentModelName);

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
                _setting.vrmAvatarMeshSettings.Add(_currentAvatarMeshSetting);
                SaveSetting();
            }
        }

        private void OnCameraChanged(Camera currentCamera)
        {

        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                _setting.avatarSelfScaling = !_setting.avatarSelfScaling;
                if (_rootSync) _rootSync.IsSync = _setting.avatarSelfScaling;
                if (_positionConstraintScaleSync) _positionConstraintScaleSync.IsSync = _setting.avatarSelfScaling;
                if (_avatarRootConstraintScaleSync) _avatarRootConstraintScaleSync.IsSync = _setting.avatarSelfScaling;
            }
        }

        private void LoadAvatar()
        {
            var avatarPath = WindowsDialogs.OpenFileDialog("Select Avatar", ".avatar");

            if (avatarPath != null)
            {
                if (_rootObject != null)
                    Destroy(_rootObject);

                _rootObject = new GameObject("LoadAvatarRoot");
                _rootObject.transform.position = Vector3.zero;
                _rootObject.transform.rotation = Quaternion.identity;
                _rootObject.transform.localScale = Vector3.one;
                _rootSync = _rootObject.AddComponent<ScaleSync>();
                _rootSync.IsSync = _setting.avatarSelfScaling;
                _rootSync.TargetTransform = GameObject.Find("HandTrackerRoot").transform;

                string dllDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).FullName;

                AssetBundle asset = AssetBundle.LoadFromFile(avatarPath);
                asset.GetAllAssetNames().ToList().ForEach(x => Debug.Log(x));

                GameObject avatar = asset.LoadAsset<GameObject>(asset.GetAllAssetNames()[0]);
                asset.Unload(false);
                if (avatar != null)
                {
                    if (_loadedAvatarModel != null)
                        DestroyImmediate(_loadedAvatarModel);

                    _loadedAvatarModel = Instantiate(avatar, _currentVRMModel.transform.position, _currentVRMModel.transform.rotation);
                    _loadedAvatarModel.transform.localScale = _currentVRMModel.transform.localScale;
                    _loadedAvatarModel.transform.SetParent(_rootObject.transform);

                    _avatarRootConstraintScaleSync = _loadedAvatarModel.AddComponent<PositionConstraintScaleSync>();
                    _avatarRootConstraintScaleSync.TargetConstraintObject = _currentVRMModel;
                    _avatarRootConstraintScaleSync.TargetScaleReferenceObject = GameObject.Find("HandTrackerRoot");
                    _avatarRootConstraintScaleSync.IsSync = _setting.avatarSelfScaling;
                    _avatarRootConstraintScaleSync.IsLocal = false;

                    var armature = _loadedAvatarModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject.AddComponent<PositionConstraintScaleSync>();
                    armature.TargetConstraintObject = _currentVRMModel.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Hips).parent.gameObject;
                    armature.TargetScaleReferenceObject = GameObject.Find("HandTrackerRoot");
                    armature.IsLocal = true;
                    armature.IsSync = false;

                    Renderer[] newRenderers = _loadedAvatarModel.GetComponentsInChildren<Renderer>(true);
                    foreach (var renderer in newRenderers)
                    {
                        renderer.gameObject.layer = AvatarLayer;
                        if (_currentAvatarMeshSetting.meshSettings.Find(x => x.meshName == renderer.gameObject.name)?.isSync == true)
                        {
                            SetBlendshapeSync();
                        }
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
                                    pos.TargetScaleReferenceObject = GameObject.Find("HandTrackerRoot");
                                    pos.IsSync = false;
                                }
                            }
                        }
                    }
                }
            }

        }

        private void OnGUI()
        {
            if (_currentVRMModel != null && _setting.alwaysDisplayGUI)
            {
                GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new GUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(240), GUILayout.Height(600)))
                    {
                        _setting.avatarSelfScaling = GUILayout.Toggle(_setting.avatarSelfScaling, "Avatar Self Scaling");
                        if (_rootObject != null)
                            _rootObject.GetComponent<ScaleSync>().IsSync = _setting.avatarSelfScaling;
                        if (_rootSync) _rootSync.IsSync = _setting.avatarSelfScaling;
                        if (_positionConstraintScaleSync) _positionConstraintScaleSync.IsSync = _setting.avatarSelfScaling;
                        if (_avatarRootConstraintScaleSync) _avatarRootConstraintScaleSync.IsSync = _setting.avatarSelfScaling;


                        if (GUILayout.Button("\nAvatar Change\n"))
                        {
                            LoadAvatar();
                        }
                        using (new GUILayout.VerticalScope(GUI.skin.box))
                        {
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
