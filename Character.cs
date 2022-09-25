//using Bolt;
//using PixelCrushers.DialogueSystem;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
//using UnityEngine.XR.Interaction.Toolkit;

namespace Helmeton.Experimental.ScenarioSystem
{
    public class Character : MonoBehaviour
    {
        [Header("Components")]

        [SerializeField]
        private CharacterMovementBehaviour _movementBehaviour;
        public CharacterMovementBehaviour MovementBehaviour
        {
            get => _movementBehaviour;
            private set => _movementBehaviour = value;
        }

        [SerializeField]
        private CharacterIKBehaviour _iKBehaviour;
        public CharacterIKBehaviour IKBehaviour
        {
            get => _iKBehaviour;
            private set => IKBehaviour = value;
        }

        [SerializeField]
        private CharacterAnimationBehaviour _animationBehaviour;
        public CharacterAnimationBehaviour AnimationBehaviour
        {
            get => _animationBehaviour;
            private set => _animationBehaviour = value;
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            AnimationBehaviour.SetMovementSmoothing(MovementBehaviour.VelocityMagnitude);
        }

        private void Init()
        {
            MovementBehaviour.InitBehaviour();
            IKBehaviour.InitBehaviour();
            AnimationBehaviour.InitBehaviour();
        }

        public void Move(Transform transform)
        {
            if (IKBehaviour.ClearIKOnMove)
                IKBehaviour.ClearIK();
            MovementBehaviour.Move(transform);
        }

        public void Rotate(Transform target)
        {
            MovementBehaviour.FaceTarget(target);
        }

        public void Teleport(Transform transform)
        {
            MovementBehaviour.Teleport(transform);
        }

        public void Animation(string state)
        {
            AnimationBehaviour.Animation(state);
        }

        public void SetLeftHandClench(float value)
        {
            AnimationBehaviour.SetLeftHandClench(value);
        }

        public void SetRightHandClench(float value)
        {
            AnimationBehaviour.SetRightHandClench(value);
        }

        public void LookAt(Transform target)
        {
            AnimationBehaviour.LookAt(target);
        }

        public void FreeLook()
        {
            AnimationBehaviour.FreeLook();
        }
    }
}
