using UnityEngine;

public class FlockingGPU : MonoBehaviour
{
    public ComputeShader shader;

    public float boidSpeed = 1f;
    public float neighbourDistance = 1f;
    public GameObject boidPrefab;
    public int boidsCount;
    public float spawnRadius;
    public Transform target;

    private int _kernelHandle;
    private ComputeBuffer _boidsBuffer;
    private Boid[] _boidsArray;
    private GameObject[] _boids;
    private int _groupSizeX;
    private int _numOfBoids;

    void Start()
    {
        _kernelHandle = shader.FindKernel("CSMain");

        uint x;
        shader.GetKernelThreadGroupSizes(_kernelHandle, out x, out _, out _);
        _groupSizeX = Mathf.CeilToInt(boidsCount / (float) x);
        // 塞满每个线程组，免得 Compute Shader 中有线程读不到数据，造成读取数据越界
        _numOfBoids = _groupSizeX * (int) x;

        InitBoids();
        InitShader();
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

    void InitShader()
    {
        _boidsBuffer = new ComputeBuffer(_numOfBoids, 6 * sizeof(float));
        _boidsBuffer.SetData(_boidsArray);

        shader.SetBuffer(_kernelHandle, "boidsBuffer", _boidsBuffer);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    void Update()
    {
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("flockPosition", target.transform.position);

        shader.Dispatch(_kernelHandle, _groupSizeX, 1, 1);

        // 阻塞等待 Compute Shader 数据传回来
        _boidsBuffer.GetData(_boidsArray);

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

    void OnDestroy()
    {
        if (_boidsBuffer != null)
        {
            _boidsBuffer.Dispose();
        }
    }
}