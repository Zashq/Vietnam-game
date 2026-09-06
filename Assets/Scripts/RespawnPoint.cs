using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    public static RespawnPoint instance;

    private void Awake()
    {
        instance = this;
    }
}
