using UnityEngine;

public class WallCollisionParticles : MonoBehaviour
{
    public GameObject collisionEffectPrefab;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector2 hitPoint = collision.contacts[0].point;

            Instantiate(collisionEffectPrefab, hitPoint, Quaternion.identity);
        }
    }
}
