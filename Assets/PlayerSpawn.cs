using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    void Start()
    {
        if (Player.instance == null)
        {
            Player p = FindAnyObjectByType<Player>();
            if (p != null)
            {
                Player.instance = p;
            }
        }

        // Kiểm tra xem Musashi có tồn tại không trước khi dịch chuyển
        if (Player.instance != null)
        {
            Player.instance.transform.position = transform.position;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Player.instance!");
        }
    }
}
