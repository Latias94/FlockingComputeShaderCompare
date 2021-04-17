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
        // 获取 Compute Shader 中定义的 numthreads
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
        // 定义大小，鸟的数量和每个鸟结构的大小，一个 Vector3 就是 3 * sizeof(float)
        // 10000 只鸟，每只占 6 * 4 bytes，总共也就占 0.234mib GPU 显存 
        _boidsBuffer = new ComputeBuffer(_numOfBoids, 6 * sizeof(float));
        _boidsBuffer.SetData(_boidsArray);
        // 设置 buffer 到 Compute Shader，同时设置要调用的计算的函数 Kernel
        shader.SetBuffer(_kernelHandle, "boidsBuffer", _boidsBuffer);
        shader.SetFloat("boidSpeed", boidSpeed);
        shader.SetVector("flockPosition", target.transform.position);
        shader.SetFloat("neighbourDistance", neighbourDistance);
        shader.SetInt("boidsCount", boidsCount);
    }

    void Update()
    {
        // 设置每一帧会变的变量
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetVector("flockPosition", target.transform.position);
        // 调用 Compute Shader Kernel 来计算
        shader.Dispatch(_kernelHandle, _groupSizeX, 1, 1);

        // 阻塞等待 Compute Shader 计算结果从 GPU 传回来
        _boidsBuffer.GetData(_boidsArray);

        // 设置鸟的 position 和 rotation
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
            // 用完主动释放 buffer
            _boidsBuffer.Dispose();
        }
    }
}