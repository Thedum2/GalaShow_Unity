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
        // 시작 위치 저장
        _startPos = transform.position;
    }

    void Update()
    {
        // 시간에 따른 사인값으로 Y 오프셋 계산
        float yOffset = Mathf.Sin(Time.time * frequency) * amplitude;
        // 원래 위치에 Y 오프셋을 더해 위치 갱신
        transform.position = _startPos + new Vector3(0f, yOffset, 0f);
    }
}
