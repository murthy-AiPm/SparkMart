using UnityEngine;

public class CustomerCheckout : GAction
{

    private Customer customer;
    public float baseCheckoutTime = 5f;
    public float timePerItem = 1f;

    void Start()
    {
        preconditions.Clear();
        effects.Clear();
        preconditions["inCheckoutQueue"] = 1;
        effects["hasCheckedOut"] = 1;
    }

    public override bool PrePerform()
    {
        customer = GetComponent<Customer>();
        if (customer == null) return false;

        GoToCheckout goToCheckout = GetComponent<GoToCheckout>();
        if (goToCheckout == null)
        {
            target = gameObject;
            duration = baseCheckoutTime + (customer.ItemsInCart * timePerItem);
            return true;
        }

        CheckoutQueueManager queue = goToCheckout.GetAssignedQueue();
        if (queue == null)
        {
            target = gameObject;
            duration = baseCheckoutTime + (customer.ItemsInCart * timePerItem);
            return true;
        }

        // If first in line, proceed with checkout
        if (queue.IsFirstInLine(gameObject))
        {
            target = queue.gameObject;
            duration = baseCheckoutTime + (customer.ItemsInCart * timePerItem);
            return true;
        }

        // Not first — check if we should switch lanes
        if (queue.QueueLength > goToCheckout.maxQueueLength)
        {
            // Leave current queue and re-evaluate
            queue.LeaveQueue(gameObject);
            SparkWorld.Instance.GetQueue("customersInCheckoutQueue").RemoveResource(gameObject);
            beliefs.RemoveState("inCheckoutQueue");
            return false;
        }

        // Stay in current queue, update position
        UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (navAgent != null)
        {
            navAgent.SetDestination(queue.GetCurrentPosition(gameObject));
        }
        return false;
    }

    public override bool PostPerform()
    {
        // Leave the physical queue
        GoToCheckout goToCheckout = GetComponent<GoToCheckout>();
        if (goToCheckout != null)
        {
            CheckoutQueueManager queue = goToCheckout.GetAssignedQueue();
            if (queue != null)
            {
                queue.LeaveQueue(gameObject);
                // Stop the customer so they don't drift
                UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null) navAgent.ResetPath();
            }
        }

        SparkWorld.Instance.GetQueue("customersInCheckoutQueue").RemoveResource(gameObject);
        customer.CompleteCheckout();
        MetricsManager metrics = Object.FindObjectOfType<MetricsManager>();
        if (metrics != null)
        {
            metrics.RecordSale(customer.TotalProfit, customer.ItemsCollected);
            metrics.RecordCustomerSatisfaction(customer.Satisfaction);
        }
        beliefs.ModifyState("hasCheckedOut", 1);
        beliefs.RemoveState("inCheckoutQueue");
        beliefs.RemoveState("readyToCheckout");
        return true;
    }
}