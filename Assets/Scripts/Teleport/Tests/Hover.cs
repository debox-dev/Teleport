using UnityEngine;

namespace DeBox.Teleport.Tests
{
    public class Hover : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            transform.position = new Vector3(
                Random.Range(-20, 20),
                Random.Range(-20, 20),
                Random.Range(-20, 20)
                );
        }
    }
}

