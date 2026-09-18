using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    public float maxHealth = 150f;
    public float currentHealth;
    public float defense = 70f;

    private SpriteRenderer spriteRenderer;
    private Animator anim;
    private Collider2D col; // dùng để tắt va chạm khi chết
    private Rigidbody2D rb;
    private AudioManager audioManager;
    public float moveSpeed = 2f;
    public bool facingRight = true;
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask groundLayer;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        audioManager = FindAnyObjectByType<AudioManager>();
        facingRight = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(currentHealth <= 0)
        {
            return;
        }

        Patrol();

    }


    private void Patrol()
    {
        anim.SetBool("isRunning", true);
        float direction = facingRight ? 1f : -1f;

        rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);

        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        bool isWallHit = Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer);
        if (!isGrounded || isWallHit)
        {
            Flip();
        }
    }


    private void Flip()
    {
        facingRight = !facingRight;
        transform.localScale = new Vector3(-transform.localScale.x, 1, 1);
    }



    // hàm này được gọi khi Musashi tấn công enemy
    public void TakeDamage(float incomingDamage)
    {
        float actualDamage = incomingDamage - defense; // sát thương trừ vào giáp

        if (actualDamage < 0)    // nếu sát thương bé hơn giáp thì nhận 1 dame
        {
            actualDamage = 1f;
        }

        currentHealth -= actualDamage;      // trừ máu
        Debug.Log(gameObject.name + "mất " + actualDamage + "hp! còn lại: " + currentHealth);

        anim.SetTrigger("hurt");
        StartCoroutine(FlashRedRoutine());  // gọi hiệu ứng chớp đỏ báo hiệu trúng đòn

        if (currentHealth <= 0)     // nếu hết máu thì gọi die()
        {
            Die();
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        // đổi màu enemy sang đỏ trong 0.1s rồi trả lại màu gốc
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        anim.SetTrigger("death");   // chạy animation death
        col.enabled = false;    // tắt va chạm để đi xuyên xác chết
        this.enabled = false;   // tắt script này
        rb.gravityScale = 0;    // tắt trọng lực để ko rơi xuống
        rb.velocity = Vector2.zero;     // vận tốc về 0 để enemy nằm im ko di chuyển
        Destroy(this.gameObject, 2f);   // xóa game object này sau 2s
    }


    private void TriggerHurtSound()
    {
        if (audioManager != null)
        {
            audioManager.PlayEnemyHurtSound();
        }
    }

    private void TriggerDeathSound()
    {
        if (audioManager != null)
        {
            audioManager.PlayEnemyDeathSound();
        }
    }

}
