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
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Create the GameObject and let its Awake/Start handle initialization
            GameObject miniMapObject = new GameObject("MinimapManager");
            UnityEngine.Object.DontDestroyOnLoad(miniMapObject);

            miniMapObject.AddComponent<MinimapManager>();

            Log.LogInfo("MinimapManager GameObject created. Component will initialize in Awake/Start.");
        }
    }

    // Manager that handles scene detection and minimap creation
    public class MinimapManager : MonoBehaviour
    {
        private string currentSceneName = "";
        private GameObject minimapObject;
        private MinimapComponent minimapComponent;

        private void Awake()
        {
            MiniMapMain.Log.LogInfo("MinimapManager Awake");

            // persist between scene loads
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            MiniMapMain.Log.LogInfo("MinimapManager Start - Beginning scene monitoring");

            currentSceneName = SceneManager.GetActiveScene().name;
            MiniMapMain.Log.LogInfo($"Current scene: {currentSceneName}");

            // Initialize minimap
            if (currentSceneName == "Main")
            {
                CreateMinimapObject();
            }
        }

        private void Update()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != currentSceneName)
            {
                MiniMapMain.Log.LogInfo($"Scene changed from {currentSceneName} to {sceneName}");
                currentSceneName = sceneName;

                // If entering Main scene, create minimap
                if (sceneName == "Main")
                {
                    CreateMinimapObject();
                }
            }
        }

        private void CreateMinimapObject()
        {
            try
            {
                // Clean up any existing instance
                if (minimapObject != null)
                {
                    Destroy(minimapObject);
                    minimapObject = null;
                    minimapComponent = null;
                }

                // Create new minimap object
                minimapObject = new GameObject("MinimapObject");
                DontDestroyOnLoad(minimapObject);

                // Add minimap component - will initialize in its own Awake
                minimapComponent = minimapObject.AddComponent<MinimapComponent>();

                MiniMapMain.Log.LogInfo("Minimap object created successfully");
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error creating minimap object: {ex.Message}");
            }
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
            MiniMapMain.Log.LogInfo("MinimapComponent Awake");
        }

        private void Start()
        {
            MiniMapMain.Log.LogInfo("MinimapComponent Start - Initializing minimap");

            isInitializing = true;

            // Create UI
            CreateMinimapUI();

            // Start "manual coroutines"
            findGameObjectsRunning = true;
            findObjectsTimer = 0f;
            findObjectsAttempts = 0;

            updateTimeRunning = true;
            updateTimeTimer = 0f;

            isInitializing = false;
        }

        private void Update()
        {
            try
            {
                if (findGameObjectsRunning)
                {
                    UpdateFindGameObjects();
                }

                if (updateTimeRunning)
                {
                    UpdateMinimapTimeManual();
                }

                // Toggle minimap with F3
                if (Input.GetKeyDown(KeyCode.F3) && minimapUIObject != null)
                {
                    isEnabled = !isEnabled;
                    minimapUIObject.SetActive(isEnabled);
                    MiniMapMain.Log.LogInfo("Minimap " + (isEnabled ? "Enabled" : "Disabled"));
                }

                // Update minimap position based on player position
                if (isEnabled && playerObject != null && mapContentObject != null)
                {
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
            findObjectsTimer += Time.deltaTime;

            // Wait 2 seconds on first update
            if (findObjectsAttempts == 0 && findObjectsTimer < 2f)
                return;

            // Check every 0.5 seconds
            if (findObjectsTimer >= 0.5f)
            {
                findObjectsTimer = 0f;
                findObjectsAttempts++;

                try
                {
                    // Locate the player
                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player_Local");
                        if (playerObject != null)
                            MiniMapMain.Log.LogInfo("Found Player_Local");
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
                                            MiniMapMain.Log.LogInfo("Found MapApp");
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
                                    MiniMapMain.Log.LogInfo("Found Map Viewport");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MiniMapMain.Log.LogError($"Error finding game objects: {ex.Message}");
                }

                // Stop conditions
                if ((mapAppObject != null && playerObject != null) || findObjectsAttempts >= 30)
                {
                    if (mapAppObject == null)
                        MiniMapMain.Log.LogWarning("Could not find Map App after multiple attempts");
                    else if (viewportObject == null)
                        MiniMapMain.Log.LogWarning("Found MapApp but could not find Viewport");
                    if (playerObject == null)
                        MiniMapMain.Log.LogWarning("Could not find Player after multiple attempts");

                    MiniMapMain.Log.LogInfo("Game object search completed");

                    // Process map content
                    if (viewportObject != null)
                    {
                        try
                        {
                            if (viewportObject.transform.childCount > 0)
                            {
                                Transform contentTransform = viewportObject.transform.GetChild(0);
                                MiniMapMain.Log.LogInfo($"Found viewport content: {contentTransform.name}");
                                Image contentImage = contentTransform.GetComponent<Image>();
                                if (contentImage != null && contentImage.sprite != null)
                                {
                                    MiniMapMain.Log.LogInfo($"Found content image with sprite: {contentImage.sprite.name}");
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
                                        MiniMapMain.Log.LogInfo("Successfully applied map sprite to minimap!");
                                        if (gridContainer != null)
                                            gridContainer.gameObject.SetActive(false);
                                    }
                                }
                                else
                                {
                                    MiniMapMain.Log.LogInfo("Content doesn't have an Image component or sprite");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MiniMapMain.Log.LogError($"Error accessing map content: {ex.Message}");
                        }
                    }

                    CreateVisualGrid();
                    findGameObjectsRunning = false;
                }
            }
        }

        private void UpdateMinimapTimeManual()
        {
            updateTimeTimer += Time.deltaTime;

            if (updateTimeTimer >= 1f)
            {
                updateTimeTimer = 0f;
                UpdateMinimapTime();
            }
        }

        private void UpdateMinimapTime()
        {
            if (cachedGameTimeText == null)
            {
                GameObject timeObj = GameObject.Find("GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
                if (timeObj != null)
                    cachedGameTimeText = timeObj.GetComponent<Text>();
            }
            if (cachedGameTimeText != null && minimapTimeText != null)
                minimapTimeText.text = cachedGameTimeText.text;
        }

        private void CreateMinimapUI()
        {
            try
            {
                // Create the minimap container.
                minimapUIObject = new GameObject("MinimapContainer");
                DontDestroyOnLoad(minimapUIObject);

                // Create a canvas for the minimap UI.
                GameObject canvasObj = new GameObject("MinimapCanvas");
                canvasObj.transform.SetParent(minimapUIObject.transform, false);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();

                // Create the minimap frame.
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

                // Create a mask to clip the map content.
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

                // Create a border for the minimap.
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

                // Create the map content container.
                mapContentObject = new GameObject("MapContent");
                mapContentObject.transform.SetParent(maskObj.transform, false);
                RectTransform contentRect = mapContentObject.AddComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(500, 500);
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.anchoredPosition = Vector2.zero;

                // Create a container for the grid overlay.
                GameObject gridObj = new GameObject("GridContainer");
                gridObj.transform.SetParent(mapContentObject.transform, false);
                gridContainer = gridObj.AddComponent<RectTransform>();
                gridContainer.sizeDelta = new Vector2(500, 500);
                gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                gridContainer.pivot = new Vector2(0.5f, 0.5f);
                gridContainer.anchoredPosition = Vector2.zero;

                // Create the minimap time display.
                CreateMinimapTimeDisplay(frameRect);
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error creating minimap UI: {ex.Message}");
            }
        }

        private void CreateMinimapTimeDisplay(Transform parent)
        {
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
            try
            {
                if (gridContainer == null)
                    return;

                int gridCount = 10;
                // Horizontal grid lines
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

                // Vertical grid lines
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
                MiniMapMain.Log.LogInfo("Visual grid created successfully");
            }
            catch (Exception ex)
            {
                MiniMapMain.Log.LogError($"Error creating visual grid: {ex.Message}");
            }
        }

        private void UpdatePlayerDirectionIndicator()
        {
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
