using System.Collections;
using PlayerSystem;
using UnityEngine;

public class Knockback : MonoBehaviour
{
    public float knockbackTime = 0.2f;
    public float hitDirectionForce = 10f;
    public float constForce = 5f;
    public float inputForce = 7.5f;

    private Color startColor;
    private Coroutine knockbackCoroutine;
    private Rigidbody2D rb2d;
    private PlayerBaseModule playerBaseModule;


    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        playerBaseModule = GetComponent<PlayerBaseModule>();
    }

    public IEnumerator KnockbackAction(Vector2 hitDirection, Vector2 constantForceDirection, float inputDirection, bool isStagger = false)
    {
        if (isStagger) playerBaseModule.state.healthState = HealthState.Stagger;
        Vector2 _hitForce;
        Vector2 _constantForce;
        Vector2 _knockbackForce;
        Vector2 _combinedForce;

        _hitForce = hitDirection * hitDirectionForce;
        _constantForce = constantForceDirection * constForce;

        float _elapsedTime = 0f;

        while (_elapsedTime < knockbackTime)
        {
            // iterate timer
            _elapsedTime += Time.fixedDeltaTime;

            //combine hitforce and constantforce
            _knockbackForce = _hitForce + _constantForce;

            //combine knockbackForce and input force
            if (inputDirection != 0)
            {
                _combinedForce = _knockbackForce + new Vector2(inputDirection, 0);

            }
            else
            {
                _combinedForce = _knockbackForce;
            }

            //apply knockback
            rb2d.linearVelocity = _combinedForce;
            yield return new WaitForFixedUpdate();
        }

        playerBaseModule.state.healthState = HealthState.Undefined;
    }

    public void CallKnockback(Vector2 hitDirection, Vector2 constantForceDirection, float inputDirection, bool isStagger = false)
    {
        knockbackCoroutine = StartCoroutine(KnockbackAction(hitDirection, constantForceDirection, inputDirection, isStagger));
    }
}
