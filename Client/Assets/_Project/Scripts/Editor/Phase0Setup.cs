using System;
using System.Collections.Generic;
using System.IO;
using PocketArcade.Core;
using PocketArcade.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PocketArcade.Editor
{
    public static class Phase0Setup
    {
        private static readonly string[] SceneNames =
            { SceneIds.BootScene, SceneIds.SelectScene, SceneIds.GameScene };

        [MenuItem("Pocket Arcade/Phase 0/1. Apply Foundation")]
        public static void ApplyFoundation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Play Mode를 종료한 뒤 실행하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CreateFolders();
                ApplyPlayerSettings();
                foreach (string name in SceneNames)
                    CreateSceneIfMissing(name);
                RegisterScenes();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
            }

            Debug.Log("Phase 0 기반 설정 적용 완료. 기존 씬 파일은 덮어쓰지 않았습니다. " +
                "2. Switch To Android 실행 후 3. Validate Foundation으로 확인하세요.");
        }

        private static void CreateFolders()
        {
            string[] paths = {
                "Art/Characters", "Art/UI", "Art/BlockGravity", "Audio",
                "Prefabs/Common", "Prefabs/Characters", "Prefabs/BlockGravity",
                "Scenes", "ScriptableObjects/Characters", "ScriptableObjects/MiniGames",
                "ScriptableObjects/BlockGravity", "Scripts/Core/Save", "Scripts/Core/SceneFlow",
                "Scripts/Runtime/Meta/Characters", "Scripts/Runtime/Meta/Currency",
                "Scripts/Runtime/Meta/MiniGames", "Scripts/Runtime/Audio",
                "Scripts/Runtime/MiniGames/BlockGravity/Domain",
                "Scripts/Runtime/MiniGames/BlockGravity/Application",
                "Scripts/Runtime/MiniGames/BlockGravity/Presentation",
                "Tests/PlayMode"
            };
            foreach (string path in paths)
                Directory.CreateDirectory("Assets/_Project/" + path);
            AssetDatabase.Refresh();
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProjectDefaults.ProductName;
            PlayerSettings.companyName = "Kang Wanseok";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,
                ProjectDefaults.AndroidApplicationId);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = ProjectDefaults.ReferenceWidth;
            PlayerSettings.defaultScreenHeight = ProjectDefaults.ReferenceHeight;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorSettings.serializationMode = SerializationMode.ForceText;

            // 공개 setter가 없는 Active Input Handling은 직렬화 속성으로 설정한다.
            var settings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input == null)
                throw new InvalidOperationException("Active Input Handling 설정을 찾을 수 없습니다.");
            if (input.intValue != 1)
            {
                input.intValue = 1;
                settings.ApplyModifiedProperties();
                Debug.LogWarning("Input System (New)을 적용했습니다. Unity Editor를 재시작하세요.");
            }
        }

        private static void CreateSceneIfMissing(string name)
        {
            string path = SceneIds.PathFor(name);
            if (File.Exists(path))
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                var camera = cameraObject.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 5f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.06f, 0.075f, 0.12f);

                new GameObject("Runtime Settings", typeof(RuntimeSettings));
                var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                    typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(ProjectDefaults.ReferenceWidth,
                    ProjectDefaults.ReferenceHeight);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                var safeArea = new GameObject("Safe Area", typeof(RectTransform));
                safeArea.transform.SetParent(canvasObject.transform, false);
                var rect = safeArea.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                safeArea.AddComponent<SafeAreaPanel>();

                var events = new GameObject("EventSystem", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
                if (!EditorSceneManager.SaveScene(scene, path))
                    throw new IOException("씬 저장 실패: " + path);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void RegisterScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string name in SceneNames)
                scenes.Add(new EditorBuildSettingsScene(SceneIds.PathFor(name), true));

            // 템플릿/사용자 씬은 삭제하지 않고 기존 목록 뒤에 보존한다.
            foreach (var existing in EditorBuildSettings.scenes)
            {
                if (scenes.Exists(item => item.path == existing.path))
                    continue;
                bool enabled = existing.path == "Assets/Scenes/SampleScene.unity" ? false : existing.enabled;
                scenes.Add(new EditorBuildSettingsScene(existing.path, enabled));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        [MenuItem("Pocket Arcade/Phase 0/2. Switch To Android")]
        public static void SwitchToAndroid()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Unity Hub에서 Android Build Support를 설치하세요.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(null);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android 타깃 전환에 실패했습니다. Console을 확인하세요.");
            Debug.Log("Android 플랫폼 프로필 활성화 완료. 전역 Scene List에 기본 씬 3개가 등록됩니다.");
        }

        [MenuItem("Pocket Arcade/Phase 0/3. Validate Foundation")]
        public static void ValidateFoundation()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Android 타깃으로 먼저 전환하세요.");
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait ||
                PlayerSettings.colorSpace != ColorSpace.Linear)
                throw new InvalidOperationException("Portrait/Linear 설정을 확인하세요.");
            var settings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            if (settings.FindProperty("activeInputHandler")?.intValue != 1)
                throw new InvalidOperationException("Input System (New) 설정을 확인하세요.");

            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < SceneNames.Length; i++)
            {
                string path = SceneIds.PathFor(SceneNames[i]);
                if (i >= scenes.Length || scenes[i].path != path || !scenes[i].enabled)
                    throw new InvalidOperationException("Build Profiles의 Scene List를 확인하세요: " + path);
                var scene = SceneManager.GetSceneByPath(path);
                bool alreadyLoaded = scene.IsValid() && scene.isLoaded;
                if (!alreadyLoaded)
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    bool hasSafeArea = false;
                    bool hasInput = false;
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        hasSafeArea |= root.GetComponentInChildren<SafeAreaPanel>(true) != null;
                        hasInput |= root.GetComponentInChildren<InputSystemUIInputModule>(true) != null;
                    }
                    if (!hasSafeArea || !hasInput)
                        throw new InvalidOperationException("Safe Area 또는 Input System 누락: " + path);
                }
                finally
                {
                    if (!alreadyLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }
            Debug.Log("Phase 0 구조 검증 통과: Portrait, Linear, New Input, Android, 기본 씬 3개. " +
                "Console 오류와 Test Runner 결과 및 실제 기기 화면은 별도로 확인하세요.");
        }
    }
}
