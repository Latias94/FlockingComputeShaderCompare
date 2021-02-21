using UnityEngine;

public struct Boid
{
    public Vector3 position;
    public Vector3 direction;

    public Boid(Vector3 pos)
    {
        position.x = pos.x;
        position.y = pos.y;
        position.z = pos.z;
        direction.x = 0;
        direction.y = 0;
        direction.z = 0;
    }
}