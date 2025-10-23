using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Frogger : MonoBehaviour
{
    // Sprites para diferentes estados de la rana (Quieta, saltando, muerta)
    [Header("Sprite Configuration")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite leapSprite;
    [SerializeField] private Sprite deadSprite;

    [Header("Movement Settings")]
    [SerializeField] private float leapDuration = 0.125f;
    [SerializeField] private float movementDistance = 1f;

    // Enum para estados del frogger
    public enum FroggerState
    {
        Idle,
        Leaping,
        Dead
    }

    // Constantes para layers
    private const string PLATFORM_LAYER = "Platform";
    private const string OBSTACLE_LAYER = "Obstacle";
    private const string BARRIER_LAYER = "Barrier";

    // Constantes para rotaciones
    private static readonly Quaternion ROTATION_UP = Quaternion.Euler(0f, 0f, 0f);
    private static readonly Quaternion ROTATION_LEFT = Quaternion.Euler(0f, 0f, 90f);
    private static readonly Quaternion ROTATION_RIGHT = Quaternion.Euler(0f, 0f, -90f);
    private static readonly Quaternion ROTATION_DOWN = Quaternion.Euler(0f, 0f, 180f);

    // Referencias y variables privadas
    private SpriteRenderer spriteRenderer;
    private Vector3 spawnPosition;
    private float farthestRow;
    private FroggerState currentState;
    private bool isInputEnabled = true;

    // Cache de LayerMasks para mejor rendimiento
    private int platformLayerMask;
    private int obstacleLayerMask;
    private int barrierLayerMask;

    private void Awake()
    {
        // Validación de componentes críticos
        if (!ValidateComponents())
        {
            Debug.LogError($"[Frogger] Faltan componentes críticos en {gameObject.name}");
            enabled = false;
            return;
        }

        // Obtener el componente SpriteRenderer y guardar la posición inicial
        spriteRenderer = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        farthestRow = spawnPosition.y;

        // Inicializar cache de LayerMasks
        InitializeLayerMasks();

        // Establecer estado inicial
        SetState(FroggerState.Idle);
    }

    private bool ValidateComponents()
    {
        // Validar sprites requeridos
        if (idleSprite == null)
        {
            Debug.LogError("[Frogger] idleSprite no está asignado");
            return false;
        }
        if (leapSprite == null)
        {
            Debug.LogError("[Frogger] leapSprite no está asignado");
            return false;
        }
        if (deadSprite == null)
        {
            Debug.LogError("[Frogger] deadSprite no está asignado");
            return false;
        }

        // Validar GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Frogger] GameManager.Instance no está disponible");
            return false;
        }

        return true;
    }

    private void InitializeLayerMasks()
    {
        platformLayerMask = LayerMask.GetMask(PLATFORM_LAYER);
        obstacleLayerMask = LayerMask.GetMask(OBSTACLE_LAYER);
        barrierLayerMask = LayerMask.GetMask(BARRIER_LAYER);
    }

    private void SetState(FroggerState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        // Cambiar sprite según el estado
        switch (currentState)
        {
            case FroggerState.Idle:
                if (spriteRenderer != null) spriteRenderer.sprite = idleSprite;
                isInputEnabled = true;
                break;
            case FroggerState.Leaping:
                if (spriteRenderer != null) spriteRenderer.sprite = leapSprite;
                isInputEnabled = false;
                break;
            case FroggerState.Dead:
                if (spriteRenderer != null) spriteRenderer.sprite = deadSprite;
                isInputEnabled = false;
                break;
        }
    }

    private void Update()
    {
        // Solo procesar input si está habilitado y no estamos saltando
        if (!isInputEnabled || currentState != FroggerState.Idle) return;

        // Procesar input de movimiento
        ProcessMovementInput();
    }

    private void ProcessMovementInput()
    {
        // Detectar input del teclado y mover la rana en la dirección correspondiente
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            HandleMovementInput(Vector3.up, ROTATION_UP);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            HandleMovementInput(Vector3.left, ROTATION_LEFT);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            HandleMovementInput(Vector3.right, ROTATION_RIGHT);
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            HandleMovementInput(Vector3.down, ROTATION_DOWN);
        }
    }

    private void HandleMovementInput(Vector3 direction, Quaternion rotation)
    {
        transform.rotation = rotation;
        Move(direction);
    }

    private void Move(Vector3 direction)
    {
        // Si no estamos en estado idle, no permitir movimiento
        if (currentState != FroggerState.Idle) return;

        Vector3 destination = transform.position + (direction * movementDistance);

        // Verificar colisiones en el destino usando diferentes Layers (con cache)
        CollisionInfo collisionInfo = CheckCollisions(destination);

        // Prevenir cualquier movimiento si hay una barrera
        if (collisionInfo.hasBarrier)
        {
            return;
        }

        // Manejar adherencia a plataformas
        HandlePlatformAttachment(collisionInfo.platform);

        // Verificar condiciones de muerte
        if (ShouldDie(collisionInfo))
        {
            transform.position = destination;
            Death();
            return;
        }

        // Si las condiciones están bien, proceder con el movimiento
        ExecuteMovement(destination);
    }

    private struct CollisionInfo
    {
        public Collider2D platform;
        public Collider2D obstacle;
        public bool hasBarrier;
        public bool hasPlatform => platform != null;
        public bool hasObstacle => obstacle != null;
    }

    private CollisionInfo CheckCollisions(Vector3 position)
    {
        return new CollisionInfo
        {
            platform = Physics2D.OverlapBox(position, Vector2.zero, 0f, platformLayerMask),
            obstacle = Physics2D.OverlapBox(position, Vector2.zero, 0f, obstacleLayerMask),
            hasBarrier = Physics2D.OverlapBox(position, Vector2.zero, 0f, barrierLayerMask) != null
        };
    }

    private void HandlePlatformAttachment(Collider2D platform)
    {
        // Adherir/despegar la rana de la plataforma de manera más segura
        if (platform != null)
        {
            // Solo adherir si no estamos ya adheridos a esta plataforma
            if (transform.parent != platform.transform)
            {
                transform.SetParent(platform.transform);
            }
        }
        else
        {
            // Solo despegar si estamos adheridos
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
        }
    }

    private bool ShouldDie(CollisionInfo collisionInfo)
    {
        // La rana muere cuando toca un obstáculo sin estar en una plataforma
        return collisionInfo.hasObstacle && !collisionInfo.hasPlatform;
    }

    private void ExecuteMovement(Vector3 destination)
    {
        // Verificar si hemos avanzado a una fila más lejana
        if (destination.y > farthestRow)
        {
            farthestRow = destination.y;
            // Validar GameManager antes de usar
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AdvancedRow();
            }
        }

        // Iniciar animación de salto
        StopAllCoroutines();
        StartCoroutine(Leap(destination));
    }

    private IEnumerator Leap(Vector3 destination)
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        // Establecer estado de salto
        SetState(FroggerState.Leaping);

        while (elapsed < leapDuration)
        {
            // Validar que aún tenemos el componente transform
            if (transform == null) yield break;

            // Mover hacia el destino gradualmente usando interpolación
            float t = elapsed / leapDuration;
            transform.position = Vector3.Lerp(startPosition, destination, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Establecer estado final del salto
        if (transform != null)
        {
            transform.position = destination; // Asegurar posición exacta
            SetState(FroggerState.Idle); // Volver al estado idle
        }
    }

    public void Respawn()
    {
        // Validar que tenemos los componentes necesarios
        if (spriteRenderer == null)
        {
            Debug.LogError("[Frogger] spriteRenderer es null en Respawn");
            return;
        }

        // Detener todas las animaciones en curso
        StopAllCoroutines();

        // Resetear transform a la posición de spawn inicial
        transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        farthestRow = spawnPosition.y;

        // Desadherir de cualquier plataforma
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        // Habilitar control del jugador y resetear estado
        gameObject.SetActive(true);
        enabled = true;
        SetState(FroggerState.Idle);
    }

    public void Death()
    {
        // Detener todas las animaciones
        StopAllCoroutines();

        // Deshabilitar control del jugador
        enabled = false;

        // Mostrar sprite de muerte y resetear rotación
        transform.rotation = Quaternion.identity;
        SetState(FroggerState.Dead);

        // Actualizar estado del juego notificando la muerte
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Died();
        }
        else
        {
            Debug.LogError("[Frogger] GameManager.Instance es null en Death");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Validaciones de seguridad
        if (other == null || !enabled) return;

        // Verificar si el objeto con el que colisionamos es un obstáculo
        bool hitObstacle = other.gameObject.layer == LayerMask.NameToLayer(OBSTACLE_LAYER);

        // Verificar si estamos sobre una plataforma
        bool onPlatform = transform.parent != null;

        // Morir solo si estamos habilitados, tocamos un obstáculo y NO estamos en una plataforma
        if (hitObstacle && !onPlatform && currentState != FroggerState.Dead)
        {
            Death();
        }
    }

    // Métodos públicos para acceso al estado
    public FroggerState GetCurrentState() => currentState;
    public bool IsInputEnabled() => isInputEnabled;
    public float GetFarthestRow() => farthestRow;

    // Validación en el Inspector
    private void OnValidate()
    {
        // Validar que la duración del salto sea positiva
        if (leapDuration <= 0f)
        {
            leapDuration = 0.125f;
        }

        // Validar que la distancia de movimiento sea positiva
        if (movementDistance <= 0f)
        {
            movementDistance = 1f;
        }
    }
}
