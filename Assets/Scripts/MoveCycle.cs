using UnityEngine;

public class MoveCycle : MonoBehaviour
{
    [Header("Movement Configuration")]
    [SerializeField] private Vector2 direction = Vector2.right; // Dirección de movimiento, por defecto derecha
    [SerializeField] private float speed = 1f;
    [SerializeField] private float size = 1f;

    [Header("Advanced Settings")]
    [SerializeField] private bool autoUpdateBounds = true; // Actualizar bordes automáticamente
    [SerializeField] private float boundsUpdateInterval = 1f; // Intervalo de actualización de bordes
    [SerializeField] private Camera targetCamera; // Referencia específica a la cámara

    // Enum para tipos de wrapping
    public enum WrapMode
    {
        Horizontal,
        Vertical,
        Both,
        None
    }

    [SerializeField] private WrapMode wrapMode = WrapMode.Horizontal;

    // Bordes de la pantalla calculados desde la cámara
    private Vector3 leftEdge;
    private Vector3 rightEdge;
    private Vector3 topEdge;
    private Vector3 bottomEdge;

    // Variables de optimización y control
    private float lastBoundsUpdate;
    private bool isInitialized;
    private Vector3 lastPosition;
    private Camera cachedCamera;

    // Constantes para optimización
    private const float EPSILON = 0.001f;

    private void Start()
    {
        InitializeMoveCycle();
    }

    private void InitializeMoveCycle()
    {
        // Validar y configurar la cámara
        if (!SetupCamera())
        {
            Debug.LogError($"[MoveCycle] No se pudo configurar la cámara para {gameObject.name}");
            enabled = false;
            return;
        }

        // Calcular bordes iniciales
        if (!UpdateScreenBounds())
        {
            Debug.LogError($"[MoveCycle] Error al calcular bordes de pantalla para {gameObject.name}");
            enabled = false;
            return;
        }

        // Validar configuración inicial
        ValidateConfiguration();

        // Guardar posición inicial
        lastPosition = transform.position;
        lastBoundsUpdate = Time.time;
        isInitialized = true;
    }

    private bool SetupCamera()
    {
        // Priorizar cámara asignada manualmente
        if (targetCamera != null)
        {
            cachedCamera = targetCamera;
            return true;
        }

        // Intentar usar Camera.main como fallback
        if (Camera.main != null)
        {
            cachedCamera = Camera.main;
            Debug.LogWarning($"[MoveCycle] Usando Camera.main como fallback para {gameObject.name}");
            return true;
        }

        // Buscar cualquier cámara activa en la escena
        Camera[] cameras = FindObjectsOfType<Camera>();
        if (cameras.Length > 0)
        {
            cachedCamera = cameras[0];
            Debug.LogWarning($"[MoveCycle] Usando primera cámara encontrada para {gameObject.name}");
            return true;
        }

        return false;
    }

    private bool UpdateScreenBounds()
    {
        if (cachedCamera == null) return false;

        try
        {
            // Calcular todos los bordes usando la cámara configurada
            Vector3 bottomLeft = cachedCamera.ViewportToWorldPoint(new Vector3(0, 0, cachedCamera.nearClipPlane));
            Vector3 topRight = cachedCamera.ViewportToWorldPoint(new Vector3(1, 1, cachedCamera.nearClipPlane));

            leftEdge = bottomLeft;
            rightEdge = new Vector3(topRight.x, bottomLeft.y, bottomLeft.z);
            bottomEdge = bottomLeft;
            topEdge = new Vector3(bottomLeft.x, topRight.y, bottomLeft.z);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MoveCycle] Error al calcular bordes: {e.Message}");
            return false;
        }
    }

    private void ValidateConfiguration()
    {
        // Validar velocidad
        if (Mathf.Abs(speed) < EPSILON)
        {
            Debug.LogWarning($"[MoveCycle] Velocidad muy baja o cero en {gameObject.name}");
        }

        // Validar tamaño
        if (size <= 0)
        {
            size = 1f;
            Debug.LogWarning($"[MoveCycle] Tamaño inválido, establecido a 1 para {gameObject.name}");
        }

        // Validar dirección
        if (direction.magnitude < EPSILON)
        {
            direction = Vector2.right;
            Debug.LogWarning($"[MoveCycle] Dirección inválida, establecida a derecha para {gameObject.name}");
        }
        else
        {
            // Normalizar dirección para consistencia
            direction = direction.normalized;
        }
    }

    private void Update()
    {
        // No procesar si no está inicializado
        if (!isInitialized) return;

        // Actualizar bordes periódicamente si está habilitado
        if (autoUpdateBounds && Time.time - lastBoundsUpdate > boundsUpdateInterval)
        {
            UpdateScreenBounds();
            lastBoundsUpdate = Time.time;
        }

        // Ejecutar movimiento
        ProcessMovement();

        // Manejar wrapping según el modo configurado
        HandleWrapping();

        // Guardar posición para la siguiente frame
        lastPosition = transform.position;
    }

    private void ProcessMovement()
    {
        // Validar que tenemos una dirección válida y velocidad
        if (direction.magnitude < EPSILON || Mathf.Abs(speed) < EPSILON) return;

        // Calcular movimiento para esta frame
        Vector3 movement = speed * Time.deltaTime * (Vector3)direction;

        // Aplicar movimiento
        transform.Translate(movement, Space.World);
    }

    private void HandleWrapping()
    {
        Vector3 currentPos = transform.position;
        bool needsWrapping = false;
        Vector3 newPosition = currentPos;

        // Manejar wrapping horizontal
        if (wrapMode == WrapMode.Horizontal || wrapMode == WrapMode.Both)
        {
            if (ShouldWrapHorizontally(currentPos, out Vector3 wrappedPos))
            {
                newPosition.x = wrappedPos.x;
                needsWrapping = true;
            }
        }

        // Manejar wrapping vertical
        if (wrapMode == WrapMode.Vertical || wrapMode == WrapMode.Both)
        {
            if (ShouldWrapVertically(currentPos, out Vector3 wrappedPos))
            {
                newPosition.y = wrappedPos.y;
                needsWrapping = true;
            }
        }

        // Aplicar wrapping si es necesario
        if (needsWrapping)
        {
            transform.position = newPosition;
        }
    }

    private bool ShouldWrapHorizontally(Vector3 position, out Vector3 wrappedPosition)
    {
        wrappedPosition = position;

        // Verificar borde derecho (movimiento hacia la derecha)
        if (direction.x > EPSILON && (position.x - size) > rightEdge.x)
        {
            wrappedPosition.x = leftEdge.x - size;
            return true;
        }
        // Verificar borde izquierdo (movimiento hacia la izquierda)
        else if (direction.x < -EPSILON && (position.x + size) < leftEdge.x)
        {
            wrappedPosition.x = rightEdge.x + size;
            return true;
        }

        return false;
    }

    private bool ShouldWrapVertically(Vector3 position, out Vector3 wrappedPosition)
    {
        wrappedPosition = position;

        // Verificar borde superior (movimiento hacia arriba)
        if (direction.y > EPSILON && (position.y - size) > topEdge.y)
        {
            wrappedPosition.y = bottomEdge.y - size;
            return true;
        }
        // Verificar borde inferior (movimiento hacia abajo)
        else if (direction.y < -EPSILON && (position.y + size) < bottomEdge.y)
        {
            wrappedPosition.y = topEdge.y + size;
            return true;
        }

        return false;
    }

    // Métodos públicos para control externo
    public void SetDirection(Vector2 newDirection)
    {
        if (newDirection.magnitude > EPSILON)
        {
            direction = newDirection.normalized;
        }
        else
        {
            Debug.LogWarning($"[MoveCycle] Dirección inválida ignorada para {gameObject.name}");
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void SetSize(float newSize)
    {
        if (newSize > 0)
        {
            size = newSize;
        }
        else
        {
            Debug.LogWarning($"[MoveCycle] Tamaño inválido ignorado para {gameObject.name}");
        }
    }

    public void ForceUpdateBounds()
    {
        if (isInitialized)
        {
            UpdateScreenBounds();
            lastBoundsUpdate = Time.time;
        }
    }

    // Métodos de acceso para información
    public Vector2 GetDirection() => direction;
    public float GetSpeed() => speed;
    public float GetSize() => size;
    public bool IsInitialized() => isInitialized;
    public Camera GetTargetCamera() => cachedCamera;

    // Validación en el Inspector
    private void OnValidate()
    {
        // Validar velocidad
        if (speed < 0)
        {
            speed = Mathf.Abs(speed);
        }

        // Validar tamaño
        if (size <= 0)
        {
            size = 1f;
        }

        // Validar intervalo de actualización
        if (boundsUpdateInterval <= 0)
        {
            boundsUpdateInterval = 1f;
        }

        // Normalizar dirección si no es cero
        if (direction.magnitude > EPSILON)
        {
            direction = direction.normalized;
        }
    }

    // Manejo de gizmos para debugging visual
    private void OnDrawGizmosSelected()
    {
        if (!isInitialized) return;

        // Dibujar bordes de pantalla
        Gizmos.color = Color.yellow;

        // Borde izquierdo
        Gizmos.DrawLine(new Vector3(leftEdge.x, bottomEdge.y, 0), new Vector3(leftEdge.x, topEdge.y, 0));

        // Borde derecho
        Gizmos.DrawLine(new Vector3(rightEdge.x, bottomEdge.y, 0), new Vector3(rightEdge.x, topEdge.y, 0));

        // Borde superior
        Gizmos.DrawLine(new Vector3(leftEdge.x, topEdge.y, 0), new Vector3(rightEdge.x, topEdge.y, 0));

        // Borde inferior
        Gizmos.DrawLine(new Vector3(leftEdge.x, bottomEdge.y, 0), new Vector3(rightEdge.x, bottomEdge.y, 0));

        // Dibujar dirección de movimiento
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, (Vector3)direction * 2f);

        // Dibujar área de tamaño del objeto
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, size);
    }
}
