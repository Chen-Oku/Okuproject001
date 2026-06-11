using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Herramienta de un solo uso: construye y conecta la UI del menu de pausa
// (boton HUD + panel de pausa + panel de opciones) en la escena Game abierta.
public static class PauseMenuBuilder
{
    const string FontGuid = "8f586378b4e144a9851e7b34d9b748ee"; // mismo TMP font que ScoreText/ComboText
    const float ButtonFontSize = 42f;
    static readonly Vector2 MenuButtonSize = new Vector2(420, 110);

    [MenuItem("Tools/Oku/Build Pause Menu")]
    public static void Build()
    {
        var canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            Debug.LogError("No se encontro 'Canvas'. Abre Assets/Scenes/Game.unity y vuelve a intentar.");
            return;
        }

        if (canvasGO.transform.Find("PauseMenuPanel") != null || canvasGO.transform.Find("PauseButton") != null)
        {
            Debug.LogWarning("El menu de pausa ya existe en esta escena. Operacion cancelada.");
            return;
        }

        var gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null) { Debug.LogError("No se encontro GameManager en la escena."); return; }

        var uiManager = Object.FindFirstObjectByType<UIManager>();
        if (uiManager == null) { Debug.LogError("No se encontro UIManager en la escena."); return; }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
        var buttonSprite     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var backgroundSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        var checkmarkSprite  = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        Undo.SetCurrentGroupName("Build Pause Menu");
        int undoGroup = Undo.GetCurrentGroup();

        // ---- Boton de pausa (HUD) ----
        var pauseButton = CreateButtonInternal("PauseButton", canvasGO.transform, "II", buttonSprite, font,
            new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -30), new Vector2(140, 140), 60f);

        // ---- Panel raiz del menu de pausa ----
        var pausePanel = CreateFullscreenPanel("PauseMenuPanel", canvasGO.transform, backgroundSprite, new Color(0f, 0f, 0f, 0.75f));
        var mainPanel    = CreateFullscreenPanel("PauseMainPanel", pausePanel.transform, null, Color.clear);
        var optionsPanel = CreateFullscreenPanel("PauseOptionsPanel", pausePanel.transform, null, Color.clear);

        // ---- Panel principal: titulo + botones ----
        CreateLabel("Title", mainPanel.transform, "PAUSA", font, 90f, FontStyles.Bold,
            new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(600, 110), Color.white);

        var resumeBtn   = CreateMenuButton("ResumeButton", mainPanel.transform, "REANUDAR", buttonSprite, font, 320);
        var restartBtn  = CreateMenuButton("RestartButton", mainPanel.transform, "REINICIAR", buttonSprite, font, 160);
        var optionsBtn  = CreateMenuButton("OptionsButton", mainPanel.transform, "OPCIONES", buttonSprite, font, 0);
        var mainMenuBtn = CreateMenuButton("MainMenuButton", mainPanel.transform, "MENU PRINCIPAL", buttonSprite, font, -160, 36f);
        var exitBtn     = CreateMenuButton("ExitButton", mainPanel.transform, "SALIR", buttonSprite, font, -320);

        // ---- Panel de opciones: titulo + toggles + volver ----
        CreateLabel("Title", optionsPanel.transform, "OPCIONES", font, 90f, FontStyles.Bold,
            new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(600, 110), Color.white);

        var musicToggle = CreateToggleRow("MusicToggle", optionsPanel.transform, "Musica", buttonSprite, checkmarkSprite, font, 80);
        var sfxToggle   = CreateToggleRow("SfxToggle", optionsPanel.transform, "Efectos de sonido", buttonSprite, checkmarkSprite, font, -60);
        var backBtn     = CreateMenuButton("BackButton", optionsPanel.transform, "ATRAS", buttonSprite, font, -320);

        optionsPanel.SetActive(false);
        pausePanel.SetActive(false);

        // ---- Componentes y wiring ----
        var pauseMenuManager = Undo.AddComponent<PauseMenuManager>(pausePanel);
        var pmmSO = new SerializedObject(pauseMenuManager);
        pmmSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        pmmSO.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        pmmSO.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
        pmmSO.FindProperty("musicToggle").objectReferenceValue = musicToggle;
        pmmSO.FindProperty("sfxToggle").objectReferenceValue = sfxToggle;
        pmmSO.ApplyModifiedPropertiesWithoutUndo();

        Undo.AddComponent<PauseInputHandler>(gameManager.gameObject);

        var uiSO = new SerializedObject(uiManager);
        uiSO.FindProperty("pauseButton").objectReferenceValue = pauseButton.gameObject;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        UnityEventTools.AddVoidPersistentListener(pauseButton.onClick, gameManager.Pause);
        UnityEventTools.AddVoidPersistentListener(resumeBtn.onClick, pauseMenuManager.OnResumeButton);
        UnityEventTools.AddVoidPersistentListener(restartBtn.onClick, pauseMenuManager.OnRestartButton);
        UnityEventTools.AddVoidPersistentListener(optionsBtn.onClick, pauseMenuManager.OnOptionsButton);
        UnityEventTools.AddVoidPersistentListener(mainMenuBtn.onClick, pauseMenuManager.OnMainMenuButton);
        UnityEventTools.AddVoidPersistentListener(exitBtn.onClick, pauseMenuManager.OnExitButton);
        UnityEventTools.AddVoidPersistentListener(backBtn.onClick, pauseMenuManager.OnBackButton);

        UnityEventTools.AddPersistentListener(musicToggle.onValueChanged, pauseMenuManager.OnMusicToggle);
        UnityEventTools.AddPersistentListener(sfxToggle.onValueChanged, pauseMenuManager.OnSfxToggle);

        Undo.CollapseUndoOperations(undoGroup);

        EditorSceneManager.MarkSceneDirty(canvasGO.scene);
        Selection.activeGameObject = pausePanel;
        Debug.Log("Menu de pausa creado dentro de Canvas (PauseButton + PauseMenuPanel). Ajusta estilos si quieres y guarda la escena.");
    }

    // --- Helpers de construccion de UI ---

    static GameObject CreateFullscreenPanel(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        if (sprite != null)
        {
            go.AddComponent<CanvasRenderer>();
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;
        }

        return go;
    }

    static Button CreateMenuButton(string name, Transform parent, string label, Sprite sprite, TMP_FontAsset font, float yOffset, float fontSize = ButtonFontSize)
    {
        return CreateButtonInternal(name, parent, label, sprite, font,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, yOffset), MenuButtonSize, fontSize);
    }

    static Button CreateButtonInternal(string name, Transform parent, string label, Sprite sprite, TMP_FontAsset font,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        CreateStretchLabel("Text", go.transform, label, font, fontSize, new Color(0.196f, 0.196f, 0.196f));

        return btn;
    }

    static Toggle CreateToggleRow(string name, Transform parent, string label, Sprite bgSprite, Sprite checkSprite, TMP_FontAsset font, float yOffset)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, yOffset);
        rt.sizeDelta = new Vector2(420, 70);

        var bgGO = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(bgGO, "Create Background");
        var bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.SetParent(rt, false);
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.pivot = new Vector2(0, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(60, 60);
        var bgImg = bgGO.GetComponent<Image>();
        bgImg.sprite = bgSprite;
        bgImg.type = Image.Type.Sliced;

        var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(checkGO, "Create Checkmark");
        var checkRt = checkGO.GetComponent<RectTransform>();
        checkRt.SetParent(bgRt, false);
        checkRt.anchorMin = new Vector2(0.5f, 0.5f);
        checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.pivot = new Vector2(0.5f, 0.5f);
        checkRt.anchoredPosition = Vector2.zero;
        checkRt.sizeDelta = new Vector2(40, 40);
        var checkImg = checkGO.GetComponent<Image>();
        checkImg.sprite = checkSprite;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(labelGO, "Create Label");
        var labelRt = labelGO.GetComponent<RectTransform>();
        labelRt.SetParent(rt, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(80, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelGO.GetComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.font = font;
        labelTmp.fontSize = 38f;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;

        return toggle;
    }

    static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, TMP_FontAsset font, float fontSize, FontStyles style,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }

    static TextMeshProUGUI CreateStretchLabel(string name, Transform parent, string text, TMP_FontAsset font, float fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;

        return tmp;
    }
}
