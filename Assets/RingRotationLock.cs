using UnityEngine;

public class RingRotationLock : MonoBehaviour
{
    void LateUpdate()
    {
        if (transform.parent != null)
        {
            transform.rotation = Quaternion.identity;
        }
    }

}