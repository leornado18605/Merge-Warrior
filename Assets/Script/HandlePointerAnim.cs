using UnityEngine;

public class HandPointerAnim : MonoBehaviour
{
    public float amplitude = 10f;   
    public float speed = 2f;       
    private Vector3 startPos;

    void OnEnable()
    {
        startPos = transform.localPosition;
    }

    public void SetStartPos(Vector3 pos)
    {
        startPos = pos;
        transform.localPosition = pos;
    }

    void Update()
    {
        transform.localPosition = startPos + Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
    }
}
