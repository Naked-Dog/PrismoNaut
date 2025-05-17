using System;
using Cinemachine;
using UnityEngine;

namespace CameraSystem
{
    public class CamScript : MonoBehaviour
    {
        public float waitingAmount;

        public CineCameraType cameraType;

        private void Start()
        {
            //Debug.Log(CameraManager.Instance);
            CameraManager.Instance.RegisterCamera(GetComponent<CinemachineVirtualCamera>());
        }
        private void OnDisable()
        {
            CameraManager.Instance.UnregisterCamera(GetComponent<CinemachineVirtualCamera>());
        }
    }

    public enum CineCameraType
    {
        Regular = 0,
        Falling,
        Dialogue,
        LookDown,
        LookUp,
        Empty,
    }
}
