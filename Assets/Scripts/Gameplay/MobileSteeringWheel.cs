using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MobileSteeringWheel : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public float steerValue;
    public float maxRotation = 180f;   // Max steering rotation visual
    private float currentRotation = 0f;

    public RectTransform wheelImage;   // Drag your steering UI image here

    public void OnDrag(PointerEventData eventData)
    {
        float rotationAmount = eventData.delta.x * 0.5f;

        currentRotation += rotationAmount;
        currentRotation = Mathf.Clamp(currentRotation, -maxRotation, maxRotation);

        steerValue = currentRotation / maxRotation;

        wheelImage.localRotation = Quaternion.Euler(0, 0, -currentRotation);
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        StartCoroutine(ReturnToCenter());
    }

    IEnumerator ReturnToCenter()
    {
        while (Mathf.Abs(currentRotation) > 1f)
        {
            currentRotation = Mathf.Lerp(currentRotation, 0f, Time.deltaTime * 5f);
            steerValue = currentRotation / maxRotation;
            wheelImage.localRotation = Quaternion.Euler(0, 0, -currentRotation);
            yield return null;
        }

        currentRotation = 0f;
        steerValue = 0f;
        wheelImage.localRotation = Quaternion.identity;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
    }
    
} 