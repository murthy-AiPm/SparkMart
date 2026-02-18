using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages an ordered queue at a checkout lane.
/// Attach to each CheckoutLane/SelfCheckout GameObject.
/// Requires child transforms named QueueSpot_0, QueueSpot_1, etc.
/// Overflow customers extend the line dynamically beyond the last spot.
/// </summary>
public class CheckoutQueueManager : MonoBehaviour {

    [Header("Settings")]
    public float overflowSpacing = 1.5f;

    [Header("Debug")]
    [SerializeField] private int currentQueueSize;

    private List<Transform> queueSpots = new List<Transform>();
    private List<GameObject> queuedCustomers = new List<GameObject>();
    private Vector3 overflowDirection;

    void Awake() {
        FindQueueSpots();
        CalculateOverflowDirection();
    }

    void FindQueueSpots() {
        queueSpots.Clear();

        // Find all QueueSpot children in order
        for (int i = 0; i < 20; i++) {
            Transform spot = transform.Find($"QueueSpot_{i}");
            if (spot != null) {
                queueSpots.Add(spot);
            } else {
                break;
            }
        }

        if (queueSpots.Count == 0) {
            Debug.LogWarning($"CheckoutQueueManager on {gameObject.name}: No QueueSpot_0, QueueSpot_1, etc. found!");
        }
    }

    void CalculateOverflowDirection() {
        if (queueSpots.Count >= 2) {
            // Direction from first spot toward last spot (the "back of line" direction)
            overflowDirection = (queueSpots[queueSpots.Count - 1].position - queueSpots[0].position).normalized;
        } else if (queueSpots.Count == 1) {
            // Default: extend backward along the lane's negative forward
            overflowDirection = -transform.forward;
        } else {
            overflowDirection = -transform.forward;
        }
    }

    /// <summary>
    /// Adds a customer to the back of the queue.
    /// Returns the position they should stand at.
    /// </summary>
    public Vector3 JoinQueue(GameObject customer) {
        if (queuedCustomers.Contains(customer)) {
            return GetPositionForIndex(queuedCustomers.IndexOf(customer));
        }

        queuedCustomers.Add(customer);
        currentQueueSize = queuedCustomers.Count;

        int index = queuedCustomers.Count - 1;
        return GetPositionForIndex(index);
    }

    /// <summary>
    /// Removes a customer from the queue (when they finish checkout or leave).
    /// Remaining customers shuffle forward.
    /// </summary>
    public void LeaveQueue(GameObject customer) {
        if (!queuedCustomers.Contains(customer)) return;

        queuedCustomers.Remove(customer);
        currentQueueSize = queuedCustomers.Count;

        // Shuffle everyone forward to their new positions
        ShuffleForward();
    }

    /// <summary>
    /// Returns true if this customer is first in line.
    /// </summary>
    public bool IsFirstInLine(GameObject customer) {
        if (queuedCustomers.Count == 0) return false;
        return queuedCustomers[0] == customer;
    }

    /// <summary>
    /// Gets the current position a queued customer should be standing at.
    /// </summary>
    public Vector3 GetCurrentPosition(GameObject customer) {
        int index = queuedCustomers.IndexOf(customer);
        if (index < 0) return transform.position;
        return GetPositionForIndex(index);
    }

    public int QueueLength => queuedCustomers.Count;
    public int SpotCount => queueSpots.Count;

    /// <summary>
    /// Gets the world position for a given queue index.
    /// Uses placed spots for defined positions, overflow math for extras.
    /// </summary>
    private Vector3 GetPositionForIndex(int index) {
        if (index < queueSpots.Count) {
            return queueSpots[index].position;
        }

        // Overflow: extend beyond the last spot
        int overflowIndex = index - queueSpots.Count + 1;
        Vector3 lastSpotPos = queueSpots.Count > 0
            ? queueSpots[queueSpots.Count - 1].position
            : transform.position;

        return lastSpotPos + overflowDirection * overflowIndex * overflowSpacing;
    }

    /// <summary>
    /// Tells all queued customers to move to their updated positions.
    /// Called after someone leaves the queue.
    /// </summary>
    private void ShuffleForward()
    {
        for (int i = 0; i < queuedCustomers.Count; i++)
        {
            GameObject customer = queuedCustomers[i];
            if (customer == null) continue;

            // Only redirect customers who are still in the checkout queue
            GAgent agent = customer.GetComponent<GAgent>();
            if (agent != null && agent.currentAction is CustomerCheckout) continue;
            //if (agent != null && agent.currentAction is CustomerGoHome) continue;

            UnityEngine.AI.NavMeshAgent navAgent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.SetDestination(GetPositionForIndex(i));
            }
        }
    }

    /// <summary>
    /// Clean up null references (destroyed customers).
    /// </summary>
    void Update() {
        bool removed = queuedCustomers.RemoveAll(c => c == null) > 0;
        if (removed) {
            currentQueueSize = queuedCustomers.Count;
            ShuffleForward();
        }
    }

    void OnDrawGizmosSelected() {
        // Draw placed spots
        for (int i = 0; i < 20; i++) {
            Transform spot = transform.Find($"QueueSpot_{i}");
            if (spot == null) break;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(spot.position, 0.3f);
            // Draw index label direction
            if (i > 0) {
                Transform prev = transform.Find($"QueueSpot_{i - 1}");
                if (prev != null) {
                    Gizmos.DrawLine(prev.position, spot.position);
                }
            }
        }
    }
}
