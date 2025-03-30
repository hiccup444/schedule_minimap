using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace MinimapMod
{
    public class MainMod : MelonMod
    {
        // Map positioning variables
        private static float mapScale = 1.2487098f;
        private static Vector2 mapOffset = new Vector2(10f, 0f);
        private static GameObject minimapObject;
        private static bool isInitializing = false;
        private static bool isEnabled = true;

        // References to track game objects
        private static GameObject mapAppObject;
        private static GameObject viewportObject;
        private static GameObject playerObject;
        private static GameObject mapContentObject;
        private static RectTransform gridContainer;

        // Update interval
        private static float updateInterval = 0.1f;
        private static float lastUpdateTime = 0f;

        // Grid properties
        private static int gridSize = 20; // Size of each grid cell in pixels
        private static Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private static Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Smoothing factor for map movement to reduce shake
        private static float smoothingFactor = 10f;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                // Only initialize in the main game scene, not menus
                if (sceneName == "Main" && !isInitializing)
                {
                    isInitializing = true;
                    MelonLogger.Msg("Detected Main scene, initializing Minimap...");

                    // Clean up existing instance
                    if (minimapObject != null)
                    {
                        UnityEngine.Object.Destroy(minimapObject);
                        minimapObject = null;
                    }

                    // Create our minimap UI
                    CreateMinimapUI();

                    // Start finding game objects
                    MelonCoroutines.Start(FindGameObjectsRoutine());

                    isInitializing = false;
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to initialize MinimapMod: {ex.Message}\nStack trace: {ex.StackTrace}");
                isInitializing = false;
            }
        }

        private System.Collections.IEnumerator FindGameObjectsRoutine()
        {
            MelonLogger.Msg("Looking for game objects...");

            // Wait a bit for all game objects to initialize
            yield return new WaitForSeconds(2f);

            int attempts = 0;
            while ((mapAppObject == null || playerObject == null) && attempts < 30)
            {
                attempts++;

                try
                {
                    // Find the player
                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player_Local");
                        if (playerObject != null)
                        {
                            MelonLogger.Msg("Found Player_Local");
                        }
                    }

                    // Find the Map App
                    if (mapAppObject == null)
                    {
                        // Why is IL2CPP like this?
                        GameObject gameplayMenu = GameObject.Find("GameplayMenu");
                        if (gameplayMenu != null)
                        {
                            MelonLogger.Msg("Found GameplayMenu");
                            Transform phoneTransform = gameplayMenu.transform.Find("Phone");
                            if (phoneTransform != null)
                            {
                                MelonLogger.Msg("Found Phone under GameplayMenu");
                                Transform phoneChildTransform = phoneTransform.Find("phone");
                                if (phoneChildTransform != null)
                                {
                                    MelonLogger.Msg("Found phone under Phone");
                                    Transform appsCanvas = phoneChildTransform.Find("AppsCanvas");
                                    if (appsCanvas != null)
                                    {
                                        MelonLogger.Msg("Found AppsCanvas");
                                        Transform mapApp = appsCanvas.Find("MapApp");
                                        if (mapApp != null)
                                        {
                                            mapAppObject = mapApp.gameObject;
                                            MelonLogger.Msg("Found MapApp");
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // If we found the MapApp, look for the viewport
                    if (mapAppObject != null && viewportObject == null)
                    {
                        Transform container = mapAppObject.transform.Find("Container");
                        if (container != null)
                        {
                            MelonLogger.Msg("Found Container in MapApp");
                            Transform scrollView = container.Find("Scroll View");
                            if (scrollView != null)
                            {
                                MelonLogger.Msg("Found Scroll View in Container");
                                Transform viewport = scrollView.Find("Viewport");
                                if (viewport != null)
                                {
                                    viewportObject = viewport.gameObject;
                                    MelonLogger.Msg("Found Map Viewport");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error finding game objects: {ex.Message}");
                }

                if (mapAppObject == null || playerObject == null)
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            if (mapAppObject == null)
            {
                MelonLogger.Warning("Could not find Map App after multiple attempts");
            }
            else if (viewportObject == null)
            {
                MelonLogger.Warning("Found MapApp but could not find Viewport");
            }

            if (playerObject == null)
            {
                MelonLogger.Warning("Could not find Player after multiple attempts");
            }

            MelonLogger.Msg("Game object search completed");

            // Now that we found the objects, let's try to access the map content
            if (viewportObject != null)
            {
                try
                {
                    // Check for content in the viewport
                    if (viewportObject.transform.childCount > 0)
                    {
                        Transform contentTransform = viewportObject.transform.GetChild(0);
                        MelonLogger.Msg($"Found viewport content: {contentTransform.name}");

                        // Get the Image component from this content if it exists
                        UnityEngine.UI.Image contentImage = contentTransform.GetComponent<UnityEngine.UI.Image>();
                        if (contentImage != null && contentImage.sprite != null)
                        {
                            MelonLogger.Msg($"Found content image with sprite: {contentImage.sprite.name}");

                            // Apply this sprite to our minimap
                            if (mapContentObject != null)
                            {
                                UnityEngine.UI.Image minimapImage = mapContentObject.GetComponent<UnityEngine.UI.Image>();
                                if (minimapImage == null)
                                {
                                    minimapImage = mapContentObject.AddComponent<UnityEngine.UI.Image>();
                                }

                                minimapImage.sprite = contentImage.sprite;
                                minimapImage.type = UnityEngine.UI.Image.Type.Simple;
                                minimapImage.preserveAspect = true;
                                // A quick disable/enable trick
                                minimapImage.enabled = false;
                                minimapImage.enabled = true;
                                MelonLogger.Msg("Successfully applied map sprite to minimap!");

                                // Hide the grid since we have the actual map
                                if (gridContainer != null)
                                {
                                    gridContainer.gameObject.SetActive(false);
                                }
                            }
                        }
                        else
                        {
                            MelonLogger.Msg("Content doesn't have an Image component or sprite");

                            // Look through children of content using a for loop instead of foreach
                            int childCount = contentTransform.childCount;
                            for (int i = 0; i < childCount; i++)
                            {
                                Transform child = contentTransform.GetChild(i);
                                UnityEngine.UI.Image childImage = child.GetComponent<UnityEngine.UI.Image>();
                                if (childImage != null && childImage.sprite != null)
                                {
                                    MelonLogger.Msg($"Found image in content child: {child.name}, Sprite: {childImage.sprite.name}");

                                    // Apply this sprite to our minimap
                                    if (mapContentObject != null)
                                    {
                                        UnityEngine.UI.Image minimapImage = mapContentObject.GetComponent<UnityEngine.UI.Image>();
                                        if (minimapImage == null)
                                        {
                                            minimapImage = mapContentObject.AddComponent<UnityEngine.UI.Image>();
                                        }

                                        minimapImage.sprite = childImage.sprite;
                                        minimapImage.type = UnityEngine.UI.Image.Type.Simple;
                                        minimapImage.preserveAspect = true;
                                        minimapImage.enabled = false;
                                        minimapImage.enabled = true;
                                        MelonLogger.Msg("Successfully applied map sprite to minimap!");

                                        // Hide the grid since we have the actual map
                                        if (gridContainer != null)
                                        {
                                            gridContainer.gameObject.SetActive(false);
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Error accessing map content: {ex.Message}");
                }
            }

            // Create the grid as a fallback if we couldn't get the map
            CreateVisualGrid();
        }

        private void CreateVisualGrid()
        {
            try
            {
                if (gridContainer == null)
                    return;

                // Create grid lines using UI images
                int gridCount = 10; // Number of grid cells in each direction

                // Create horizontal grid lines
                for (int i = 0; i <= gridCount; i++)
                {
                    GameObject lineObj = new GameObject($"HLine_{i}");
                    lineObj.transform.SetParent(gridContainer, false);

                    RectTransform lineRect = lineObj.AddComponent<RectTransform>();
                    lineRect.sizeDelta = new Vector2(gridCount * gridSize, 1); // Full grid width, 1px height
                    lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lineRect.pivot = new Vector2(0.5f, 0.5f);

                    float yPos = (i - gridCount / 2) * gridSize;
                    lineRect.anchoredPosition = new Vector2(0, yPos);

                    UnityEngine.UI.Image lineImage = lineObj.AddComponent<UnityEngine.UI.Image>();
                    lineImage.color = gridColor;
                }

                // Create vertical grid lines
                for (int i = 0; i <= gridCount; i++)
                {
                    GameObject lineObj = new GameObject($"VLine_{i}");
                    lineObj.transform.SetParent(gridContainer, false);

                    RectTransform lineRect = lineObj.AddComponent<RectTransform>();
                    lineRect.sizeDelta = new Vector2(1, gridCount * gridSize); // 1px width, full grid height
                    lineRect.anchorMin = new Vector2(0.5f, 0.5f);
                    lineRect.anchorMax = new Vector2(0.5f, 0.5f);
                    lineRect.pivot = new Vector2(0.5f, 0.5f);

                    float xPos = (i - gridCount / 2) * gridSize;
                    lineRect.anchoredPosition = new Vector2(xPos, 0);

                    UnityEngine.UI.Image lineImage = lineObj.AddComponent<UnityEngine.UI.Image>();
                    lineImage.color = gridColor;
                }

                MelonLogger.Msg("Visual grid created successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating visual grid: {ex.Message}");
            }
        }

        // Update the minimap every frame.
        public override void OnUpdate()
        {
            try
            {
                // Toggle minimap display with F3
                if (Input.GetKeyDown(KeyCode.F3) && minimapObject != null)
                {
                    isEnabled = !isEnabled;
                    minimapObject.SetActive(isEnabled);
                    MelonLogger.Msg("Minimap " + (isEnabled ? "Enabled" : "Disabled"));
                }

                // Update minimap position based on player's position, smoothing movement.
                if (isEnabled && playerObject != null && mapContentObject != null)
                {
                    Vector3 playerPos = playerObject.transform.position;

                    float mapX = -playerPos.x * mapScale;
                    float mapY = -playerPos.z * mapScale;

                    RectTransform contentRect = mapContentObject.GetComponent<RectTransform>();
                    if (contentRect != null)
                    {
                        Vector2 targetPos = new Vector2(mapX, mapY) + mapOffset;
                        contentRect.anchoredPosition = Vector2.Lerp(contentRect.anchoredPosition, targetPos, Time.deltaTime * smoothingFactor);
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in OnUpdate: {ex.Message}");
            }
        }

        // Overloaded helper method that creates a static marker with a specified color.
        private void AddStaticMarker(Vector3 worldPos, Color markerColor)
        {
            if (mapContentObject == null)
            {
                MelonLogger.Warning("Map content object is null; cannot add marker.");
                return;
            }

            GameObject marker = new GameObject("StaticMarker");
            marker.transform.SetParent(mapContentObject.transform, false);

            RectTransform markerRect = marker.AddComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(5f, 5f);

            // Calculate the marker's anchored position in the map content.
            markerRect.anchoredPosition = new Vector2(worldPos.x * mapScale, worldPos.z * mapScale);

            UnityEngine.UI.Image markerImage = marker.AddComponent<UnityEngine.UI.Image>();
            markerImage.color = markerColor;

            MelonLogger.Msg("Static marker added at mapped position: " + markerRect.anchoredPosition);
        }
        private void AddDefaultMarkers()
        {
            // White markers (using Color.white)
            AddStaticMarker(new Vector3(-67.17f, -3.03f, 138.31f), Color.white);
            AddStaticMarker(new Vector3(-79.88f, -2.26f, 85.13f), Color.white);
            AddStaticMarker(new Vector3(-179.99f, -3.03f, 113.69f), Color.white);

            // Red markers (using Color.red)
            AddStaticMarker(new Vector3(-68.44f, -1.49f, 35.37f), Color.red);
            AddStaticMarker(new Vector3(-34.55f, -1.54f, 27.06f), Color.red);
            AddStaticMarker(new Vector3(70.33f, 1.37f, -10.01f), Color.red);
        }

        // Create the minimap UI and add default markers.
        private void CreateMinimapUI()
        {
            try
            {
                // Create a minimap container
                minimapObject = new GameObject("MinimapContainer");
                UnityEngine.Object.DontDestroyOnLoad(minimapObject);

                // Create a canvas for UI elements
                GameObject canvasObj = new GameObject("MinimapCanvas");
                canvasObj.transform.SetParent(minimapObject.transform, false);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;

                // Add a UI scaler
                UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                // Add a raycaster for UI interaction
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Create a minimap frame
                GameObject frameObj = new GameObject("MinimapFrame");
                frameObj.transform.SetParent(canvasObj.transform, false);
                RectTransform frameRect = frameObj.AddComponent<RectTransform>();
                frameRect.sizeDelta = new Vector2(150, 150);
                frameRect.anchorMin = new Vector2(1, 0);
                frameRect.anchorMax = new Vector2(1, 0);
                frameRect.pivot = new Vector2(1, 0);
                frameRect.anchoredPosition = new Vector2(-20, 20);

                // Add frame background and border
                UnityEngine.UI.Image frameImage = frameObj.AddComponent<UnityEngine.UI.Image>();
                frameImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

                // Add a mask to clip the map content
                GameObject maskObj = new GameObject("MinimapMask");
                maskObj.transform.SetParent(frameObj.transform, false);
                RectTransform maskRect = maskObj.AddComponent<RectTransform>();
                maskRect.sizeDelta = new Vector2(140, 140);
                maskRect.anchorMin = new Vector2(0.5f, 0.5f);
                maskRect.anchorMax = new Vector2(0.5f, 0.5f);
                maskRect.pivot = new Vector2(0.5f, 0.5f);
                maskRect.anchoredPosition = Vector2.zero;

                // Add a mask component
                UnityEngine.UI.Mask mask = maskObj.AddComponent<UnityEngine.UI.Mask>();
                mask.showMaskGraphic = true;

                // Add a background for the mask
                UnityEngine.UI.Image maskImage = maskObj.AddComponent<UnityEngine.UI.Image>();
                maskImage.color = bgColor;

                // Add map content container
                mapContentObject = new GameObject("MapContent");
                mapContentObject.transform.SetParent(maskObj.transform, false);
                RectTransform contentRect = mapContentObject.AddComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(500, 500); // Make larger to contain the map
                contentRect.anchorMin = new Vector2(0.5f, 0.5f);
                contentRect.anchorMax = new Vector2(0.5f, 0.5f);
                contentRect.pivot = new Vector2(0.5f, 0.5f);
                contentRect.anchoredPosition = Vector2.zero;

                // Create a container for the grid
                GameObject gridObj = new GameObject("GridContainer");
                gridObj.transform.SetParent(mapContentObject.transform, false);
                gridContainer = gridObj.AddComponent<RectTransform>();
                gridContainer.sizeDelta = new Vector2(500, 500);
                gridContainer.anchorMin = new Vector2(0.5f, 0.5f);
                gridContainer.anchorMax = new Vector2(0.5f, 0.5f);
                gridContainer.pivot = new Vector2(0.5f, 0.5f);
                gridContainer.anchoredPosition = Vector2.zero;

                // Add player marker at center
                GameObject markerObj = new GameObject("PlayerMarker");
                markerObj.transform.SetParent(maskObj.transform, false);
                RectTransform markerRect = markerObj.AddComponent<RectTransform>();
                markerRect.sizeDelta = new Vector2(5f, 5f); // Adjust as needed
                markerRect.anchorMin = new Vector2(0.5f, 0.5f);
                markerRect.anchorMax = new Vector2(0.5f, 0.5f);
                markerRect.pivot = new Vector2(0.5f, 0.5f);
                markerRect.anchoredPosition = Vector2.zero;

                UnityEngine.UI.Image markerImage = markerObj.AddComponent<UnityEngine.UI.Image>();
                markerImage.color = new Color(0.2f, 0.6f, 1f, 1f);

                MelonLogger.Msg("Minimap UI created successfully");

                // Add the default static markers to the minimap
                AddDefaultMarkers();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating minimap UI: {ex.Message}");
            }
        }
    }
}
