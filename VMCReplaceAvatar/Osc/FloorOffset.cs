using FloorOffsetSender.Osc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VMC;

namespace VMCReplaceAvatar.Osc
{
    public class FloorOffset : MonoBehaviour
    {
        private GameObject _scaleSyncTarget;
        private float _currentScale;
        private Vector3 _hipsPosition;
        private Transform _hipsTransform;

        private int _port = 39740;

        internal List<SendTask> sendTasks = new List<SendTask>();

        internal class SendTask
        {
            internal float offset = 0f;
            internal int port = 0;
            internal OscClient client = null;
        }

        private void Start()
        {
            _scaleSyncTarget = GameObject.Find("HandTrackerRoot");
            _currentScale = _scaleSyncTarget.transform.localScale.y;

            VMCEvents.OnModelLoaded += OnModelLoaded;

        }

        private void OnDestroy()
        {
            foreach (SendTask sendTask in sendTasks)
            {
                RemoveTask(sendTask.port);
            }
        }

        private void OnModelLoaded(GameObject model)
        {
            var anim = model.GetComponent<Animator>();
            if (anim != null)
            {
                _hipsTransform = anim.GetBoneTransform(HumanBodyBones.Hips);
                _hipsPosition = _hipsTransform.localPosition;
                AddSendTask("127.0.0.1", _port);
            }
        }

        private void AddSendTask(string address = "127.0.0.1", int port = 39740)
        {
            if (_hipsTransform == null)
            {
                Debug.LogError($"Hips Transform Not Found.");
                return;
            }

            SendTask sendTask = new SendTask();
            sendTask.offset = (_hipsPosition.y - _hipsPosition.y * (1.0f / _currentScale)) * 0.95f;
            sendTask.port = port;
            sendTask.client = new OscClient(address, port);
            if (sendTask.client != null)
            {
                sendTasks.Add(sendTask);
            }
            else
                Debug.LogError($"Instance of OscClient Not Starting.");
        }

        internal void RemoveTask(int port)
        {
            foreach (SendTask sendTask in sendTasks)
            {
                if (sendTask.port == port)
                {
                    sendTasks.Remove(sendTask);
                    break;
                }
            }
        }

        private async Task SendData()
        {
            await Task.Run(() => {
                try
                {
                    foreach (SendTask sendTask in sendTasks)
                    {
                        sendTask.client.Send("/VMC/Ext/Floor", "Floor", new float[] {
                            sendTask.offset
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ExternalSender Thread : {e}");
                }
            });
        }

        private void LateUpdate()
        {
            if (_scaleSyncTarget != null && _hipsTransform != null)
            {
                if (_currentScale != _scaleSyncTarget.transform.localScale.y)
                {
                    _currentScale = _scaleSyncTarget.transform.localScale.y;

                    var diff = (_hipsPosition.y - _hipsPosition.y * (1.0f / _currentScale)) * 0.9f;
                    sendTasks.ForEach((sendTask) => {
                        sendTask.offset = diff;
                    });
                    Debug.Log($"Scale Changed : {_currentScale} {diff}");
                }
            }

            Task.Run(() => SendData());
        }
    }
}
