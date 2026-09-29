using UnityEngine;

public class Bat : EnemyBase
{
    [Header("Bat Settings")]
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.5f;
    public float attackDamage = 15f;

    [Header("Ground Detection (For Death)")]
    public Transform groundCheck;
    public LayerMask groundLayer;

    private float lastAttackTime;
    private bool facingRight = true;
    private bool isDeadAndGrounded = false; // Ngăn code chạy lặp lại khi xác đã nằm đất

    protected override void Start()
    {
        base.Start();
        rb.gravityScale = 0f; // Dơi bay lơ lửng, tắt trọng lực mặc định
    }

    void Update()
    {
        // 1. XỬ LÝ QUÁ TRÌNH RƠI KHI CHẾT
        if (currentHealth <= 0)
        {
            if (!isDeadAndGrounded)
            {
                // Bắn tia kiểm tra xem xác rơi đã chạm đất chưa
                bool isGroundedHit = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
                if (isGroundedHit)
                {
                    isDeadAndGrounded = true;
                    rb.velocity = Vector2.zero; // Dừng nảy/trượt
                    rb.gravityScale = 0f;       // Tắt trọng lực lại để xác nằm im

                    anim.SetBool("isGrounded", true); // Chuyển sang hình death.png
                    Destroy(gameObject, 2f);          // Xóa game object sau 2 giây
                }
            }
            return; // Đã chết thì ngắt luôn, không chạy AI bên dưới nữa
        }

        if (player == null) return;

        // 2. KHÓA AI KHI BỊ THƯƠNG HOẶC ĐANG CẮN
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Hurt") || stateInfo.IsName("Attack"))
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 3. LOGIC BAY ĐUỔI THEO & TẤN CÔNG
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        else if (distanceToPlayer <= chaseRange)
        {
            ChasePlayer();
        }
        else
        {
            rb.velocity = Vector2.zero; // Lơ lửng đứng yên nếu Musashi chạy xa
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.velocity = direction * moveSpeed;

        if (direction.x > 0 && !facingRight) Flip();
        else if (direction.x < 0 && facingRight) Flip();
    }

    private void AttackPlayer()
    {
        rb.velocity = Vector2.zero;
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            anim.SetTrigger("attack");
            lastAttackTime = Time.time;
        }
    }

    private void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
    }

    // Gắn hàm này vào Animation Event trong tab Animation của clip Attack
    public void DealDamage()
    {
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            playerScript.TakeDamage(attackDamage);
        }
    }

    // Ghi đè hàm Die của EnemyBase
    protected override void Die()
    {
        TriggerDeathSound();
        anim.SetTrigger("death"); // Kích hoạt trạng thái fly-to-fall

        col.enabled = false;      // Tắt va chạm để Musashi đi xuyên qua xác
        rb.gravityScale = 2f;     // CỰC KỲ QUAN TRỌNG: Bật trọng lực để kéo dơi rớt xuống

        // Không gọi Destroy() ở đây như EnemyBase, ta sẽ Destroy ở hàm Update khi chạm đất
    }

    // Vẽ vòng tròn kiểm tra đất để dễ căn chỉnh trong Scene
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, 0.2f);
        }
    }
}