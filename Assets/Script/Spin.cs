using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] private float speed = 360f;       
    [SerializeField] private float spinDuration = 3f;  

    private float timer;
    private bool spinning;

    private void OnEnable()
    {
        transform.rotation = Quaternion.identity;
        timer = 0f;
        spinning = true;
    }

    void Update()
    {
        if (!spinning) return;

        timer += Time.deltaTime;
        if (timer >= spinDuration)
        {
            spinning = false;
            return;
        }

        transform.Rotate(Vector3.forward * speed * Time.deltaTime);
    }
}
