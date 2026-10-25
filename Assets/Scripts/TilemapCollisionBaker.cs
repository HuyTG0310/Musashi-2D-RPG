using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tự động kích hoạt và tạo hình học va chạm cho Tilemap ngay khi Scene vừa load (Awake).
/// Chạy ở ExecutionOrder(-100) để đảm bảo mặt sàn CÓ COLLIDER trước khi Player và Enemy rơi xuống.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TilemapCollisionBaker : MonoBehaviour
{
    private void Awake()
    {
        BakeColliders();
    }

    public void BakeColliders()
    {
        TilemapCollider2D tc = GetComponent<TilemapCollider2D>();
        CompositeCollider2D cc = GetComponent<CompositeCollider2D>();
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            // Đảm bảo không bị giới hạn va chạm ở 1 layer nào
            rb.includeLayers = 0;
        }

        if (tc != null)
        {
            tc.enabled = true;
            tc.ProcessTilemapChanges();
        }

        if (cc != null)
        {
            cc.GenerateGeometry();

            // Phao cứu sinh: Nếu CompositeCollider2D không sinh ra được path nào,
            // tắt usedByComposite để TilemapCollider2D tự tạo Box Collider 1x1 cho từng ô.
            // Điều này đảm bảo 100% không bao giờ bị rơi xuyên sàn!
            if (cc.pathCount == 0 && tc != null)
            {
#if UNITY_2023_1_OR_NEWER
                tc.compositeOperation = Collider2D.CompositeOperation.None;
#else
                tc.usedByComposite = false;
#endif
                tc.ProcessTilemapChanges();
                Debug.LogWarning($"[TilemapCollisionBaker] {gameObject.name}: CompositeCollider2D pathCount=0, da chuyen sang Grid Collider rieng le de tranh roi xuyen san.");
            }
            else
            {
                Debug.Log($"[TilemapCollisionBaker] {gameObject.name}: Da tao CompositeCollider2D thanh cong voi {cc.pathCount} duong bao.");
            }
        }
    }
}
