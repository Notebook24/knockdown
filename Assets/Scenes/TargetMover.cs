using UnityEngine;

// Put on: every cylinder under "Targets" (next to the Target script).
// Pick the Move Type in the Inspector for each cylinder.
[RequireComponent(typeof(Rigidbody))]
public class TargetMover : MonoBehaviour
{
    public enum MoveType { Normal, Fast, StopAndGo, UpDown }

    [Header("Type")]
    public MoveType moveType = MoveType.Normal;

    [Header("X Speed")]
    public float normalSpeed = 100f;
    public float fastSpeed = 200f;
    public bool randomStartDirection = true;

    [Header("X Range (distance left/right of start)")]
    public float normalRangeX = 6f;
    public float fastRangeX = 20f;
    public float stopAndGoRangeX = 20f;   // 50% more than Fast
    public float upDownRangeX = 6f;      // 50% more than Fast

    [Header("Stop And Go (type 3)")]
    public Vector2 moveDuration = new Vector2(1.5f, 3.5f);
    public Vector2 stopDuration = new Vector2(0.5f, 1.5f);

    [Header("Up Down (type 4)")]
    public float bobHeight = 1.5f;
    public float bobFrequency = 2f;
    public Vector2 bobActiveDuration = new Vector2(2f, 4f);
    public Vector2 bobRestDuration = new Vector2(1f, 3f);
    public float maxVerticalSpeed = 8f;

    [Header("When Hit")]
    [Tooltip("Stop controlling the cylinder once something with a Rigidbody (the bullet) hits it")]
    public bool releaseOnHit = true;

    Rigidbody rb;
    Target target;
    float startX, startY;
    float dir = 1f;

    // stop and go
    bool stopped;
    float stateTimer;

    // up down
    bool bobbing;
    float bobTimer;
    float bobWeight;
    float bobPhase;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        target = GetComponent<Target>();
    }

    void Start()
    {
        startX = rb.position.x;
        startY = rb.position.y;

        if (randomStartDirection)
            dir = Random.value < 0.5f ? -1f : 1f;

        // Hold the cylinder in place physically while it's being moved by script
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;

        stateTimer = Random.Range(moveDuration.x, moveDuration.y);
        bobTimer = Random.Range(bobRestDuration.x, bobRestDuration.y);
        bobPhase = Random.value * Mathf.PI * 2f;
    }

    float GetRange()
    {
        switch (moveType)
        {
            case MoveType.Normal:    return normalRangeX;
            case MoveType.Fast:      return fastRangeX;
            case MoveType.StopAndGo: return stopAndGoRangeX;
            case MoveType.UpDown:    return upDownRangeX;
            default:                 return fastRangeX;
        }
    }

    void FixedUpdate()
    {
        if (target != null && target.isOut) { Release(); return; }

        float speed = (moveType == MoveType.Normal) ? normalSpeed : fastSpeed;

        // ---- Type 3: random stops ----
        if (moveType == MoveType.StopAndGo)
        {
            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0f)
            {
                stopped = !stopped;
                stateTimer = stopped
                    ? Random.Range(stopDuration.x, stopDuration.y)
                    : Random.Range(moveDuration.x, moveDuration.y);
            }
            if (stopped) speed = 0f;
        }

        // ---- Left/right bounce ----
        float range = GetRange();
        float x = rb.position.x;
        if (x >= startX + range) dir = -1f;
        else if (x <= startX - range) dir = 1f;

        // ---- Type 4: random up/down ----
        float vy = 0f;
        if (moveType == MoveType.UpDown)
        {
            bobTimer -= Time.fixedDeltaTime;
            if (bobTimer <= 0f)
            {
                bobbing = !bobbing;
                bobTimer = bobbing
                    ? Random.Range(bobActiveDuration.x, bobActiveDuration.y)
                    : Random.Range(bobRestDuration.x, bobRestDuration.y);
            }

            // Smoothly fade the bobbing in/out so it never snaps
            bobWeight = Mathf.MoveTowards(bobWeight, bobbing ? 1f : 0f, Time.fixedDeltaTime * 2f);

            float offset = Mathf.Sin(Time.time * bobFrequency + bobPhase) * bobHeight * bobWeight;
            float desiredY = startY + offset;
            vy = Mathf.Clamp((desiredY - rb.position.y) / Time.fixedDeltaTime,
                             -maxVerticalSpeed, maxVerticalSpeed);
        }

        rb.linearVelocity = new Vector3(dir * speed, vy, 0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!releaseOnHit) return;

        // Hit by something with a Rigidbody that isn't another moving target (e.g. the Bullet)
        Rigidbody other = collision.rigidbody;
        if (other != null && other.GetComponent<TargetMover>() == null)
            Release();
    }

    // Hand the cylinder back to normal physics so it can be knocked down
    void Release()
    {
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        enabled = false;
    }
}