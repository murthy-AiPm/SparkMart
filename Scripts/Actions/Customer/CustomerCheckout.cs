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

        target = gameObject;
        duration = baseCheckoutTime + (customer.ItemsInCart * timePerItem);
        return true;
    }

    public override bool PostPerform()
    {
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