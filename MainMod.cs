using System;
using System.Collections.Generic;
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
        private const float markerXAdjustment = 5f;

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
        private GameObject contractPoIIconPrefab; // Cached IconContainer prefab for ContractPoI
        private static GameObject mapAppObject;
        private static GameObject viewportObject;
        private static GameObject playerObject;
        private static GameObject mapContentObject;
        private static RectTransform gridContainer;

        // List to track all ContractPoI markers
        private List<GameObject> contractPoIMarkers = new List<GameObject>();

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * 0.0174532924f;
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
                guiBgRect = new Rect(Screen.width - 175, 220, 160, 100);
                minimapToggleRectAdjusted = new Rect(Screen.width - 170, 230, 150, 25);
                timeToggleRectAdjusted = new Rect(Screen.width - 170, 260, 150, 25);
                doubleSizeToggleRectAdjusted = new Rect(Screen.width - 170, 290, 150, 25);
            }
            else
            {
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
            GUI.color = Color.white;
            GUI.Label(new Rect(position.x + 50, position.y, position.width - 50, position.height), label);
            Rect switchBg = new Rect(position.x + 5, position.y + 3, 40, 18);
            GUI.color = state ? Color.green : Color.gray;
            GUI.Box(switchBg, "");
            Rect handle = new Rect(state ? switchBg.x + 22 : switchBg.x + 2, switchBg.y + 2, 14, 14);
            GUI.color = Color.white;
            GUI.Box(handle, "");
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
                timeRect.anchoredPosition = new Vector2(0, 40);
            }
            else
            {
                timeRect.anchoredPosition = new Vector2(0, 10);
            }

            minimapTimeContainer = timeRect;
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
            if (cachedGameTimeText == null)
            {
                GameObject timeObj = GameObject.Find("GameplayMenu/Phone/phone/HomeScreen/InfoBar/Time");
                if (timeObj != null)
                    cachedGameTimeText = timeObj.GetComponent<Text>();
            }
            if (cachedGameTimeText != null && minimapTimeText != null)
            {
                string originalText = cachedGameTimeText.text;
                string[] tokens = originalText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 3)
                {
                    string timePart = tokens[0] + " " + tokens[1];
                    string dayPart = tokens[tokens.Length - 1];
                    minimapTimeText.text = dayPart + "\n" + timePart;
                }
                else
                    minimapTimeText.text = originalText;
            }
        }

        private static int gridSize = 20;
        private static Color gridColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private static Color bgColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private static float smoothingFactor = 10f;

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            try
            {
                if (sceneName == "Main" && !isInitializing)
                {
                    isInitializing = true;
                    MelonLogger.Msg("Detected Main scene, initializing Minimap...");
                    if (minimapObject != null)
                    {
                        UnityEngine.Object.Destroy(minimapObject);
                        minimapObject = null;
                    }
                    CreateMinimapUI();
                    MelonCoroutines.Start(FindGameObjectsRoutine());
                    isInitializing = false;
                    CacheContractPoIIcon();
                    MelonCoroutines.Start(UpdateMinimapTimeCoroutine());
                    MelonCoroutines.Start(ContractPoICheckerWorld());
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
                    if (playerObject == null)
                    {
                        playerObject = GameObject.Find("Player_Local");
                        if (playerObject != null)
                            MelonLogger.Msg("Found Player_Local");
                    }
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

            if (cachedPlayerMarker != null)
            {
                Image markerImage = cachedPlayerMarker.GetComponent<Image>();
                if (markerImage != null && markerImage.color.Equals(new Color(0.2f, 0.6f, 1f, 1f)))
                {
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
                minimapFrameRect.anchoredPosition = new Vector2(-20, -60);
            }
            else
            {
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
            mapScale = 1.2487098f;
            ResetMapContentPosition();
            UpdateMinimapFramePosition();
            UpdateTimeDisplayPosition();
        }

        private void ResetMapContentPosition()
        {
            if (playerObject == null || mapContentObject == null || minimapDisplayObject == null)
                return;
            Vector3 playerPos = playerObject.transform.position;
            float mapX = -playerPos.x * mapScale;
            float mapY = -playerPos.z * mapScale;
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
            Vector2 presetOffset = doubleSizeEnabled ? new Vector2(-131f, -141f) : new Vector2(-61f, -71f);
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
                minimapObject = new GameObject("MinimapContainer");
                UnityEngine.Object.DontDestroyOnLoad(minimapObject);
                GameObject canvasObj = new GameObject("MinimapCanvas");
                canvasObj.transform.SetParent(minimapObject.transform, false);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
                GameObject frameObj = new GameObject("MinimapFrame");
                frameObj.transform.SetParent(canvasObj.transform, false);
                minimapFrameRect = frameObj.AddComponent<RectTransform>();
                minimapFrameRect.sizeDelta = new Vector2(150, 150);
                minimapFrameRect.anchorMin = new Vector2(1, 1);
                minimapFrameRect.anchorMax = new Vector2(1, 1);
                minimapFrameRect.pivot = new Vector2(1, 1);
                minimapFrameRect.anchoredPosition = new Vector2(-20, -20);
                minimapDisplayObject = new GameObject("MinimapDisplay");
                minimapDisplayObject.transform.SetParent(frameObj.transform, false);
                RectTransform displayRect = minimapDisplayObject.AddComponent<RectTransform>();
                displayRect.anchorMin = new Vector2(0, 0);
                displayRect.anchorMax = new Vector2(1, 1);
                displayRect.offsetMin = new Vector2(0, 50);
                displayRect.offsetMax = Vector2.zero;
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
                Sprite circleSprite = CreateCircleSprite(140, Color.white);
                maskImage.sprite = circleSprite;
                maskImage.type = Image.Type.Sliced;
                maskImage.color = Color.white;
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
                CreateMinimapTimeDisplay(minimapFrameRect);

                GameObject contentObj = GameObject.Find("GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content");
                if (contentObj != null)
                {
                    Transform playerPoI = contentObj.transform.Find("PlayerPoI(Clone)");
                    if (playerPoI != null)
                    {
                        Transform iconContainer = playerPoI.Find("IconContainer");
                        if (iconContainer != null)
                        {
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
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    guiVisible = !guiVisible;
                }
                if (isEnabled && playerObject != null && mapContentObject != null && minimapDisplayObject != null)
                {
                    Vector3 playerPos = playerObject.transform.position;
                    float mapX = -playerPos.x * mapScale;
                    float mapY = -playerPos.z * mapScale;
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
            if (cachedDirectionIndicator == null)
            {
                Transform found = cachedPlayerMarker.transform.Find("DirectionIndicator");
                if (found != null)
                {
                    cachedDirectionIndicator = found as RectTransform;
                }
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
                        GameObject marker = UnityEngine.Object.Instantiate(iconContainer.gameObject);
                        marker.name = "StaticMarker_White";
                        marker.transform.SetParent(mapContentObject.transform, false);
                        RectTransform markerRect = marker.GetComponent<RectTransform>();
                        if (markerRect != null)
                        {
                            markerRect.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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
            float posX = worldPos.x * mapScale;
            float posY = worldPos.z * mapScale;
            markerRect.anchoredPosition = new Vector2(posX, posY);
            Image markerImage = marker.AddComponent<Image>();
            markerImage.color = Color.red;
            MelonLogger.Msg("Red static marker added at mapped position: " + markerRect.anchoredPosition);
        }

        private void AddDefaultMarkers()
        {
            AddWhiteStaticMarker(new Vector3(-67.17f, -3.03f, 138.31f));
            AddWhiteStaticMarker(new Vector3(-79.88f, -2.26f, 85.13f));
            AddWhiteStaticMarker(new Vector3(-179.99f, -3.03f, 113.69f));
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
                    minimapTimeContainer.anchoredPosition = new Vector2(0, 40);
                }
                else
                {
                    minimapTimeContainer.anchoredPosition = new Vector2(0, 10);
                }
            }
        }

        // Helper recursive search (using a for-loop for IL2CPP compatibility)
        private void RecursiveFind(Transform current, string targetName, List<Transform> result)
        {
            if (current.name == targetName)
                result.Add(current);
            for (int i = 0; i < current.childCount; i++)
            {
                RecursiveFind(current.GetChild(i), targetName, result);
            }
        }

        // Coroutine that every 20 seconds checks for active ContractPoI clones in the Contracts tree.
        private System.Collections.IEnumerator ContractPoICheckerWorld()
        {
            while (true)
            {
                yield return new WaitForSeconds(20f);

                GameObject contractsRoot = GameObject.Find("Managers/@Quests/Contracts");
                List<Transform> activeCPs = new List<Transform>();
                if (contractsRoot != null)
                {
                    Transform contractsTransform = contractsRoot.transform;
                    for (int i = 0; i < contractsTransform.childCount; i++)
                    {
                        RecursiveFind(contractsTransform.GetChild(i), "ContractPoI(Clone)", activeCPs);
                    }
                }
                else
                {
                    RemoveAllContractPoIMarkers();
                    continue;
                }

                // Only keep active CPs.
                activeCPs.RemoveAll(cp => !cp.gameObject.activeInHierarchy);

                float threshold = 0.1f;
                foreach (Transform cp in activeCPs)
                {
                    Vector3 wp = cp.position;
                    Vector2 desiredPos = new Vector2(wp.x * mapScale, wp.z * mapScale);
                    desiredPos.x -= markerXAdjustment;

                    bool markerFound = false;
                    for (int i = 0; i < contractPoIMarkers.Count; i++)
                    {
                        GameObject marker = contractPoIMarkers[i];
                        if (marker == null) continue;
                        RectTransform rt = marker.GetComponent<RectTransform>();
                        if (rt != null && Vector2.Distance(rt.anchoredPosition, desiredPos) < threshold)
                        {
                            markerFound = true;
                            break;
                        }
                    }
                    if (!markerFound)
                    {
                        AddContractPoIMarkerWorld(cp);
                    }
                }

                // Remove markers that no longer correspond to any active CP.
                for (int i = contractPoIMarkers.Count - 1; i >= 0; i--)
                {
                    GameObject marker = contractPoIMarkers[i];
                    if (marker == null)
                    {
                        contractPoIMarkers.RemoveAt(i);
                        continue;
                    }
                    RectTransform rt = marker.GetComponent<RectTransform>();
                    bool stillExists = false;
                    if (rt != null)
                    {
                        Vector2 markerPos = rt.anchoredPosition;
                        foreach (Transform cp in activeCPs)
                        {
                            Vector2 desiredPos = new Vector2(cp.position.x * mapScale, cp.position.z * mapScale);
                            desiredPos.x -= markerXAdjustment;
                            if (Vector2.Distance(markerPos, desiredPos) < threshold)
                            {
                                stillExists = true;
                                break;
                            }
                        }
                    }
                    if (!stillExists)
                    {
                        UnityEngine.Object.Destroy(marker);
                        contractPoIMarkers.RemoveAt(i);
                    }
                }
            }
        }

        private void RemoveAllContractPoIMarkers()
        {
            foreach (GameObject marker in contractPoIMarkers)
            {
                if (marker != null)
                    UnityEngine.Object.Destroy(marker);
            }
            contractPoIMarkers.Clear();
        }

        private void AddContractPoIMarkerWorld(Transform cpTransform)
        {
            if (cpTransform == null)
            {
                return;
            }

            Vector3 wp = cpTransform.position;
            Vector2 minimapPos = new Vector2(wp.x * mapScale, wp.z * mapScale);
            // Apply the X adjustment.
            minimapPos.x -= markerXAdjustment;

            if (contractPoIIconPrefab == null)
            {
                CacheContractPoIIcon();
                if (contractPoIIconPrefab == null)
                {
                    return;
                }
            }

            GameObject marker = UnityEngine.Object.Instantiate(contractPoIIconPrefab);
            marker.name = "ContractPoIMarker_" + cpTransform.GetInstanceID();

            if (mapContentObject != null)
            {
                marker.transform.SetParent(mapContentObject.transform, false);
            }
            else
            {
                return;
            }

            RectTransform rt = marker.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = minimapPos;
                rt.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else
            {
                MelonLogger.Warning("AddContractPoIMarkerWorld: Marker does not have a RectTransform.");
            }

            contractPoIMarkers.Add(marker);
        }

        private void CacheContractPoIIcon()
        {
            // Only cache if not already cached.
            if (contractPoIIconPrefab != null)
                return;

            // Use the full known path.
            string path = "GameplayMenu/Phone/phone/AppsCanvas/MapApp/Container/Scroll View/Viewport/Content/ContractPoI(Clone)/IconContainer";
            GameObject cpIcon = GameObject.Find(path);
            if (cpIcon != null)
            {
                contractPoIIconPrefab = cpIcon;
            }
            else
            {
                MelonLogger.Warning("CacheContractPoIIcon: Could not find ContractPoI IconContainer at path: " + path);
            }
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
