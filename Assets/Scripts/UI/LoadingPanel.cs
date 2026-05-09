using TMPro;
using UnityEngine;

public class LoadingPanel : UIPanel
{
    private TextMeshProUGUI statusText;
    private RectTransform progressFill;

    private string baseStatus = "Fortifying walls, scouting enemies, warming up engines";
    private float elapsed;
    private float progressCycle = 2.2f;
    private float maxFillWidth;

    public override void Initialize()
    {
        base.Initialize();

        statusText = transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        progressFill = transform.Find("ProgressBarBackground/ProgressFill") as RectTransform;

        if (progressFill != null)
        {
            maxFillWidth = ((RectTransform)progressFill.parent).rect.width;
        }
    }

    public override void OnShow()
    {
        base.OnShow();
        elapsed = 0f;
        UpdateVisuals(0f);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        UpdateVisuals(elapsed);
    }

    private void UpdateVisuals(float timeValue)
    {
        UpdateStatusText(timeValue);
        UpdateProgressBar(timeValue);
    }

    private void UpdateStatusText(float timeValue)
    {
        if (statusText == null)
        {
            return;
        }

        int dotCount = (int)(timeValue * 2f) % 4;
        statusText.text = baseStatus + new string('.', dotCount);
    }

    private void UpdateProgressBar(float timeValue)
    {
        if (progressFill == null || maxFillWidth <= 0f)
        {
            return;
        }

        float t = Mathf.PingPong(timeValue / progressCycle, 1f);
        float width = Mathf.Lerp(maxFillWidth * 0.2f, maxFillWidth * 0.92f, t);
        progressFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}
