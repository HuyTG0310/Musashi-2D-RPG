using UnityEngine;

public class Coin : MonoBehaviour
{
    public int coinValue = 1;
    public AudioClip pickupSound;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là Player không
        Player player = collision.GetComponent<Player>();

        if (player != null)
        {
            player.AddCoin(coinValue); // Cộng tiền cho Musashi

            // Phát âm thanh "Ting!" (nếu bạn có dùng AudioManager)
            if (pickupSound != null)
            {
                FindAnyObjectByType<AudioManager>()?.PlayerSFX(pickupSound);
            }

            // Xóa đồng xu khỏi màn hình
            Destroy(gameObject);
        }
    }
}
