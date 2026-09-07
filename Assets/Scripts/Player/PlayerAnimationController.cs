using UnityEngine;
using MazeRunner.Customization;

namespace MazeRunner.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterRenderer))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private PlayerController playerController;
        private CharacterRenderer characterRenderer;
        private Animator[] layerAnimators;
        
        private enum AnimationState { Idle, Run, Jump, Fall }
        private AnimationState currentState = AnimationState.Idle;
        
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            characterRenderer = GetComponent<CharacterRenderer>();
            
            layerAnimators = new Animator[8];
            layerAnimators[0] = GetLayerAnimator(CustomizationCategoryType.Body);
            layerAnimators[1] = GetLayerAnimator(CustomizationCategoryType.Eyes);
            layerAnimators[2] = GetLayerAnimator(CustomizationCategoryType.Hair);
            layerAnimators[3] = GetLayerAnimator(CustomizationCategoryType.Shirt);
            layerAnimators[4] = GetLayerAnimator(CustomizationCategoryType.Pants);
            layerAnimators[5] = GetLayerAnimator(CustomizationCategoryType.Shoes);
            layerAnimators[6] = GetLayerAnimator(CustomizationCategoryType.Hat);
            layerAnimators[7] = GetLayerAnimator(CustomizationCategoryType.Accessory);
        }
        
        private Animator GetLayerAnimator(CustomizationCategoryType type)
        {
            SpriteRenderer renderer = characterRenderer.GetRenderer(type);
            if (renderer == null) return null;
            
            Animator animator = renderer.GetComponent<Animator>();
            if (animator == null)
            {
                animator = renderer.gameObject.AddComponent<Animator>();
            }
            return animator;
        }
        
        private void Update()
        {
            AnimationState newState = DetermineState();
            
            if (newState != currentState)
            {
                currentState = newState;
                ApplyState(currentState);
            }
            
            UpdateFlip();
        }
        
        private AnimationState DetermineState()
        {
            if (!playerController.IsGrounded)
            {
                return playerController.Velocity.y > 0 ? AnimationState.Jump : AnimationState.Fall;
            }
            
            return Mathf.Abs(playerController.Velocity.x) > 0.1f ? AnimationState.Run : AnimationState.Idle;
        }
        
        private void ApplyState(AnimationState state)
        {
            string stateName = state.ToString();
            
            foreach (Animator animator in layerAnimators)
            {
                if (animator != null && animator.runtimeAnimatorController != null)
                {
                    animator.Play(stateName);
                }
            }
        }
        
        private void UpdateFlip()
        {
            if (Mathf.Abs(playerController.Velocity.x) > 0.1f)
            {
                float scaleX = playerController.Velocity.x > 0 ? 1f : -1f;
                
                foreach (Animator animator in layerAnimators)
                {
                    if (animator != null)
                    {
                        Transform t = animator.transform;
                        t.localScale = new Vector3(scaleX, t.localScale.y, t.localScale.z);
                    }
                }
            }
        }
    }
}
