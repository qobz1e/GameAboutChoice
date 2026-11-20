using UnityEngine;

public class SideCamera : MonoBehaviour
{
    public Transform target;
    public float left = 4, right = 24;
    public float maxHeight = 1.8f;
    void LateUpdate()
    {
        if (!target) return;
        float x = Mathf.Clamp(target.position.x + 2, left, right);
        transform.position = new Vector3(x, Mathf.Clamp(target.position.y, 1.8f, maxHeight), -10);
    }
}
