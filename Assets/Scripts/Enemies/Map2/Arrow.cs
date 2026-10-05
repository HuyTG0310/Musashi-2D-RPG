using UnityEngine;

namespace Assets.Scripts.Enemies.Map2
{
    public class Arrow : MonoBehaviour
    {
        public float speed = 10f;
        public float damage = 25f;
        public float lifetime = 3f;
        private Rigidbody2D rb;
        private bool movingRight = true;

        public void Setup(bool facingRight, float arrowDamage)
        {
            movingRight = facingRight;
            damage = arrowDamage;

            // Xoay sprite mũi tên theo hướng bắn
            float absX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(movingRight ? absX : -absX, transform.localScale.y, transform.localScale.z);

            Destroy(gameObject, lifetime);
        }

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float dir = movingRight ? 1f : -1f;
                rb.velocity = new Vector2(dir * speed, 0);
            }
        }

        void Update()
        {
            // Đảm bảo duy trì vận tốc bay nếu không dùng gravity
            if (rb != null)
            {
                float dir = movingRight ? 1f : -1f;
                rb.velocity = new Vector2(dir * speed, rb.velocity.y);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            // Kiểm tra xem có trúng Musashi (Player) không
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }

            // Trúng mặt đất hoặc chướng ngại vật
            if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                Destroy(gameObject);
            }
        }
    }
}
