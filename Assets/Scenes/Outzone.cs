using UnityEngine;

// Put on: OutZone (the red platform).
// Player touches it -> respawn at start. Target lands on it -> counts as "off the platform".
// Works whether the collider is a Trigger or solid.
public class OutZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)  => Check(other.gameObject);
    void OnCollisionEnter(Collision c)   => Check(c.gameObject);

    void Check(GameObject go)
    {
        PlayerController player = go.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.Respawn();
            return;
        }

        Target t = go.GetComponentInParent<Target>();
        if (t != null) t.MarkOut();
    }
}