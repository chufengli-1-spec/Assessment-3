using UnityEngine;

public class WallCollisionParticles : MonoBehaviour
{
    public GameObject collisionEffectPrefab; // 拖入粒子Prefab

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 判断是不是墙
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 取第一个接触点位置
            Vector2 hitPoint = collision.contacts[0].point;

            // 在碰撞点生成粒子特效
            Instantiate(collisionEffectPrefab, hitPoint, Quaternion.identity);
        }
    }
}
