using System;
using Unity.VisualScripting;
using UnityEngine;

namespace PlayerSystem
{
    public class CirclePowerModule
    {
        private EventBus eventBus;
        private PlayerState playerState;
        private Rigidbody2D rb2d;
        private TriggerEventHandler leftTrigger;
        private TriggerEventHandler rightTrigger;
        private Knockback knockback;

        private PlayerMovementScriptable movementValues;

        private float powerTimeLeft = 0f;
        private float cooldownTimeLeft = 0f;
        private bool isActive = false;
        private int directionValue = 1;

        public CirclePowerModule(EventBus eventBus, PlayerState playerState, Rigidbody2D rb2d, TriggerEventHandler leftTrigger, TriggerEventHandler rightTrigger, Knockback knockback, PlayerMovementScriptable movementValues)
        {
            this.eventBus = eventBus;
            this.playerState = playerState;
            this.rb2d = rb2d;
            this.leftTrigger = leftTrigger;
            this.rightTrigger = rightTrigger;
            this.knockback = knockback;
            this.movementValues = movementValues;

            eventBus.Subscribe<CirclePowerInputEvent>(activate);
        }

        private void activate(CirclePowerInputEvent e)
        {
            if (isActive) return;
            if (cooldownTimeLeft > 0f) return;
            isActive = true;
            playerState.activePower = Power.Circle;
            directionValue = playerState.facingDirection == Direction.Right ? 1 : -1;
            rb2d.velocity = new Vector2(movementValues.circlePowerForce * directionValue, 0f);
            powerTimeLeft = movementValues.circlePowerDuration;

            TriggerEventHandler triggerToActivate = playerState.facingDirection == Direction.Right ? rightTrigger : leftTrigger;
            triggerToActivate.OnTriggerEnter2DAction.AddListener(onTriggerEnter);
            eventBus.Subscribe<UpdateEvent>(reduceTimeLeft);
            eventBus.Subscribe<UpdateEvent>(deactivateOnMomentumLoss);
            eventBus.Publish(new ToggleCirclePowerEvent(true));
        }

        private void reduceTimeLeft(UpdateEvent e)
        {
            powerTimeLeft -= Time.deltaTime;
            if (0 < powerTimeLeft) return;
            deactivate();
        }

        private void deactivate()
        {
            isActive = false;
            playerState.activePower = Power.None;

            leftTrigger.OnTriggerEnter2DAction.RemoveListener(onTriggerEnter);
            rightTrigger.OnTriggerEnter2DAction.RemoveListener(onTriggerEnter);

            cooldownTimeLeft = movementValues.circlePowerCooldown;
            eventBus.Subscribe<UpdateEvent>(reduceCooldown);

            eventBus.Unsubscribe<UpdateEvent>(reduceTimeLeft);
            eventBus.Unsubscribe<UpdateEvent>(deactivateOnMomentumLoss);
            eventBus.Publish(new ToggleCirclePowerEvent(false));
        }

        private void reduceCooldown(UpdateEvent e)
        {
            cooldownTimeLeft -= Time.deltaTime;
            if (cooldownTimeLeft > 0f) return;

            eventBus.Unsubscribe<UpdateEvent>(reduceCooldown);
        }

        private void deactivateOnMomentumLoss(UpdateEvent e)
        {
            if (0.1f < Mathf.Abs(rb2d.velocity.x)) return;
            deactivate();
        }

        private void onTriggerEnter(Collider2D other)
        {
            if (other.CompareTag("Breakable"))
            {
                eventBus.Publish(new PlayPlayerSounEffect("CircleBreakWall", 0.5f));
            }

            other.gameObject.GetComponent<IPlayerPowerInteractable>()?.PlayerPowerInteraction(playerState);

            if (other.gameObject.layer == 6)
            {
                deactivate();
                knockback.CallKnockback(Vector2.zero, Vector2.up, 0f);
            }
        }
    }
}