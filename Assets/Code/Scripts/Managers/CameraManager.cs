using UnityEngine;

public class CameraManager : MonoBehaviour
{
    #region INPUT

    // Mouse buttons
    private const int RotateBlockMouseButton = 0;

    // Axis names
    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";
    private const string ScrollAxis = "Mouse ScrollWheel";

    #endregion

    [Header("Target")]
    [Tooltip("Assign a stomach/hips pivot object here.")]
    [SerializeField] private Transform target;

    [Header("Zoom")]
    [SerializeField] private float distance = 3.5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float zoomSmoothness = 10f;

    [Header("Rotation")]
    [SerializeField] private float sensitivity = 3f;
    [SerializeField] private float minPitch = -60f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Dynamic Pivot")]
    [Tooltip("Neutral camera center.")]
    [SerializeField] private float stomachHeight = 0f;

    [Tooltip("How much pivot shifts.")]
    [SerializeField] private float pivotShiftAmount = 0.8f;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private float collisionOffset = 0.1f;
    [SerializeField] private float collisionSmoothness = 15f;

    private float _yaw;
    private float _pitch = 20f;

    private float _targetDistance;

    #region UNITY

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;

        _yaw = angles.y;
        _pitch = angles.x;

        _targetDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (!target) return;

        HandleRotation();
        HandleZoom();

        UpdateCamera();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    #endregion

    #region INPUT HANDLING

    private void HandleRotation()
    {
        // Block camera rotation while controlling limb
        if (Input.GetMouseButton(RotateBlockMouseButton))
            return;

        float mouseX = Input.GetAxis(MouseXAxis);
        float mouseY = Input.GetAxis(MouseYAxis);

        _yaw += mouseX * sensitivity;
        _pitch -= mouseY * sensitivity;

        _pitch = Mathf.Clamp(
            _pitch,
            minPitch,
            maxPitch
        );
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis(ScrollAxis);

        if (Mathf.Abs(scroll) > 0.0001f)
        {
            _targetDistance -= scroll * zoomSpeed;

            _targetDistance = Mathf.Clamp(
                _targetDistance,
                minDistance,
                maxDistance
            );
        }

        distance = Mathf.Lerp(
            distance,
            _targetDistance,
            Time.deltaTime * zoomSmoothness
        );
    }

    #endregion

    #region CAMERA

    private void UpdateCamera()
    {
        float pitch01 = Mathf.InverseLerp(
            minPitch,
            maxPitch,
            _pitch
        );

        // -1 = bottom
        //  0 = middle
        // +1 = top
        float centeredPitch = (pitch01 - 0.5f) * 2f;

        // Dynamic center shift
        float dynamicOffset =
            -centeredPitch * pivotShiftAmount;

        Vector3 pivot =
            target.position +
            Vector3.up * (
                stomachHeight + dynamicOffset
            );

        Quaternion rotation = Quaternion.Euler(
            _pitch,
            _yaw,
            0f
        );

        Vector3 desiredCameraPosition =
            pivot +
            rotation * new Vector3(
                0f,
                0f,
                -distance
            );

        Vector3 direction =
            (desiredCameraPosition - pivot).normalized;

        float targetDistance = distance;

        // Collision
        if (Physics.SphereCast(
            pivot,
            collisionRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask
        ))
        {
            targetDistance =
                hit.distance - collisionOffset;

            targetDistance = Mathf.Max(
                targetDistance,
                minDistance
            );
        }

        Vector3 finalPosition =
            pivot +
            direction * targetDistance;

        transform.position = Vector3.Lerp(
            transform.position,
            finalPosition,
            Time.deltaTime * collisionSmoothness
        );

        transform.LookAt(pivot);
    }

    #endregion
}