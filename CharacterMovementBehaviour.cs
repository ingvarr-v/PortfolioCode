using Helmeton.Experimental.ScenarioSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Helmeton
{
    public class CharacterMovementBehaviour : MonoBehaviour
    {
        [Header("Component References")]

        [SerializeField]
        private NavMeshAgent _navMeshAgent;
        public NavMeshAgent NavMeshAgent
        {
            get => _navMeshAgent;
            private set => _navMeshAgent = value;
        }

        [SerializeField]
        private InteractionListener _navMeshAgentListener;
        public InteractionListener NavMeshAgentListener
        {
            get => _navMeshAgentListener;
            private set => _navMeshAgentListener = value;
        }

        [Header("Options")]

        public float RotationToTargetSpeed = 2f;

        private float _velocityMagnitude;
        public float VelocityMagnitude => _velocityMagnitude;

        private Transform _currentTargetTransform;

        private Coroutine _rotationRoutine; 

        public void InitBehaviour()
        {
            if (_navMeshAgent == null)
            {
                _navMeshAgent = transform.GetComponent<NavMeshAgent>();
            }

            _navMeshAgent.enabled = false;
        }

        private void Update()
        {
            UpdateMovement();
        }

        public void Move(Transform target)
        {
            if (_rotationRoutine != null)
                StopCoroutine(_rotationRoutine);
            NavMeshAgent.enabled = true;
            NavMeshAgent.SetDestination(target.position);
            _currentTargetTransform = target;
        }

        public void Teleport(Transform target)
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
        }

        private void OnPathComplete(Transform target)
        {
            FaceTarget(target);
            NavMeshAgentListener.Interact();
        }

        private void UpdateMovement()
        {
            if (!NavMeshAgent.enabled)
                return;

            if (!NavMeshAgent.pathPending)
            {
                if (NavMeshAgent.remainingDistance <= NavMeshAgent.stoppingDistance)
                {
                    if (!NavMeshAgent.hasPath || NavMeshAgent.velocity.sqrMagnitude == 0f)
                    {
                        OnPathComplete(_currentTargetTransform);
                        NavMeshAgent.enabled = false;
                    }
                }
            }

            _velocityMagnitude = NavMeshAgent.velocity.magnitude;
        }

        public void FaceTarget(Transform target)
        {
            if (_rotationRoutine != null)
                StopCoroutine(_rotationRoutine);
            _rotationRoutine = StartCoroutine(FaceTargetRoutine(target));
        }

        private IEnumerator FaceTargetRoutine(Transform target)
        {
            float elapsedTime = 0f;

            while (elapsedTime < RotationToTargetSpeed)
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, (elapsedTime / RotationToTargetSpeed));
                elapsedTime += Time.deltaTime;

                yield return null;
            }

            transform.rotation = target.rotation;
            yield return null;
        }
    }
}
