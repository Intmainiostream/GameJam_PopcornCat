using UnityEngine;

public class FloatingEye : MonoBehaviour
{
    [SerializeField] float amplitude = 0.3f;
    [SerializeField] float speed     = 2f;

    Vector3 startPos;

    void Start() => startPos = transform.position;

    void Update()
    {
        transform.position = startPos + new Vector3(0f, Mathf.Sin(Time.time * speed) * amplitude, 0f);
    }
}
