using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MetricsDashboardUI : MonoBehaviour {

    [Header("References")]
    public MetricsManager metricsManager;
    public TimeScaleController timeScaleController;

    [Header("Metric Displays")]
    public TextMeshProUGUI customersServedText;
    public TextMeshProUGUI profitText;
    public TextMeshProUGUI totalItemText;
    public TextMeshProUGUI satisfactionText;
    public TextMeshProUGUI securityRatingText;

    [Header("Time Controls")]
    public TextMeshProUGUI timeScaleText;
    public Slider timeScaleSlider;
    public Button pauseButton;
    public TextMeshProUGUI pauseButtonText;

    [Header("Export")]
    public Button exportButton;

    public float updateInterval = 0.5f;
    private float lastUpdateTime;

    void Start() {
        if (metricsManager == null)
            metricsManager = FindObjectOfType<MetricsManager>();
        if (timeScaleController == null)
            timeScaleController = FindObjectOfType<TimeScaleController>();

        SetupUI();
        UpdateDisplay();
    }

    void SetupUI() {
        if (timeScaleSlider != null && timeScaleController != null) {
            timeScaleSlider.minValue = 0f;
            timeScaleSlider.maxValue = 1f;
            timeScaleSlider.value = timeScaleController.GetTimeScaleNormalized();
            timeScaleSlider.onValueChanged.AddListener(OnTimeScaleSliderChanged);
        }

        if (pauseButton != null) {
            pauseButton.onClick.AddListener(OnPauseButtonClicked);
        }

        if (exportButton != null) {
            exportButton.onClick.AddListener(OnExportButtonClicked);
        }

        if (metricsManager != null) {
            metricsManager.OnMetricsUpdated += UpdateDisplay;
        }

        if (timeScaleController != null) {
            timeScaleController.OnTimeScaleChanged.AddListener(OnTimeScaleChanged);
            timeScaleController.OnPaused.AddListener(UpdateTimeDisplay);
            timeScaleController.OnResumed.AddListener(UpdateTimeDisplay);
        }
    }

    void Update() {
        if (Time.unscaledTime - lastUpdateTime >= updateInterval) {
            UpdateDisplay();
            lastUpdateTime = Time.unscaledTime;
        }
    }

    public void UpdateDisplay() {
        if (metricsManager == null) return;

        if (customersServedText != null)
            customersServedText.text = $"Customers Served: {metricsManager.TotalCustomersServed}";

        if (totalItemText != null)
            totalItemText.text = $"Items Sold: {metricsManager.TotalItemsSold}";

        if (profitText != null)
            profitText.text = $"Profit: ${metricsManager.TotalProfit:F2}";

        if (satisfactionText != null) {
            float sat = metricsManager.AverageSatisfaction;
            satisfactionText.text = $"Satisfaction: {sat:F1}%";
            satisfactionText.color = sat >= 70f ? Color.green : (sat >= 50f ? Color.yellow : Color.red);
        }

        if (securityRatingText != null) {
            float sec = metricsManager.SecurityRating;
            securityRatingText.text = $"Security: {sec:F1}%";
            securityRatingText.color = sec >= 80f ? Color.green : (sec >= 60f ? Color.yellow : Color.red);
        }

        UpdateTimeDisplay();
    }

    void UpdateTimeDisplay() {
        if (timeScaleController == null) return;

        if (timeScaleText != null)
            timeScaleText.text = timeScaleController.GetSpeedDisplayString();

        if (pauseButtonText != null)
            pauseButtonText.text = timeScaleController.IsPaused ? "Resume" : "Pause";
    }

    void OnTimeScaleSliderChanged(float value) {
        if (timeScaleController != null)
            timeScaleController.SetTimeScaleNormalized(value);
    }

    void OnPauseButtonClicked() {
        if (timeScaleController != null)
            timeScaleController.TogglePause();
    }

    void OnTimeScaleChanged(float newScale) {
        UpdateTimeDisplay();
        if (timeScaleSlider != null)
            timeScaleSlider.SetValueWithoutNotify(timeScaleController.GetTimeScaleNormalized());
    }

    void OnExportButtonClicked() {
        if (metricsManager != null) {
            metricsManager.ExportToCSV();
            Debug.Log("Metrics exported!");
        }
    }

    void OnDestroy() {
        if (metricsManager != null)
            metricsManager.OnMetricsUpdated -= UpdateDisplay;
    }
}
