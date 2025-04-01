using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace MinimapMod
{
    public class MainMod : MelonMod
    {

        private bool guiVisible = false;
        private bool minimapEnabled = true;
        private bool timeBarEnabled = true;

        private Rect minimapToggleRect = new Rect(Screen.width - 170, 260, 150, 25);
        private Rect timeToggleRect = new Rect(Screen.width - 170, 250, 150, 25);
        private Rect guiBackgroundRect = new Rect(Screen.width - 175, 290, 160, 100);
        private GameObject minimapDisplayObject; // Holds the map display (mask, border, map content, grid)
        private RectTransform minimapTimeContainer; // Reference to the time container rect transform
        private bool doubleSizeEnabled = false;         // Whether 2x size is enabled
        private RectTransform minimapFrameRect;           // Reference to the minimap frame

        // Map positioning variables
        private static float mapScale = 1.2487098f;
        private Vector2 manualOffset = new Vector2(-61f, -71f);
        private Vector2 baseManualOffset = new Vector2(-61f, -71f);
        private static GameObject minimapObject;
        private static bool isInitializing = false;
        private static bool isEnabled = true;
        private RectTransform cachedDirectionIndicator;
        private GameObject cachedPlayerMarker;
        private Text cachedGameTimeText;  // Cache for the game time text
        private GameObject cachedMapContent;  // Cache for the map content object
        private Transform cachedPropertyPoI;  // Cache for the PropertyPoI transform

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

        public override void OnGUI()
        {
            if (!guiVisible)
                return;

            Rect guiBgRect;
            Rect minimapToggleRectAdjusted;
            Rect timeToggleRectAdjusted;
            Rect doubleSizeToggleRectAdjusted;

            if (!doubleSizeEnabled)
            {
                // 1x mode: Use your original fixed layout (which is perfect)
                guiBgRect = new Rect(Screen.width - 175, 220, 160, 100);
                minimapToggleRectAdjusted = new Rect(Screen.width - 170, 230, 150, 25);
                timeToggleRectAdjusted = new Rect(Screen.width - 170, 260, 150, 25);
                doubleSizeToggleRectAdjusted = new Rect(Screen.width - 170, 290, 150, 25);
            }
            else
            {
                // 2x mode: Use statically defined values.
                // Adjust these values based on your desired layout.
                guiBgRect = new Rect(Screen.width - 250, 380, 160, 100);
                minimapToggleRectAdjusted = new Rect(Screen.width - 255, 390, 300, 25);
                timeToggleRectAdjusted = new Rect(Screen.width - 255, 420, 300, 25);
                doubleSizeToggleRectAdjusted = new Rect(Screen.width - 255, 450, 300, 25);
            }

            GUI.color = Color.gray;
            GUI.Box(guiBgRect, "");

            DrawToggle(minimapToggleRectAdjusted, "Minimap", ref minimapEnabled, () =>
            {
                if (minimapDisplayObject != null)
                    minimapDisplayObject.SetActive(minimapEnabled);

                if (minimapTimeContainer != null)
                {
                    if (minimapEnabled)
                    {
                        minimapTimeContainer.anchorMin = new Vector2(0.5f, 0);
                        minimapTimeContainer.anchorMax = new Vector2(0.5f, 0);
                        minimapTimeContainer.pivot = new Vector2(0.5f, 1);
                        // (Time container’s anchoredPosition is set in CreateMinimapTimeDisplay)
                    }
                    else
                    {
                        minimapTimeContainer.anchorMin = new Vector2(1, 1);
                        minimapTimeContainer.anchorMax = new Vector2(1, 1);
                        minimapTimeContainer.pivot = new Vector2(1, 1);
                        minimapTimeContainer.anchoredPosition = Vector2.zero;
                    }
                }
            });

            DrawToggle(timeToggleRectAdjusted, "Time", ref timeBarEnabled, () =>
            {
                if (minimapTimeContainer != null)
                    minimapTimeContainer.gameObject.SetActive(timeBarEnabled);
            });

            DrawToggle(doubleSizeToggleRectAdjusted, "2x Size", ref doubleSizeEnabled, () =>
            {
                UpdateMinimapSize();
            });
        }


        private void DrawToggle(Rect position, string label, ref bool state, Action onToggle)
        {
            // Draw label
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 50, position.y, position.width - 50, position.height), label);

            // Switch background
            Rect switchBg = new Rect(position.x + 5, position.y + 3, 40, 18);
            GUI.color = state ? Color.green : Color.gray;
            GUI.Box(switchBg, "");

            // Handle
            Rect handle = new Rect(state ? switchBg.x + 22 : switchBg.x + 2, switchBg.y + 2, 14, 14);
            GUI.color = Color.white;
            GUI.Box(handle, "");

            // Invisible button overlay
            if (GUI.Button(position, new GUIContent(""), GUIStyle.none))
            {
                state = !state;
                onToggle.Invoke();
            }
        }

        private void CreateMinimapTimeDisplay(Transform parent)
        {
            GameObject timeContainer = new GameObject("MinimapTimeContainer");
            timeContainer.transform.SetParent(parent, false);
            RectTransform timeRect = timeContainer.AddComponent<RectTransform>();
            timeRect.sizeDelta = new Vector2(100, 50);
            timeRect.anchorMin = new Vector2(0.5f, 0);
            timeRect.anchorMax = new Vector2(0.5f, 0);
            timeRect.pivot = new Vector2(0.5f, 1);

            if (doubleSizeEnabled)
            {
                timeRect.anchoredPosition = new Vector2(0, -60);
            }
            else
            {
                timeRect.anchoredPosition = new Vector2(0, 10);
            }

            minimapTimeContainer = timeRect; // cache it

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
            minimapTimeText.horizontalOverflow = HorizontalWrapMode.Overflow;
            minimapTimeText.verticalOverflow = VerticalWrapMode.Overflow;
        }



        private void UpdateMinimapTime()
        {
            // If we haven't cached the time text yet, try to find it.
            if (cachedGameTimeText == null)
            {
                GameObject timeObj = GameObject.Find("GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
                if (timeObj != null)
                {
                    cachedGameTimeText = timeObj.GetComponent<Text>();
                }
            }

            if (cachedGameTimeText != null && minimapTimeText != null)
            {
                string originalText = cachedGameTimeText.text; // Expected format: "10:20 AM Monday"
                string[] tokens = originalText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 3)
                {
                    // Assume the time is the first two tokens and the day is the last token.
                    string timePart = tokens[0] + " " + tokens[1];
                    string dayPart = tokens[tokens.Length - 1];
                    minimapTimeText.text = dayPart + "\n" + timePart;
                }
                else
                {
                    // Fallback: if the format is unexpected, just use the original text.
                    minimapTimeText.text = originalText;
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
                    MelonCoroutines.Start(ContractPoIChecker());
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
            if (cachedPlayerMarker != null)
            {
                Image markerImage = cachedPlayerMarker.GetComponent<Image>();
                if (markerImage != null && markerImage.color.Equals(new Color(0.2f, 0.6f, 1f, 1f)))
                {
                    // If we haven't cached the map content yet, try to find it
                    if (cachedMapContent == null)
                    {
                        cachedMapContent = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
                    }

                    if (cachedMapContent != null)
                    {
                        Transform playerPoI = cachedMapContent.transform.Find("PlayerPoI(Clone)");
                        if (playerPoI != null)
                        {
                            Transform realIcon = playerPoI.Find("IconContainer");
                            if (realIcon != null)
                            {
                                GameObject newMarker = UnityEngine.Object.Instantiate(realIcon.gameObject);
                                newMarker.name = "PlayerMarker";
                                newMarker.transform.SetParent(cachedPlayerMarker.transform.parent, false);
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
                                UnityEngine.Object.Destroy(cachedPlayerMarker);
                                cachedPlayerMarker = newMarker;
                                MelonLogger.Msg("Replaced fallback player marker with real player icon.");
                            }
                        }
                    }
                }
            }

            // Now add static markers using the PropertyPoI
            if (cachedMapContent == null)
            {
                cachedMapContent = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
            }

            if (cachedMapContent != null)
            {
                if (cachedPropertyPoI == null)
                {
                    cachedPropertyPoI = cachedMapContent.transform.Find("PropertyPoI(Clone)");
                }

                if (cachedPropertyPoI != null)
                {
                    Transform iconContainer = cachedPropertyPoI.Find("IconContainer");
                    if (iconContainer != null)
                    {
                        AddDefaultMarkers();
                    }
                }
            }
        }

        private void UpdateMinimapFramePosition()
        {
            if (doubleSizeEnabled)
            {
                // Lower the 2x minimap by 15 pixels relative to the 1x position.
                minimapFrameRect.anchoredPosition = new Vector2(-20, -60);
            }
            else
            {
                // Restore the original 1x position.
                minimapFrameRect.anchoredPosition = new Vector2(-20, -20);
            }
        }
        private void UpdateMinimapSize()
        {
            float factor = doubleSizeEnabled ? 2f : 1f;

            if (minimapFrameRect != null)
            {
                minimapFrameRect.sizeDelta = new Vector2(150, 150) * factor;
            }

            if (minimapDisplayObject != null)
            {
                RectTransform displayRect = minimapDisplayObject.GetComponent<RectTransform>();
                if (displayRect != null)
                {
                    displayRect.offsetMin = new Vector2(0, 50 * factor);
                }
            }

            Transform maskTransform = minimapDisplayObject.transform.Find("MinimapMask");
            if (maskTransform != null)
            {
                RectTransform maskRect = maskTransform.GetComponent<RectTransform>();
                if (maskRect != null)
                {
                    maskRect.sizeDelta = new Vector2(140, 140) * factor;
                }
            }

            Transform borderTransform = minimapDisplayObject.transform.Find("MinimapBorder");
            if (borderTransform != null)
            {
                RectTransform borderRect = borderTransform.GetComponent<RectTransform>();
                if (borderRect != null)
                {
                    borderRect.sizeDelta = new Vector2(150, 150) * factor;
                }
            }

            // Keep mapScale constant.
            mapScale = 1.2487098f;

            ResetMapContentPosition();
            UpdateMinimapFramePosition();

            // Now update the time display position for the current mode.
            UpdateTimeDisplayPosition();
        }

        private void ResetMapContentPosition()
        {
            if (playerObject == null || mapContentObject == null || minimapDisplayObject == null)
                return;

            // Convert player world position.
            Vector3 playerPos = playerObject.transform.position;
            float mapX = -playerPos.x * mapScale;
            float mapY = -playerPos.z * mapScale;

            // Calculate dynamic center of the minimap mask.
            Transform maskTransform = minimapDisplayObject.transform.Find("MinimapMask");
            Vector2 dynamicCenter = Vector2.zero;
            if (maskTransform != null)
            {
                RectTransform maskRect = maskTransform.GetComponent<RectTransform>();
                if (maskRect != null)
                {
                    dynamicCenter = new Vector2(maskRect.rect.width * 0.5f, maskRect.rect.height * 0.5f);
                }
            }

            // Use preset offsets:
            // For 1x: presetOffset = (-61, -71)
            // For 2x: presetOffset = (-131, -141)
            Vector2 presetOffset = doubleSizeEnabled ? new Vector2(-131f, -141f) : new Vector2(-61f, -71f);

            // Compute final target position.
            Vector2 targetPos = new Vector2(mapX, mapY) + dynamicCenter + presetOffset;

            RectTransform contentRect = mapContentObject.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.anchoredPosition = targetPos;
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
                minimapFrameRect = frameObj.AddComponent<RectTransform>();
                minimapFrameRect.sizeDelta = new Vector2(150, 150);
                minimapFrameRect.anchorMin = new Vector2(1, 1);
                minimapFrameRect.anchorMax = new Vector2(1, 1);
                minimapFrameRect.pivot = new Vector2(1, 1);
                minimapFrameRect.anchoredPosition = new Vector2(-20, -20);

                // *****************
                // Create a new container for the minimap display (the actual map)
                minimapDisplayObject = new GameObject("MinimapDisplay");
                minimapDisplayObject.transform.SetParent(frameObj.transform, false);
                RectTransform displayRect = minimapDisplayObject.AddComponent<RectTransform>();
                // Let the display occupy the upper part of the frame leaving room at the bottom for time.
                displayRect.anchorMin = new Vector2(0, 0);
                displayRect.anchorMax = new Vector2(1, 1);
                // Reserve 50 pixels at the bottom for the time display.
                displayRect.offsetMin = new Vector2(0, 50);
                displayRect.offsetMax = Vector2.zero;
                // *****************

                // Create a mask to clip the map content.
                GameObject maskObj = new GameObject("MinimapMask");
                maskObj.transform.SetParent(minimapDisplayObject.transform, false);
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
                maskImage.type = Image.Type.Sliced;
                maskImage.color = Color.white;

                // Create a border for the minimap by adding an Image behind the mask.
                GameObject borderObj = new GameObject("MinimapBorder");
                borderObj.transform.SetParent(minimapDisplayObject.transform, false);
                RectTransform borderRect = borderObj.AddComponent<RectTransform>();
                borderRect.sizeDelta = new Vector2(150, 150);
                borderRect.anchorMin = new Vector2(0.5f, 0.5f);
                borderRect.anchorMax = new Vector2(0.5f, 0.5f);
                borderRect.pivot = new Vector2(0.5f, 0.5f);
                borderRect.anchoredPosition = Vector2.zero;
                borderObj.transform.SetSiblingIndex(0);
                Image borderImage = borderObj.AddComponent<Image>();
                Sprite BorderCircleSprite = CreateCircleSprite(150, Color.black);
                borderImage.sprite = BorderCircleSprite;
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

                // Create the time display as a child of the frame (outside the minimap display)
                CreateMinimapTimeDisplay(minimapFrameRect);

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
                            cachedPlayerMarker = UnityEngine.Object.Instantiate(iconContainer.gameObject);
                            cachedPlayerMarker.name = "PlayerMarker";
                            cachedPlayerMarker.transform.SetParent(maskObj.transform, false);
                            RectTransform iconRect = cachedPlayerMarker.GetComponent<RectTransform>();
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
                // Toggle GUI visibility with F3.
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    guiVisible = !guiVisible;
                }

                // Update the map content position.
                if (isEnabled && playerObject != null && mapContentObject != null && minimapDisplayObject != null)
                {
                    Vector3 playerPos = playerObject.transform.position;
                    float mapX = -playerPos.x * mapScale;
                    float mapY = -playerPos.z * mapScale;

                    // Calculate dynamic center of the minimap mask.
                    Transform maskTransform = minimapDisplayObject.transform.Find("MinimapMask");
                    Vector2 dynamicCenter = Vector2.zero;
                    if (maskTransform != null)
                    {
                        RectTransform maskRect = maskTransform.GetComponent<RectTransform>();
                        if (maskRect != null)
                        {
                            dynamicCenter = new Vector2(maskRect.rect.width * 0.5f, maskRect.rect.height * 0.5f);
                        }
                    }

                    // Preset offset: (-61, -71) for 1x, (-131, -141) for 2x.
                    Vector2 presetOffset = doubleSizeEnabled ? new Vector2(-131f, -141f) : new Vector2(-61f, -71f);

                    Vector2 targetPos = new Vector2(mapX, mapY) + dynamicCenter + presetOffset;

                    RectTransform contentRect = mapContentObject.GetComponent<RectTransform>();
                    if (contentRect != null)
                    {
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
            if (cachedPlayerMarker == null || playerObject == null)
                return;

            // If we haven't cached the direction indicator yet, try to find it.
            if (cachedDirectionIndicator == null)
            {
                Transform found = cachedPlayerMarker.transform.Find("DirectionIndicator");
                if (found != null)
                {
                    cachedDirectionIndicator = found as RectTransform;
                }
                else
                {
                    // Create it once if it doesn't exist.
                    GameObject directionObj = new GameObject("DirectionIndicator");
                    directionObj.transform.SetParent(cachedPlayerMarker.transform, false);
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

            // If we haven't cached the map content yet, try to find it
            if (cachedMapContent == null)
            {
                cachedMapContent = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
            }

            if (cachedMapContent != null)
            {
                // If we haven't cached the PropertyPoI yet, try to find it
                if (cachedPropertyPoI == null)
                {
                    cachedPropertyPoI = cachedMapContent.transform.Find("PropertyPoI(Clone)");
                }

                if (cachedPropertyPoI != null)
                {
                    Transform iconContainer = cachedPropertyPoI.Find("IconContainer");
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

        private void UpdateTimeDisplayPosition()
        {
            if (minimapTimeContainer != null)
            {
                if (doubleSizeEnabled)
                {
                    // For 2x mode, set the time container to a fixed position.
                    // Adjust these numbers until it appears correctly.
                    minimapTimeContainer.anchoredPosition = new Vector2(0, 40);
                }
                else
                {
                    // For 1x mode, use the original position.
                    minimapTimeContainer.anchoredPosition = new Vector2(0, 10);
                }
            }
        }

        private System.Collections.IEnumerator ContractPoIChecker()
        {
            while (true)
            {
                MelonLogger.Msg("ContractPoIChecker: Checking for ContractPoI(Clone)...");
                yield return new WaitForSeconds(20f);

                // Find the MapApp Content object.
                GameObject contentObj = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
                if (contentObj == null)
                {
                    MelonLogger.Warning("ContractPoIChecker: MapApp Content not found; removing ContractPoI marker if exists.");
                    RemoveContractPoIMarker();
                    continue;
                }

                // Find the ContractPoI(Clone) object under Content.
                Transform contractPoITransform = contentObj.transform.Find("ContractPoI(Clone)");
                if (contractPoITransform != null)
                {
                    MelonLogger.Msg("ContractPoIChecker: ContractPoI(Clone) found.");
                    // ContractPoI is present, so ensure the marker is added.
                    if (GameObject.Find("ContractPoIMarker") == null)
                    {
                        MelonLogger.Msg("ContractPoIChecker: No marker found; adding marker now.");
                        AddContractPoIMarker();
                    }
                    else
                    {
                        MelonLogger.Msg("ContractPoIChecker: Marker already exists.");
                    }
                }
                else
                {
                    MelonLogger.Warning("ContractPoIChecker: ContractPoI(Clone) NOT found; removing any existing marker.");
                    RemoveContractPoIMarker();
                }
            }
        }

        private void RemoveContractPoIMarker()
        {
            GameObject existingMarker = GameObject.Find("ContractPoIMarker");
            if (existingMarker != null)
            {
                UnityEngine.Object.Destroy(existingMarker);
                MelonLogger.Msg("RemoveContractPoIMarker: ContractPoI marker removed.");
            }
            else
            {
                MelonLogger.Msg("RemoveContractPoIMarker: No ContractPoI marker found to remove.");
            }
        }

        private void AddContractPoIMarker()
        {
            // Try to find the MapApp Content object.
            GameObject contentObj = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
            if (contentObj == null)
            {
                MelonLogger.Warning("AddContractPoIMarker: MapApp Content not found; cannot add ContractPoI marker.");
                return;
            }

            // Find the ContractPoI(Clone) object under Content.
            Transform contractPoITransform = contentObj.transform.Find("ContractPoI(Clone)");
            if (contractPoITransform == null)
            {
                MelonLogger.Warning("AddContractPoIMarker: ContractPoI(Clone) not found under Content.");
                return;
            }

            // Retrieve its local position.
            Vector3 contractPoILocalPos = contractPoITransform.localPosition;
            MelonLogger.Msg("AddContractPoIMarker: ContractPoI local position: " + contractPoILocalPos);

            // Convert the local position into minimap space.
            // Adjust conversionFactor as needed.
            float conversionFactor = 285f;
            Vector2 markerPos = new Vector2(contractPoILocalPos.x / conversionFactor, contractPoILocalPos.y / conversionFactor);
            MelonLogger.Msg("AddContractPoIMarker: Converted marker position: " + markerPos);

            // Find the IconContainer inside ContractPoI(Clone)
            Transform iconContainerTransform = contractPoITransform.Find("IconContainer");
            if (iconContainerTransform == null)
            {
                MelonLogger.Warning("AddContractPoIMarker: IconContainer not found under ContractPoI(Clone).");
                return;
            }

            // Instantiate a copy of the IconContainer to serve as our static marker.
            GameObject marker = UnityEngine.Object.Instantiate(iconContainerTransform.gameObject);
            marker.name = "ContractPoIMarker";

            // Parent it to your minimap content.
            if (mapContentObject != null)
            {
                marker.transform.SetParent(mapContentObject.transform, false);
            }
            else
            {
                MelonLogger.Warning("AddContractPoIMarker: Map content object is null; cannot add ContractPoI marker.");
                return;
            }

            // Set the marker’s RectTransform anchored position using our converted value.
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            if (markerRect != null)
            {
                markerRect.anchoredPosition = markerPos;
                MelonLogger.Msg("AddContractPoIMarker: Marker anchoredPosition set to: " + markerPos);
            }
            else
            {
                MelonLogger.Warning("AddContractPoIMarker: IconContainer copy does not have a RectTransform.");
            }

            MelonLogger.Msg("AddContractPoIMarker: ContractPoI marker added.");
        }

        private void CreateFallbackPlayerMarker(GameObject parent)
        {
            cachedPlayerMarker = new GameObject("PlayerMarker");
            cachedPlayerMarker.transform.SetParent(parent.transform, false);
            RectTransform markerRect = cachedPlayerMarker.AddComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(5f, 5f);
            markerRect.anchorMin = new Vector2(0.5f, 0.5f);
            markerRect.anchorMax = new Vector2(0.5f, 0.5f);
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            Image markerImage = cachedPlayerMarker.AddComponent<Image>();
            markerImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            MelonLogger.Msg("Fallback player marker created.");
        }
    }
}
