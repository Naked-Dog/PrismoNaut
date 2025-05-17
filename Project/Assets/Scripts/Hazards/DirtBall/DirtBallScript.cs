using System.Collections;
using System.Runtime.CompilerServices;
using System.Xml;
using PlayerSystem;
using Unity.VisualScripting;
using UnityEngine;

public class DirtBallScript : MonoBehaviour
{
    [SerializeField] private float impulseLimit;
    [SerializeField]  private float maxSpeed = 10;
    private Rigidbody2D rb;
    public DirtSpawner spawner;
    private Animator anim;
    private string randomAnim;
    private string currentAnim;
    private const float baseDeathTime = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        randomAnim = Random.Range(0, 2) == 0 ? "RollingRight" : "RollingLeft";
    }

    //private void FixedUpdate()
    //{
    //    rb.linearVelocity = direction * speed * Time.fixedDeltaTime * 20;
    //}

    private void LateUpdate()
    {
        ControlRollingAnim();
    }

    public void setInitialSpeed(Vector2 direction, float speed)
    {
        rb.linearVelocity = direction * speed;
        rb.AddForce(direction * speed, ForceMode2D.Impulse);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float normalImpulse = collision.contacts[0].normalImpulse;
        PlayerBaseModule player = collision.gameObject.GetComponent<PlayerBaseModule>();
        if (player && player.state.activePower == PlayerSystem.Power.Drill) return;
        if (normalImpulse > impulseLimit)
        {
            Death();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<DirtSpawner>())
        {
            StartCoroutine(DieAfterTime(baseDeathTime));
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<DirtSpawner>()) StopAllCoroutines();
    }

    IEnumerator DieAfterTime(float time)
    {
        yield return new WaitForSeconds(time);

        Death();
    }

    private void Death()
    {
        spawner.SpawnDirtBall(spawner.reloadTime);
        Destroy(gameObject, 0.1f);//animation later
    }

    private void ControlRollingAnim()
    {
        string animClip;
        if (rb.linearVelocityX > 0)
        {
            animClip = "RollingRight";
        }
        else if (rb.linearVelocityX < 0)
        {
            animClip = "RollingLeft";
        }
        else
        {
            animClip = randomAnim;
        }

        if(currentAnim != animClip)
        {
            currentAnim = animClip;
            anim.Play(animClip);
        }

        float minSpeed = Mathf.Abs(rb.linearVelocityY) > 0 ? 0.3f : 0;
        float LerpSpeed = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(rb.linearVelocityX));
        float animationSpeed = Mathf.Max(LerpSpeed, minSpeed);
        anim.speed = animationSpeed;
    }
}
