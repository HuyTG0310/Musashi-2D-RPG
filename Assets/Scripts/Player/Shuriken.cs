using UnityEngine;

public class Shuriken : MonoBehaviour
{

    public float rotateSpeed = 600;
    public float moveSpeed = 5;
    private Rigidbody2D rb;
    public Player player;
    public bool movingRight;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = FindAnyObjectByType<Player>();
        // lấy hướng ném của phi tiêu khi khởi tạo dựa vào hướng của player
        movingRight = player.facingRight;
    }

    // Update is called once per frame
    void Update()
    {
        // xoay shuriken
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
        if (movingRight)
        {
            rb.velocity = new Vector2(moveSpeed, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(-moveSpeed, rb.velocity.y);
        }

    }
}
