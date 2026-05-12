using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    private float startPos;
    private float length;
    private float startPosY;
    
    public GameObject cam;
    public float parallaxEffect = 0.5f; // 0 = sumusunod sa camera, 1 = hindi gumagalaw
    
    [Header("Infinite Scrolling")]
    public bool enableInfiniteScroll = true;
    
    [Header("Vertical Follow")]
    public bool followVertical = true;
    public float verticalOffset = 0f;
    
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        if (cam == null)
            cam = Camera.main.gameObject;
            
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
        {
            length = spriteRenderer.bounds.size.x;
        }
        else
        {
            // Kung walang SpriteRenderer, manual i-set ang length
            length = 20f; // Default value, adjust mo sa inspector
        }
        
        startPos = transform.position.x;
        startPosY = transform.position.y;
    }
    
    void FixedUpdate()
    {
        if (cam == null) return;
        
        float cameraX = cam.transform.position.x;
        float cameraY = cam.transform.position.y;
        
        // ── Horizontal Movement with Parallax ─────────────────
        float distance = cameraX * parallaxEffect;
        float newX = startPos + distance;
        
        // Infinite scrolling
        if (enableInfiniteScroll && spriteRenderer != null)
        {
            // Calculate kung gaano kalayo ang nalipat ng background
            float temp = (cameraX * (1 - parallaxEffect));
            
            // Kapag lumampas na sa haba ng sprite, i-reset ang posisyon
            if (temp > startPos + length)
            {
                startPos += length;
            }
            else if (temp < startPos - length)
            {
                startPos -= length;
            }
            
            newX = startPos + distance;
        }
        
        // ── Vertical Movement ─────────────────────────────────
        float newY = transform.position.y;
        
        if (followVertical)
        {
            // Sumusunod sa camera vertically
            newY = cameraY + verticalOffset;
        }
        else
        {
            // Manatili sa original Y position
            newY = startPosY;
        }
        
        // Apply new position
        transform.position = new Vector3(newX, newY, transform.position.z);
    }
    
    // Para i-reset ang background position (tawagin kung nag-reset ang level)
    public void ResetPosition()
    {
        startPos = transform.position.x;
        startPosY = transform.position.y;
    }
}