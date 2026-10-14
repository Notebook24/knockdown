using UnityEngine;

// Put on: Bullet prefab (Sphere with Rigidbody + SphereCollider).
[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public float lifetime = 5f;
    public float hitForce = 8f;         // extra shove on targets

    // Set by Gun when the bullet is spawned
    [HideInInspector] public AudioClip hitClip;
    [HideInInspector] public float hitVolume = 1f;

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = true;                                                   // bullet drops under gravity again
        rb.linearDamping = 0f;                                                  // no slowdown over distance
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;   // no tunneling
    }

    void Start() => Destroy(gameObject, lifetime);

    void OnCollisionEnter(Collision c)
    {
        if (c.rigidbody != null && c.gameObject.CompareTag("Target"))
        {
            c.rigidbody.AddForceAtPosition(transform.forward * hitForce, c.GetContact(0).point, ForceMode.Impulse);
            Play2D(hitClip, hitVolume);   // sound only when a target is hit
        }

        Destroy(gameObject);            // bullet disappears after hitting anything
    }

    // Plays a clip at full volume regardless of distance, then cleans itself up
    public static void Play2D(AudioClip clip, float volume)
    {
        if (clip == null) return;
        GameObject go = new GameObject("SFX");
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.spatialBlend = 0f;          // 2D: not quieter when the target is far away
        src.Play();
        Destroy(go, clip.length);
    }
}