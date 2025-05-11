using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem
{
    // Use this class to gatekeep powers
    // If the player is not supposed to have a power, then don't instantiate it
    public class PlayerPowersModule
    {
        private EventBus eventBus;
        private PlayerState playerState;
        private ShieldPowerModule squarePower;
        private DrillPowerModule trianglePower;
        private DodgePowerModule circlePower;
        private CancelPowerModule cancelPower;
        private Rigidbody2D rb2d;
        private PhysicsEventsRelay drillPhysicsRelay;
        private PhysicsEventsRelay drillExitPhysicsRelay;
        private PhysicsEventsRelay shieldPhysicsRelay;
        private FixedJoint2D drillJoint;
        private Collider2D dodgeCollider;
        private Collider2D playerCollider;
        private PlayerBaseModule baseModule;

        public PlayerPowersModule(
            EventBus eventBus,
            PlayerState playerState,
            Rigidbody2D rb2d,
            PhysicsEventsRelay drillPhysicsRelay,
            PhysicsEventsRelay drillExitPhysicsRelay,
            FixedJoint2D drillJoint,
            PhysicsEventsRelay shieldPhysicsRelay,
            Collider2D dodgeCollider,
            Collider2D playerCollider,
            PlayerBaseModule baseModule)
        {
            this.eventBus = eventBus;
            this.playerState = playerState;
            this.rb2d = rb2d;
            this.drillPhysicsRelay = drillPhysicsRelay;
            this.drillExitPhysicsRelay = drillExitPhysicsRelay;
            this.drillJoint = drillJoint;
            this.shieldPhysicsRelay = shieldPhysicsRelay;
            this.dodgeCollider = dodgeCollider;
            this.playerCollider = playerCollider;
            this.baseModule = baseModule;

            squarePower = new ShieldPowerModule(eventBus, playerState, rb2d, shieldPhysicsRelay, baseModule);
            trianglePower = new DrillPowerModule(eventBus, playerState, rb2d, drillPhysicsRelay, drillExitPhysicsRelay, drillJoint, baseModule);
            circlePower = new DodgePowerModule(eventBus, playerState, rb2d, dodgeCollider, playerCollider, baseModule);
            cancelPower = new CancelPowerModule(eventBus,playerState);

        }

        public void SetPowerAvailable(Power power, bool isAvailable)
        {
            switch (power)
            {
                case Power.Square:
                    playerState.isSquarePowerAvailable = isAvailable;
                    squarePower ??= new ShieldPowerModule(eventBus, playerState, rb2d, shieldPhysicsRelay, baseModule);
                    break;
                case Power.Triangle:
                    playerState.isTrianglePowerAvailable = isAvailable;
                    trianglePower ??= new DrillPowerModule(eventBus, playerState, rb2d, drillPhysicsRelay, drillExitPhysicsRelay, drillJoint, baseModule);
                    break;
                case Power.Circle:
                    playerState.isCirclePowerAvailable = isAvailable;
                    circlePower ??= new DodgePowerModule(eventBus, playerState, rb2d, dodgeCollider, playerCollider, baseModule);
                    break;
            }
        }
    }
}