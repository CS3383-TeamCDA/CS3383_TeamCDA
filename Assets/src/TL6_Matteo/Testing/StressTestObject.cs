using UnityEngine;

public class StressTestObject : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;

    void Update()
    {
        transform.Rotate(
            Vector3.forward * rotationSpeed * Time.deltaTime
        );
    }
}
