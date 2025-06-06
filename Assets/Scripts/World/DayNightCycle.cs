using UnityEngine;
using UnityEngine.UI;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float dayDurationInSeconds = 120f;
    [SerializeField] private float currentTimeOfDay = 0.5f;

    [Header("Lighting Settings")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Gradient lightColor;
    [SerializeField] private AnimationCurve lightIntensity;

    [Header("UI Settings")]
    [SerializeField] private Slider timeSlider;
    [SerializeField] private Image sliderFill;
    [SerializeField] private Gradient timeGradient;

    private bool isDaytime = true;
    private float dayProgress = 0f;

    private void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }

        UpdateLighting();
        UpdateUI();
    }

    private void Update()
    {
        currentTimeOfDay += Time.deltaTime / dayDurationInSeconds;
        currentTimeOfDay %= 1f; 

        bool newDaytime = currentTimeOfDay > 0.25f && currentTimeOfDay < 0.75f;
        if (newDaytime != isDaytime)
        {
            isDaytime = newDaytime;
            OnDayNightTransition();
        }

        UpdateLighting();
        UpdateUI();
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
    }

    private void OnDayNightTransition()
    {
        Debug.Log(isDaytime ? "Day" : "Night");
    }

    public float GetCurrentTimeNormalized() => currentTimeOfDay;
    public bool IsDaytime() => isDaytime;
    public float GetDayProgress() => dayProgress;
}