using System;
using UnityEngine;

public class AutoWorldToggle : MonoBehaviour
{
    [SerializeField] float interval    = 3f;
    [SerializeField] float warningTime = 1f;

    public static AutoWorldToggle Instance { get; private set; }
    public static event Action OnWarning;
    public bool CurrentDesiredState { get; private set; }

    float timer;
    bool warnedThisCycle;

    void Awake()
    {
        Instance = this;
        timer = interval;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (!warnedThisCycle && timer <= warningTime)
        {
            warnedThisCycle = true;
            OnWarning?.Invoke();
        }

        if (timer > 0f) return;

        timer = interval;
        warnedThisCycle = false;
        CurrentDesiredState = !CurrentDesiredState;

        if (!PlayerMovement.IsPlayerOverriding)
            PlayerMovement.BroadcastWorldState(CurrentDesiredState);
    }
}
