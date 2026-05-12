using UnityEngine;

public class Spike : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerDeath death = other.GetComponent<PlayerDeath>();
        if (death != null) death.Die();
    }
}
