using System.Collections;
using DG.Tweening;
using PlayerSystem;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FormState
{
    Undefined = -1,
    Circle = 0,
    Square = 1,
    Triangle = 2
}

public enum LifeIconState
{
    Undefined,
    Normal,
    Damaged,
    Off,
}
public enum ObstacleType
{
    Asteroid,
    Hollow,
    Block,
}

public enum PlatformType
{
    MovingPlatform,
    FallingPlatform,
    CrumblingPlatform,
}

public enum CollectableType
{
    Prism,
    Gem,
    Power,
}

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private PlayerBaseModule player;


    [Header("Collectables")]
    [SerializeField] private int levelTargetGems = 3;
    [SerializeField] private int collectedGems = 0;
    [SerializeField] private int collectedPrisms = 0;
    [SerializeField] private CollectablesUIController collectablesUIController;

    [Header("GameWin")]
    [SerializeField] private GameObject[] endGamePortals;


    #region Old Code
    public static int maxLifes = 3;
    public static FormState startingForm = FormState.Square;
    #endregion

    private void Awake()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerBaseModule>();
    }
    private void Start()
    {
        collectablesUIController.InitUI(collectedGems, collectedPrisms);
    }

    public void GetGem()
    {
        collectedGems++;
        collectablesUIController.UpdateGemsUI(collectedGems);
        if (collectedGems == levelTargetGems)
        {
            //clear level
            Debug.Log("Game Cleared");
            StartCoroutine(EnableGameEndPortals());
        }
    }

    public void GetPrism()
    {
        collectedPrisms++;
        collectablesUIController.UpdatePrismsUI(collectedPrisms);
    }

    private IEnumerator EnableGameEndPortals()
    {

        foreach (GameObject endGamePortal in endGamePortals)
        {
            endGamePortal.transform.localScale = Vector2.zero;
            endGamePortal.SetActive(true);
            InputActionMap playerInput = InputSystem.actions.FindActionMap("Player");
            playerInput.Disable();
        }

        yield return new WaitForSeconds(2f);
    }

}


