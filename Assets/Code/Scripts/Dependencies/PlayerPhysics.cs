using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    [SerializeField] private Transform _root;
    [Header("Limb Rigidbodies")]
    [SerializeField] private Rigidbody _leftHand;
    [SerializeField] private Rigidbody _rightHand;
    [SerializeField] private Rigidbody _leftFoot;
    [SerializeField] private Rigidbody _rightFoot;

    [Header("Limb ConfigurableJoints")]
    [SerializeField] private ConfigurableJoint _leftHandJoint;
    [SerializeField] private ConfigurableJoint _rightHandJoint;
    [SerializeField] private ConfigurableJoint _leftFootJoint;
    [SerializeField] private ConfigurableJoint _rightFootJoint;

    [Header("Settings")]
    [SerializeField] private float _spring = 300f;
    [SerializeField] private float _damper = 30f;
    [SerializeField] private float _force = 1500f;
    [SerializeField] private float _forwardLimbForce = 25f;

    private Vector2 _lh, _rh, _lf, _rf;

    private JointDrive _drive;

    private void Awake()
    {
        _drive = new JointDrive
        {
            positionSpring = _spring,
            positionDamper = _damper,
            maximumForce = _force
        };
    }

    public void SetLimbInput(LimbType limb, Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);

        switch (limb)
        {
            case LimbType.LeftHand: _lh = input; break;
            case LimbType.RightHand: _rh = input; break;
            case LimbType.LeftFoot: _lf = input; break;
            case LimbType.RightFoot: _rf = input; break;
        }
    }

    private void FixedUpdate()
    {
        Apply(_leftHandJoint, _leftHand, _lh);
        Apply(_rightHandJoint, _rightHand, _rh);
        Apply(_leftFootJoint, _leftFoot, _lf);
        Apply(_rightFootJoint, _rightFoot, _rf);
    }

    private void Apply(ConfigurableJoint joint, Rigidbody rb, Vector2 input)
    {
        if (!joint) return;

        joint.angularXDrive = _drive;
        joint.angularYZDrive = _drive;

        float pitch = Mathf.Clamp(input.y, -1f, 1f);
        float yaw = Mathf.Clamp(input.x, -1f, 1f);

        joint.targetRotation = Quaternion.Euler(pitch * 70f, yaw * 70f, 0f);

        ApplyForward(rb, input);
    }

    private void ApplyForward(Rigidbody rb, Vector2 input)
    {
        if (Mathf.Abs(input.y) < 0.01f) return;

        Vector3 forward = _root.forward;
        forward.y = 0f;
        forward.Normalize();

        float amount = Mathf.Clamp(input.y, -1f, 1f);

        rb.AddForce(forward * amount * _forwardLimbForce, ForceMode.Acceleration);
    }

    public enum LimbType
    {
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot
    }
}