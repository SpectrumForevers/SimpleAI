using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class AIController : MonoBehaviour
{
    // inventory data
    [HideInInspector] public int materialCounter;
    [HideInInspector] public int maxMaterial;


    // object on area data
    [HideInInspector] public List<GameObject> materials = new List<GameObject>();
    [SerializeField] public List<GameObject> enemyWarehouses;
    [HideInInspector] public List<GameObject> boostersOnArea = new List<GameObject>();
    [SerializeField] List<GameObject> tramplins;

    //Idle Setting
    [SerializeField] int chanceForIdle = 35;
    [SerializeField] float idleTime = 1f;
    [SerializeField] float changeIdleTime = 2f;

    // Warehouse set
    [SerializeField] GameObject warehouse;

    // AI state start
    public AIStates state = AIStates.GoTo;
    [SerializeField] GameObject Win;
    [SerializeField] GameObject Loose;

    //bot data
    [SerializeField] public string botName;
    [SerializeField] bool debugMode = false;

    // coroutin cheker
    Coroutine coroutine;

    //ini agent
    NavMeshAgent agent;
    private void Start()
    {
        
        StartDateIn();
    }

    private void Update()
    {
        //first stage
        UpdateDateIn();
        //second stage
        MaterialCounterCalculation();
        CheckState();

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == Tags.abyss)
        {
            agent.enabled = false;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == Tags.abyss)
        {
            agent.enabled = true;
        }
    }
    void DisableCoroutines()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }

    }

    // start and update data initialization
    void UpdateDateIn()
    {
        materials = Spawner.instance.GetMaterials();
        materialCounter = GetComponent<NewPlayerInventory>().materialCount;
        boostersOnArea = GameManager.instance.boosters;
    }
    void StartDateIn()
    {
        agent = GetComponent<NavMeshAgent>();
        maxMaterial = GetComponent<NewPlayerInventory>()._inventoryLimit;
    }


    #region States
    public void CheckState()
    {
        switch (state)
        {
            case AIStates.Idle:

                break;
            case AIStates.GoTo:
                if (coroutine == null)
                {
                    coroutine = StartCoroutine(MaterialSearcher());
                }
                break;
                
            case AIStates.GoToWarehouse:
                if (gameObject.GetComponent<NavMeshAgent>().enabled == true)
                {

                    GoToWarehouse(warehouse);

                }

                break;
            case AIStates.Win:
                WinReaction();
                break;
            case AIStates.Loose:
                LooseReaction();
                break;
        }
    }
    #endregion

    public void MaterialCounterCalculation()
    {
        if (materialCounter >= maxMaterial)
        {
            if (state == AIStates.GoTo)
            {
                DisableCoroutines();
                state = AIStates.GoToWarehouse;
                
            }
        }

        if (materialCounter == 0)
        {
            if (state != AIStates.Win
                && state != AIStates.Loose
                && state != AIStates.Idle
                && state != AIStates.GoTo)
            {
                DisableCoroutines();
                state = AIStates.GoTo;
                
            }
        }

    }

    #region FindMaterial
    GameObject FindNearMaterial()
    {
        if (materials.Count == 0)
        {
            return null;
        }
        GameObject nearbyMaterial = materials[0];
        float minDistance = 0f;

        if (materials.Count != 0)
        {
            minDistance = Vector3.Distance(gameObject.transform.position, materials[0].transform.position);
            nearbyMaterial = materials[0];
        }
        foreach (GameObject material in materials)
        {
            if (Vector3.Distance(gameObject.transform.position, material.transform.position) < minDistance)
            {
                minDistance = Vector3.Distance(gameObject.transform.position, material.transform.position);
                if (debugMode == true)
                {
                    Debug.Log("material" + minDistance + " bot name: " + gameObject.name);
                }
                nearbyMaterial = material;
            }
        }
        
        return nearbyMaterial;
    }

    void SetDestination(GameObject destination)
    {
        if (destination == null)
        {
            state = AIStates.Idle;
            return;
        }
        if (gameObject.GetComponent<NavMeshAgent>().enabled == true)
        {
            agent.SetDestination(destination.transform.position);
            
        }
        
    }
    public void IdleTime()
    {
        DisableCoroutines();
        StartCoroutine(WaitIdle(idleTime));
    }

    IEnumerator WaitIdle(float idleTime)
    {
        AIStates bufferStates;
        bufferStates = state;

        state = AIStates.Idle;

        int chance = Random.Range(0, 100);

        if (chance <= chanceForIdle)
        {
            idleTime = changeIdleTime;
        }
        if (agent.enabled == true)
        {
            agent.SetDestination(gameObject.transform.position);
        }
        
        yield return new WaitForSeconds(idleTime);

        state = bufferStates;
    }

    IEnumerator MaterialSearcher()
    {
        
        while (true)
        {

            
            SetDestination(FindNearObj(FindWarehouseToSteal(), FindNearMaterial(), NearBooster(), NearTramplin()));

            yield return new WaitForFixedUpdate();
        }
    }

    #endregion

    #region GoToWarehouse

    void GoToWarehouse(GameObject warehosue)
    {
        agent.SetDestination(warehosue.transform.position);
    }

    #endregion

    GameObject NearBooster()
    {
        if (boostersOnArea.Count == 0)
        {
            return null;
        }
        GameObject nearbyBooster = boostersOnArea[0];
        float minDistToBoost = Vector3.Distance(gameObject.transform.position, nearbyBooster.transform.position);
        foreach (GameObject booster in boostersOnArea)
        {
            if (Vector3.Distance(gameObject.transform.position, booster.transform.position) < minDistToBoost)
            {
                minDistToBoost = Vector3.Distance(gameObject.transform.position, booster.transform.position);
                nearbyBooster = booster;
                if (debugMode == true)
                {
                    Debug.Log("nearbyBooster " + nearbyBooster + " bot name: " + gameObject.name);
                }
            }
        }
        return nearbyBooster;
    }


    //Steal
    GameObject FindWarehouseToSteal()
    {
        GameObject nearbyWarehouse = null;
        float minDistanceToWarehouse = Vector3.Distance(gameObject.transform.position, enemyWarehouses[0].transform.position);
        
        foreach (GameObject warehouse in enemyWarehouses)
        {
            if (warehouse.GetComponent<NewWareHouseInventory>().canSteal == false || warehouse.GetComponent<NewWareHouseInventory>().inventory.Count == 0)
            {
                continue;
            }
            if(Vector3.Distance(gameObject.transform.position, warehouse.transform.position) <= minDistanceToWarehouse)
            {
                minDistanceToWarehouse = Vector3.Distance(gameObject.transform.position, warehouse.transform.position);
                if (debugMode == true)
                {
                    Debug.Log("minDistanceToWarehouse " + minDistanceToWarehouse + " bot name: " + gameObject.name);
                }
                nearbyWarehouse = warehouse;
            }
        }
        return nearbyWarehouse;
    }

    GameObject FindNearObj(GameObject nearWarehouse, GameObject nearMaterial, GameObject nearBooster, GameObject nearTramplin)
    {
        float bufDistanceHomeWarehouse = 0f;
        float bufDistanceMaterial = 0f;
        float bufDistanceWarehouse = 0f;
        float bufDistanceBooster = 0f;
        //float bufDistanceTramplin = 0f;
        float minDist = 0f;
        if (warehouse != null)
        {
            bufDistanceHomeWarehouse = Vector3.Distance(gameObject.transform.position, warehouse.transform.position);
            minDist = bufDistanceHomeWarehouse;
        }
        /*if (nearTramplin != null)
        {
            bufDistanceTramplin = Vector3.Distance(gameObject.transform.position, nearTramplin.transform.position);
            minDist = bufDistanceTramplin;
        }*/
        if (nearMaterial != null)
        {
            bufDistanceMaterial = Vector3.Distance(gameObject.transform.position, nearMaterial.transform.position);
            minDist = bufDistanceMaterial;
            if (debugMode == true)
            {
                Debug.Log("bufDistanceMaterial " + bufDistanceMaterial);
            }
        }
        if (nearWarehouse != null)
        {
            bufDistanceWarehouse = Vector3.Distance(gameObject.transform.position, nearWarehouse.transform.position);
            if (debugMode == true)
            {
                Debug.Log("bufDistanceWarehouse home " + bufDistanceHomeWarehouse);
            }
            if (debugMode == true)
            {
                Debug.Log("bufDistanceWarehouse fore steal: " + bufDistanceWarehouse);
            }
            minDist = bufDistanceWarehouse;
        }
        if (nearBooster != null)
        {
            bufDistanceBooster = Vector3.Distance(gameObject.transform.position, nearBooster.transform.position);
            minDist = bufDistanceBooster;
            if (debugMode == true)
            {
                Debug.Log("nearBoster" + bufDistanceBooster + " bot name: " + gameObject.name);
            }
        }
        //if (minDist > bufDistanceTramplin && bufDistanceTramplin != 0)
        //{
         //   minDist = bufDistanceTramplin;
      //  }

        if (minDist > bufDistanceMaterial && bufDistanceMaterial != 0)
        {
            minDist = bufDistanceMaterial;
        }
        if (minDist > bufDistanceHomeWarehouse && materialCounter > 1)
        {
            minDist = bufDistanceHomeWarehouse;
        }
        if (minDist > bufDistanceWarehouse && bufDistanceWarehouse != 0)
        {
            minDist = bufDistanceWarehouse;
        }

        if (minDist > bufDistanceBooster && bufDistanceBooster != 0)
        {
            minDist = bufDistanceBooster;
        }
        //if (minDist == bufDistanceTramplin && bufDistanceTramplin != 0)
        //{
        //    return nearTramplin;
        //}
        if (minDist == bufDistanceMaterial && bufDistanceMaterial != 0)
        {

            return nearMaterial;
        }
        if (minDist == bufDistanceHomeWarehouse && bufDistanceHomeWarehouse != 0)
        {

            return warehouse;
        }
        if (minDist == bufDistanceWarehouse && bufDistanceWarehouse != 0)
        {

            return nearWarehouse;
        }
        if (minDist == bufDistanceBooster && bufDistanceBooster != 0)
        {

            return nearBooster;

        }

        return null;
    }

    GameObject NearTramplin()
    {
        
        if (tramplins.Count == 0)
        {
            return null;
        }
        GameObject nearTramplin = tramplins[0];
        float minDistanceToTramplin = Vector3.Distance(gameObject.transform.position, tramplins[0].transform.position);
        foreach (GameObject tramplin in tramplins)
        {
            if (Vector3.Distance(gameObject.transform.position, tramplin.transform.position) < minDistanceToTramplin)
            {
                minDistanceToTramplin = Vector3.Distance(gameObject.transform.position, tramplin.transform.position);
                nearTramplin = tramplin;
            }
        }

        return nearTramplin;
    }

    //Win and Loose

    void WinReaction()
    {
        //agent.SetDestination(gameObject.transform.position);
        agent.enabled = false;
        DisableCoroutines();
        
    }

    void LooseReaction()
    {
        //agent.SetDestination(gameObject.transform.position);
        agent.enabled = false;
        DisableCoroutines();
       
    }

}


    public enum AIStates
{
    Idle,
    GoTo,
    GoToWarehouse,
    Win,
    Loose
}