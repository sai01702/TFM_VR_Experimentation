using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.InputSystem;

public class PlaceHatWithRaycast : MonoBehaviour
{
    [Header("Detección de Stands")]
    [Tooltip("Layer para detectar los HatStands")]
    public LayerMask standLayerMask;

    [Tooltip("Distancia máxima para detectar con raycast")]
    public float rayDistance = 8f;

    [Tooltip("Radio de detección adicional para facilitar la colocación")]
    public float detectionRadius = 1.5f;

    [Header("Colocación Precisa")]
    [Tooltip("Offset vertical desde el centro del stand")]
    public float heightOffset = 0.1f;

    [Tooltip("Tiempo que permanece kinematic tras colocarse")]
    public float stabilizeTime = 1f;

    [Tooltip("Usar bounds del collider para posición exacta")]
    public bool useColliderBounds = true;

    [Header("Magnetismo")]
    [Tooltip("Distancia para activar el magnetismo automático")]
    public float magnetDistance = 3f;

    [Tooltip("Velocidad del magnetismo")]
    public float magnetStrength = 5f;

    [Header("Feedback Visual")]
    [Tooltip("Material para highlight del stand válido")]
    public Material highlightMaterial;

    private XRGrabInteractable grab;
    private Transform handTransform;
    private HatStandTrigger currentTargetStand;
    private HatStandTrigger nearbyStand;
    private Material originalStandMaterial;
    private Renderer standRenderer;
    private bool isBeingGrabbed = false;
    private bool isPlacedOnStand = false;
    private Vector3 originalScale;
    private bool listenersSubscribed = false;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        
        // Guardar escala original
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Subscribe to grab events ONLY when enabled.
    /// This ensures short-distance mode (disabled) won't teleport hats.
    /// </summary>
    void OnEnable()
    {
        if (grab != null && !listenersSubscribed)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
            listenersSubscribed = true;
        }
    }

    /// <summary>
    /// Unsubscribe from grab events when disabled.
    /// This stops the long-distance placement behavior for short-distance mode.
    /// </summary>
    void OnDisable()
    {
        if (grab != null && listenersSubscribed)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            listenersSubscribed = false;
        }

        // Clean up any active state
        ClearStandHighlight();
        isBeingGrabbed = false;
        handTransform = null;
        currentTargetStand = null;
    }

    private void OnDestroy()
    {
        // Safety cleanup in case OnDisable wasn't called
        if (grab != null && listenersSubscribed)
        {
            grab.selectEntered.RemoveListener(OnGrabbed);
            grab.selectExited.RemoveListener(OnReleased);
            listenersSubscribed = false;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Double-check we're enabled (safety check)
        if (!enabled) return;

        handTransform = args.interactorObject.transform;
        isBeingGrabbed = true;
        isPlacedOnStand = false;
        
        // Reactivar físicas si estaba en un stand
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // Double-check we're enabled - if disabled, don't teleport!
        if (!enabled)
        {
            // Just clean up state, no teleport
            isBeingGrabbed = false;
            handTransform = null;
            currentTargetStand = null;
            return;
        }

        isBeingGrabbed = false;
        
        // Limpiar highlight si existe
        ClearStandHighlight();

        // Intentar colocar el sombrero en un stand (ONLY if enabled)
        HatStandTrigger targetStand = FindBestStand();
        
        if (targetStand != null)
        {
            PlaceHatOnStand(targetStand);
        }

        handTransform = null;
        currentTargetStand = null;
    }

    void Update()
    {
        if (!isBeingGrabbed || handTransform == null) return;

        // Buscar stand válido continuamente mientras se mantiene agarrado
        HatStandTrigger bestStand = FindBestStand();
        
        // Actualizar highlight
        UpdateStandHighlight(bestStand);

        // Aplicar magnetismo si está cerca de un stand
        ApplyMagnetism(bestStand);
    }

    private HatStandTrigger FindBestStand()
    {
        HatStandTrigger bestStand = null;
        float bestScore = float.MaxValue;
        bool foundDirectHit = false;

        // PRIORIDAD 1: Raycast directo desde la mano (más importante)
        Ray ray = new Ray(handTransform.position, handTransform.forward);
        RaycastHit[] directHits = Physics.RaycastAll(ray, rayDistance, standLayerMask);
        
        // Ordenar hits por distancia para tomar el más cercano en la línea de sight
        System.Array.Sort(directHits, (hit1, hit2) => hit1.distance.CompareTo(hit2.distance));
        
        foreach (RaycastHit hit in directHits)
        {
            HatStandTrigger stand = hit.collider.GetComponent<HatStandTrigger>();
            if (stand != null)
            {
                // Encontramos un hit directo - esta es la máxima prioridad
                bestStand = stand;
                foundDirectHit = true;
                Debug.DrawRay(handTransform.position, handTransform.forward * hit.distance, Color.green, 0.1f);
                break; // Tomar el primer (más cercano) hit directo
            }
        }

        // Si encontramos un hit directo, devolver inmediatamente
        if (foundDirectHit)
        {
            return bestStand;
        }

        // PRIORIDAD 2: SphereCast desde la mano (menos preciso pero más tolerante)
        RaycastHit[] sphereHits = Physics.SphereCastAll(handTransform.position, detectionRadius, handTransform.forward, rayDistance, standLayerMask);
        
        float bestSphereCastScore = float.MaxValue;
        HatStandTrigger bestSphereCastStand = null;
        
        foreach (RaycastHit sphereHit in sphereHits)
        {
            HatStandTrigger stand = sphereHit.collider.GetComponent<HatStandTrigger>();
            if (stand != null)
            {
                // Calcular score basado en:
                // 1. Distancia del raycast
                // 2. Ángulo respecto a la dirección de la mano
                Vector3 directionToStand = (stand.transform.position - handTransform.position).normalized;
                float dot = Vector3.Dot(handTransform.forward, directionToStand);
                float angle = Mathf.Acos(Mathf.Clamp01(dot)) * Mathf.Rad2Deg;
                
                // Score que combina distancia y ángulo (menor es mejor)
                float score = sphereHit.distance + (angle * 0.1f); // Penalizar ángulos grandes
                
                if (score < bestSphereCastScore)
                {
                    bestSphereCastScore = score;
                    bestSphereCastStand = stand;
                }
                
                Debug.DrawRay(handTransform.position, handTransform.forward * sphereHit.distance, Color.yellow, 0.1f);
            }
        }

        if (bestSphereCastStand != null)
        {
            return bestSphereCastStand;
        }

        // PRIORIDAD 3: Solo como último recurso, buscar por proximidad muy cercana
        // Pero SOLO si está muy cerca y en la dirección general correcta
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, magnetDistance * 0.5f, standLayerMask);
        
        float bestProximityScore = float.MaxValue;
        HatStandTrigger bestProximityStand = null;
        
        foreach (Collider col in nearbyColliders)
        {
            HatStandTrigger stand = col.GetComponent<HatStandTrigger>();
            if (stand != null)
            {
                Vector3 directionToStand = (stand.transform.position - handTransform.position).normalized;
                float dot = Vector3.Dot(handTransform.forward, directionToStand);
                
                // Solo considerar stands que están al menos vagamente en la dirección correcta
                if (dot > 0.3f) // Aproximadamente 70 grados de tolerancia
                {
                    float distance = Vector3.Distance(transform.position, stand.transform.position);
                    float angle = Mathf.Acos(Mathf.Clamp01(dot)) * Mathf.Rad2Deg;
                    
                    // Score que fuertemente penaliza ángulos grandes
                    float score = distance + (angle * 0.5f);
                    
                    if (score < bestProximityScore)
                    {
                        bestProximityScore = score;
                        bestProximityStand = stand;
                    }
                }
            }
        }

        return bestProximityStand;
    }

    private void UpdateStandHighlight(HatStandTrigger newTarget)
    {
        if (currentTargetStand != newTarget)
        {
            // Limpiar highlight anterior
            ClearStandHighlight();

            // Aplicar nuevo highlight
            if (newTarget != null && highlightMaterial != null)
            {
                standRenderer = newTarget.GetComponent<Renderer>();
                if (standRenderer != null)
                {
                    originalStandMaterial = standRenderer.material;
                    standRenderer.material = highlightMaterial;
                }
            }

            currentTargetStand = newTarget;
        }
    }

    private void ClearStandHighlight()
    {
        if (standRenderer != null && originalStandMaterial != null)
        {
            standRenderer.material = originalStandMaterial;
            standRenderer = null;
            originalStandMaterial = null;
        }
    }

    private void ApplyMagnetism(HatStandTrigger targetStand)
    {
        if (targetStand == null || isPlacedOnStand) return;

        float distance = Vector3.Distance(transform.position, targetStand.transform.position);
        
        if (distance <= magnetDistance)
        {
            // Aplicar una fuerza sutil hacia el stand
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 targetPosition = CalculateExactStandPosition(targetStand);
                Vector3 direction = (targetPosition - transform.position).normalized;
                float magnetForce = Mathf.Lerp(magnetStrength, 0, distance / magnetDistance);
                
                rb.AddForce(direction * magnetForce, ForceMode.Force);
                
                // Opcional: también alinear rotación gradualmente
                Quaternion targetRotation = CalculateStandRotation(targetStand);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }
        }
    }

    private Vector3 CalculateExactStandPosition(HatStandTrigger stand)
    {
        Vector3 standPosition;

        if (useColliderBounds)
        {
            // Usar el centro superior del collider del stand
            Collider standCollider = stand.GetComponent<Collider>();
            if (standCollider != null)
            {
                Bounds bounds = standCollider.bounds;
                standPosition = new Vector3(bounds.center.x, bounds.max.y + heightOffset, bounds.center.z);
            }
            else
            {
                // Fallback al transform del stand
                standPosition = stand.transform.position + Vector3.up * heightOffset;
            }
        }
        else
        {
            // Usar el transform del stand con offset
            standPosition = stand.transform.position + Vector3.up * heightOffset;
        }

        return standPosition;
    }

    private Quaternion CalculateStandRotation(HatStandTrigger stand)
    {
        // Mantener la rotación Y del stand pero usar rotación neutral en X y Z
        Vector3 standEuler = stand.transform.eulerAngles;
        return Quaternion.Euler(0, standEuler.y, 0);
    }

    private void PlaceHatOnStand(HatStandTrigger stand)
    {
        if (stand == null) return;

        isPlacedOnStand = true;

        // Calcular posición exacta del stand
        Vector3 exactPosition = CalculateExactStandPosition(stand);
        Quaternion exactRotation = CalculateStandRotation(stand);

        // Colocar el sombrero en la posición exacta
        transform.position = exactPosition;
        transform.rotation = exactRotation;
        
        // Asegurar escala original
        transform.localScale = originalScale;

        // Desactivar físicas para que se mantenga en su lugar
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            // Reactivar kinematic después de un tiempo más largo para estabilidad
            CancelInvoke(nameof(ReactivatePhysics));
            Invoke(nameof(ReactivatePhysics), stabilizeTime);
        }

        Debug.Log($"🧢 Sombrero '{name}' colocado perfectamente en el stand '{stand.name}' en posición {exactPosition}");
    }

    private void ReactivatePhysics()
    {
        // Solo reactivar si no está siendo agarrado
        if (!isBeingGrabbed)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    // Método público para forzar estabilización (útil para debugging)
    public void ForceStabilize()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        isPlacedOnStand = true;
    }

    // Método de ayuda para visualizar el área de detección en el editor
    void OnDrawGizmosSelected()
    {
        if (handTransform != null)
        {
            // Dibujar raycast
            Gizmos.color = Color.green;
            Gizmos.DrawRay(handTransform.position, handTransform.forward * rayDistance);
            
            // Dibujar área de detección
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(handTransform.position, detectionRadius);
        }

        // Dibujar área de magnetismo
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, magnetDistance);

        // Dibujar posición de colocación si hay un stand cercano
        if (currentTargetStand != null)
        {
            Vector3 exactPos = CalculateExactStandPosition(currentTargetStand);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(exactPos, 0.05f);
        }
    }
}

