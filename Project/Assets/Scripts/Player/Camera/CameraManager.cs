using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Unity.VisualScripting;

namespace CameraSystem
{
    public class CameraManager : MonoBehaviour
    {
        static List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

        public static CinemachineVirtualCamera activeCamera = null;

        [SerializeField] private CameraScriptable cameraValues;

        [SerializeField] private Transform baseFollowObject;
        public bool forcedTransition;

        public static CameraManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void setNewFollowTarget(Transform newTarget)
        {
            foreach (var cam in cameras)
            {
                cam.Follow = newTarget;
            }
        }

        public void ChangeCamera(CinemachineVirtualCamera camera)
        {
            if(activeCamera == camera) return;
            StopAllCoroutines();
            StartCoroutine(CameraChangeCheck(camera));
        }
        private IEnumerator CameraChangeCheck(CinemachineVirtualCamera newCamera)
        {
            CineCameraType activeCam = activeCamera ? activeCamera.GetComponent<CamScript>().cameraType: CineCameraType.Regular;
            CineCameraType newCam = newCamera.GetComponent<CamScript>().cameraType;
            if (activeCam == CineCameraType.Empty && newCam != CineCameraType.Empty && !forcedTransition) yield break;

            float waitingAmount = newCamera.gameObject.GetComponent<CamScript>().waitingAmount;
            activeCamera = newCamera;
            yield return new WaitForSeconds(waitingAmount);

            newCamera.Priority = 10;

            foreach (CinemachineVirtualCamera cam in cameras) 
            { 
                if(cam != newCamera) cam.Priority = 0; 
            }
            forcedTransition = false;
        }

        public void ForceChangeCamera(CinemachineVirtualCamera camera)
        {
            StartCoroutine(DoForcedCameraChange(camera));
        }
        private IEnumerator DoForcedCameraChange(CinemachineVirtualCamera camera)
        {
            forcedTransition = true;
            ChangeCamera(camera);
            yield return null;
            forcedTransition = false;
        }

        public void RegisterCamera(CinemachineVirtualCamera camera)
        {
            cameras.Add(camera);
            setNewFollowTarget(baseFollowObject);
        }

        public void UnregisterCamera(CinemachineVirtualCamera camera)
        {
            cameras.Remove(camera);
        }

        public bool IsCameraActive(CinemachineVirtualCamera camera) => camera == activeCamera;

        public CinemachineVirtualCamera SearchCamera(CineCameraType cameraType)
        {
            foreach (CinemachineVirtualCamera cam in cameras)
            {
                if (cam.GetComponent<CamScript>().cameraType == cameraType) return cam;
            }
            return null;
        }
    }
}