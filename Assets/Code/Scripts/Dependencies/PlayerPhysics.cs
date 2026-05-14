using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
    [SerializeField]
    private float _recoverDelay = 2f;

    private float _fallTimer;

    [SerializeField]
    private float _recoverySpeed = 1f;

    private bool _recovering;
    private float _recoveryWeight;

    [SerializeField] private PlayerManager _player;

    [SerializeField] private Transform _root;

    [Header("Main Body Rigidbody")]
    [SerializeField] private Rigidbody _bodyRb;

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

    [Header("Body ConfigurableJoints")]
    [SerializeField] private ConfigurableJoint _bodyJoint;

    [SerializeField] private ConfigurableJoint _leftUpperArmJoint;
    [SerializeField] private ConfigurableJoint _rightUpperArmJoint;

    [SerializeField] private ConfigurableJoint _leftArmJoint;
    [SerializeField] private ConfigurableJoint _rightArmJoint;

    [SerializeField] private ConfigurableJoint _leftHipsJoint;
    [SerializeField] private ConfigurableJoint _rightHipsJoint;

    [SerializeField] private ConfigurableJoint _leftKneeJoint;
    [SerializeField] private ConfigurableJoint _rightKneeJoint;

    [Header("Limb Control")]
    [SerializeField] private float _limbSpring = 300f;
    [SerializeField] private float _limbDamper = 30f;
    [SerializeField] private float _limbForce = 1500f;

    [SerializeField] private float _activeLimbSpring = 80f;
    [SerializeField] private float _activeLimbDamper = 8f;
    [SerializeField] private float _activeLimbForce = 1500f;

    [SerializeField] private float _movementForce = 25f;

    [Header("Body Rotation")]
    [SerializeField] private float _turnTorque = 150f;

    [SerializeField]
    private float _maxAngularVelocity = 6f;

    [Header("Fall Detection")]
    [SerializeField]
    private float _fallAngle = 30f;

    [SerializeField]
    private float _recoverAngle = 25;

    [Header("Body Drive")]
    [SerializeField] private float _bodySpring = 1500f;
    [SerializeField] private float _bodyDamper = 150f;
    [SerializeField] private float _bodyForce = 999999f;

    [Header("Upper Arm Drive")]
    [SerializeField] private float _upperArmSpring = 50f;
    [SerializeField] private float _upperArmDamper = 5f;
    [SerializeField] private float _upperArmForce = 999999f;

    [Header("Arm Drive")]
    [SerializeField] private float _armSpring = 100f;
    [SerializeField] private float _armDamper = 10f;
    [SerializeField] private float _armForce = 999999f;

    [Header("Hips Drive")]
    [SerializeField] private float _hipsSpring = 1000f;
    [SerializeField] private float _hipsDamper = 100f;
    [SerializeField] private float _hipsForce = 999999f;

    [Header("Knee Drive")]
    [SerializeField] private float _kneeSpring = 500f;
    [SerializeField] private float _kneeDamper = 50f;
    [SerializeField] private float _kneeForce = 999999f;

    private float _bodyYaw;

    private Vector2 _lh;
    private Vector2 _rh;
    private Vector2 _lf;
    private Vector2 _rf;

    private float _leftHipPose;
    private float _rightHipPose;

    private float _leftKneePose;
    private float _rightKneePose;

    private float _leftUpperArmPoseX;
    private float _rightUpperArmPoseX;

    private float _leftArmPoseX;
    private float _rightArmPoseX;

    private float _leftUpperArmPoseZ;
    private float _rightUpperArmPoseZ;

    private JointDrive _limbDrive;
    private JointDrive _activeLimbDrive;

    private JointDrive _bodyDrive;
    private JointDrive _upperArmDrive;
    private JointDrive _armDrive;
    private JointDrive _hipsDrive;
    private JointDrive _kneeDrive;

    #region UNITY

    private void Awake()
    {
        _limbDrive = CreateDrive(
            _limbSpring,
            _limbDamper,
            _limbForce
        );

        _activeLimbDrive = CreateDrive(
            _activeLimbSpring,
            _activeLimbDamper,
            _activeLimbForce
        );

        _bodyDrive = CreateDrive(
            _bodySpring,
            _bodyDamper,
            _bodyForce
        );

        _upperArmDrive = CreateDrive(
            _upperArmSpring,
            _upperArmDamper,
            _upperArmForce
        );

        _armDrive = CreateDrive(
            _armSpring,
            _armDamper,
            _armForce
        );

        _hipsDrive = CreateDrive(
            _hipsSpring,
            _hipsDamper,
            _hipsForce
        );

        _kneeDrive = CreateDrive(
            _kneeSpring,
            _kneeDamper,
            _kneeForce
        );
    }

    private void FixedUpdate()
    {
        if (
            _player.movementState ==
            PlayerManager.MovementState.Falling
        )
        {
            _fallTimer += Time.fixedDeltaTime;
        }
        else
        {
            _fallTimer = 0f;
        }

        if (_recovering)
        {
            _recoveryWeight +=
                Time.fixedDeltaTime *
                _recoverySpeed;

            _recoveryWeight =
                Mathf.Clamp01(
                    _recoveryWeight
                );

            if (_recoveryWeight >= 1f)
            {
                Vector3 localEuler =
                    _bodyRb.transform.localEulerAngles;

                float x =
                    NormalizeAngle(localEuler.x);

                float bodyOffset =
                    Mathf.Abs(x - (-90f));

                if (bodyOffset < _recoverAngle)
                {
                    _recovering = false;

                    _player.SetMovementState(
                        PlayerManager.MovementState.Grounded
                    );
                }
            }
        }

        HandleFallState();

        HandleGroundedBalance();

        ApplyLimb(
            _leftHandJoint,
            _leftHand,
            _lh,
            true,
            PlayerManager.MovementState.Grounded
        );

        ApplyLimb(
            _rightHandJoint,
            _rightHand,
            _rh,
            false,
            PlayerManager.MovementState.Grounded
        );

        ApplyLimb(
            _leftFootJoint,
            _leftFoot,
            _lf,
            true,
            PlayerManager.MovementState.Grounded
        );

        ApplyLimb(
            _rightFootJoint,
            _rightFoot,
            _rf,
            false,
            PlayerManager.MovementState.Grounded
        );
    }

    #endregion

    #region FALL DETECTION

    private void HandleFallState()
    {
        if (!_bodyRb || !_player)
            return;

        // Ignore fall detection while recovering
        if (_recovering)
            return;

        Vector3 localEuler =
            _bodyRb.transform.localEulerAngles;

        float x =
            NormalizeAngle(localEuler.x);

        float bodyOffset =
            Mathf.Abs(x - (-90f));

        float angle =
            bodyOffset;

        if (
            angle > _fallAngle &&
            _player.movementState !=
            PlayerManager.MovementState.Falling
        )
        {
            _player.SetMovementState(
                PlayerManager.MovementState.Falling
            );
        }
        else if (
            angle < _recoverAngle &&
            _player.movementState ==
            PlayerManager.MovementState.Falling
        )
        {
            _player.SetMovementState(
                PlayerManager.MovementState.Grounded
            );
        }

        Debug.Log(
            $"STATE [{_player.movementState}] | " +
            $"CURRENT={angle:F1}"
        );
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;

        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    #endregion

    #region BODY ROTATION

    public void RotateBody(float input)
    {
        if (!_bodyRb)
            return;

        if (
            _player.movementState ==
            PlayerManager.MovementState.Falling
        )
            return;

        _bodyYaw += input * 2f;

        Vector3 torque =
            _bodyRb.transform.up *
            input *
            _turnTorque;

        _bodyRb.AddTorque(
            torque,
            ForceMode.Acceleration
        );

        _bodyRb.angularVelocity =
            Vector3.ClampMagnitude(
                _bodyRb.angularVelocity,
                _maxAngularVelocity
            );
    }

    #endregion

    #region BALANCE

    private void HandleGroundedBalance()
    {
        if (!_player)
            return;

        bool grounded =
            _player.movementState ==
            PlayerManager.MovementState.Grounded
            || _recovering;

        JointDrive drive =
            grounded
            ? _bodyDrive
            : CreateZeroDrive();

        ApplyBalance(
            _bodyJoint,
            drive
        );

        ApplyBalance(
            _leftUpperArmJoint,
            grounded
            ? _upperArmDrive
            : CreateZeroDrive()
        );

        ApplyBalance(
            _rightUpperArmJoint,
            grounded
            ? _upperArmDrive
            : CreateZeroDrive()
        );

        ApplyBalance(
            _leftArmJoint,
            grounded
            ? _armDrive
            : CreateZeroDrive()
        );

        ApplyBalance(
            _rightArmJoint,
            grounded
            ? _armDrive
            : CreateZeroDrive()
        );

        ApplyJointDriveOnly(
            _leftHipsJoint,
            grounded
            ? _hipsDrive
            : CreateZeroDrive()
        );

        ApplyJointDriveOnly(
            _rightHipsJoint,
            grounded
            ? _hipsDrive
            : CreateZeroDrive()
        );

        ApplyJointDriveOnly(
            _leftKneeJoint,
            grounded
            ? _kneeDrive
            : CreateZeroDrive()
        );

        ApplyJointDriveOnly(
            _rightKneeJoint,
            grounded
            ? _kneeDrive
            : CreateZeroDrive()
        );
    }

    private void ApplyJointDriveOnly(
        ConfigurableJoint joint,
        JointDrive drive
    )
    {
        if (!joint)
            return;

        joint.rotationDriveMode =
            RotationDriveMode.Slerp;

        joint.slerpDrive = drive;
    }

    private void ApplyBalance(
        ConfigurableJoint joint,
        JointDrive drive
    )
    {
        if (!joint)
            return;

        joint.rotationDriveMode =
            RotationDriveMode.Slerp;

        JointDrive finalDrive = drive;

        if (_recovering)
        {
            finalDrive.positionSpring *= 5f;
            finalDrive.positionDamper *= 3f;
        }

        joint.slerpDrive = finalDrive;

        if (joint == _bodyJoint)
        {
            joint.targetRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    _bodyYaw
                );
        }
        else
        {
            joint.targetRotation =
                Quaternion.identity;
        }
    }

    #endregion

    #region LIMB INPUT

    public void SetLimbInput(
        LimbType limb,
        Vector2 input
    )
    {
        input =
            Vector2.ClampMagnitude(
                input,
                5f
            );

        switch (limb)
        {
            case LimbType.LeftHand:
                _lh = input;
                break;

            case LimbType.RightHand:
                _rh = input;
                break;

            case LimbType.LeftFoot:
                _lf = input;
                break;

            case LimbType.RightFoot:
                _rf = input;
                break;
        }
    }

    #endregion

    #region LIMB CONTROL

private void ApplyLimb(ConfigurableJoint joint, Rigidbody rb, Vector2 input, bool invert, PlayerManager.MovementState state)
{
    if (!joint) return;

    bool active = input.sqrMagnitude > 0.0001f;
    JointDrive drive = active ? _activeLimbDrive : _limbDrive;

    joint.rotationDriveMode = RotationDriveMode.XYAndZ;
    joint.angularXDrive = drive;
    joint.angularYZDrive = drive;

    float dragMultiplier = 15f;
    float horizontal = Mathf.Clamp(input.x * dragMultiplier, -5f, 5f);
    float vertical = Mathf.Clamp(input.y * dragMultiplier, -5f, 5f);

    if (invert) horizontal *= -1f;

    // Separate configuration variables for independent speed control
    float armPoseSpeed = 256f;
    float legPoseSpeed = 4f;

    // ARMS
// ARMS
if (rb == _leftHand || rb == _rightHand)
{
    if (rb == _leftHand)
    {
        // RIGHT SLIDE => LEFT ARM GO BACK
        _leftUpperArmPoseZ += -horizontal * armPoseSpeed;
        _leftArmPoseX -= vertical * armPoseSpeed;

        _leftUpperArmPoseZ = Mathf.Clamp(_leftUpperArmPoseZ, -90f, 90f);
        _leftArmPoseX = Mathf.Clamp(_leftArmPoseX, -5f, 120f);

        _leftUpperArmJoint.targetRotation =
            Quaternion.Euler(0f, 0f, -_leftUpperArmPoseZ);

        _leftArmJoint.targetRotation =
            Quaternion.Euler(0f, 0f, -_leftArmPoseX);
    }
    else if (rb == _rightHand)
    {
        // RIGHT SLIDE => RIGHT ARM GO FORWARD
        _rightUpperArmPoseZ += horizontal * armPoseSpeed;
        _rightArmPoseX -= vertical * armPoseSpeed;

        _rightUpperArmPoseZ = Mathf.Clamp(_rightUpperArmPoseZ, -90f, 90f);
        _rightArmPoseX = Mathf.Clamp(_rightArmPoseX, -5f, 120f);

        _rightUpperArmJoint.targetRotation =
            Quaternion.Euler(0f, 0f, _rightUpperArmPoseZ);

        _rightArmJoint.targetRotation =
            Quaternion.Euler(0f, 0f, _rightArmPoseX);
    }
}
    // LEGS
    else
    {
        if (rb == _leftFoot)
        {
            _leftHipPose += horizontal * legPoseSpeed;
            _leftKneePose += vertical * legPoseSpeed;

            _leftHipPose = Mathf.Clamp(_leftHipPose, -70f, 70f);
            _leftKneePose = Mathf.Clamp(_leftKneePose, -120f, 5f);

            _leftHipsJoint.targetRotation = Quaternion.Euler(-_leftHipPose, 0f, 0f);
            _leftKneeJoint.targetRotation = Quaternion.Euler(_leftKneePose, 0f, 0f);
        }
        else if (rb == _rightFoot)
        {
            _rightHipPose += horizontal * legPoseSpeed;
            _rightKneePose += vertical * legPoseSpeed;

            _rightHipPose = Mathf.Clamp(_rightHipPose, -70f, 70f);
            _rightKneePose = Mathf.Clamp(_rightKneePose, -120f, 5f);

            _rightHipsJoint.targetRotation = Quaternion.Euler(_rightHipPose, 0f, 0f);
            _rightKneeJoint.targetRotation = Quaternion.Euler(_rightKneePose, 0f, 0f);
        }

        ApplyMovementForce(rb, new Vector2(horizontal, 0f));
    }
}

public void ResetInputs()
{
    _lh = Vector2.zero;
    _rh = Vector2.zero;

    _lf = Vector2.zero;
    _rf = Vector2.zero;

    // return poses slowly
    _leftHipPose =
        Mathf.Lerp(
            _leftHipPose,
            0f,
            Time.deltaTime * 8f
        );

    _rightHipPose =
        Mathf.Lerp(
            _rightHipPose,
            0f,
            Time.deltaTime * 8f
        );

    _leftKneePose =
        Mathf.Lerp(
            _leftKneePose,
            0f,
            Time.deltaTime * 8f
        );

    _rightKneePose =
        Mathf.Lerp(
            _rightKneePose,
            0f,
            Time.deltaTime * 8f
        );

_leftUpperArmPoseX =
    Mathf.Lerp(
        _leftUpperArmPoseX,
        0f,
        Time.deltaTime * 8f
    );

_rightUpperArmPoseX =
    Mathf.Lerp(
        _rightUpperArmPoseX,
        0f,
        Time.deltaTime * 8f
    );

_leftUpperArmPoseZ =
    Mathf.Lerp(
        _leftUpperArmPoseZ,
        0f,
        Time.deltaTime * 8f
    );

_rightUpperArmPoseZ =
    Mathf.Lerp(
        _rightUpperArmPoseZ,
        0f,
        Time.deltaTime * 8f
    );

_leftArmPoseX =
    Mathf.Lerp(
        _leftArmPoseX,
        0f,
        Time.deltaTime * 8f
    );

_rightArmPoseX =
    Mathf.Lerp(
        _rightArmPoseX,
        0f,
        Time.deltaTime * 8f
    );
}

    private void ApplyMovementForce(
        Rigidbody rb,
        Vector2 input
    )
    {
        if (input.sqrMagnitude < 0.0001f)
            return;

        Vector3 localDirection =
            new Vector3(
                input.x,
                0f,
                input.y
            );

        Vector3 worldDirection =
            _root.TransformDirection(
                localDirection
            );

        worldDirection.y = 0f;

        worldDirection.Normalize();

        rb.AddForce(
            worldDirection *
            _movementForce,
            ForceMode.Acceleration
        );
    }

    #endregion

    #region HELPERS

    public void RecoverToGrounded()
    {
        if (_fallTimer < _recoverDelay)
            return;

        if (!_bodyRb)
            return;

        _recovering = true;

        _recoveryWeight = 0f;

        _bodyRb.angularVelocity =
            Vector3.zero;
    }

    private JointDrive CreateDrive(
        float spring,
        float damper,
        float force
    )
    {
        return new JointDrive
        {
            positionSpring = spring,
            positionDamper = damper,
            maximumForce = force
        };
    }

    private JointDrive CreateZeroDrive()
    {
        return new JointDrive
        {
            positionSpring = 0f,
            positionDamper = 0f,
            maximumForce = 0f
        };
    }

    #endregion

    #region ENUMS

    public enum LimbType
    {
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot
    }

    #endregion
}