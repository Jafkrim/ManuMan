using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private MovementState _movementState;
    [SerializeField] private ActionState _actionState;

    [Header("References")]
    [SerializeField] private PlayerPhysics _physics;
    [SerializeField] private PlayerInteraction _interaction;
    private readonly List<ConditionEffect> _activeConditions = new();

    public MovementState movementState => _movementState;
    public ActionState actionState => _actionState;
    public PlayerPhysics physics => _physics;
    public PlayerInteraction interaction => _interaction;

    private void Update()
    {
        UpdateConditions();
    }

    public void SetMovementState(MovementState state)
    {
        _movementState = state;
    }

    public void SetActionState(ActionState state)
    {
        _actionState = state;
    }

    public bool HasCondition(ConditionType type)
    {
        return _activeConditions.Exists(c => c.type == type);
    }

    private void UpdateConditions()
    {
        for (int i = _activeConditions.Count - 1; i >= 0; i--)
        {
            var c = _activeConditions[i];
            c.duration -= Time.deltaTime;

            if (c.duration <= 0f)
                _activeConditions.RemoveAt(i);
        }
    }

    public void AddCondition(ConditionType type, float duration, float intensity)
    {
        // check if already exists
        for (int i = 0; i < _activeConditions.Count; i++)
        {
            if (_activeConditions[i].type == type)
            {
                // refresh + overwrite stronger effect
                _activeConditions[i].duration = Mathf.Max(_activeConditions[i].duration, duration);
                _activeConditions[i].intensity = intensity;
                return;
            }
        }

        // if not exists → add new
        _activeConditions.Add(new ConditionEffect
        {
            type = type,
            duration = duration,
            intensity = intensity
        });
    }

    public void RemoveCondition(ConditionType type)
    {
        _activeConditions.RemoveAll(c => c.type == type);
    }

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

    [System.Serializable]
    private class ConditionEffect
    {
        public ConditionType type;
        public float duration;
        public float intensity;
    }
}