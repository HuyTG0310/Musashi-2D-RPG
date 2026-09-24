using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 150f;
    public float currentHealth;
    public float defense = 70f;
    public float moveSpeed = 2f;


    protected Animator anim;
    protected Collider2D col; // dùng để tắt va chạm khi chết
    protected Rigidbody2D rb;

    protected Player playerScript;  // script để gọi hàm gây sát thương lên player
    protected Transform player;   // lưu vị trí của player để quái di chuyển theo tấn công


    [Header("Audio Settings")]
    protected AudioManager audioManager;
    public AudioClip hurtSound;
    public AudioClip deathSound;


    protected virtual void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        audioManager = FindAnyObjectByType<AudioManager>();
        playerScript = FindAnyObjectByType<Player>();
        if (playerScript != null)
        {
            player = playerScript.gameObject.transform;     // tham chiếu đến component transform của player
        }
    }



    // hàm này được gọi khi Musashi tấn công enemy (logic áp dụng cho mọi enemy)
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
        TriggerHurtSound();     // phát âm thanh khi bị trúng đòn

        if (currentHealth <= 0)     // nếu hết máu thì gọi die()
        {
            Die();
        }
    }

    // hàm chung nhưng logic có thể bổ sung thêm phần thưởng tùy enemy
    protected virtual void Die()
    {
        TriggerDeathSound();    // phát âm thanh khi bị tiêu diệt
        anim.SetTrigger("death");   // chạy animation death
        col.enabled = false;    // tắt va chạm để đi xuyên xác chết
        this.enabled = false;   // tắt script này
        rb.gravityScale = 0;    // tắt trọng lực để ko rơi xuống
        rb.velocity = Vector2.zero;     // vận tốc về 0 để enemy nằm im ko di chuyển
        Destroy(this.gameObject, 2f);   // xóa game object này sau 2s
    }


    protected void TriggerHurtSound()
    {
        if (audioManager != null && hurtSound != null)
        {
            audioManager.PlayerSFX(hurtSound);
        }
    }

    protected void TriggerDeathSound()
    {
        if (audioManager != null && deathSound != null)
        {
            audioManager.PlayerSFX(deathSound);
        }
    }

}
