using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    // Viên lửa do FireWorm phun ra. Bay thẳng (animation "Move"), rồi tự nổ (animation "Explosion")
    // khi: chạm player, HOẶC chạm tường, HOẶC bay đủ xa (hết maxDistance).
    // Gắn script này vào Prefab "FireProjectile". Prefab cần có:
    // SpriteRenderer, Animator (Move/Explosion), Rigidbody2D (Kinematic), Collider2D (Is Trigger).
    public class FireProjectile : MonoBehaviour
    {
        [Header("Fire Projectile Movement")]
        public float speed = 6f;
        public float maxDistance = 6f;  // bay xa quá khoảng này thì tự nổ dù không trúng gì

        [Header("Fire Projectile Damage")]
        public float explosionRadius = 1.5f; // bán kính AoE lúc nổ
        public float damage = 25f;
        public LayerMask playerLayer;
        public LayerMask wallLayer;          // layer tường/nền để biết khi nào "chạm tường"

        [Header("Fire Projectile Sounds")]
        public AudioClip explosionSound;

        private Animator anim;
        private Vector3 startPosition;
        private float direction = 1f;
        private bool hasExploded = false;


        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        private void Start()
        {
            startPosition = transform.position;
        }

        // Gọi ngay sau khi Instantiate, để set hướng bay đúng theo phía FireWorm đang quay mặt
        public void SetDirection(float dir)
        {
            direction = dir;
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void Update()
        {
            if (hasExploded) return;

            // di chuyển thẳng theo hướng đã set
            transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);

            // kiểm tra đã bay đủ xa maxDistance chưa, nếu có thì tự nổ
            float distanceTraveled = Vector3.Distance(startPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                Explode();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasExploded) return;

            // trúng player
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                Explode();
                return;
            }

            // trúng tường
            if (((1 << other.gameObject.layer) & wallLayer) != 0)
            {
                Explode();
                return;
            }
        }

        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            // dừng bay ngay lập tức
            if (anim != null)
            {
                anim.SetTrigger("explode");
            }

            // gây damage AoE ngay tại vị trí nổ
            Collider2D hitPlayer = Physics2D.OverlapCircle(transform.position, explosionRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player playerHit = hitPlayer.GetComponentInParent<Player>();
                if (playerHit != null)
                {
                    playerHit.TakeDamage(damage);
                }
            }

            if (explosionSound != null)
            {
                AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            }
        }

        // Gọi bằng Animation Event ở frame cuối clip Explosion, để tự huỷ viên lửa sau khi hiệu ứng nổ chạy xong
        public void DestroyAfterExplosion()
        {
            Destroy(gameObject);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}