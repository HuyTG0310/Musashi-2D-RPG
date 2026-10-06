using UnityEngine;

public class Shuriken : MonoBehaviour
{
    [Header("Movement Settings")]
    public float rotateSpeed = 600;
    public float moveSpeed = 5;
    public bool movingRight;

    [Header("Combat Settings")]
    public float damage = 100;
    public float lifetime = 3f;      // Thời gian tự hủy nếu bay trượt
    public GameObject hitEffect;     // (Tùy chọn) Hiệu ứng nổ/máu

    private Rigidbody2D rb;
    private Player player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindAnyObjectByType<Player>();

        // lấy hướng ném của phi tiêu khi khởi tạo dựa vào hướng của player
        movingRight = player.facingRight;

        // Bổ sung: Tự động hủy phi tiêu sau vài giây để khỏi bị rác bộ nhớ
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Xoay shuriken (Tôi thêm dấu trừ để nếu bạn ném sang trái nó sẽ lật chiều xoay cho tự nhiên hơn, bạn có thể chỉnh lại nếu không thích)
        float rotationDirection = movingRight ? -1f : 1f;
        transform.Rotate(0, 0, rotateSpeed * rotationDirection * Time.deltaTime);

        // Di chuyển
        if (movingRight)
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        }
    }

    // ==========================================
    // PHẦN BỔ SUNG: XỬ LÝ VA CHẠM VÀ GÂY SÁT THƯƠNG
    // ==========================================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. Kiểm tra xem thứ vừa chạm có phải là Enemy (Rat, Bat, Slime...) không
        EnemyBase enemy = collision.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage); // Gọi hàm trừ máu
            Explode();                // Phi tiêu biến mất
            return;
        }

        // 2. Kiểm tra xem phi tiêu có cắm vào mặt đất / tường không
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Sinh ra hiệu ứng tóe lửa/máu (nếu bạn có kéo prefab vào Inspector)
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Xóa phi tiêu khỏi màn hình
        Destroy(gameObject);
    }
}