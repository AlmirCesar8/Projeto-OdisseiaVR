using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            Vector3 direction = Camera.main.transform.position - transform.position;
            direction.y = 0f; // ignora inclinação vertical
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
