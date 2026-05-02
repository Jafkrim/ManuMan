using UnityEngine;

public class PlayerPhysics : MonoBehaviour
{
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
    [SerializeField] private float _forceMultiplier = 50f;

    private Vector3 _currentForce;

    public void ApplyLimbForce(LimbType limb, Vector2 input)
    {
        Vector3 force = new Vector3(input.x, input.y, 0f) * _forceMultiplier;

        switch (limb)
        {
            case LimbType.LeftHand:
                _leftHand.AddForce(force, ForceMode.Acceleration);
                break;

            case LimbType.RightHand:
                _rightHand.AddForce(force, ForceMode.Acceleration);
                break;

            case LimbType.LeftFoot:
                _leftFoot.AddForce(force, ForceMode.Acceleration);
                break;

            case LimbType.RightFoot:
                _rightFoot.AddForce(force, ForceMode.Acceleration);
                break;
        }
    }

    public enum LimbType
    {
        LeftHand,
        RightHand,
        LeftFoot,
        RightFoot
    }
}