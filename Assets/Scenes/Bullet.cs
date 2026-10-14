using UnityEngine;

// Put on: Bullet prefab (Sphere with Rigidbody + SphereCollider).
public class Bullet : MonoBehaviour
{
    public float lifetime = 5f;
    public float hitForce = 8f;         // extra shove on targets

    void Start() => Destroy(gameObject, lifetime);

    void OnCollisionEnter(Collision c)
    {
        if (c.rigidbody != null && c.gameObject.CompareTag("Target"))
            c.rigidbody.AddForceAtPosition(transform.forward * hitForce, c.GetContact(0).point, ForceMode.Impulse);

        Destroy(gameObject);            // bullet disappears after hitting anything
    }
}