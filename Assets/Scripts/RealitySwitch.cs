using UnityEngine;

public class RealitySwitch : MonoBehaviour
{
    [Header("Eye UI")]
    public GameObject eyeBg;
    public GameObject eyeFill;

    private bool hasEye = false;

    void Start()
    {
        // Hide UI at start
        eyeBg.SetActive(false);
        eyeFill.SetActive(false);
    }

    void Update()
    {
        // Press SPACE after getting the eye
        if (hasEye && Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Real World Activated!");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detect Eye Object
        if (collision.CompareTag("Eye"))
        {
            hasEye = true;

            // Show UI
            eyeBg.SetActive(true);
            eyeFill.SetActive(true);

            // Remove Eye object
            Destroy(collision.gameObject);

            Debug.Log("Eye collected!");
        }
    }
}