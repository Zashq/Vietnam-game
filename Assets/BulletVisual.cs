using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    public float speed = 200f;
    public float lifeTime = 0.5f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
