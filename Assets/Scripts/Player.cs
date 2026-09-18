using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    //private Animator anim;

    public float moveSpeed = 5f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    public float jumpForce = 8f;

    public AudioManager audioManager;

    public bool isGrounded;
    public bool isRunning;
    public bool isWalking;
    public bool isHoldingShift;
    public bool isMoving;
    public bool facingRight;

    public GameObject shurikenPrefab;
    public Transform firePoint;

    public void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");


        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);


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

        // kiểm tra nhân vật trên mặt đất
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        anim.SetBool("isGrounded", isGrounded);

        // get key down là kiểm tra đè phím
        isHoldingShift = Input.GetKey(KeyCode.LeftShift);

        // nếu ở trên mặt đất và di chuyển mà ko đè shift trái là walk
        isWalking = isGrounded && isMoving && !isHoldingShift;

        // nếu trên mặt đất và vừa di chuyển vừa đè shift trái là run
        isRunning = isGrounded && isMoving && isHoldingShift;

        anim.SetBool("isRunning", isRunning);
        anim.SetBool("isWalking", isWalking);

        // Unity định nghĩa phím nhảy là "Jump"
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
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



    private Animator anim;

    // Biến đếm Combo
    public int noOfClicks = 0;
    private float lastClickedTime = 0;
    public float maxComboDelay = 1f; // chờ 1s nếu ko bấm tiếp thì reset combo

    private void Start()
    {
        facingRight = true;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleMovement();

        // 1. Nếu quá thời gian không chém tiếp -> Reset combo về 0
        if (Time.time - lastClickedTime > maxComboDelay)
        {
            noOfClicks = 0;
        }

        // 2. Nhận phím bấm (Dùng nút J thay vì chuột để chuẩn Form game RPG)
        if (Input.GetKeyDown(KeyCode.J) && isGrounded && !isRunning)
        {
            OnClick();
        }

        // 3. Xử lý thời điểm ngắt đòn đánh hiện tại để nối sang đòn tiếp theo
        HandleComboTransitions();


        HandleDefend();
        ThrowShuriken();
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
        if (Input.GetKeyDown(KeyCode.U))
        {
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


    public void SpawnShuriken()
    {
        Instantiate(shurikenPrefab, firePoint.position, firePoint.rotation);
    }
}
