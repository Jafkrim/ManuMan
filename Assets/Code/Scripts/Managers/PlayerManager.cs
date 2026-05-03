using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private MovementState _movementState;
    [SerializeField] private ActionState _actionState;

    [Header("References")]
    [SerializeField] private PlayerPhysics _physics;
    [SerializeField] private PlayerInteraction _interaction;

    private readonly List<ConditionEffect> _conditions = new();

    private PlayerPhysics.LimbType? _activeLimb;

    public MovementState movementState => _movementState;
    public ActionState actionState => _actionState;
    public PlayerPhysics.LimbType? ActiveLimb => _activeLimb;

    private void Update()
    {
        TickConditions();

        HandleLimbSelection();
        HandleLimbMovement();
    }

    // ---------------- INPUT ----------------

    private void HandleLimbSelection()
    {
        if (Input.GetKeyDown(KeyCode.A))
            _activeLimb = PlayerPhysics.LimbType.LeftHand;

        else if (Input.GetKeyDown(KeyCode.F))
            _activeLimb = PlayerPhysics.LimbType.RightHand;

        else if (Input.GetKeyDown(KeyCode.S))
            _activeLimb = PlayerPhysics.LimbType.LeftFoot;

        else if (Input.GetKeyDown(KeyCode.D))
            _activeLimb = PlayerPhysics.LimbType.RightFoot;
    }

    private void HandleLimbMovement()
    {
        if (_activeLimb == null) return;
        if (!Input.GetMouseButton(0)) return; // LMB required
        if (!CanMove()) return;

        Vector2 mouseDelta = GetMouseDelta();

        // small deadzone to avoid jitter
        if (mouseDelta.sqrMagnitude < 0.0001f) return;

        MoveLimb(_activeLimb.Value, mouseDelta);
    }

    private Vector2 GetMouseDelta()
    {
        return new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );
    }

    // ---------------- CORE CONTROL ----------------

    public void MoveLimb(PlayerPhysics.LimbType limb, Vector2 input)
    {
        _physics.SetLimbInput(limb, input);
    }

    public void TryInteract(Transform cam)
    {
        if (!CanAct()) return;
        _interaction.TryInteract(this, cam);
    }

    // ---------------- STATE ----------------

    public void SetMovementState(MovementState state) => _movementState = state;
    public void SetActionState(ActionState state) => _actionState = state;

    public bool CanMove() => _movementState != MovementState.Falling;
    public bool CanAct() => _actionState != ActionState.Locked;

    // ---------------- CONDITIONS ----------------

    private void TickConditions()
    {
        for (int i = _conditions.Count - 1; i >= 0; i--)
        {
            _conditions[i].duration -= Time.deltaTime;

            if (_conditions[i].duration <= 0f)
                _conditions.RemoveAt(i);
        }
    }

    public void AddCondition(ConditionType type, float duration, float intensity)
    {
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (_conditions[i].type == type)
            {
                _conditions[i].duration = Mathf.Max(_conditions[i].duration, duration);
                _conditions[i].intensity = intensity;
                return;
            }
        }

        _conditions.Add(new ConditionEffect(type, duration, intensity));
    }

    // ---------------- ENUMS ----------------

    public enum MovementState
    {
        Grounded,
        Climbing,
        Hanging,
        Falling,
        Swimming
    }

    public enum ActionState
    {
        None,
        Recovering,
        Interacting,
        Locked
    }

    public enum ConditionType
    {
        BlurVision,
        UnstableControl,
        CameraShake
    }

    private class ConditionEffect
    {
        public ConditionType type;
        public float duration;
        public float intensity;

        public ConditionEffect(ConditionType t, float d, float i)
        {
            type = t;
            duration = d;
            intensity = i;
        }
    }
}