using UnityEngine;
using UnityEngine.Events;

public class Gem : MonoBehaviour, ICollectable
{
    public CollectableType CollectableType { get; set; }
    public UnityEvent collect;
    public GameManager gameManager { get; set; }

    private void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameManager>();
        collect.AddListener(gameManager.GetGem);
    }

    public void Collect()
    {
        gameObject.SetActive(false);
        collect.Invoke();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Collect();
        }
    }
}