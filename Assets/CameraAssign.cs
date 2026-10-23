using Cinemachine;
using UnityEngine;

public class CameraAssign : MonoBehaviour
{
    void Start()
    {
        // Khi Map 2 load xong, tự động tìm Musashi và gán vào mục tiêu Follow của Camera
        if (Player.instance != null)
        {
            CinemachineVirtualCamera vcam = GetComponent<CinemachineVirtualCamera>();
            vcam.Follow = Player.instance.transform;
        }
    }
}
