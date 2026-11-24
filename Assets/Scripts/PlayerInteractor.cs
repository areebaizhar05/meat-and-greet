using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement; // addign this for game voer concept for now will make game amnager later 

//PLAYER CONTROLS:
// E = use the closest thing nearby (counter: pick up plate, customer: serve).
//on empty non itneractable it says :mnothign to itneract with
// Q = clean the closest dirty table.
// F = kill aN ANOMALY OR CUSTOMER 
// R = restart after game over
//3 strikes (kill customer / serve anomaly / anomaly escapes) = game over, R to restart
public class PlayerInteractor : MonoBehaviour
{
    public float radius = 1.2f;

    //is the player carrying a plate? (Only one at a time.)
    public bool holdingPlate;
    public GameObject heldPlateVisual;


    //game over: mutliple conditions leads to total 3 strikes and causes game over
    public int maxStrikes = 3;   //3 mistakes = game over
    int strikes;
    bool gameOver;
    //-----


    CafeCustomerSpawner spawner;

    void Start()
    {
        spawner = FindFirstObjectByType<CafeCustomerSpawner>();
        GameAnalytics.RoundStarted();
    }

    void Update()
    {
        if (Keyboard.current == null) return;
        if (heldPlateVisual != null) heldPlateVisual.SetActive(holdingPlate);

        //game over cncpet: everything is frozen, only R works.
        if (gameOver)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f;   // unfreeze
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);   // start over
            }
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
            PressE();

        if (Keyboard.current.qKey.wasPressedThisFrame)
            PressQ();

        if (Keyboard.current.fKey.wasPressedThisFrame)
            PressF();
    }

    void PressE()
    {
        Interactable target = FindClosest();
        if (target != null)
            target.Interact(this);
        else
            Debug.Log("[Player] Nothing to interact with");
    }

    void PressF()
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            CafeCustomer customer = hit.GetComponentInParent<CafeCustomer>();
            if (customer != null) // && customer.isAnomaly had this condition but now we kill both :) and make life difficult
            {
                //customer.Kill();
                //return;
                //leaving the above two linees uncommented cause will need em later whene we make gamee manager
                //specific for gameovere now
                bool wasNormalCustomer = !customer.isAnomaly;
                customer.Kill();

                //if (wasNormalCustomer)
                //{
                //    customersKilled++;
                //    Debug.Log("[Risk] Customers killed: " + customersKilled + "/" + maxKills);

                //    if (customersKilled >= maxKills)
                //    {
                //        gameOver = true;
                //        Time.timeScale = 0f;   // freeze the game
                //        Debug.Log("GAME OVER! You killed " + customersKilled + " customers. Press R to restart.");
                //    }
                //}
                if (wasNormalCustomer)
                    AddStrike("Killed a customer"); // now part of the strike category 
                return;
            }
        }
        Debug.Log("[ACTION: KILL] Nobody nearby"); //changed the message to not specific to anomaly but general killing
    }

    //a mistake: killed a customer, served an anomaly, or let an anomaly leave.
    public void AddStrike(string reason)
    {
        if (gameOver) return;
        strikes++;
        Debug.Log("[Strike] " + reason + " (" + strikes + "/" + maxStrikes + ")");

        if (strikes >= maxStrikes)
        {
            gameOver = true;
            Time.timeScale = 0f;   //freeze the game
            Debug.Log("GAME OVER! 3 strikes. Press R to restart.");
            //GameAnalytics.Record("gameOver", "3 strikes", strikes);
            //GameAnalytics.Flush();
        }
    }
    void PressQ()
    {
        bool cleaned = spawner != null && spawner.CleanTableNear(transform.position, radius);
        if (!cleaned)
            Debug.Log("[Table] No dirty table nearby");
    }

    // Find the closest object in range that can be used right now.
    Interactable FindClosest()
    {
        Interactable closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, radius))
        {
            Interactable thing = hit.GetComponentInParent<Interactable>();
            if (thing == null || !thing.CanInteract(this)) continue;

            float distance = Vector2.Distance(hit.transform.position, transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = thing;
            }
        }
        return closest;
    }

    // Shows the reach circle in the Scene view when the Player is selected.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
