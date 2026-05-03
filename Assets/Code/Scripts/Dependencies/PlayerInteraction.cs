using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private float interactRange = 2f;

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
}