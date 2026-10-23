using Cinemachine;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        // Kiểm tra xem Musashi có tồn tại không trước khi dịch chuyển
        if (Player.instance != null)
        {
            Player.instance.transform.position = transform.position;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Player.instance! Nếu bạn đang test trực tiếp ở Map 2, hãy kéo 1 Prefab Musashi thả tạm vào Scene.");
        }
    }
}
