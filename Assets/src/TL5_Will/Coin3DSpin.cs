using UnityEngine;

public class Coin3DSpin : MonoBehaviour
{
    [SerializeField] float spinSpeed = 120f;
    [SerializeField] float bobHeight = 0.1f;
    [SerializeField] float bobSpeed = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        // Optional floating effect
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPos + new Vector3(0f, y, 0f);
    }
}
