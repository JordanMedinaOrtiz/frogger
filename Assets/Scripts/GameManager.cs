using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)] // Esto hace que se ejecute antes que otros scripts
public class GameManager : MonoBehaviour
{
    // Instancia singleton para acceso global
    public static GameManager Instance { get; private set; }

    [Header("Game Configuration")]
    [SerializeField] private int initialLives = 3;
    [SerializeField] private int levelTimeLimit = 30;
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private float gameOverDelay = 1f;
    [SerializeField] private float newLevelDelay = 1f;

    [Header("Score Configuration")]
    [SerializeField] private int pointsPerRow = 10;
    [SerializeField] private int checkpointBasePoints = 50;
    [SerializeField] private int timeMultiplier = 20;
    [SerializeField] private int levelCompletionBonus = 1000;

    [Header("Game References")]
    [SerializeField] private CheckPoint[] checkPoints;
    [SerializeField] private Frogger frogger;
    [SerializeField] private GameObject gameOverMenu;

    [Header("UI References")]
    [SerializeField] private Text timeText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text scoreText;

    // Enum para estados del juego
    public enum GameState
    {
        Menu,
        Playing,
        GameOver,
        LevelComplete,
        Paused
    }

    // Propiedades públicas de solo lectura para el estado del juego
    public int lives { get; private set; }
    public int score { get; private set; }
    public int time { get; private set; }
    public GameState currentState { get; private set; }

    // Variables privadas para control interno
    private Coroutine timerCoroutine;
    private Coroutine playAgainCoroutine;
    private bool isInitialized;

    // Cache para validaciones
    private readonly HashSet<KeyCode> restartKeys = new HashSet<KeyCode> { KeyCode.Return, KeyCode.Space };

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (Instance != null) {
            Debug.LogWarning($"[GameManager] Múltiples instancias detectadas. Destruyendo {gameObject.name}");
            DestroyImmediate(gameObject); // Destruir duplicados
            return;
        }

        Instance = this; // Establecer esta instancia como la única

        // Validar componentes críticos
        if (!ValidateComponents())
        {
            Debug.LogError("[GameManager] Faltan componentes críticos. GameManager deshabilitado.");
            enabled = false;
            return;
        }

        InitializeGame();
    }

    private bool ValidateComponents()
    {
        bool isValid = true;

        // Validar referencias de  juego
        if (frogger == null)
        {
            Debug.LogError("[GameManager] Frogger reference is missing");
            isValid = false;
        }

        if (checkPoints == null || checkPoints.Length == 0)
        {
            Debug.LogError("[GameManager] CheckPoints array is null or empty");
            isValid = false;
        }
        else
        {
            // Validar que todos los checkpoints existen
            for (int i = 0; i < checkPoints.Length; i++)
            {
                if (checkPoints[i] == null)
                {
                    Debug.LogError($"[GameManager] CheckPoint at index {i} is null");
                    isValid = false;
                }
            }
        }

        if (gameOverMenu == null)
        {
            Debug.LogError("[GameManager] GameOver menu reference is missing");
            isValid = false;
        }

        // Validar referencias de UI
        if (timeText == null)
        {
            Debug.LogError("[GameManager] Time text reference is missing");
            isValid = false;
        }

        if (livesText == null)
        {
            Debug.LogError("[GameManager] Lives text reference is missing");
            isValid = false;
        }

        if (scoreText == null)
        {
            Debug.LogError("[GameManager] Score text reference is missing");
            isValid = false;
        }

        return isValid;
    }

    private void InitializeGame()
    {
        // Inicializar valores por defecto
        lives = initialLives;
        score = 0;
        time = levelTimeLimit;
        currentState = GameState.Menu;
        isInitialized = true;
    }

    private void OnDestroy()
    {
        // Limpiar la referencia singleton al destruir el objeto
        if (Instance == this) {
            Instance = null;
        }

        // Detener todas las corrutinas para evitar errores
        StopAllCoroutines();
    }

    private void Start()
    {
        // Solo iniciar el juego si la inicialización fue exitosa
        if (isInitialized)
        {
            StartNewGame();
        }
    }

    private void StartNewGame()
    {
        if (!isInitialized)
        {
            Debug.LogError("[GameManager] Cannot start new game - not initialized");
            return;
        }

        // Detener cualquier corrutina en ejecución
        StopAllGameCoroutines();

        // Ocultar menú de fin de juego
        SafeSetActive(gameOverMenu, false);

        // Resetear estadísticas del juego
        SetScore(0);
        SetLives(initialLives);

        // Cambiar estado del juego
        SetGameState(GameState.Playing);

        StartNewLevel();
    }

    private void StartNewLevel()
    {
        if (!isInitialized)
        {
            Debug.LogError("[GameManager] Cannot start new level - not initialized");
            return;
        }

        // Resetear todos los checkpoints como no ocupados
        ResetAllCheckpoints();

        // Hacer reaparecer al jugador
        RespawnPlayer();
    }

    private void ResetAllCheckpoints()
    {
        if (checkPoints == null) return;

        for (int i = 0; i < checkPoints.Length; i++)
        {
            if (checkPoints[i] != null)
            {
                checkPoints[i].enabled = false;
            }
            else
            {
                Debug.LogWarning($"[GameManager] CheckPoint at index {i} is null during reset");
            }
        }
    }

    private void StopAllGameCoroutines()
    {
        // Detener corrutinas específicas si están corriendo
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }

        if (playAgainCoroutine != null)
        {
            StopCoroutine(playAgainCoroutine);
            playAgainCoroutine = null;
        }

        // Detener todas las demás corrutinas como respaldo
        StopAllCoroutines();
    }

    private void SafeSetActive(GameObject gameObject, bool active)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(active);
        }
        else
        {
            Debug.LogWarning("[GameManager] Attempted to set active state on null GameObject");
        }
    }

    private void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        GameState previousState = currentState;
        currentState = newState;
    }

    private void RespawnPlayer()
    {
        if (frogger == null)
        {
            Debug.LogError("[GameManager] Cannot respawn - frogger reference is null");
            return;
        }

        // Hacer reaparecer a la rana en su posición inicial
        frogger.Respawn();

        // Detener cualquier temporizador anterior e iniciar uno nuevo
        StopAllGameCoroutines();
        StartLevelTimer();
    }

    private void StartLevelTimer()
    {
        if (currentState != GameState.Playing) return;

        timerCoroutine = StartCoroutine(LevelTimerCoroutine(levelTimeLimit));
    }

    private IEnumerator LevelTimerCoroutine(int duration)
    {
        // Validar duración
        if (duration <= 0)
        {
            Debug.LogError($"[GameManager] Invalid timer duration: {duration}");
            yield break;
        }

        // Inicializar el temporizador con la duración especificada
        time = duration;
        UpdateTimeUI();

        // Reducir tiempo cada segundo
        while (time > 0 && currentState == GameState.Playing)
        {
            yield return new WaitForSeconds(1f); // Esperar 1 segundo

            // Verificar si el juego sigue activo
            if (currentState != GameState.Playing) yield break;

            time--;
            UpdateTimeUI();
        }

        // Cuando se acaba el tiempo, la rana muere, solo si el juego sigue activo
        if (currentState == GameState.Playing && frogger != null)
        {
            frogger.Death();
        }

        timerCoroutine = null;
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
        {
            timeText.text = time.ToString();
        }
        else
        {
            Debug.LogWarning("[GameManager] timeText reference is null");
        }
    }

    public void Died()
    {
        // Solo procesar muerte si el juego está activo
        if (currentState != GameState.Playing)
        {
            Debug.LogWarning("[GameManager] Died() called but game is not in Playing state");
            return;
        }

        // Detener el temporizador al morir
        StopAllGameCoroutines();

        // Reducir una vida cuando el jugador muere
        SetLives(lives - 1);

        // Si quedan vidas, reaparecer después del delay configurado
        if (lives > 0)
        {
            StartCoroutine(DelayedRespawn());
        }
        else
        {
            // Si no quedan vidas, terminar el juego después del delay configurado
            StartCoroutine(DelayedGameOver());
        }
    }

    private IEnumerator DelayedRespawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Verificar que el juego sigue en estado válido
        if (currentState == GameState.Playing && lives > 0)
        {
            RespawnPlayer();
        }
    }

    private IEnumerator DelayedGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        // Cambiar estado del juego
        SetGameState(GameState.GameOver);

        // Ocultar la rana y mostrar menú de fin de juego
        if (frogger != null && frogger.gameObject != null)
        {
            frogger.gameObject.SetActive(false);
        }

        SafeSetActive(gameOverMenu, true);

        // Detener todos los temporizadores
        StopAllGameCoroutines();

        // Esperar input del jugador para jugar de nuevo
        playAgainCoroutine = StartCoroutine(WaitForPlayAgainInput());
    }

    private IEnumerator WaitForPlayAgainInput()
    {
        // Esperar hasta que el jugador presione una tecla válida para jugar de nuevo
        while (currentState == GameState.GameOver)
        {
            // Verificar múltiples teclas de reinicio
            foreach (KeyCode key in restartKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    playAgainCoroutine = null;
                    StartNewGame();
                    yield break;
                }
            }

            yield return null; // Esperar un frame
        }

        playAgainCoroutine = null;
    }

    public void AdvancedRow()
    {
        // Solo otorgar puntos si el juego está activo
        if (currentState != GameState.Playing) return;

        // Otorgar puntos cuando el jugador avanza una fila hacia adelante
        SetScore(score + pointsPerRow);
    }

    public void HomeOccupied()
    {
        CheckPointOccupied();
    }

    public void CheckPointOccupied()
    {
        // Solo procesar si el juego está activo
        if (currentState != GameState.Playing)
        {
            Debug.LogWarning("[GameManager] CheckPointOccupied() called but game is not in Playing state");
            return;
        }

        // Detener el temporizador
        StopAllGameCoroutines();

        // Ocultar la rana temporalmente cuando llega a un checkpoint
        if (frogger != null && frogger.gameObject != null)
        {
            frogger.gameObject.SetActive(false);
        }

        // Calcular puntos bonus basado en tiempo restante
        int bonusPoints = CalculateTimeBonus();
        int totalPoints = checkpointBasePoints + bonusPoints;
        SetScore(score + totalPoints);

        // Verificar si se completaron todas las checkpoints
        if (IsLevelCleared())
        {
            StartCoroutine(HandleLevelCompletion());
        }
        else
        {
            // Aún faltan checkpoints por completar, reaparecer
            StartCoroutine(DelayedRespawnAfterCheckpoint());
        }
    }

    private int CalculateTimeBonus()
    {
        // Validar que el tiempo sea positivo
        return Mathf.Max(0, time * timeMultiplier);
    }

    private IEnumerator HandleLevelCompletion()
    {
        SetGameState(GameState.LevelComplete);

        // Nivel completado, otorgar vida extra y bonus
        SetLives(lives + 1);
        SetScore(score + levelCompletionBonus);

        yield return new WaitForSeconds(newLevelDelay);

        // Verificar que el estado sigue siendo válido
        if (currentState == GameState.LevelComplete)
        {
            SetGameState(GameState.Playing);
            StartNewLevel();
        }
    }

    private IEnumerator DelayedRespawnAfterCheckpoint()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Verificar que el juego sigue activo
        if (currentState == GameState.Playing)
        {
            RespawnPlayer();
        }
    }

    private bool IsLevelCleared()
    {
        // Validar que el array de checkpoints existe
        if (checkPoints == null || checkPoints.Length == 0)
        {
            Debug.LogWarning("[GameManager] CheckPoints array is null or empty");
            return false;
        }

        // Verificar si todos los checkpoints están ocupados
        for (int i = 0; i < checkPoints.Length; i++)
        {
            if (checkPoints[i] == null)
            {
                Debug.LogWarning($"[GameManager] CheckPoint at index {i} is null");
                continue;
            }

            if (!checkPoints[i].enabled)
            {
                return false; // Si algún checkpoint no está ocupado, nivel no completado
            }
        }

        return true; // Los checkpoints están ocupados, nivel completado
    }

    private void SetScore(int newScore)
    {
        // Validar que el score no sea negativo
        if (newScore < 0)
        {
            Debug.LogWarning($"[GameManager] Attempted to set negative score: {newScore}");
            newScore = 0;
        }

        // Actualizar puntuación y mostrar en UI
        score = newScore;
        UpdateScoreUI();
    }

    private void SetLives(int newLives)
    {
        // Validar que las vidas no sean negativas
        if (newLives < 0)
        {
            Debug.LogWarning($"[GameManager] Attempted to set negative lives: {newLives}");
            newLives = 0;
        }

        // Actualizar vidas y mostrar en UI
        lives = newLives;
        UpdateLivesUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
        else
        {
            Debug.LogWarning("[GameManager] scoreText reference is null");
        }
    }

    private void UpdateLivesUI()
    {
        if (livesText != null)
        {
            livesText.text = lives.ToString();
        }
        else
        {
            Debug.LogWarning("[GameManager] livesText reference is null");
        }
    }

    // Métodos públicos para acceso a información del juego
    public GameState GetCurrentState() => currentState;
    public bool IsGameActive() => currentState == GameState.Playing;
    public int GetCurrentTime() => time;

    // Validación en el Inspector
    private void OnValidate()
    {
        // Validar configuraciones del juego
        if (initialLives <= 0)
        {
            initialLives = 3;
            Debug.LogWarning("[GameManager] initialLives must be positive, set to 3");
        }

        if (levelTimeLimit <= 0)
        {
            levelTimeLimit = 30;
            Debug.LogWarning("[GameManager] levelTimeLimit must be positive, set to 30");
        }

        if (respawnDelay < 0f)
        {
            respawnDelay = 1f;
            Debug.LogWarning("[GameManager] respawnDelay cannot be negative, set to 1");
        }

        if (pointsPerRow <= 0)
        {
            pointsPerRow = 10;
            Debug.LogWarning("[GameManager] pointsPerRow must be positive, set to 10");
        }
    }

    // Método para pausar/despausar el juego
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            Time.timeScale = 1f;
        }
    }
}
