using UnityEngine;

// Put on: every object under "Targets" (also set their Tag to "Target").
[RequireComponent(typeof(Rigidbody))]
public class Target : MonoBehaviour
{
    public bool isOut { get; private set; }

    public void MarkOut()
    {
        if (isOut) return;
        isOut = true;
        LevelManager.Instance.CheckWin();
    }
}