using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float baseJumpSpeed = 7f;
    public float gravity = -20f;
    [HideInInspector] public bool canMove = true;
    [HideInInspector] public bool flipOnRespawn = true;   // true = turn 180° on respawn (original behavior)

    [Header("Look")]
    public float mouseSensitivity = 0.05f;
    public Vector3 cameraLocalPosition = new Vector3(0f, 1.6f, 0f);

    [Header("Gun (position relative to the camera)")]
    public Vector3 gunLocalPosition = new Vector3(0.3f, -0.3f, 0.6f);
    public Vector3 gunLocalEuler = Vector3.zero;

    CharacterController controller;
    Transform cam;
    float verticalVelocity;
    bool grounded;
    float lastGroundedTime;
    float pitch;
    bool respawnQueued;
    Vector3 startPosition;
    Quaternion startRotation;

    public float CurrentJumpSpeed { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public int RespawnCount { get; private set; }
    public CharacterController Controller => controller;

    void Awake()
    {
        // Safety net: a too-high value saved in the scene can never get through
        mouseSensitivity = Mathf.Clamp(mouseSensitivity, 0.01f, 0.15f);

        controller = GetComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        // Attach the camera to the player
        Camera camera = GetComponentInChildren<Camera>();
        if (camera == null) camera = Camera.main;
        cam = camera.transform;
        cam.SetParent(transform);
        cam.localPosition = cameraLocalPosition;
        cam.localRotation = Quaternion.identity;

        // Attach the gun to the camera so it follows where you look
        Gun gun = GetComponentInChildren<Gun>();
        if (gun != null)
        {
            gun.transform.SetParent(cam, false);
            gun.transform.localPosition = gunLocalPosition;
            gun.transform.localRotation = Quaternion.Euler(gunLocalEuler);
        }

        // Old body colliders would fight with the CharacterController, so turn them off
        foreach (Collider c in GetComponentsInChildren<Collider>())
            if (c != controller) c.enabled = false;

        // First person: hide the body mesh, but keep the gun visible
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            if (r.GetComponentInParent<Gun>() == null)
                r.enabled = false;

        startPosition = transform.position;
        startRotation = transform.rotation;
        CurrentJumpSpeed = baseJumpSpeed;
        CurrentMoveSpeed = moveSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (respawnQueued) { respawnQueued = false; Respawn(); }

        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null) return;

        // Cursor lock toggle
        if (kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Mouse look
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // WASD
        Vector2 input = Vector2.zero;
        if (canMove)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
        }

        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move = Vector3.ClampMagnitude(move, 1f) * CurrentMoveSpeed;

        // Jump and gravity
        bool canJump = grounded || Time.time - lastGroundedTime < 0.15f;

        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        if (canMove && canJump && kb.spaceKey.wasPressedThisFrame)
        {
            verticalVelocity = CurrentJumpSpeed;
            lastGroundedTime = -10f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        CollisionFlags flags = controller.Move(move * Time.deltaTime);
        grounded = (flags & CollisionFlags.Below) != 0;
        if (grounded) lastGroundedTime = Time.time;
    }

    // Called when the CharacterController bumps into a solid collider
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (IsOutZone(hit.collider.transform))
            respawnQueued = true;
    }

    // Safe to call from physics callbacks; respawns at the start of the next Update
    public void RequestRespawn() => respawnQueued = true;

    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
        verticalVelocity = 0f;
    }

    public void AddMoveSpeed(float bonus) { CurrentMoveSpeed += bonus; }
    public void AddJumpBonus(float bonus) { CurrentJumpSpeed += bonus; }

    // Sets where you respawn (GameManager uses this for each level)
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        startPosition = position;
        startRotation = rotation;
    }

    public void Respawn()
    {
        RespawnCount++;
        controller.enabled = false;
        transform.SetPositionAndRotation(startPosition,
            startRotation * Quaternion.Euler(0f, flipOnRespawn ? 180f : 0f, 0f));
        controller.enabled = true;
        verticalVelocity = 0f;
        pitch = 0f;
        cam.localRotation = Quaternion.identity;
        CurrentJumpSpeed = baseJumpSpeed;
        CurrentMoveSpeed = moveSpeed;
    }

    // Called if the red cube is set as "Is Trigger"
    void OnTriggerEnter(Collider other)
    {
        if (IsOutZone(other.transform))
            respawnQueued = true;
    }

    static bool IsOutZone(Transform t)
    {
        if (t.GetComponentInParent<OutZone>() != null) return true;
        for (Transform p = t; p != null; p = p.parent)
            if (p.name == "OutZone") return true;
        return false;
    }
}