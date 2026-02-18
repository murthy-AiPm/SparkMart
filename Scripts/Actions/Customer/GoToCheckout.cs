using UnityEngine;
using System.Collections.Generic;

public class GoToCheckout : GAction
{

    private Customer customer;

    [Header("Queue Threshold")]
    [Tooltip("Max customers in a lane's queue before this customer skips it")]
    public int maxQueueLength = 4;
    private CheckoutQueueManager assignedQueue;
    void Start()
    {
        preconditions.Clear();
        effects.Clear();
        preconditions["doneShopping"] = 1;
        effects["inCheckoutQueue"] = 1;
    }

    public override bool PrePerform()
    {
        customer = GetComponent<Customer>();
        if (customer == null) return false;

        // Find the best checkout lane: closest + not overcrowded
        GameObject bestLane = FindBestCheckoutLane();

        if (bestLane == null)
        {
            return false;
        }

        target = bestLane;
     //   assignedQueue = bestLane.GetComponent<CheckoutQueueManager>();

        // Track this customer in the checkout queue
        SparkWorld.Instance.GetQueue("customersInCheckoutQueue").AddResource(gameObject);

        return true;
    }

    public override bool PostPerform()
    {
        beliefs.ModifyState("inCheckoutQueue", 1);
        return true;
    }

    /// <summary>
    /// Finds the best checkout lane based on distance and queue length.
    /// Skips lanes that are too crowded. Falls back to any staffed lane if all are busy.
    /// Also considers self-checkout lanes.
    /// </summary>
    private GameObject FindBestCheckoutLane()
    {
        GameObject[] checkoutLanes = GameObject.FindGameObjectsWithTag("CheckoutLane");
        GameObject[] selfCheckouts = GameObject.FindGameObjectsWithTag("SelfCheckout");

        List<LaneCandidate> candidates = new List<LaneCandidate>();

        // Evaluate staffed checkout lanes
        foreach (GameObject lane in checkoutLanes)
        {
            CheckoutLane laneComponent = lane.GetComponent<CheckoutLane>();
            if (laneComponent == null || !laneComponent.IsStaffed) continue;

            int queueCount = CountCustomersHeadingToLane(lane);
            float distance = Vector3.Distance(transform.position, lane.transform.position);

            candidates.Add(new LaneCandidate
            {
                laneObject = lane,
                distance = distance,
                queueLength = queueCount,
                isSelfCheckout = false
            });
        }

        // Evaluate self-checkout lanes
        foreach (GameObject lane in selfCheckouts)
        {
            CheckoutLane laneComponent = lane.GetComponent<CheckoutLane>();
            if (laneComponent == null) continue;

            int queueCount = CountCustomersHeadingToLane(lane);
            float distance = Vector3.Distance(transform.position, lane.transform.position);

            candidates.Add(new LaneCandidate
            {
                laneObject = lane,
                distance = distance,
                queueLength = queueCount,
                isSelfCheckout = true
            });
        }

        if (candidates.Count == 0) return null;

        // First pass: find closest lane that isn't overcrowded
        GameObject bestLane = null;
        float bestDistance = float.MaxValue;

        foreach (var candidate in candidates)
        {
            if (candidate.queueLength >= maxQueueLength) continue;

            // Persona preference: Quick Shoppers prefer self-checkout
            float adjustedDistance = candidate.distance;
            if (customer.Persona.prefersSelfCheckout && candidate.isSelfCheckout)
            {
                adjustedDistance *= 0.7f; // 30% distance bonus for preferred type
            }
            else if (!customer.Persona.prefersSelfCheckout && !candidate.isSelfCheckout)
            {
                adjustedDistance *= 0.85f; // 15% bonus for staffed if they prefer it
            }

            if (adjustedDistance < bestDistance)
            {
                bestDistance = adjustedDistance;
                bestLane = candidate.laneObject;
            }
        }

        // Fallback: if ALL lanes are overcrowded, just pick the closest one anyway
        if (bestLane == null)
        {
            float closestDist = float.MaxValue;
            foreach (var candidate in candidates)
            {
                if (candidate.distance < closestDist)
                {
                    closestDist = candidate.distance;
                    bestLane = candidate.laneObject;
                }
            }
        }

        return bestLane;
    }

    /// <summary>
    /// Counts how many customers are currently heading to or queued at a specific lane.
    /// Uses the shared checkout queue to approximate.
    /// </summary>
    private int CountCustomersHeadingToLane(GameObject lane)
    {
        int count = 0;
        foreach (GAgent agent in SparkWorld.Instance.GetAllAgents())
        {
            if (agent == null || agent.gameObject == gameObject) continue;
            if (agent.currentAction != null && agent.currentAction.target == lane)
            {
                count++;
            }
        }
        return count;
    }

    private struct LaneCandidate
    {
        public GameObject laneObject;
        public float distance;
        public int queueLength;
        public bool isSelfCheckout;
    }

    public CheckoutQueueManager GetAssignedQueue()
    {
        return assignedQueue;
    }
}