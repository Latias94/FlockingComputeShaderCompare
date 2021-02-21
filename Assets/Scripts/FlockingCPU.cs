using UnityEngine;

public class FlockingCPU : MonoBehaviour
{
    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;

    private Boid[] _boidsArray;
    private GameObject[] _boids;
    private int _numOfBoids;

    void Start()
    {
        _numOfBoids = boidsCount;

        InitBoids();
    }

    private void InitBoids()
    {
        _boids = new GameObject[_numOfBoids];
        _boidsArray = new Boid[_numOfBoids];

        for (int i = 0; i < _numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            _boidsArray[i] = new Boid(pos);
            _boids[i] = Instantiate(boidPrefab, pos, Quaternion.identity);
            _boidsArray[i].direction = _boids[i].transform.forward;
        }
    }

    void Update()
    {
        for (var i = 0; i < _boidsArray.Length; i++)
        {
            var boid = _boidsArray[i];

            Vector3 separation = Vector3.zero;
            Vector3 alignment = Vector3.zero;
            Vector3 cohesion = target.transform.position;
            uint nearbyCount = 1;

            for (int j = 0; j < _boidsArray.Length; j++)
            {
                if (j == i)
                    continue;
                Boid tempBoid = _boidsArray[j];

                Vector3 tempBoidPosition = tempBoid.position;
                Vector3 offset = boid.position - tempBoidPosition;
                float dist = Mathf.Max(offset.magnitude, 0.000001f); // 防止 offset 为 0，下面做除法分母

                if (dist < neighbourDistance)
                {
                    separation +=
                        offset * (float) (1.0 / dist - 1.0 / neighbourDistance); // 两个 boid 越接近，产生的 separation 越大
                    alignment += _boidsArray[j].direction;
                    cohesion += tempBoidPosition;

                    nearbyCount += 1;
                }
            }

            float avg = 1.0f / nearbyCount;
            alignment *= avg;
            cohesion *= avg;
            cohesion = Vector3.Normalize(cohesion - boid.position);
            Vector3 direction = alignment + separation + cohesion;

            // 方向加点偏移
            _boidsArray[i].direction = Vector3.Lerp(direction, Vector3.Normalize(boid.direction), 0.94f);
            // 计算最终位置
            _boidsArray[i].position += boid.direction * boidSpeed * Time.deltaTime;
        }

        // 应用位置和旋转到 GameObject
        for (int i = 0; i < _boidsArray.Length; i++)
        {
            _boids[i].transform.localPosition = _boidsArray[i].position;

            if (!_boidsArray[i].direction.Equals(Vector3.zero))
            {
                _boids[i].transform.rotation = Quaternion.LookRotation(_boidsArray[i].direction);
            }
        }
    }
}