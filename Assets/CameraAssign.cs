using Cinemachine;
using UnityEngine;

public class CameraAssign : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    void Start()
    {
        TryAssign();
    }

    void Update()
    {
        if (vcam != null && vcam.Follow == null)
        {
            TryAssign();
        }
    }

    private void TryAssign()
    {
        if (Player.instance != null && vcam != null)
        {
            vcam.Follow = Player.instance.transform;
        }
        else if (vcam != null && vcam.Follow == null)
        {
            Player p = FindAnyObjectByType<Player>();
            if (p != null)
            {
                vcam.Follow = p.transform;
            }
        }
    }
}
