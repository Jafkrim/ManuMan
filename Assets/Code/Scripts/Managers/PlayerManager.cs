using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    #region INPUT

    private const KeyCode RecoverKey = KeyCode.Space;
    private const KeyCode LeftHandKey = KeyCode.A;
    private const KeyCode RightHandKey = KeyCode.F;
    private const KeyCode LeftFootKey = KeyCode.S;
    private const KeyCode RightFootKey = KeyCode.D;

    private const int LimbMoveMouseButton = 0;

    private const string MouseXAxis = "Mouse X";
    private const string MouseYAxis = "Mouse Y";

    #endregion

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

    #region UNITY

    private void Update()
    {
        TickConditions();

        HandleLimbSelection();
        HandleLimbMovement();
        HandleRecovery();
        HandleSlowMotion();
    }

    #endregion

    #region INPUT HANDLING

    private void HandleRecovery()
    {
        if (!Input.GetKeyDown(RecoverKey))
            return;

        if (_movementState != MovementState.Falling)
            return;

        _physics.RecoverToGrounded();
    }

    public GameManager gameManager;
    private void HandleSlowMotion()
    {
        if (!Input.GetKeyDown(RecoverKey))
            return;

        if (_movementState != MovementState.Climbing)
            return;

        gameManager.SlowDownTime();
    }

    private void HandleLimbSelection()
    {
        if (Input.GetKeyDown(LeftHandKey))
            ToggleLimb(PlayerPhysics.LimbType.LeftHand);
        else if (Input.GetKeyDown(RightHandKey))
            ToggleLimb(PlayerPhysics.LimbType.RightHand);
        else if (Input.GetKeyDown(LeftFootKey))
            ToggleLimb(PlayerPhysics.LimbType.LeftFoot);
        else if (Input.GetKeyDown(RightFootKey))
            ToggleLimb(PlayerPhysics.LimbType.RightFoot);
    }

    private void ToggleLimb(PlayerPhysics.LimbType limb)
    {
        if (_activeLimb == limb)
            _activeLimb = null;
        else
            _activeLimb = limb;
    }

    private void HandleLimbMovement()
    {
        _physics.ResetInputs();

        bool holdingLmb =
            Input.GetMouseButton(
                LimbMoveMouseButton
            );

        if (!holdingLmb)
            return;

        if (!CanMove())
            return;

        Vector2 mouseDelta =
            GetMouseDelta();

        // BODY ROTATION MODE
        if (_activeLimb == null)
        {
            _physics.RotateBody(
                mouseDelta.x
            );

            return;
        }

        // Deadzone
        if (mouseDelta.sqrMagnitude < 0.0001f)
            return;

        // LIMB CONTROL MODE
        MoveLimb(
            _activeLimb.Value,
            mouseDelta
        );
    }

    private Vector2 GetMouseDelta()
    {
        return new Vector2(
            Input.GetAxis(MouseXAxis),
            Input.GetAxis(MouseYAxis)
        );
    }

    #endregion

    #region CORE CONTROL

    public void MoveLimb(
        PlayerPhysics.LimbType limb,
        Vector2 input
    )
    {
        _physics.SetLimbInput(limb, input);
    }

    public void TryInteract(Transform cam)
    {
        if (!CanAct()) return;

        _interaction.TryInteract(this, cam);
    }

    #endregion

    #region STATE

    public void SetMovementState(MovementState state)
        => _movementState = state;

    public void SetActionState(ActionState state)
        => _actionState = state;

    public bool CanMove()
        => true;

    public bool CanAct()
        => _actionState != ActionState.Locked;

    #endregion

    #region CONDITIONS

    private void TickConditions()
    {
        for (int i = _conditions.Count - 1; i >= 0; i--)
        {
            _conditions[i].duration -= Time.deltaTime;

            if (_conditions[i].duration <= 0f)
                _conditions.RemoveAt(i);
        }
    }

    public void AddCondition(
        ConditionType type,
        float duration,
        float intensity
    )
    {
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (_conditions[i].type == type)
            {
                _conditions[i].duration = Mathf.Max(
                    _conditions[i].duration,
                    duration
                );

                _conditions[i].intensity = intensity;

                return;
            }
        }

        _conditions.Add(
            new ConditionEffect(
                type,
                duration,
                intensity
            )
        );
    }

    #endregion

    #region ENUMS

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

    #endregion

    #region INTERNAL

    private class ConditionEffect
    {
        public ConditionType type;
        public float duration;
        public float intensity;

        public ConditionEffect(
            ConditionType t,
            float d,
            float i
        )
        {
            type = t;
            duration = d;
            intensity = i;
        }
    }

    #endregion
}