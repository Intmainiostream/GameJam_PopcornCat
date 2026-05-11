using UnityEngine;
using UnityEngine.UI;

public class VisionaryUI : MonoBehaviour
{
    [SerializeField] Image eyeFill;
    [SerializeField] PlayerMovement player;

    void Update()
    {
        if (player == null || eyeFill == null) return;

        if (player.IsOnCooldown)
            eyeFill.fillAmount = 1f - player.CooldownFraction;
        else
            eyeFill.fillAmount = player.VisionFraction;
    }
}
