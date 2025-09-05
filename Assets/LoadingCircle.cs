using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f; 

    void Update()
    {
        transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
    }
}
