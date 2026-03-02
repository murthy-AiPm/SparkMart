using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// World-space UI panel attached to each shelf.
/// Displays shelf name, stock count, status, and a fill bar.
/// Automatically updates via Shelf.OnStockChanged event.
/// Billboards to always face the main camera.
/// </summary>
public class ShelfUI : MonoBehaviour {

    [Header("UI References")]
    public TextMeshProUGUI shelfNameText;
    public TextMeshProUGUI stockCountText;
    public TextMeshProUGUI statusText;
    public Image stockBarFill;
    public Image backgroundPanel;
    public Canvas shelfCanvas;

    [Header("Colors")]
    public Color colorHealthy   = new Color(0.18f, 0.80f, 0.44f); // green
    public Color colorLow       = new Color(1.00f, 0.76f, 0.03f); // yellow
    public Color colorEmpty     = new Color(0.90f, 0.20f, 0.20f); // red
    public Color bgNormal       = new Color(0.05f, 0.05f, 0.05f, 0.82f);
    public Color bgEmpty        = new Color(0.20f, 0.04f, 0.04f, 0.88f);



    // ── internals ──────────────────────────────────────────────────
    private Shelf shelf;

  

    // ── lifecycle ──────────────────────────────────────────────────


    void Start() {
        

        // Walk up the hierarchy to find the Shelf component
        // (ShelfUI lives on the Canvas child, Shelf lives on the parent)
        shelf = GetComponentInParent<Shelf>();

        if (shelf == null) {
            Debug.LogError($"[ShelfUI] No Shelf component found in parent hierarchy of {gameObject.name}. " +
                           "Make sure ShelfUI canvas is a child of the shelf GameObject.");
            enabled = false;
            return;
        }

        // Subscribe to stock changes
        shelf.OnStockChanged += OnStockChanged;

        // Apply initial height offset
        //Vector3 localPos = transform.localPosition;
        //localPos.y = heightOffset;
        //transform.localPosition = localPos;

        // Force an immediate refresh
        Refresh();
    }

    void OnDestroy() {
        if (shelf != null) {
            shelf.OnStockChanged -= OnStockChanged;
        }
    }

    // ── event handler ──────────────────────────────────────────────
    private void OnStockChanged(Shelf changedShelf) {
        Refresh();
    }

    // ── update (billboard only) ────────────────────────────────────


    // ── refresh display ────────────────────────────────────────────
    private void Refresh() {
        if (shelf == null || shelf.shelfData == null) return;

        // ── name ──
        if (shelfNameText != null) {
            shelfNameText.text = shelf.shelfData.shelfName.ToUpper();
        }

        // ── count ──
        if (stockCountText != null) {
            stockCountText.text = $"{shelf.CurrentStock} / {shelf.shelfData.maxStock}";
        }

        // ── determine state ──
        StockState state = GetStockState();

        // ── status label ──
        if (statusText != null) {
            switch (state) {
                case StockState.Healthy:
                    statusText.text  = "● IN STOCK";
                    statusText.color = colorHealthy;
                    break;
                case StockState.Low:
                    statusText.text  = "▲ LOW STOCK";
                    statusText.color = colorLow;
                    break;
                case StockState.Empty:
                    statusText.text  = "✕ OUT OF STOCK";
                    statusText.color = colorEmpty;
                    break;
            }
        }

        // ── bar fill ──
        if (stockBarFill != null) {
            stockBarFill.fillAmount = shelf.StockPercentage;

            stockBarFill.color = state switch {
                StockState.Healthy => colorHealthy,
                StockState.Low     => colorLow,
                StockState.Empty   => colorEmpty,
                _                  => colorHealthy
            };
        }

        // ── background tint ──
        if (backgroundPanel != null) {
            backgroundPanel.color = (state == StockState.Empty) ? bgEmpty : bgNormal;
        }
    }

    // ── helpers ────────────────────────────────────────────────────
    private StockState GetStockState() {
        if (!shelf.HasStock())    return StockState.Empty;
        if (shelf.NeedsRestock()) return StockState.Low;
        return StockState.Healthy;
    }

    private enum StockState { Healthy, Low, Empty }

#if UNITY_EDITOR
    void OnValidate() {
        // Allow tweaking colors in play mode
        if (Application.isPlaying && shelf != null) Refresh();
    }
#endif
}
