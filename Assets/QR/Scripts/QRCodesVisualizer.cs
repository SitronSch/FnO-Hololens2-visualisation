﻿
using Microsoft.MixedReality.SampleQRCodes;
using System.Collections.Generic;
using UnityEngine;

namespace QRTracking
{
    public class QRCodesVisualizer : MonoBehaviour, IQRUpdate
    {
        public GameObject qrCodePrefab;

        private SortedDictionary<System.Guid, GameObject> qrCodesObjectsList;
        private Queue<ActionData> pendingActions = new Queue<ActionData>();
        private bool clearExisting = false;

        bool _enableQrUpdate = true;

        [SerializeField] bool _useSceneObjectAsPrefab = false;

        struct ActionData
        {
            public enum Type
            {
                Added,
                Updated,
                Removed
            };
            public Type type;
            public Microsoft.MixedReality.QR.QRCode qrCode;

            public ActionData(Type type, Microsoft.MixedReality.QR.QRCode qRCode) : this()
            {
                this.type = type;
                qrCode = qRCode;
            }
        }

        // Use this for initialization
        void Start()
        {
            Debug.Log("QRCodesVisualizer start");
            qrCodesObjectsList = new SortedDictionary<System.Guid, GameObject>();

            QRCodesManager.Instance.QRCodesTrackingStateChanged += Instance_QRCodesTrackingStateChanged;
            QRCodesManager.Instance.QRCodeAdded += Instance_QRCodeAdded;
            QRCodesManager.Instance.QRCodeUpdated += Instance_QRCodeUpdated;
            QRCodesManager.Instance.QRCodeRemoved += Instance_QRCodeRemoved;
            if (qrCodePrefab == null)
            {
                throw new System.Exception("Prefab not assigned");
            }
        }
        private void Instance_QRCodesTrackingStateChanged(object sender, bool status)
        {
            if (!status)
            {
                clearExisting = true;
            }
        }

        private void Instance_QRCodeAdded(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeAdded");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Added, e.Data));
            }
        }

        private void Instance_QRCodeUpdated(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            if (_enableQrUpdate)
            {
                Debug.Log("QRCodesVisualizer Instance_QRCodeUpdated");

                lock (pendingActions)
                {
                    pendingActions.Enqueue(new ActionData(ActionData.Type.Updated, e.Data));
                }
            }
        }

        private void Instance_QRCodeRemoved(object sender, QRCodeEventArgs<Microsoft.MixedReality.QR.QRCode> e)
        {
            Debug.Log("QRCodesVisualizer Instance_QRCodeRemoved");

            lock (pendingActions)
            {
                pendingActions.Enqueue(new ActionData(ActionData.Type.Removed, e.Data));
            }
        }

        private void HandleEvents()
        {
            lock (pendingActions)
            {
                while (pendingActions.Count > 0)
                {
                    var action = pendingActions.Dequeue();
                    Debug.Log($"QRCodesVisualizer HandleEvents: {action.type}");

                    if(action.type==ActionData.Type.Added&& action.qrCode.LastDetectedTime < QRCodesManager.Instance.WatcherStart)
                    {
                        foreach(var i in qrCodesObjectsList)
                        {
                            Destroy(i.Value);
                        }
                        qrCodesObjectsList.Clear();

                    }
                    else if (action.type == ActionData.Type.Added)
                    {
                        if(_useSceneObjectAsPrefab)
                        {           
                            qrCodePrefab.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodePrefab.GetComponent<QRCode>().qrCode = action.qrCode;
                            qrCodesObjectsList.Add(action.qrCode.Id, qrCodePrefab);
                        }
                        else
                        {
                            GameObject qrCodeObject = Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                            qrCodeObject.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                            qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;
                            qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                        }                
                    }
                    else if (action.type == ActionData.Type.Updated)
                    {
                        if (!qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            if (_useSceneObjectAsPrefab)
                            {
                                qrCodePrefab.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                                qrCodePrefab.GetComponent<QRCode>().qrCode = action.qrCode;
                                qrCodesObjectsList.Add(action.qrCode.Id, qrCodePrefab);
                            }
                            else
                            {
                                GameObject qrCodeObject = Instantiate(qrCodePrefab, new Vector3(0, 0, 0), Quaternion.identity);
                                qrCodeObject.GetComponent<SpatialGraphNodeTracker>().Id = action.qrCode.SpatialGraphNodeId;
                                qrCodeObject.GetComponent<QRCode>().qrCode = action.qrCode;
                                qrCodesObjectsList.Add(action.qrCode.Id, qrCodeObject);
                            }        
                        }
                    }
                    else if (action.type == ActionData.Type.Removed)
                    {
                        if (qrCodesObjectsList.ContainsKey(action.qrCode.Id))
                        {
                            Destroy(qrCodesObjectsList[action.qrCode.Id]);
                            qrCodesObjectsList.Remove(action.qrCode.Id);
                        }
                    }
                }
            }
            if (clearExisting)
            {
                clearExisting = false;
                foreach (var obj in qrCodesObjectsList)
                {
                    Destroy(obj.Value);
                }
                qrCodesObjectsList.Clear();

            }
        }

        // Update is called once per frame
        void Update()
        {
            HandleEvents();
        }
        public void EnableQRUpdate(bool value)
        {
            _enableQrUpdate = value;
        }
    
    }

}