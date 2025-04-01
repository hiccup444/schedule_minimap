using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using AOT;
using Il2CppInterop.Runtime.Injection;
using static SimpleMinimap.MinimapManager;

namespace SimpleMinimap
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Schedule I.exe")]
    public class MiniMapMain : BasePlugin
    {
        internal static new ManualLogSource Log;

        public override void Load()
        {
            Log = base.Log;
            Log.LogError($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Check and register MinimapManager type
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(MinimapManager)))
            {
                Log.LogError("Registering MinimapManager with IL2CPP");
                ClassInjector.RegisterTypeInIl2Cpp(typeof(MinimapManager));
            }
            else
            {
                Log.LogError("MinimapManager is already registered with IL2CPP");
            }

            // Check and register MinimapComponent type
            if (!ClassInjector.IsTypeRegisteredInIl2Cpp(typeof(MinimapComponent)))
            {
                Log.LogError("Registering MinimapComponent with IL2CPP");
                ClassInjector.RegisterTypeInIl2Cpp(typeof(MinimapComponent));
            }
            else
            {
                Log.LogError("MinimapComponent is already registered with IL2CPP");
            }

            // Create the GameObject and let its Awake/Start handle initialization
            GameObject miniMapObject = new GameObject("MinimapManager");
            UnityEngine.Object.DontDestroyOnLoad(miniMapObject);

            // Add our main component
            miniMapObject.AddComponent<MinimapManager>();

            Log.LogError("MinimapManager GameObject created. Component will initialize in Awake/Start.");
        }
    }

    // Manager that handles scene detection and minimap creation
    public class MinimapManager : MonoBehaviour
    {
        private string currentSceneName = "";
        private GameObject minimapObject;
        public MinimapComponent minimapComponent;

        private void Awake()
        {
            MiniMapMain.Log.LogError($"<MethodName> entered");
            MiniMapMain.Log.LogError("MinimapManager Awake");
            // persist between scene loads
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            MiniMapMain.Log.LogError($"<MethodName> entered");
            try
            {
                MiniMapMain.Log.LogError("MinimapManager Start - Beginning scene monitoring");

                // Check current scene
                currentSceneName = SceneManager.GetActiveScene().name;
                MiniMapMain.Log.LogError($"Current scene: {currentSceneName}");

                // Initialize minimap if we're already in the Main scene
                if (currentSceneName == "Main")
                {
                    MiniMapMain.Log.LogError("Already in Main scene, creating minimap");
                    CreateMinimapObject();
                }
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error in MinimapManager Start: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        private void Update()
        {
            MiniMapMain.Log.LogError($"<MethodName> entered");
            try
            {
                // Scene change detection
                string sceneName = SceneManager.GetActiveScene().name;
                if (sceneName != currentSceneName)
                {
                    MiniMapMain.Log.LogError($"Scene changed from {currentSceneName} to {sceneName}");
                    currentSceneName = sceneName;

                    // If entering Main scene, create minimap
                    if (sceneName == "Main")
                    {
                        MiniMapMain.Log.LogError("Detected Main scene, creating minimap");
                        CreateMinimapObject();
                    }
                }
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error in MinimapManager Update: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        private void CreateMinimapObject()
        {
            MiniMapMain.Log.LogError($"<MethodName> entered");
            try
            {
                MiniMapMain.Log.LogError("CreateMinimapObject called");

                // Clean up any existing instance
                if (minimapObject != null)
                {
                    MiniMapMain.Log.LogError("Destroying existing minimap object");
                    Destroy(minimapObject);
                    minimapObject = null;
                    minimapComponent = null;
                }

                // Create new minimap object
                minimapObject = new GameObject("MinimapObject");
                MiniMapMain.Log.LogError("Created new GameObject for minimap");
                DontDestroyOnLoad(minimapObject);

                // Add minimap component - will initialize in its own Awake
                minimapComponent = minimapObject.AddComponent<MinimapComponent>();
                if (minimapComponent != null)
                    MiniMapMain.Log.LogError("MinimapComponent added successfully");
                else
                    MiniMapMain.Log.LogError("Failed to add MinimapComponent to GameObject");

                MiniMapMain.Log.LogError("Minimap object created successfully");
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error creating minimap object: {ex.Message}\nStack trace: {ex.StackTrace}");
            }
        }

        // Main component that handles the minimap functionality
        public class MinimapComponent : MonoBehaviour
        {
            // Map settings and UI references
            private float mapScale = 1.2487098f;
            private Vector2 mapOffset = new Vector2(10f, 0f);
            private GameObject minimapUIObject;
            private bool isInitializing = false;
            private bool isEnabled = true;
            private RectTransform cachedDirectionIndicator;
            private GameObject cachedPlayerMarker;
            private Text cachedGameTimeText;
            private GameObject cachedMapContent;
            private Transform cachedPropertyPoI;
            private Text minimapTimeText;
            private GameObject mapAppObject;
            private GameObject viewportObject;
            private GameObject playerObject;
            private GameObject mapContentObject;
            private RectTransform gridContainer;

            // Grid properties
            private int gridSize = 20;
            private Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);

            // Find GameObjects state
            private bool findGameObjectsRunning = false;
            private float findObjectsTimer = 0f;
            private int findObjectsAttempts = 0;

            // Update time state
            private bool updateTimeRunning = false;
            private float updateTimeTimer = 0f;

            private void Awake()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                try
                {
                    MiniMapMain.Log.LogError("MinimapComponent Awake");
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error in MinimapComponent Awake: {ex.Message}\nStack trace: {ex.StackTrace}");
                }
            }

            private void Start()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                try
                {
                    MiniMapMain.Log.LogError("MinimapComponent Start - Initializing minimap");
                    isInitializing = true;

                    // Create UI
                    MiniMapMain.Log.LogError("Creating minimap UI");
                    CreateMinimapUI();
                    MiniMapMain.Log.LogError("Minimap UI created");

                    // Start manual coroutines (note: these are called in Update, not via StartCoroutine)
                    MiniMapMain.Log.LogError("Setting flags to begin manual object search and time update");
                    findGameObjectsRunning = true;
                    findObjectsTimer = 0f;
                    findObjectsAttempts = 0;
                    updateTimeRunning = true;
                    updateTimeTimer = 0f;

                    isInitializing = false;
                    MiniMapMain.Log.LogError("MinimapComponent initialization completed");
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error in MinimapComponent Start: {ex.Message}\nStack trace: {ex.StackTrace}");
                }
            }

            private void Update()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                try
                {
                    // These methods are called manually every frame (instead of using StartCoroutine)
                    if (findGameObjectsRunning)
                    {
                        MiniMapMain.Log.LogError("Update: Calling UpdateFindGameObjects");
                        UpdateFindGameObjects();
                    }

                    if (updateTimeRunning)
                    {
                        MiniMapMain.Log.LogError("Update: Calling UpdateMinimapTimeManual");
                        UpdateMinimapTimeManual();
                    }

                    // Toggle minimap with F3
                    if (Input.GetKeyDown(KeyCode.F3) && minimapUIObject != null)
                    {
                        isEnabled = !isEnabled;
                        minimapUIObject.SetActive(isEnabled);
                        MiniMapMain.Log.LogError("Minimap " + (isEnabled ? "Enabled" : "Disabled"));
                    }

                    // Update minimap position based on player position
                    MiniMapMain.Log.LogError("Checking playerObject and mapContentObject");
                    if (isEnabled && playerObject != null && mapContentObject != null)
                    {
                        MiniMapMain.Log.LogError("Passed null check, accessing transform");
                        Vector3 playerPos = playerObject.transform.position;
                        float mapX = -playerPos.x * mapScale;
                        float mapY = -playerPos.z * mapScale;
                        RectTransform contentRect = mapContentObject.GetComponent<RectTransform>();
                        if (contentRect != null)
                        {
                            Vector2 targetPos = new Vector2(mapX, mapY) + mapOffset;
                            contentRect.anchoredPosition = Vector2.Lerp(contentRect.anchoredPosition, targetPos, Time.deltaTime * 10f);
                            UpdatePlayerDirectionIndicator();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error in Update: {ex.Message}");
                }
            }

            private void UpdateFindGameObjects()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                MiniMapMain.Log.LogError("UpdateFindGameObjects: Entering method");
                findObjectsTimer += Time.deltaTime;

                // Wait 2 seconds on first update
                if (findObjectsAttempts == 0 && findObjectsTimer < 2f)
                {
                    MiniMapMain.Log.LogError("UpdateFindGameObjects: Waiting for initial 2 seconds");
                    return;
                }

                // Check every 0.5 seconds
                if (findObjectsTimer >= 0.5f)
                {
                    MiniMapMain.Log.LogError("UpdateFindGameObjects: Timer reached 0.5 seconds");
                    findObjectsTimer = 0f;
                    findObjectsAttempts++;
                    MiniMapMain.Log.LogError($"UpdateFindGameObjects: Attempt {findObjectsAttempts}");

                    try
                    {
                        // Locate the player
                        if (playerObject == null)
                        {
                            playerObject = GameObject.Find("Player_Local");
                            if (playerObject != null)
                                MiniMapMain.Log.LogError("UpdateFindGameObjects: Found Player_Local");
                        }

                        // Locate the Map App
                        if (mapAppObject == null)
                        {
                            GameObject gameplayMenu = GameObject.Find("GameplayMenu");
                            if (gameplayMenu != null)
                            {
                                Transform phoneTransform = gameplayMenu.transform.Find("Phone");
                                if (phoneTransform != null)
                                {
                                    Transform phoneChildTransform = phoneTransform.Find("phone");
                                    if (phoneChildTransform != null)
                                    {
                                        Transform appsCanvas = phoneChildTransform.Find("AppsCanvas");
                                        if (appsCanvas != null)
                                        {
                                            Transform mapApp = appsCanvas.Find("MapApp");
                                            if (mapApp != null)
                                            {
                                                mapAppObject = mapApp.gameObject;
                                                MiniMapMain.Log.LogError("UpdateFindGameObjects: Found MapApp");
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Locate the viewport under Map App
                        if (mapAppObject != null && viewportObject == null)
                        {
                            Transform container = mapAppObject.transform.Find("Container");
                            if (container != null)
                            {
                                Transform scrollView = container.Find("Scroll View");
                                if (scrollView != null)
                                {
                                    Transform viewport = scrollView.Find("Viewport");
                                    if (viewport != null)
                                    {
                                        viewportObject = viewport.gameObject;
                                        MiniMapMain.Log.LogError("UpdateFindGameObjects: Found Map Viewport");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MiniMapMain.Log.LogError($"UpdateFindGameObjects: Error finding game objects: {ex.Message}");
                    }

                    // Stop conditions
                    if ((mapAppObject != null && playerObject != null) || findObjectsAttempts >= 30)
                    {
                        if (mapAppObject == null)
                            MiniMapMain.Log.LogWarning("UpdateFindGameObjects: Could not find Map App after multiple attempts");
                        else if (viewportObject == null)
                            MiniMapMain.Log.LogWarning("UpdateFindGameObjects: Found MapApp but could not find Viewport");
                        if (playerObject == null)
                            MiniMapMain.Log.LogWarning("UpdateFindGameObjects: Could not find Player after multiple attempts");

                        MiniMapMain.Log.LogError("UpdateFindGameObjects: Game object search completed");

                        // Process map content if available.
                        if (viewportObject != null)
                        {
                            try
                            {
                                if (viewportObject.transform.childCount > 0)
                                {
                                    Transform contentTransform = viewportObject.transform.GetChild(0);
                                    MiniMapMain.Log.LogError($"UpdateFindGameObjects: Found viewport content: {contentTransform.name}");
                                    Image contentImage = contentTransform.GetComponent<Image>();
                                    if (contentImage != null && contentImage.sprite != null)
                                    {
                                        MiniMapMain.Log.LogError($"UpdateFindGameObjects: Found content image with sprite: {contentImage.sprite.name}");
                                        if (mapContentObject != null)
                                        {
                                            Image minimapImage = mapContentObject.GetComponent<Image>();
                                            if (minimapImage == null)
                                                minimapImage = mapContentObject.AddComponent<Image>();
                                            minimapImage.sprite = contentImage.sprite;
                                            minimapImage.type = Image.Type.Simple;
                                            minimapImage.preserveAspect = true;
                                            // Force update via disable/enable
                                            minimapImage.enabled = false;
                                            minimapImage.enabled = true;
                                            MiniMapMain.Log.LogError("UpdateFindGameObjects: Successfully applied map sprite to minimap!");
                                            if (gridContainer != null)
                                                gridContainer.gameObject.SetActive(false);
                                        }
                                    }
                                    else
                                    {
                                        MiniMapMain.Log.LogError("UpdateFindGameObjects: Content doesn't have an Image component or sprite");
                                    }
                                }
                            }
                            catch (Exception exProcess)
                            {
                                MiniMapMain.Log.LogError($"UpdateFindGameObjects: Error accessing map content: {exProcess.Message}");
                            }
                        }

                        CreateVisualGrid();
                        findGameObjectsRunning = false;
                    }
                }
            }

            private void UpdateMinimapTimeManual()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                MiniMapMain.Log.LogError("UpdateMinimapTimeManual: Entering method");
                updateTimeTimer += Time.deltaTime;

                if (updateTimeTimer >= 1f)
                {
                    MiniMapMain.Log.LogError("UpdateMinimapTimeManual: 1 second elapsed, updating minimap time");
                    updateTimeTimer = 0f;
                    UpdateMinimapTime();
                }
            }

            private void UpdateMinimapTime()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                if (cachedGameTimeText == null)
                {
                    GameObject timeObj = GameObject.Find("GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
                    if (timeObj != null)
                    {
                        MiniMapMain.Log.LogError("UpdateMinimapTime: Found time object");
                        cachedGameTimeText = timeObj.GetComponent<Text>();
                    }
                }
                if (cachedGameTimeText != null && minimapTimeText != null)
                {
                    minimapTimeText.text = cachedGameTimeText.text;
                    MiniMapMain.Log.LogError("UpdateMinimapTime: Updated minimap time");
                }
            }

            private void CreateMinimapUI()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                try
                {
                    MiniMapMain.Log.LogError("CreateMinimapUI: Starting UI creation");
                    minimapUIObject = new GameObject("MinimapContainer");
                    DontDestroyOnLoad(minimapUIObject);

                    GameObject canvasObj = new GameObject("MinimapCanvas");
                    canvasObj.transform.SetParent(minimapUIObject.transform, false);
                    Canvas canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 9999;
                    canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasObj.AddComponent<GraphicRaycaster>();

                    GameObject frameObj = new GameObject("MinimapFrame");
                    frameObj.transform.SetParent(canvasObj.transform, false);
                    RectTransform frameRect = frameObj.AddComponent<RectTransform>();
                    frameRect.sizeDelta = new Vector2(150, 150);
                    frameRect.anchorMin = new Vector2(1, 1);
                    frameRect.anchorMax = new Vector2(1, 1);
                    frameRect.pivot = new Vector2(1, 1);
                    frameRect.anchoredPosition = new Vector2(-20, -20);
                    Image frameImage = frameObj.AddComponent<Image>();
                    frameImage.color = new Color(0.1f, 0.1f, 0.1f, 0f);

                    GameObject maskObj = new GameObject("MinimapMask");
                    maskObj.transform.SetParent(frameObj.transform, false);
                    RectTransform maskRect = maskObj.AddComponent<RectTransform>();
                    maskRect.sizeDelta = new Vector2(140, 140);
                    maskRect.anchorMin = new Vector2(0.5f, 0.5f);
                    maskRect.anchorMax = new Vector2(0.5f, 0.5f);
                    maskRect.pivot = new Vector2(0.5f, 0.5f);
                    maskRect.anchoredPosition = Vector2.zero;
                    Mask mask = maskObj.AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                    Image maskImage = maskObj.AddComponent<Image>();
                    maskImage.sprite = CreateCircleSprite(140, Color.white);
                    maskImage.type = Image.Type.Sliced;
                    maskImage.color = Color.white;

                    GameObject borderObj = new GameObject("MinimapBorder");
                    borderObj.transform.SetParent(frameObj.transform, false);
                    RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                    borderRect.sizeDelta = new Vector2(150, 150);
                    borderRect.anchorMin = new Vector2(0.5f, 0.5f);
                    borderRect.anchorMax = new Vector2(0.5f, 0.5f);
                    borderRect.pivot = new Vector2(0.5f, 0.5f);
                    borderRect.anchoredPosition = Vector2.zero;
                    borderObj.transform.SetSiblingIndex(0);
                    Image borderImage = borderObj.AddComponent<Image>();
                    borderImage.sprite = CreateCircleSprite(150, Color.black);
                    borderImage.type = Image.Type.Sliced;
                    borderImage.color = Color.black;

                    mapContentObject = new GameObject("MapContent");
                    mapContentObject.transform.SetParent(maskObj.transform, false);
                    RectTransform contentRect = mapContentObject.AddComponent<RectTransform>();
                    contentRect.sizeDelta = new Vector2(500, 500);
                    contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                    contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                    contentRect.pivot = new Vector2(0.5f, 0.5f);
                    contentRect.anchoredPosition = Vector2.zero;

                    GameObject gridObj = new GameObject("GridContainer");
                    gridObj.transform.SetParent(mapContentObject.transform, false);
                    gridContainer = gridObj.AddComponent<RectTransform>();
                    gridContainer.sizeDelta = new Vector2(500, 500);
                    gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                    gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                    gridContainer.pivot = new Vector2(0.5f, 0.5f);
                    gridContainer.anchoredPosition = Vector2.zero;

                    CreateMinimapTimeDisplay(frameRect);
                    MiniMapMain.Log.LogError("CreateMinimapUI: Completed UI creation");
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error creating minimap UI: {ex.Message}");
                }
            }

            private void CreateMinimapTimeDisplay(Transform parent)
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                GameObject timeContainer = new GameObject("MinimapTimeContainer");
                timeContainer.transform.SetParent(parent, false);
                RectTransform timeRect = timeContainer.AddComponent<RectTransform>();
                timeRect.sizeDelta = new Vector2(100, 30);
                timeRect.anchorMin = new Vector2(0.5f, 0);
                timeRect.anchorMax = new Vector2(0.5f, 0);
                timeRect.pivot = new Vector2(0.5f, 1);
                timeRect.anchoredPosition = new Vector2(0, -10);
                Image bgImage = timeContainer.AddComponent<Image>();
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                GameObject timeTextObj = new GameObject("MinimapTime");
                timeTextObj.transform.SetParent(timeContainer.transform, false);
                RectTransform textRect = timeTextObj.AddComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                minimapTimeText = timeTextObj.AddComponent<Text>();
                minimapTimeText.text = "Time";
                minimapTimeText.alignment = TextAnchor.MiddleCenter;
                minimapTimeText.color = Color.white;
                minimapTimeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            private Sprite CreateCircleSprite(int diameter, Color color)
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.ARGB32, false);
                Color transparent = new Color(0, 0, 0, 0);
                for (int y = 0; y < diameter; y++)
                {
                    for (int x = 0; x < diameter; x++)
                    {
                        tex.SetPixel(x, y, transparent);
                    }
                }
                int radius = diameter / 2;
                Vector2 center = new Vector2(radius, radius);
                for (int y = 0; y < diameter; y++)
                {
                    for (int x = 0; x < diameter; x++)
                    {
                        if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                            tex.SetPixel(x, y, color);
                    }
                }
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f));
            }

            private void CreateVisualGrid()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                try
                {
                    if (gridContainer == null)
                        return;

                    int gridCount = 10;
                    // Horizontal grid lines.
                    for (int i = 0; i <= gridCount; i++)
                    {
                        GameObject lineObj = new GameObject($"HLine_{i}");
                        lineObj.transform.SetParent(gridContainer, false);
                        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
                        lineRect.sizeDelta = new Vector2(gridCount * gridSize, 1);
                        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                        lineRect.pivot = new Vector2(0.5f, 0.5f);
                        float yPos = (i - gridCount / 2) * gridSize;
                        lineRect.anchoredPosition = new Vector2(0, yPos);
                        Image lineImage = lineObj.AddComponent<Image>();
                        lineImage.color = gridColor;
                    }

                    // Vertical grid lines.
                    for (int i = 0; i <= gridCount; i++)
                    {
                        GameObject lineObj = new GameObject($"VLine_{i}");
                        lineObj.transform.SetParent(gridContainer, false);
                        RectTransform lineRect = lineObj.AddComponent<RectTransform>();
                        lineRect.sizeDelta = new Vector2(1, gridCount * gridSize);
                        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                        lineRect.pivot = new Vector2(0.5f, 0.5f);
                        float xPos = (i - gridCount / 2) * gridSize;
                        lineRect.anchoredPosition = new Vector2(xPos, 0);
                        Image lineImage = lineObj.AddComponent<Image>();
                        lineImage.color = gridColor;
                    }
                    MiniMapMain.Log.LogError("Visual grid created successfully");
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error creating visual grid: {ex.Message}");
                }
            }

            private void UpdatePlayerDirectionIndicator()
            {
                MiniMapMain.Log.LogError($"<MethodName> entered");
                if (cachedPlayerMarker == null || playerObject == null)
                    return;

                if (cachedDirectionIndicator == null)
                {
                    Transform found = cachedPlayerMarker.transform.Find("DirectionIndicator");
                    if (found != null)
                        cachedDirectionIndicator = found as RectTransform;
                    else
                    {
                        GameObject directionObj = new GameObject("DirectionIndicator");
                        directionObj.transform.SetParent(cachedPlayerMarker.transform, false);
                        cachedDirectionIndicator = directionObj.AddComponent<RectTransform>();
                        cachedDirectionIndicator.sizeDelta = new Vector2(6f, 6f);
                        Image indicatorImage = directionObj.AddComponent<Image>();
                        indicatorImage.color = Color.white;
                    }
                }

                cachedDirectionIndicator.pivot = new Vector2(0.5f, 0.5f);
                float orbitDistance = 15f;
                float playerAngle = playerObject.transform.rotation.eulerAngles.y;
                float rad = (90f - playerAngle) * Mathf.Deg2Rad;
                Vector2 offset = new Vector2(
                    orbitDistance * Mathf.Cos(rad),
                    orbitDistance * Mathf.Sin(rad)
                );
                cachedDirectionIndicator.anchoredPosition = offset;
            }
        }
    }
}
