using UnityEngine;

namespace Assets.Scripts.Enemies.Map3
{
    public class Huntress1 : EnemyBase
    {
        [Header("Huntress1 Ranges")]
        public float chaseRange = 8f;    // tầm phát hiện + đuổi theo player
        public float throwRange = 5f;    // trong khoảng này (và ngoài meleeRange) thì đứng ném spear
        public float meleeRange = 1.3f;  // player áp sát hơn khoảng này thì chuyển sang combo cận chiến

        // đang thực hiện 1 chuỗi hành động (combo cận chiến HOẶC ném spear) thì không làm gì khác
        private bool isBusy = false;

        [Header("Huntress1 Movement")]
        public bool facingRight = true;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float patrolDistance = 3f;
        private Vector3 startPosition;

        [Header("Huntress1 Attack Point (dùng chung cho cả melee lẫn ném spear)")]
        public Transform attackPoint;

        [Header("Huntress1 Melee Combo (3 đòn liên tiếp)")]
        public float meleeRadius = 0.6f;
        public float meleeDamageHit1 = 10f;
        public float meleeDamageHit2 = 10f;
        public float meleeDamageHit3 = 20f; // đòn cuối combo thường mạnh hơn
        public float meleeCooldown = 2.5f;  // hồi chiêu giữa 2 lần bắt đầu combo (không phải giữa từng hit trong combo)
        public float lastMeleeTime;

        [Header("Huntress1 Ranged Throw")]
        public GameObject spearPrefab;   // kéo Prefab "Spear" (có script Spear.cs) vào đây
        public float throwCooldown = 2f;
        public float lastThrowTime;

        public LayerMask playerLayer;

        [Header("Huntress1 Sounds")]
        public AudioClip meleeSound;
        public AudioClip throwSound;


        protected override void Start()
        {
            base.Start();
            facingRight = true;
            float randomOffset = Random.Range(-0.5f, 0.5f);
            moveSpeed += randomOffset;
            startPosition = transform.position;
        }


        void FixedUpdate()
        {
            //if (isDead || currentHealth <= 0 || player == null)
            //{
            //    return;
            //}

            // đang thực hiện combo hoặc ném thì đứng yên, không xét lại trạng thái khác
            if (isBusy)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (playerScript.currentHealth <= 0)
            {
                Patrol();
                return;
            }

            // ưu tiên cận chiến nếu player áp sát
            if (distanceToPlayer <= meleeRange)
            {
                MeleeAttack();
            }
            // xa hơn chút thì đứng ném spear
            else if (distanceToPlayer <= throwRange)
            {
                ThrowAtPlayer();
            }
            // xa hơn nữa thì đuổi theo cho vào tầm
            else if (distanceToPlayer <= chaseRange)
            {
                ChasePlayer();
            }
            else
            {
                Patrol();
            }
        }


        private void Flip()
        {
            facingRight = !facingRight;
            transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
        }


        private void FaceTowardsPlayer()
        {
            float xDifference = player.position.x - transform.position.x;
            if (xDifference > 0 && !facingRight)
            {
                Flip();
            }
            else if (xDifference < 0 && facingRight)
            {
                Flip();
            }
        }


        private void Patrol()
        {
            anim.SetBool("isRunning", true);

            float direction = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

            if (transform.position.x >= startPosition.x + patrolDistance && facingRight)
            {
                Flip();
            }
            else if (transform.position.x <= startPosition.x - patrolDistance && !facingRight)
            {
                Flip();
            }

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
            if (!isGrounded)
            {
                Flip();
            }
        }


        private void ChasePlayer()
        {
            anim.SetBool("isRunning", true);

            float flipDeadzone = 0.15f;
            float xDifference = player.position.x - transform.position.x;

            if (xDifference > flipDeadzone && !facingRight)
            {
                Flip();
            }
            else if (xDifference < -flipDeadzone && facingRight)
            {
                Flip();
            }

            float moveDirection = facingRight ? 1f : -1f;
            rb.velocity = new Vector2(moveDirection * moveSpeed, rb.velocity.y);
        }


        // =========================================================
        // MELEE COMBO (3 đòn liên tiếp)
        // =========================================================

        private void MeleeAttack()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);
            FaceTowardsPlayer();

            if (isBusy) return;

            if (Time.time >= lastMeleeTime + meleeCooldown)
            {
                isBusy = true;
                anim.SetTrigger("meleeAttack"); // chỉ cần bắn trigger này 1 lần,
                                                // Animator tự nối tiếp 3 đòn (Exit Time, không cần code can thiệp)
                lastMeleeTime = Time.time;
            }
        }

        // Gọi bằng Animation Event tại đúng frame của TỪNG đòn trong combo (đặt 3 Event, mỗi cái trong 1 clip/frame riêng)
        public void DealMeleeDamage1()
        {
            DealMeleeDamage(meleeDamageHit1);
        }

        public void DealMeleeDamage2()
        {
            DealMeleeDamage(meleeDamageHit2);
        }

        public void DealMeleeDamage3()
        {
            DealMeleeDamage(meleeDamageHit3);
        }

        private void DealMeleeDamage(float damageAmount)
        {
            Collider2D hitPlayer = Physics2D.OverlapCircle(attackPoint.position, meleeRadius, playerLayer);
            if (hitPlayer != null)
            {
                Player hitPlayerScript = hitPlayer.GetComponentInParent<Player>();
                if (hitPlayerScript != null)
                {
                    hitPlayerScript.TakeDamage(damageAmount);
                }
            }
        }

        private void TriggerMeleeSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(meleeSound);
            }
        }


        // =========================================================
        // RANGED THROW (ném spear)
        // =========================================================

        private void ThrowAtPlayer()
        {
            rb.velocity = Vector2.zero;
            anim.SetBool("isRunning", false);
            FaceTowardsPlayer();

            if (isBusy) return;

            if (Time.time >= lastThrowTime + throwCooldown)
            {
                isBusy = true;
                anim.SetTrigger("throw");
                lastThrowTime = Time.time;
            }
        }

        // Gọi bằng Animation Event, đặt đúng tại frame enemy tung tay ném spear ra
        public void SpawnSpear()
        {
            if (spearPrefab == null || attackPoint == null)
            {
                Debug.LogWarning(gameObject.name + ": chưa gán spearPrefab hoặc attackPoint!");
                return;
            }

            GameObject spearObj = Instantiate(spearPrefab, attackPoint.position, Quaternion.identity);
            Spear spearScript = spearObj.GetComponent<Spear>();

            if (spearScript != null)
            {
                float direction = facingRight ? 1f : -1f;
                spearScript.SetDirection(direction);
                spearScript.playerLayer = playerLayer;
            }
        }

        private void TriggerThrowSound()
        {
            if (audioManager != null)
            {
                audioManager.PlayerSFX(throwSound);
            }
        }


        // =========================================================
        // DÙNG CHUNG: kết thúc combo/ném, cho phép hành động tiếp
        // =========================================================

        // Gọi bằng Animation Event ở frame cuối cùng của CẢ combo cận chiến LẪN animation ném
        public void EndAction()
        {
            isBusy = false;
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, throwRange);

            if (attackPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(attackPoint.position, meleeRadius);
            }
        }
    }


    // =========================================================
    // SPEAR (viên giáo bay) - gộp chung file với Huntress1 để tiện quản lý
    // Gắn class này vào Prefab "Spear" (viên giáo bay).
    // Prefab cần có: SpriteRenderer, Rigidbody2D (Body Type = Kinematic hoặc Dynamic, Gravity Scale = 0),
    // Collider2D (tick "Is Trigger").
    // =========================================================
    public class Spear : MonoBehaviour
    {
        [Header("Spear Movement")]
        public float speed = 8f;           // tốc độ bay của spear
        public float lifeTime = 5f;        // tự huỷ sau bao nhiêu giây nếu không trúng gì

        [Header("Spear Damage")]
        public float damage = 20f;
        public LayerMask playerLayer;

        // hướng bay, được set ngay khi enemy Instantiate spear ra (1 = phải, -1 = trái)
        private float direction = 1f;
        private bool hasHit = false; // tránh gây damage 2 lần nếu OnTriggerEnter2D bị gọi liên tiếp

        private void Start()
        {
            // tự huỷ sau lifeTime giây nếu bay mãi không trúng gì (bay ra khỏi màn hình)
            Destroy(gameObject, lifeTime);
        }

        // Gọi hàm này ngay sau khi Instantiate spear để set hướng bay đúng theo phía enemy đang quay mặt
        public void SetDirection(float dir)
        {
            direction = dir;

            // lật sprite spear cho đúng hướng bay
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * direction;
            transform.localScale = scale;
        }

        private void Update()
        {
            // di chuyển thẳng theo hướng đã set, tốc độ đều
            transform.position += new Vector3(direction * speed * Time.deltaTime, 0f, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasHit) return; // đã gây damage rồi thì bỏ qua các va chạm tiếp theo

            // kiểm tra có phải Player không (dùng layer để lọc, tránh trúng enemy khác/tường)
            if (((1 << other.gameObject.layer) & playerLayer) != 0)
            {
                Player playerHit = other.GetComponentInParent<Player>();
                if (playerHit != null)
                {
                    playerHit.TakeDamage(damage);
                    hasHit = true;
                    Destroy(gameObject); // spear biến mất ngay khi trúng player
                }
            }
        }
    }
}