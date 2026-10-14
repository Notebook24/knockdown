using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletSpeed = 40f;
    public float fireRate = 0.2f;
    public float maxAimDistance = 200f;
    public float bulletLifetime = 5f;

    [Header("Ammo")]
    public int maxAmmo = 25;

    [Header("Counter UI")]
    [Range(0.03f, 0.2f)] public float fontScreenHeight = 0.08f;

    public int Ammo { get; private set; }
    [HideInInspector] public bool canShoot = true;   // GameManager turns this off on game over / win

    float nextFire;
    PlayerController player;
    Camera cam;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        cam = Camera.main;
        Ammo = maxAmmo;
    }

    public void ResetAmmo()
    {
        Ammo = maxAmmo;
        canShoot = true;
    }

    void Update()
    {
        if (Mouse.current == null || !canShoot) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;
        if (Ammo <= 0) return;

        if (Mouse.current.leftButton.isPressed && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate;
            Ammo--;
            Shoot();
        }
    }

    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 target = ray.origin + ray.direction * maxAimDistance;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, ~0, QueryTriggerInteraction.Ignore))
            target = hit.point;

        Vector3 dir = (target - firePoint.position).normalized;
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));

        if (player != null && bullet.TryGetComponent(out Collider bulletCol))
            Physics.IgnoreCollision(bulletCol, player.Controller);

        if (bullet.TryGetComponent(out Rigidbody rb))
            rb.linearVelocity = dir * bulletSpeed;

        Destroy(bullet, bulletLifetime);
    }

    void OnGUI()
    {
        DrawCrosshair();
        
        GUIStyle style = new GUIStyle();
        style.fontSize = Mathf.RoundToInt(Screen.height * fontScreenHeight);
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.UpperRight;
        style.wordWrap = false;
        style.clipping = TextClipping.Overflow;

        string text = "Bullets: " + Ammo + "/" + maxAmmo;
        Rect r = new Rect(0f, 20f, Screen.width - 40f, Screen.height * 0.2f);

        style.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), text, style);

        style.normal.textColor = Ammo <= 5 ? Color.red : Color.white;
        GUI.Label(r, text, style);
    }

    void DrawCrosshair()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float len = Screen.height * 0.012f;   // length of each line
        float gap = Screen.height * 0.006f;   // empty space in the middle
        float th  = Mathf.Max(2f, Screen.height * 0.002f);  // thickness
        float o   = 1.5f;                     // black outline size

        Texture2D tex = Texture2D.whiteTexture;

        // Black outline first, then the white/green lines on top
        for (int pass = 0; pass < 2; pass++)
        {
            float e = pass == 0 ? o : 0f;
            GUI.color = pass == 0 ? Color.black : Color.green;

            GUI.DrawTexture(new Rect(cx - gap - len - e, cy - th / 2 - e, len + 2 * e, th + 2 * e), tex); // left
            GUI.DrawTexture(new Rect(cx + gap - e,       cy - th / 2 - e, len + 2 * e, th + 2 * e), tex); // right
            GUI.DrawTexture(new Rect(cx - th / 2 - e, cy - gap - len - e, th + 2 * e, len + 2 * e), tex);  // top
            GUI.DrawTexture(new Rect(cx - th / 2 - e, cy + gap - e,       th + 2 * e, len + 2 * e), tex);  // bottom
        }
        GUI.color = Color.white;
    }
}