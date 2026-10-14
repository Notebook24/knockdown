using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class Level
    {
        public Transform targetsRoot;      // parent of the targets (e.g. "Targets")
        public Transform targetPlatform;   // the platform(s) the targets sit on (e.g. "TargetPlatform")
        public Transform playerSpawn;      // where you stand for this level (empty for level 1)
    }

    [Tooltip("Leave empty to auto-fill from names: Platform1, Platform2... with Targets / TargetPlatform next to them")]
    public Level[] levels;

    public float lastBulletWait = 5f;     // seconds to wait after the last bullet
    public float heightTolerance = 0.3f;  // how far below the platform top still counts as "on" it
    public float platformMargin = 1f;     // extra sideways room around each platform

    enum State { Playing, Waiting, GameOver, Win }
    State state = State.Playing;

    static int pendingLevel = 0;          // survives the scene reload when you press R
    int currentLevel;
    float waitEnd;

    Gun gun;
    PlayerController player;
    Bounds platformBounds;                // whole TargetPlatform (used for the player's look direction)
    Bounds[] platformPieces;              // every individual platform under TargetPlatform
    Transform[] targets;

    void Start()
    {
        gun = FindFirstObjectByType<Gun>();
        player = FindFirstObjectByType<PlayerController>();

        if (levels == null || levels.Length == 0) AutoFillLevels();

        currentLevel = Mathf.Clamp(pendingLevel, 0, levels.Length - 1);
        StartLevel(currentLevel, currentLevel > 0);
    }

    void AutoFillLevels()
    {
        var list = new System.Collections.Generic.List<Level>();
        for (int i = 1; i < 20; i++)
        {
            Transform platform = FindByName("Platform" + i);
            if (platform == null) break;

            Transform root = platform.parent;
            var lvl = new Level();
            lvl.targetsRoot = root != null ? root.Find("Targets") : null;
            lvl.targetPlatform = root != null ? root.Find("TargetPlatform") : null;
            lvl.playerSpawn = i > 1 ? platform.Find("Cube (1)") : null;
            list.Add(lvl);
        }

        if (list.Count == 0)
        {
            var lvl = new Level();
            lvl.targetsRoot = FindByName("Targets");
            lvl.targetPlatform = FindByName("TargetPlatform");
            list.Add(lvl);
        }
        levels = list.ToArray();
    }

    Transform FindByName(string n)
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == n) return t;
        return null;
    }

    void StartLevel(int index, bool teleport)
    {
        currentLevel = index;
        Level lvl = levels[index];

        // Level 2 (index 1): 35 bullets and respawn facing the other way
        bool isLevel2 = index == 1;
        int ammoForLevel = isLevel2 ? 35 : 25;
        if (player != null) player.flipOnRespawn = !isLevel2;   // must be set BEFORE the Respawn() below

        if (lvl.targetsRoot == null || lvl.targetPlatform == null)
            Debug.LogWarning("GameManager: level " + (index + 1) + " is missing Targets Root or Target Platform.");

        // Remember all targets of this level
        if (lvl.targetsRoot != null)
        {
            targets = new Transform[lvl.targetsRoot.childCount];
            for (int i = 0; i < targets.Length; i++) targets[i] = lvl.targetsRoot.GetChild(i);
        }
        else targets = new Transform[0];

        // Remember the platform area
        if (lvl.targetPlatform != null)
        {
            platformBounds = GetBounds(lvl.targetPlatform);   // used for the player's look direction

            // One bounds per renderer, so each platform is checked on its own
            Renderer[] rs = lvl.targetPlatform.GetComponentsInChildren<Renderer>();
            platformPieces = new Bounds[rs.Length];
            for (int i = 0; i < rs.Length; i++) platformPieces[i] = rs[i].bounds;
        }
        else platformPieces = new Bounds[0];

        if (teleport && lvl.playerSpawn != null && player != null)
        {
            Bounds b = GetBounds(lvl.playerSpawn);
            Vector3 pos = new Vector3(b.center.x, b.max.y + 0.1f, b.center.z);

            Vector3 look = platformBounds.center - pos;
            look.y = 0f;
            Quaternion rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity;

            player.SetCheckpoint(pos, rot);
            player.Respawn();
        }

        if (gun != null) gun.SetMaxAmmo(ammoForLevel);   // sets the cap and refills
        if (player != null) player.canMove = true;
        state = State.Playing;
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || gun == null) return;

        switch (state)
        {
            case State.Playing:
                if (TargetsLeft() == 0) Win();
                else if (gun.Ammo <= 0)
                {
                    state = State.Waiting;
                    waitEnd = Time.time + lastBulletWait;
                }
                break;

            case State.Waiting:
                if (TargetsLeft() == 0) Win();
                else if (Time.time >= waitEnd) GameOver();
                break;

            case State.GameOver:
                if (kb.rKey.wasPressedThisFrame)
                {
                    pendingLevel = currentLevel;   // restart the same level
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                break;

            case State.Win:
                if (kb.yKey.wasPressedThisFrame && currentLevel < levels.Length - 1)
                    StartLevel(currentLevel + 1, true);
                else if (kb.rKey.wasPressedThisFrame && currentLevel >= levels.Length - 1)
                {
                    pendingLevel = 0;              // beat the last level: start over
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
                break;
        }
    }

    void Win()
    {
        state = State.Win;
        gun.canShoot = false;
        if (player != null) player.canMove = false;
    }

    void GameOver()
    {
        state = State.GameOver;
        gun.canShoot = false;
        if (player != null) player.canMove = false;
    }

    // Number of targets still standing on ANY platform
    int TargetsLeft()
    {
        int count = 0;
        foreach (Transform t in targets)
        {
            if (t == null) continue;   // destroyed = knocked down

            Vector3 c = GetBounds(t).center;
            foreach (Bounds p in platformPieces)
            {
                bool above = c.y > p.max.y - heightTolerance;
                bool inside = c.x > p.min.x - platformMargin && c.x < p.max.x + platformMargin
                           && c.z > p.min.z - platformMargin && c.z < p.max.z + platformMargin;
                if (above && inside) { count++; break; }   // on this platform, count it once
            }
        }
        return count;
    }

    static Bounds GetBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    // ---------- UI ----------
    void OnGUI()
    {
        if (state == State.Waiting)
        {
            int secs = Mathf.CeilToInt(waitEnd - Time.time);
            DrawCentered("Checking last shot... " + secs, Color.yellow, 0.05f, 0.35f);
        }
        else if (state == State.GameOver)
        {
            DrawCentered("GAME OVER", Color.red, 0.12f, 0.3f);
            DrawCentered("Press R to start again", Color.white, 0.05f, 0.45f);
        }
        else if (state == State.Win)
        {
            DrawCentered("CONGRATS!", Color.green, 0.12f, 0.3f);
            if (currentLevel < levels.Length - 1)
                DrawCentered("Press Y to proceed to the next level", Color.white, 0.05f, 0.45f);
            else
                DrawCentered("You finished every level! Press R to play again", Color.white, 0.05f, 0.45f);
        }
    }

    void DrawCentered(string text, Color color, float sizeFrac, float yFrac)
    {
        GUIStyle s = new GUIStyle();
        s.fontSize = Mathf.RoundToInt(Screen.height * sizeFrac);
        s.fontStyle = FontStyle.Bold;
        s.alignment = TextAnchor.MiddleCenter;
        s.clipping = TextClipping.Overflow;

        Rect r = new Rect(0f, Screen.height * yFrac, Screen.width, Screen.height * 0.15f);

        s.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 4f, r.y + 4f, r.width, r.height), text, s);
        s.normal.textColor = color;
        GUI.Label(r, text, s);
    }
}