using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float dayDurationInSeconds = 240f;
    [SerializeField] private float currentTimeOfDay = 0.3f;
    [SerializeField] private int totalDays = 5;
    private int currentDay = 0;

    [Header("Lighting Settings")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Gradient lightColor;
    [SerializeField] private AnimationCurve lightIntensity;

    [Header("UI Settings")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text daysLeftText; 
    [SerializeField] private Image sliderFill;
    [SerializeField] private Gradient timeGradient;

    private bool isDaytime = true;
    private float dayProgress = 0f;
    private bool newDayStarted = false;

    private void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }

        UpdateLighting();
        UpdateUI();
        UpdateDaysLeftText();
    }

    private void Update()
    {
        float previousTime = currentTimeOfDay;
        currentTimeOfDay += Time.deltaTime / dayDurationInSeconds;
        currentTimeOfDay %= 1f;

        if (previousTime > currentTimeOfDay)
        {
            currentDay++;
            newDayStarted = true;
            UpdateDaysLeftText();
            GameManager.Instance.CheckTimeLimit(currentDay, totalDays);
        }

        bool newDaytime = currentTimeOfDay > 0.25f && currentTimeOfDay < 0.75f;
        if (newDaytime != isDaytime)
        {
            isDaytime = newDaytime;
            OnDayNightTransition();
        }

        bool isNightTime = currentTimeOfDay < 0.2f || currentTimeOfDay > 0.8f;
        UnitManager.Instance?.UpdateNightDebuff(isNightTime);

        UpdateLighting();
        UpdateUI();
    }

    private void UpdateDaysLeftText()
    {
        if (daysLeftText != null)
        {
            int daysLeft = totalDays - currentDay + 1;
            daysLeftText.text = $"{daysLeft} Days Left";
        }
    }

    private void UpdateLighting()
    {
        if (directionalLight != null)
        {
            float sunAngle = currentTimeOfDay * 360f;
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(sunAngle - 90f, -30f, 0));

            directionalLight.color = lightColor.Evaluate(currentTimeOfDay);
            directionalLight.intensity = lightIntensity.Evaluate(currentTimeOfDay);

            RenderSettings.ambientIntensity = Mathf.Lerp(0.3f, 1f, lightIntensity.Evaluate(currentTimeOfDay));
        }
    }

    private void UpdateUI()
    {
        if (timeSlider != null)
        {
            timeSlider.value = currentTimeOfDay;
        }

        if (sliderFill != null)
        {
            sliderFill.color = timeGradient.Evaluate(currentTimeOfDay);
        }

        // Update 24-hour time display
        if (timeText != null)
        {
            timeText.text = Get24HourTime();
        }
    }

    private string Get24HourTime()
    {
        // Convert 0-1 value to 24-hour time (00:00 - 23:59)
        float totalMinutes = currentTimeOfDay * 1440f; // 24*60 minutes
        int hours = Mathf.FloorToInt(totalMinutes / 60f);
        int minutes = Mathf.FloorToInt(totalMinutes % 60f);

        // Format as XX:XX with leading zeros
        return $"{hours:D2}:{minutes:D2}";
    }

    private void OnDayNightTransition()
    {
        Debug.Log(isDaytime ? "Day" : "Night");
    }

    public float GetCurrentTimeNormalized() => currentTimeOfDay;
    public bool IsDaytime() => isDaytime;
    public float GetDayProgress() => dayProgress;
    public string GetCurrent24HourTime() => Get24HourTime(); // Public access to the formatted time
}