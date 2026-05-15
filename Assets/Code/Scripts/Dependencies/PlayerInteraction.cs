using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerManager player;
    public GameObject leftHand;
    public GameObject rightHand;
    public GameObject leftFoot;
    public GameObject rightFoot;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private float interactRange = 2f;

    [Header("Hand Lock")]
    [SerializeField] private float handLockRadius = 0.2f;
    [SerializeField] private float handUnlockHoldDuration = 1f;
    [SerializeField] private float handRelockCooldown = 2f;
    private const string StoneTag = "Stone";

    private Rigidbody leftHandRb;
    private Rigidbody rightHandRb;
    private Rigidbody leftFootRb;
    private Rigidbody rightFootRb;
    private FixedJoint leftHandLock;
    private FixedJoint rightHandLock;
    private FixedJoint leftFootLock;
    private FixedJoint rightFootLock;
    private float leftHandHoldTime;
    private float rightHandHoldTime;
    private float leftFootHoldTime;
    private float rightFootHoldTime;
    private float leftHandRelockTimer;
    private float rightHandRelockTimer;
    private float leftFootRelockTimer;
    private float rightFootRelockTimer;

    private void Awake()
    {
        if (player == null)
            player = FindObjectOfType<PlayerManager>();

        leftHandRb = leftHand.GetComponent<Rigidbody>();
        rightHandRb = rightHand.GetComponent<Rigidbody>();
        leftFootRb = leftFoot.GetComponent<Rigidbody>();
        rightFootRb = rightFoot.GetComponent<Rigidbody>();
    }

    public void TryInteract(PlayerManager player, Transform cam)
    {
        Ray ray = new Ray(cam.position, cam.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            return;

        HandleInteraction(hit, player);
    }

    private void HandleInteraction(RaycastHit hit, PlayerManager player)
    {
        switch (hit.collider.tag)
        {
            case "Stone":
                player.SetMovementState(PlayerManager.MovementState.Climbing);
                player.SetActionState(PlayerManager.ActionState.Interacting);
                break;

            case "Rope":
                player.SetMovementState(PlayerManager.MovementState.Hanging);
                player.SetActionState(PlayerManager.ActionState.Interacting);
                break;

            case "BrokenBridge":
                player.SetMovementState(PlayerManager.MovementState.Falling);
                player.SetActionState(PlayerManager.ActionState.Recovering);
                break;

            case "SafetyRope":
                player.SetMovementState(PlayerManager.MovementState.Hanging);
                player.SetActionState(PlayerManager.ActionState.Recovering);
                break;
        }
    }

    public bool CanMoveLimb(PlayerPhysics.LimbType limb)
    {
        Rigidbody limbRb = GetLimbRigidbody(limb);
        FixedJoint lockJoint = GetLockJoint(limb);

        UpdateCooldown(limb);

        Collider stoneCollider = FindStoneContact(limbRb.position);

        if (lockJoint == null && stoneCollider != null && GetCooldown(limb) <= 0f)
            lockJoint = CreateHandLock(limbRb, stoneCollider);

        if (lockJoint != null)
        {
            bool isActiveLimb = player != null
                && player.ActiveLimb.HasValue
                && player.ActiveLimb.Value == limb;

            if (stoneCollider == null)
            {
                Destroy(lockJoint);
                lockJoint = null;
                SetHoldTime(limb, 0f);
                SetCooldown(limb, handRelockCooldown);
                SetLockJoint(limb, lockJoint);
                return true;
            }

            if (isActiveLimb && Input.GetMouseButton(0))
                SetHoldTime(limb, GetHoldTime(limb) + Time.deltaTime);
            else
                SetHoldTime(limb, 0f);

            if (isActiveLimb && GetHoldTime(limb) >= handUnlockHoldDuration)
            {
                Destroy(lockJoint);
                lockJoint = null;
                SetHoldTime(limb, 0f);
                SetCooldown(limb, handRelockCooldown);
                SetLockJoint(limb, lockJoint);
                return true;
            }

            SetLockJoint(limb, lockJoint);
            return false;
        }

        SetLockJoint(limb, lockJoint);
        return true;
    }

    private Rigidbody GetLimbRigidbody(PlayerPhysics.LimbType limb)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                return leftHandRb;
            case PlayerPhysics.LimbType.RightHand:
                return rightHandRb;
            case PlayerPhysics.LimbType.LeftFoot:
                return leftFootRb;
            case PlayerPhysics.LimbType.RightFoot:
                return rightFootRb;
        }

        return null;
    }

    private FixedJoint GetLockJoint(PlayerPhysics.LimbType limb)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                return leftHandLock;
            case PlayerPhysics.LimbType.RightHand:
                return rightHandLock;
            case PlayerPhysics.LimbType.LeftFoot:
                return leftFootLock;
            case PlayerPhysics.LimbType.RightFoot:
                return rightFootLock;
        }

        return null;
    }

    private void SetLockJoint(
        PlayerPhysics.LimbType limb,
        FixedJoint joint
    )
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                leftHandLock = joint;
                break;
            case PlayerPhysics.LimbType.RightHand:
                rightHandLock = joint;
                break;
            case PlayerPhysics.LimbType.LeftFoot:
                leftFootLock = joint;
                break;
            case PlayerPhysics.LimbType.RightFoot:
                rightFootLock = joint;
                break;
        }
    }

    private float GetHoldTime(PlayerPhysics.LimbType limb)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                return leftHandHoldTime;
            case PlayerPhysics.LimbType.RightHand:
                return rightHandHoldTime;
            case PlayerPhysics.LimbType.LeftFoot:
                return leftFootHoldTime;
            case PlayerPhysics.LimbType.RightFoot:
                return rightFootHoldTime;
        }

        return 0f;
    }

    private void SetHoldTime(PlayerPhysics.LimbType limb, float value)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                leftHandHoldTime = value;
                break;
            case PlayerPhysics.LimbType.RightHand:
                rightHandHoldTime = value;
                break;
            case PlayerPhysics.LimbType.LeftFoot:
                leftFootHoldTime = value;
                break;
            case PlayerPhysics.LimbType.RightFoot:
                rightFootHoldTime = value;
                break;
        }
    }

    private float GetCooldown(PlayerPhysics.LimbType limb)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                return leftHandRelockTimer;
            case PlayerPhysics.LimbType.RightHand:
                return rightHandRelockTimer;
            case PlayerPhysics.LimbType.LeftFoot:
                return leftFootRelockTimer;
            case PlayerPhysics.LimbType.RightFoot:
                return rightFootRelockTimer;
        }

        return 0f;
    }

    private void SetCooldown(PlayerPhysics.LimbType limb, float value)
    {
        switch (limb)
        {
            case PlayerPhysics.LimbType.LeftHand:
                leftHandRelockTimer = value;
                break;
            case PlayerPhysics.LimbType.RightHand:
                rightHandRelockTimer = value;
                break;
            case PlayerPhysics.LimbType.LeftFoot:
                leftFootRelockTimer = value;
                break;
            case PlayerPhysics.LimbType.RightFoot:
                rightFootRelockTimer = value;
                break;
        }
    }

    private void UpdateCooldown(PlayerPhysics.LimbType limb)
    {
        float timer = GetCooldown(limb);
        if (timer <= 0f) return;

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        SetCooldown(limb, timer);
    }

    private Collider FindStoneContact(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(
            position,
            handLockRadius,
            interactLayer,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].CompareTag(StoneTag))
                return hits[i];
        }

        return null;
    }

    private FixedJoint CreateHandLock(
        Rigidbody handRb,
        Collider target
    )
    {
        FixedJoint joint = handRb.gameObject.AddComponent<FixedJoint>();
        joint.breakForce = Mathf.Infinity;
        joint.breakTorque = Mathf.Infinity;
        joint.enableCollision = false;
        joint.autoConfigureConnectedAnchor = false;

        Vector3 contactPoint = target.ClosestPoint(handRb.position);
        joint.anchor = handRb.transform.InverseTransformPoint(contactPoint);

        if (target.attachedRigidbody != null)
        {
            joint.connectedBody = target.attachedRigidbody;
            joint.connectedAnchor =
                target.attachedRigidbody.transform.InverseTransformPoint(contactPoint);
        }
        else
        {
            joint.connectedAnchor = contactPoint;
        }

        return joint;
    }
}