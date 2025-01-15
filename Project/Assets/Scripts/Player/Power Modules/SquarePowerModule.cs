using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;

namespace PlayerSystem
{
    public class SquarePowerModule
    {
        private EventBus eventBus;
        private PlayerState playerState;
        private Rigidbody2D rb2d;
        private TriggerEventHandler groundTrigger;
        private Knockback knockback;
        private PlayerMovementScriptable movementValues;

        private readonly float minPowerDuration = 0.2f;
        private float powerTimeSum = 0f;
        private float cooldownTimeLeft = 0f;
        private bool isActive = false;

        public SquarePowerModule(EventBus eventBus, PlayerState playerState, Rigidbody2D rb2d, TriggerEventHandler groundTrigger, Knockback knockback, PlayerMovementScriptable movementValues)
        {
            this.eventBus = eventBus;
            this.playerState = playerState;
            this.rb2d = rb2d;
            this.groundTrigger = groundTrigger;
            this.playerState = playerState;
            this.knockback = knockback;
            this.movementValues = movementValues;

            eventBus.Subscribe<SquarePowerInputEvent>(togglePower);
        }

        private void togglePower(SquarePowerInputEvent e)
        {
            if (e.toggle == isActive) return;
            if (e.toggle) activate();
            else /*if (minPowerDuration < powerTimeSum)*/ deactivate();
            // Todo: Current implementation of minPowerDuration feels uncomfortable during play, figure a way to better handle deactivation input
        }

        private void activate()
        {
            if (0f < cooldownTimeLeft) return;

            isActive = true;
            playerState.activePower = Power.Square;
            if (playerState.groundState == GroundState.Grounded) rb2d.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            powerTimeSum = 0;
            rb2d.velocity = new Vector2(0, movementValues.squarePowerForce);
            groundTrigger.OnTriggerEnter2DAction.AddListener(onTriggerEnter);

            eventBus.Subscribe<UpdateEvent>(addTimeSum);
            eventBus.Publish(new ToggleSquarePowerEvent(true));
        }

        private void addTimeSum(UpdateEvent e)
        {
            powerTimeSum += Time.deltaTime;
        }

        private void onTriggerEnter(Collider2D other)
        {
            if (other.CompareTag("Breakable"))
            {
                eventBus.Publish(new PlayPlayerSounEffect("SquareBreakWall", 0.5f));
            }
            if (other.gameObject.CompareTag("Platform"))
            {
                if (other.gameObject.GetComponent<IPlatform>().PlatformType == PlatformType.CrumblingPlatform) eventBus.Publish(new PlayPlayerSounEffect("SquareBlock"));
            }
            other.gameObject.GetComponent<IPlayerPowerInteractable>()?.PlayerPowerInteraction(playerState);
            if (other.gameObject.CompareTag("Spike"))
            {
                knockback.CallKnockback(Vector2.zero, Vector2.up * movementValues.spikeKnockbackForce, Input.GetAxisRaw("Horizontal"));
                eventBus.Publish(new PlayPlayerSounEffect("SquareBlockSpikes"));
                deactivate();
            }
            if (other.gameObject.CompareTag("Ground"))
            {
                rb2d.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
                eventBus.Publish(new PlayPlayerSounEffect("SquareBlock"));
            }
        }

        private void deactivate()
        {
            isActive = false;
            playerState.activePower = Power.None;
            rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
            cooldownTimeLeft = movementValues.squarePowerCooldown;
            groundTrigger.OnTriggerEnter2DAction.RemoveListener(onTriggerEnter);

            eventBus.Subscribe<UpdateEvent>(reduceCooldown);
            eventBus.Unsubscribe<UpdateEvent>(addTimeSum);
            eventBus.Publish(new ToggleSquarePowerEvent(false));
        }

        private void reduceCooldown(UpdateEvent e)
        {
            cooldownTimeLeft -= Time.deltaTime;
            if (0 < cooldownTimeLeft) return;

            eventBus.Unsubscribe<UpdateEvent>(reduceCooldown);
        }
    }
}