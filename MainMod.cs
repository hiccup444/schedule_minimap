using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

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
        private RectTransform cachedDirectionIndicator;

        // References to track game objects
        private Text minimapTimeText;
        private static GameObject mapAppObject;
        private static GameObject viewportObject;
        private static GameObject playerObject;
        private static GameObject mapContentObject;
        private static RectTransform gridContainer;

        // Rotates a vector by a given angle (in degrees)
        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * 0.0174532924f; // degrees to radians
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void CreateMinimapTimeDisplay(Transform parent)
        {
            // Create a container for the time display.
            GameObject timeContainer = new GameObject("MinimapTimeContainer");
            timeContainer.transform.SetParent(parent, false);
            RectTransform timeRect = timeContainer.AddComponent<RectTransform>();
            // Set size of the container.
            timeRect.sizeDelta = new Vector2(100, 30);
            // Anchor it to the bottom center of the parent (e.g., the minimap frame).
            timeRect.anchorMin = new Vector2(0.5f, 0);
            timeRect.anchorMax = new Vector2(0.5f, 0);
            timeRect.pivot = new Vector2(0.5f, 1);
            // Position it slightly below the parent's bottom edge.
            timeRect.anchoredPosition = new Vector2(0, -10);

            // Add a semi-transparent grey background.
            Image bgImage = timeContainer.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // Create the text object.
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
            // Use the built-in Arial font.
            minimapTimeText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private void UpdateMinimapTime()
        {
            // Locate the in-game time text.
            GameObject timeObj = GameObject.Find("GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
            if (timeObj != null)
            {
                Text gameTimeText = timeObj.GetComponent<Text>();
                if (gameTimeText != null && minimapTimeText != null)
                {
                    minimapTimeText.text = gameTimeText.text;
                }
            }
        }

        // Grid properties
        private static int gridSize = 20; // grid cell size in pixels
        private static Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private static Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1f);

        // Smoothing factor for map movement
        private static float smoothingFactor = 10f;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                if (sceneName == "Main" && !isInitializing)
                {
                    isInitializing = true;
                    MelonLogger.Msg("Detected Main scene, initializing Minimap...");

                    // Clean up any existing minimap instance
                    if (minimapObject != null)
                    {
                        UnityEngine.Object.Destroy(minimapObject);
                        minimapObject = null;
                    }

                    // Create the minimap UI (which includes player marker setup)
                    CreateMinimapUI();

                    // Start locating required game objects
                    MelonCoroutines.Start(FindGameObjectsRoutine());
                    isInitializing = false;
                    MelonCoroutines.Start(UpdateMinimapTimeCoroutine());
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
            yield return new WaitForSeconds(2f);
            int attempts = 0;
            while ((mapAppObject == null || playerObject == null) && attempts < 30)
            {
                attempts++;
                try
                {
                    // Locate the player
                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player_Local");
                        if (playerObject != null)
                            MelonLogger.Msg("Found Player_Local");
                    }
                    // Locate the Map App
                    if (mapAppObject == null)
                    {
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
                    // Locate the viewport under Map App
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
                    yield return new WaitForSeconds(0.5f);
            }
            if (mapAppObject == null)
                MelonLogger.Warning("Could not find Map App after multiple attempts");
            else if (viewportObject == null)
                MelonLogger.Warning("Found MapApp but could not find Viewport");
            if (playerObject == null)
                MelonLogger.Warning("Could not find Player after multiple attempts");
            MelonLogger.Msg("Game object search completed");

            // Access the map content to apply the map sprite
            if (viewportObject != null)
            {
                try
                {
                    if (viewportObject.transform.childCount > 0)
                    {
                        Transform contentTransform = viewportObject.transform.GetChild(0);
                        MelonLogger.Msg($"Found viewport content: {contentTransform.name}");
                        Image contentImage = contentTransform.GetComponent<Image>();
                        if (contentImage != null && contentImage.sprite != null)
                        {
                            MelonLogger.Msg($"Found content image with sprite: {contentImage.sprite.name}");
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
                                MelonLogger.Msg("Successfully applied map sprite to minimap!");
                                if (gridContainer != null)
                                    gridContainer.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            MelonLogger.Msg("Content doesn't have an Image component or sprite");
                            int childCount = contentTransform.childCount;
                            for (int i = 0; i < childCount; i++)
                            {
                                Transform child = contentTransform.GetChild(i);
                                Image childImage = child.GetComponent<Image>();
                                if (childImage != null && childImage.sprite != null)
                                {
                                    MelonLogger.Msg($"Found image in content child: {child.name}, Sprite: {childImage.sprite.name}");
                                    if (mapContentObject != null)
                                    {
                                        Image minimapImage = mapContentObject.GetComponent<Image>();
                                        if (minimapImage == null)
                                            minimapImage = mapContentObject.AddComponent<Image>();
                                        minimapImage.sprite = childImage.sprite;
                                        minimapImage.type = Image.Type.Simple;
                                        minimapImage.preserveAspect = true;
                                        minimapImage.enabled = false;
                                        minimapImage.enabled = true;
                                        MelonLogger.Msg("Successfully applied map sprite to minimap!");
                                        if (gridContainer != null)
                                            gridContainer.gameObject.SetActive(false);
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
            CreateVisualGrid();

            // Optional: Replace fallback player marker if real asset is available now.
            GameObject currentMarker = GameObject.Find("PlayerMarker");
            if (currentMarker != null)
            {
                Image markerImage = currentMarker.GetComponent<Image>();
                if (markerImage != null && markerImage.color.Equals(new Color(0.2f, 0.6f, 1f, 1f)))
                {
                    GameObject contentObj = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
                    if (contentObj != null)
                    {
                        Transform playerPoI = contentObj.transform.Find("PlayerPoI(Clone)");
                        if (playerPoI != null)
                        {
                            Transform realIcon = playerPoI.Find("IconContainer");
                            if (realIcon != null)
                            {
                                GameObject newMarker = UnityEngine.Object.Instantiate(realIcon.gameObject);
                                newMarker.name = "PlayerMarker";
                                newMarker.transform.SetParent(currentMarker.transform.parent, false);
                                RectTransform newRect = newMarker.GetComponent<RectTransform>();
                                if (newRect != null)
                                {
                                    newRect.anchoredPosition = Vector2.zero;
                                    newRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                                }
                                // Remove the arrow
                                Transform arrowImage = newMarker.transform.Find("Image");
                                if (arrowImage != null)
                                {
                                    MelonLogger.Msg("Removing arrow from player marker");
                                    UnityEngine.Object.Destroy(arrowImage.gameObject);
                                }
                                UnityEngine.Object.Destroy(currentMarker);
                                MelonLogger.Msg("Replaced fallback player marker with real player icon.");
                            }
                        }
                    }
                }
            }

            // Now add static markers using the PropertyPoI
            GameObject contentObj2 = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
            if (contentObj2 != null)
            {
                Transform propertyPoI = contentObj2.transform.Find("PropertyPoI(Clone)");
                if (propertyPoI != null)
                {
                    Transform iconContainer = propertyPoI.Find("IconContainer");
                    if (iconContainer != null)
                    {
                        AddDefaultMarkers();
                    }
                }
            }
        }

        private void CreateMinimapUI()
        {
            try
            {
                // Create the minimap container and mark it as DontDestroyOnLoad.
                minimapObject = new GameObject("MinimapContainer");
                UnityEngine.Object.DontDestroyOnLoad(minimapObject);

                // Create a canvas for the minimap UI.
                GameObject canvasObj = new GameObject("MinimapCanvas");
                canvasObj.transform.SetParent(minimapObject.transform, false);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;

                // Add a CanvasScaler.
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                // Add a GraphicRaycaster.
                canvasObj.AddComponent<GraphicRaycaster>();

                // Create the minimap frame.
                GameObject frameObj = new GameObject("MinimapFrame");
                frameObj.transform.SetParent(canvasObj.transform, false);
                RectTransform frameRect = frameObj.AddComponent<RectTransform>();
                frameRect.sizeDelta = new Vector2(150, 150);
                // Set anchors and pivot to top-right.
                frameRect.anchorMin = new Vector2(1, 1);
                frameRect.anchorMax = new Vector2(1, 1);
                frameRect.pivot = new Vector2(1, 1);
                // Offset from top-right (negative X moves left, negative Y moves down)
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
                // Instead of using an external resource, create a circular sprite at runtime.
                Sprite circleSprite = CreateCircleSprite(140, Color.white);
                maskImage.sprite = circleSprite;
                maskImage.type = Image.Type.Sliced; // Sliced works well with masks.
                maskImage.color = Color.white;

                // Create a border for the minimap by adding an Image behind the mask.
                GameObject borderObj = new GameObject("MinimapBorder");
                borderObj.transform.SetParent(frameObj.transform, false);
                RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                // Set the border to be slightly larger than the mask.
                borderRect.sizeDelta = new Vector2(150, 150);
                borderRect.anchorMin = new Vector2(0.5f, 0.5f);
                borderRect.anchorMax = new Vector2(0.5f, 0.5f);
                borderRect.pivot = new Vector2(0.5f, 0.5f);
                borderRect.anchoredPosition = Vector2.zero;
                borderObj.transform.SetSiblingIndex(0);
                Image borderImage = borderObj.AddComponent<Image>();
                // Use a circular sprite for the border (you can use the same CreateCircleSprite function)
                Sprite BorderCircleSprite = CreateCircleSprite(150, Color.black);
                borderImage.sprite = BorderCircleSprite;
                borderImage.type = Image.Type.Sliced;
                borderImage.color = Color.black;

                // Create the map content container.
                mapContentObject = new GameObject("MapContent");
                mapContentObject.transform.SetParent(maskObj.transform, false);
                RectTransform contentRect = mapContentObject.AddComponent<RectTransform>();
                contentRect.sizeDelta = new Vector2(500, 500); // Adjust as needed.
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

                // Create the time display as a child of the minimap frame.
                CreateMinimapTimeDisplay(frameRect);

                // --- PLAYER ICON SETUP ---
                // Look for the Content object, then "PlayerPoI(Clone)", then "IconContainer".
                GameObject contentObj = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
                if (contentObj != null)
                {
                    Transform playerPoI = contentObj.transform.Find("PlayerPoI(Clone)");
                    if (playerPoI != null)
                    {
                        Transform iconContainer = playerPoI.Find("IconContainer");
                        if (iconContainer != null)
                        {
                            // Clone the IconContainer as the player marker.
                            GameObject playerIcon = UnityEngine.Object.Instantiate(iconContainer.gameObject);
                            playerIcon.name = "PlayerMarker";
                            playerIcon.transform.SetParent(maskObj.transform, false);
                            RectTransform iconRect = playerIcon.GetComponent<RectTransform>();
                            if (iconRect != null)
                            {
                                iconRect.anchoredPosition = Vector2.zero;
                                iconRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            }
                            MelonLogger.Msg("Player icon cloned successfully.");
                        }
                        else
                        {
                            MelonLogger.Msg("IconContainer not found under PlayerPoI(Clone). Using fallback marker.");
                            CreateFallbackPlayerMarker(maskObj);
                        }
                    }
                    else
                    {
                        MelonLogger.Msg("PlayerPoI(Clone) not found under Content. Using fallback marker.");
                        CreateFallbackPlayerMarker(maskObj);
                    }
                }
                else
                {
                    MelonLogger.Msg("Content object not found. Using fallback marker.");
                    CreateFallbackPlayerMarker(maskObj);
                }
                // --- END PLAYER ICON SETUP ---

                MelonLogger.Msg("Minimap UI created successfully.");
                // (Optional) Call AddDefaultMarkers() if you have static markers to add.
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating minimap UI: {ex.Message}");
            }
        }

        private void CreateVisualGrid()
        {
            try
            {
                if (gridContainer == null)
                    return;
                int gridCount = 10;
                // Horizontal lines
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
                // Vertical lines
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
                MelonLogger.Msg("Visual grid created successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error creating visual grid: {ex.Message}");
            }
        }

        public override void OnUpdate()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F3) && minimapObject != null)
                {
                    isEnabled = !isEnabled;
                    minimapObject.SetActive(isEnabled);
                    MelonLogger.Msg("Minimap " + (isEnabled ? "Enabled" : "Disabled"));
                }
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
                        UpdatePlayerDirectionIndicator();
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error in OnUpdate: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator UpdateMinimapTimeCoroutine()
        {
            while (true)
            {
                UpdateMinimapTime();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdatePlayerDirectionIndicator()
        {
            GameObject playerMarker = GameObject.Find("PlayerMarker");
            if (playerMarker == null || playerObject == null)
                return;

            // If we haven't cached the direction indicator yet, try to find it.
            if (cachedDirectionIndicator == null)
            {
                Transform found = playerMarker.transform.Find("DirectionIndicator");
                if (found != null)
                {
                    cachedDirectionIndicator = found as RectTransform;
                }
                else
                {
                    // Create it once if it doesn't exist.
                    GameObject directionObj = new GameObject("DirectionIndicator");
                    directionObj.transform.SetParent(playerMarker.transform, false);
                    cachedDirectionIndicator = directionObj.AddComponent<RectTransform>();
                    cachedDirectionIndicator.sizeDelta = new Vector2(6f, 6f);
                    Image indicatorImage = directionObj.AddComponent<Image>();
                    indicatorImage.color = Color.white;
                }
            }

            // Now update the cached direction indicator.
            cachedDirectionIndicator.pivot = new Vector2(0.5f, 0.5f);
            float orbitDistance = 15f;
            float playerAngle = playerObject.transform.rotation.eulerAngles.y;
            float rad = (90f - playerAngle) * 0.0174532924f;
            Vector2 offset = new Vector2(
                orbitDistance * Mathf.Cos(rad),
                orbitDistance * Mathf.Sin(rad)
            );
            cachedDirectionIndicator.anchoredPosition = offset;
        }

        private Sprite CreateCircleSprite(int diameter, Color color)
        {
            Texture2D tex = new Texture2D(diameter, diameter, TextureFormat.ARGB32, false);
            // Make texture fully transparent first.
            Color transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
            // Draw the circle.
            int radius = diameter / 2;
            Vector2 center = new Vector2(radius, radius);
            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) <= radius)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f));
        }

        private void AddWhiteStaticMarker(Vector3 worldPos)
        {
            if (mapContentObject == null)
            {
                MelonLogger.Warning("Map content object is null; cannot add marker.");
                return;
            }

            // Locate the real content that holds PropertyPoI.
            GameObject realContent = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
            if (realContent != null)
            {
                // Find the PropertyPoI(Clone) and then its child IconContainer.
                Transform propertyPoI = realContent.transform.Find("PropertyPoI(Clone)");
                if (propertyPoI != null)
                {
                    Transform iconContainer = propertyPoI.Find("IconContainer");
                    if (iconContainer != null)
                    {
                        // Clone the IconContainer to use as the marker.
                        GameObject marker = UnityEngine.Object.Instantiate(iconContainer.gameObject);
                        marker.name = "StaticMarker_White";
                        marker.transform.SetParent(mapContentObject.transform, false);
                        RectTransform markerRect = marker.GetComponent<RectTransform>();
                        if (markerRect != null)
                        {
                            // Adjust scale if necessary.
                            markerRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                            // Compute marker position in the minimap.
                            float posX = worldPos.x * mapScale;
                            float posY = worldPos.z * mapScale;
                            markerRect.anchoredPosition = new Vector2(posX, posY);
                            MelonLogger.Msg("White static marker added at mapped position: " + markerRect.anchoredPosition);
                            return;
                        }
                    }
                }
            }
            MelonLogger.Msg("White static marker asset not found. No marker added.");
        }

        private void AddRedStaticMarker(Vector3 worldPos)
        {
            if (mapContentObject == null)
            {
                MelonLogger.Warning("Map content object is null; cannot add marker.");
                return;
            }
            GameObject marker = new GameObject("StaticMarker_Red");
            marker.transform.SetParent(mapContentObject.transform, false);
            RectTransform markerRect = marker.AddComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(5f, 5f);
            // Compute position using world coordinates (multiplied by mapScale)
            float posX = worldPos.x * mapScale;
            float posY = worldPos.z * mapScale;
            markerRect.anchoredPosition = new Vector2(posX, posY);
            Image markerImage = marker.AddComponent<Image>();
            markerImage.color = Color.red;
            MelonLogger.Msg("Red static marker added at mapped position: " + markerRect.anchoredPosition);
        }

        private void AddDefaultMarkers()
        {
            // White markers using the in-game asset.
            AddWhiteStaticMarker(new Vector3(-67.17f, -3.03f, 138.31f));
            AddWhiteStaticMarker(new Vector3(-79.88f, -2.26f, 85.13f));
            AddWhiteStaticMarker(new Vector3(-179.99f, -3.03f, 113.69f));

            // Red markers using a simple red square.
            AddRedStaticMarker(new Vector3(-68.44f, -1.49f, 35.37f));
            AddRedStaticMarker(new Vector3(-34.55f, -1.54f, 27.06f));
            AddRedStaticMarker(new Vector3(70.33f, 1.37f, -10.01f));
        }

        private void CreateFallbackPlayerMarker(GameObject parent)
        {
            GameObject markerObj = new GameObject("PlayerMarker");
            markerObj.transform.SetParent(parent.transform, false);
            RectTransform markerRect = markerObj.AddComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(5f, 5f);
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            Image markerImage = markerObj.AddComponent<Image>();
            markerImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            MelonLogger.Msg("Fallback player marker created.");
        }
    }
}
