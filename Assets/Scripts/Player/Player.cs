using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;     // thời gian hồi chiêu
    private bool isDashing;
    private float lastDashTime;


    [Header("Stats")]
    public float maxStamina = 100f;
    public float currentStamina;


    [Header("States")]
    public bool isGrounded;
    public bool isRunning;
    public bool isWalking;
    public bool isHoldingShift;
    public bool isMoving;
    public bool facingRight = true;

    [Header("Combat Stats")]
    public float attackDamage = 100f;
    public GameObject shurikenPrefab;
    public Transform firePoint;
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public float attackRange = 0.5f;

    // Biến đếm Combo
    public int noOfClicks = 0;
    private float lastClickedTime = 0;
    public float maxComboDelay = 1f; // chờ 1s nếu ko bấm tiếp thì reset combo


    public AudioManager audioManager;

    private void Start()
    {
        facingRight = true;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        currentStamina = maxStamina;
    }


    private void Update()
    {
        // nếu đang lướt thì ko di chuyển, đánh, chém,..
        if (isDashing)
        {
            return;
        }

        HandleMovement();
        HandleDash();
        HandleComboTransitions();
        HandleDefend();
        ThrowShuriken();

        // 1. reset combo nếu quá thời gian
        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        // 2. nhận phím tấn công (J)
        if (Input.GetKeyDown(KeyCode.J) && isGrounded && !isRunning)
        {
            OnClick();
        }
    }


    public void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
            facingRight = true;
        }
        else if (moveInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
            facingRight = false;
        }

        isMoving = moveInput != 0;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer); // kiểm tra nhân vật trên mặt đất
        anim.SetBool("isGrounded", isGrounded);


        isHoldingShift = Input.GetKey(KeyCode.LeftShift);        // get key down kiểm tra đè phím
        isWalking = isGrounded && isMoving && !isHoldingShift;        // nếu ở trên mặt đất và di chuyển mà ko đè shift trái là walk
        isRunning = isGrounded && isMoving && isHoldingShift;         // nếu trên mặt đất và vừa di chuyển vừa đè shift trái là run

        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isWalking", isWalking);


        if (isRunning)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed * 2, rb.velocity.y);    // nếu chạy thì vận tốc x2
        }
        else
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);    // nếu ko chạy thì vận tốc bình thường
        }


        if (Input.GetButtonDown("Jump") && isGrounded)  // "Jump" = phím space hoặc nút Y trên tay cầm
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    // hàm này được gọi trong animation attack
    private void CheckHit()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            EnemyBase enemyScript = enemyCollider.gameObject.GetComponent<EnemyBase>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(attackDamage);
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private void HandleDash()
    {
        // nhấn phím C + hết cooldown + đủ 2 thể lực
        if (Input.GetKeyDown(KeyCode.C) && Time.time >= lastDashTime + dashCooldown && currentStamina >= 2)
        {
            StartCoroutine(DashRoutine());
        }
    }


    private IEnumerator DashRoutine()
    {
        // khi bắt đầu lướt bỏ qua collider
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), true);

        // 1. bắt đầu lướt
        isDashing = true;
        currentStamina -= 2;
        anim.SetTrigger("dash");


        // khóa trọng lực (ko bị rơi khi lướt trên không)
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        float dashDirection = facingRight ? 1 : -1;
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0);

        // chờ trong khoảng tgian lướt 
        yield return new WaitForSeconds(dashDuration);

        // kết thúc lướt
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);
        rb.gravityScale = originalGravity;
        isDashing = false;
        lastDashTime = Time.time;
    }



    public void TakeDamage()
    {
        anim.SetTrigger("hurt");
    }


    public void Die()
    {
        anim.SetTrigger("death");
        this.enabled = false;
    }






    private void OnClick()
    {
        // Ghi lại thời gian lúc bấm
        lastClickedTime = Time.time;

        // Tăng số lần đếm nhấp chuột (Tối đa là 3)
        noOfClicks++;
        noOfClicks = Mathf.Clamp(noOfClicks, 0, 3);

        // Bấm nhát đầu tiên -> Bật hoạt ảnh 1
        if (noOfClicks == 1)
        {
            anim.SetBool("atk1", true);
        }

    }


    private void ThrowShuriken()
    {
        // nhấn U + đủ 4 thể lực
        if (Input.GetKeyDown(KeyCode.U) && currentStamina >= 4)
        {
            currentStamina -= 4;
            anim.SetTrigger("throw");
        }
    }

    private void HandleDefend()
    {
        if (isGrounded && Input.GetKeyDown(KeyCode.K))
        {
            anim.SetBool("isDefending", true);
            rb.velocity = new Vector2(0, rb.velocity.y);
        }
        else if (isGrounded && Input.GetKeyUp(KeyCode.K))
        {
            anim.SetBool("isDefending", false);
        }
    }

    private void HandleComboTransitions()
    {
        // -- KIỂM TRA ĐÒN 1 --
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("MusashiAtk1"))
        {
            float timePassed = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

            // Trường hợp 1: Có bấm chém tiếp -> Cho phép ngắt đòn 1 ở 70% để combo
            if (noOfClicks >= 2 && timePassed >= 0.7f)
            {
                // nếu atk2 chưa bật thì mới bật tránh bị lặp
                if (!anim.GetBool("atk2"))
                {
                    anim.SetBool("atk1", false);
                    anim.SetBool("atk2", true);
                }

            }
            // Trường hợp 2: Không bấm tiếp -> Đợi đòn 1 chạy trọn vẹn 100% (1.0f) mới thu kiếm
            else if (noOfClicks == 1 && timePassed >= 1.0f)
            {
                anim.SetBool("atk1", false);
                noOfClicks = 0; // Trả biến đếm về 0
            }
        }

        // -- KIỂM TRA ĐÒN 2 --
        else if (anim.GetCurrentAnimatorStateInfo(0).IsName("MusashiAtk2"))
        {
            float timePassed = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

            if (noOfClicks >= 3 && timePassed >= 0.7f)
            {
                if (!anim.GetBool("atk3"))
                {
                    anim.SetBool("atk2", false);
                    anim.SetBool("atk3", true);
                }
            }
            else if (noOfClicks == 2 && timePassed >= 1.0f)
            {
                anim.SetBool("atk2", false);
                noOfClicks = 0;
            }
        }

        // -- KIỂM TRA ĐÒN 3 (Đòn chốt) --
        else if (anim.GetCurrentAnimatorStateInfo(0).IsName("MusashiAtk3"))
        {
            float timePassed = anim.GetCurrentAnimatorStateInfo(0).normalizedTime;

            // Đòn 3 là đòn cuối cùng, không thể combo thêm nên luôn đợi 100% rồi thu kiếm
            if (timePassed >= 1.0f)
            {
                anim.SetBool("atk3", false);
                noOfClicks = 0;
            }
        }
    }


    public void TriggerAttackSound()
    {
        if (audioManager != null)
        {
            audioManager.PlayAttackSound();
        }
    }

    public void TriggerThrowShurikenSound()
    {
        if (audioManager != null)
        {
            audioManager.PlayThrowShurikenSound();
        }
    }

    public void TriggerDashSound()
    {
        if (audioManager != null)
        {
            audioManager.PlayDashSound();
        }
    }


    public void SpawnShuriken()
    {
        Instantiate(shurikenPrefab, firePoint.position, firePoint.rotation);
    }
}
