using UnityEngine;

public class NPCIdleMovement : MonoBehaviour
{
    [Header("Idle Movement Settings")]
    public bool enableIdleMovement = true;
    public float bobSpeed = 1.5f;
    public float bobAmount = 0.02f;
    public float rotationSpeed = 0.5f;
    public float rotationAmount = 3f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float timeOffset;
    
    private void Start()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        timeOffset = Random.Range(0f, 10f);
    }
    
    private void Update()
    {
        if (!enableIdleMovement) return;
        
        float time = Time.time + timeOffset;
        
        float bobY = Mathf.Sin(time * bobSpeed) * bobAmount;
        transform.position = originalPosition + new Vector3(0, bobY, 0);
        
        float rotationY = Mathf.Sin(time * rotationSpeed) * rotationAmount;
        transform.rotation = originalRotation * Quaternion.Euler(0, rotationY, 0);
    }
    
    public void ResetToOriginal()
    {
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}
