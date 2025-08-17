using UnityEngine;

public class RedTest : MonoBehaviour
{
    [Tooltip("상하 이동 범위")]
    public float amplitude = 1f;
    [Tooltip("이동 속도")]
    public float frequency = 1f;

    private Vector3 _startPos;
    
    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        transform.position = _startPos + new Vector3(0f, yOffset, 0f);
    }
}
