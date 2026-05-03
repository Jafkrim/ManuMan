using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -5f);
    [SerializeField] private Vector3 fixedEuler = new Vector3(0f, 0f, 0f);

    void LateUpdate()
    {
        if (!target) return;

        Debug.Log(target.position.y);

        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(fixedEuler);
    }
}