using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "Navigate To Target 2D",
        description: "Navigates a GameObject towards another GameObject using NavMeshAgent." +
        "\nIf NavMeshAgent is not available on the [Agent] or its children, moves the Agent using its transform.",
        story: "[Agent] navigates to [Target]",
        category: "Action/Navigation",
        id: "d704055fc1a59a2ee32d4c80cb04eed0")]
    public partial class NavigateToTarget2DAction : Action
    {
        public enum TargetPositionMode
        {
            ClosestPointOnAnyCollider,      // Use the closest point on any collider, including child objects
            ClosestPointOnTargetCollider,   // Use the closest point on the target's own collider only
            ExactTargetPosition             // Use the exact position of the target, ignoring colliders
        }

        [SerializeReference] public BlackboardVariable<GameObject> Agent;
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        [SerializeReference] public BlackboardVariable<float> Speed = new BlackboardVariable<float>(1.0f);
        [SerializeReference] public BlackboardVariable<float> DistanceThreshold = new BlackboardVariable<float>(0.2f);
        [SerializeReference] public BlackboardVariable<string> AnimatorSpeedParam = new BlackboardVariable<string>("SpeedMagnitude");

        // This will only be used in movement without a navigation agent.
        [SerializeReference] public BlackboardVariable<float> SlowDownDistance = new BlackboardVariable<float>(1.0f);
        [Tooltip("Defines how the target position is determined for navigation:" +
            "\n- ClosestPointOnAnyCollider: Use the closest point on any collider, including child objects" +
            "\n- ClosestPointOnTargetCollider: Use the closest point on the target's own collider only" +
            "\n- ExactTargetPosition: Use the exact position of the target, ignoring colliders. Default if no collider is found.")]
        [SerializeReference] public BlackboardVariable<TargetPositionMode> m_TargetPositionMode = new(TargetPositionMode.ClosestPointOnAnyCollider);

        private NavMeshAgent m_NavMeshAgent;
        private Animator m_Animator;
        private float m_PreviousStoppingDistance;
        private Vector3 m_LastTargetPosition;
        private Vector3 m_ColliderAdjustedTargetPosition;
        private float m_ColliderOffset;

        protected override Status OnStart()
        {
            if (Agent.Value == null || Target.Value == null)
            {
                return Status.Failure;
            }

            return Initialize();
        }

        protected override Status OnUpdate()
        {
            if (Agent.Value == null || Target.Value == null)
            {
                return Status.Failure;
            }

            // Check if the target position has changed.
            bool boolUpdateTargetPosition = !Mathf.Approximately(m_LastTargetPosition.x, Target.Value.transform.position.x) 
                || !Mathf.Approximately(m_LastTargetPosition.y, Target.Value.transform.position.y) 
                || !Mathf.Approximately(m_LastTargetPosition.z, Target.Value.transform.position.z);

            if (boolUpdateTargetPosition)
            {
                m_LastTargetPosition = Target.Value.transform.position;
                m_ColliderAdjustedTargetPosition = GetPositionColliderAdjusted();
            }

            float distance = Vector2.Distance(
                new Vector2(Agent.Value.transform.position.x, Agent.Value.transform.position.y),
                new Vector2(m_ColliderAdjustedTargetPosition.x, m_ColliderAdjustedTargetPosition.y));
                
            if (distance <= (DistanceThreshold + m_ColliderOffset))
            {
                return Status.Success;
            }

            if (m_NavMeshAgent != null)
            {
                if (boolUpdateTargetPosition)
                {
                    m_NavMeshAgent.SetDestination(m_ColliderAdjustedTargetPosition);
                }
            }
            else
            {
                float speed = Speed;

                if (SlowDownDistance > 0.0f && distance < SlowDownDistance)
                {
                    float ratio = distance / SlowDownDistance;
                    speed = Mathf.Max(0.1f, Speed * ratio);
                }

                Vector3 agentPosition = Agent.Value.transform.position;
                Vector3 toDestination = m_ColliderAdjustedTargetPosition - agentPosition;
                toDestination.y = 0.0f;
                toDestination.Normalize();
                agentPosition += toDestination * (speed * Time.deltaTime);
                Agent.Value.transform.position = agentPosition;

                if (toDestination != Vector3.zero)
                {
                    if (toDestination.x != 0)
                    {
                        Vector3 localScale = Agent.Value.transform.localScale;
                        localScale.x = -Mathf.Sign(toDestination.x) * Mathf.Abs(localScale.x);
                        Agent.Value.transform.localScale = localScale;
                    }
                }   
            }

            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (m_Animator != null)
            {
                m_Animator.SetFloat(AnimatorSpeedParam, 0);
            }

            if (m_NavMeshAgent != null)
            {
                if (m_NavMeshAgent.isOnNavMesh)
                {
                    m_NavMeshAgent.ResetPath();
                }
                m_NavMeshAgent.stoppingDistance = m_PreviousStoppingDistance;
            }

            m_NavMeshAgent = null;
            m_Animator = null;
        }

        protected override void OnDeserialize()
        {
            Initialize();
        }

        private Status Initialize()
        {
            m_LastTargetPosition = Target.Value.transform.position;
            m_ColliderAdjustedTargetPosition = GetPositionColliderAdjusted();

            // Add the extents of the colliders to the stopping distance.
            m_ColliderOffset = 0.0f;
            Collider agentCollider = Agent.Value.GetComponentInChildren<Collider>();
            if (agentCollider != null)
            {
                Vector3 colliderExtents = agentCollider.bounds.extents;
                m_ColliderOffset += Mathf.Max(colliderExtents.x, colliderExtents.z);
            }

            if (GetDistanceXZ() <= (DistanceThreshold + m_ColliderOffset))
            {
                return Status.Success;
            }

            // If using animator, set speed parameter.
            m_Animator = Agent.Value.GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                m_Animator.SetFloat(AnimatorSpeedParam, Speed);
            }

            // If using a navigation mesh, set target position for navigation mesh agent.
            m_NavMeshAgent = Agent.Value.GetComponentInChildren<NavMeshAgent>();
            if (m_NavMeshAgent != null)
            {
                if (m_NavMeshAgent.isOnNavMesh)
                {
                    m_NavMeshAgent.ResetPath();
                }
                m_NavMeshAgent.speed = Speed;
                m_PreviousStoppingDistance = m_NavMeshAgent.stoppingDistance;

                m_NavMeshAgent.stoppingDistance = DistanceThreshold + m_ColliderOffset;
                m_NavMeshAgent.SetDestination(m_ColliderAdjustedTargetPosition);
            }

            return Status.Running;
        }

        private Vector3 GetPositionColliderAdjusted()
        {
            switch (m_TargetPositionMode.Value)
            {
                case TargetPositionMode.ClosestPointOnAnyCollider:
                    Collider anyCollider = Target.Value.GetComponentInChildren<Collider>(includeInactive: false);
                    if (anyCollider == null || anyCollider.enabled == false) 
                        break;
                    return anyCollider.ClosestPoint(Agent.Value.transform.position);
                case TargetPositionMode.ClosestPointOnTargetCollider:
                    Collider targetCollider = Target.Value.GetComponent<Collider>();
                    if (targetCollider == null || targetCollider.enabled == false) 
                        break;
                    return targetCollider.ClosestPoint(Agent.Value.transform.position);
            }

            // Default to target position.
            return Target.Value.transform.position;
        }

        private float GetDistanceXZ()
        {
            Vector3 agentPosition = new Vector3(Agent.Value.transform.position.x, m_ColliderAdjustedTargetPosition.y, Agent.Value.transform.position.z);
            return Vector3.Distance(agentPosition, m_ColliderAdjustedTargetPosition);
        }
    }
}

