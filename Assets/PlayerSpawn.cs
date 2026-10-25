using Cinemachine;
using UnityEngine;

public class PlayerSpawn : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Vector3 defaultSpawnPosition = new Vector3(3.5f, 19.7f, 0f);

    void Start()
    {
        // Tìm Player.instance (có thể từ DontDestroyOnLoad scene trước)
        if (Player.instance == null)
        {
            Player p = FindAnyObjectByType<Player>();
            if (p != null)
            {
                Player.instance = p;
            }
        }

        if (Player.instance != null)
        {
            // Dừng velocity để tránh Musashi bị văng sau khi teleport
            Rigidbody2D rb = Player.instance.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            // Tự động bảo vệ: nếu tọa độ PlayerSpawn trong Scene cũ bị thấp (dưới hố chông Y < 18)
            // hoặc âm (map cũ), luôn tự động đưa lên vị trí cầu gỗ chuẩn (Y = 19.7)
            Vector3 finalPos = transform.position;
            if (finalPos.y < 18f || finalPos.x < 0f)
            {
                finalPos = defaultSpawnPosition;
            }

            Vector3 delta = finalPos - Player.instance.transform.position;
            Player.instance.transform.position = finalPos;

            // Tự động gán và warp Cinemachine camera theo Musashi
            CinemachineVirtualCamera vcam = FindAnyObjectByType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = Player.instance.transform;
                vcam.OnTargetObjectWarped(Player.instance.transform, delta);
            }

            Debug.Log($"[PlayerSpawn] Đã dịch chuyển Musashi đến vị trí chuẩn: {finalPos}");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawn] Không tìm thấy Player.instance!");
        }
    }
}
