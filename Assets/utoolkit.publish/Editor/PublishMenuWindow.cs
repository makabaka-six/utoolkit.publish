using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UToolkit.PublishTool;

namespace UToolkit.Publish.Editor
{
    public class PublishMenuWindow : EditorWindow
    {
        private const int LEFT_MENU_WIDTH = 266;
        public List<PublishSetting> Settings = new List<PublishSetting>();
        public List<string> SettingsNames = new List<string>();
        private int _selectIndex = 0;
        private UnityEditor.Editor _editor;
        private Vector2 _scroll;
        private Vector2 _leftMenuScroll;

        private PublishSetting _select;

        private UnityEditor.Editor _settingEditor = null;

        private UnityEditor.Editor SettingEditor
        {
            get
            {
                if (_select == null)
                {
                    return null;
                }

                if (_settingEditor != null && _settingEditor.target != _select)
                {
                    DestroyImmediate(_settingEditor, true);
                }

                if (_settingEditor == null)
                {
                    _settingEditor = UnityEditor.Editor.CreateEditor(_select);
                }

                return _settingEditor;
            }
        }

        [MenuItem("UToolkit/PublishTool/发布UPM包")]
        private static void Open()
        {
            var win = GetWindow<PublishMenuWindow>();
            win.minSize = new Vector2(800, 800);
            win.titleContent = new GUIContent("发布UPM包");
            win.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            var guids = AssetDatabase.FindAssets("t:UToolkit.Publish.PublishSetting", new[] { "Assets" });
            Settings = new List<PublishSetting>();
            SettingsNames = new List<string>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<PublishSetting>(path);
                if (setting != null)
                {
                    Settings.Add(setting);
                    SettingsNames.Add(setting.name);
                }
            }
        }

        private void OnGUI()
        {
            var area = new Rect(Vector2.zero, position.size);
            var leftArea = area;
            leftArea.width = LEFT_MENU_WIDTH;
            DrawLeftGUI(leftArea);
            var line = leftArea;
            line.x = leftArea.xMax;
            line.width = 1;
            EditorGUI.DrawRect(line, Color.black);
            var rightArea = line;
            rightArea.x = line.xMax;
            rightArea.width = area.xMax - rightArea.x;
            DrawRightGUI(rightArea);
            Repaint();
        }

        private void OnFocus()
        {
            LoadSettings();
        }

        //绘制左侧gui
        private void DrawLeftGUI(Rect area)
        {
            var rect = area;
            var center = area.center;
            rect.width -= 8;
            rect.height -= 8;
            rect.center = center;
            GUILayout.BeginArea(rect);
            _leftMenuScroll = GUILayout.BeginScrollView(_leftMenuScroll);
            GUILayout.Label("发布配置", Styles.BoldLabel);
            GUILayout.Label(new GUIContent("不知道如何创建配置?\nproject视窗下，右键菜单->Create->utoolkit->publish->创建UPM发布配置", Styles.IconInfo), Styles.HelpBox);
            GUILayout.Space(5);

            //绘制
            foreach (var element in Settings)
            {
                var style = _select == element ? Styles.MenuButtonActive : Styles.MenuButton;
                if (GUILayout.Button(element.name, style, GUILayout.Height(32)))
                {
                    _select = element;
                }

                GUILayout.Space(1);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        //绘制右侧gui
        private void DrawRightGUI(Rect area)
        {
            var rect = area;
            var center = area.center;
            rect.width -= 8;
            rect.height -= 8;
            rect.center = center;
            if (_select == null)
            {
                GUI.Label(rect, "未选择发布配置", Styles.BoldLabel);
            }
            else
            {
                var titleRect = rect;
                titleRect.height = 26;
                GUI.Label(titleRect, _select.name, Styles.BoldLabelMiddle);
                var contentRect = titleRect;
                contentRect.y = titleRect.yMax;
                contentRect.y += 4;
                contentRect.height = rect.yMax - contentRect.y;
                GUILayout.BeginArea(contentRect);

                EditorGUILayout.ObjectField(_select, typeof(Object));
                _scroll = GUILayout.BeginScrollView(_scroll);
                if (SettingEditor != null)
                {
                    SettingEditor.OnInspectorGUI();
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }
        }

        public static TextAsset CreatePackageJson(string savePath)
        {
            //转成本地路径
            savePath = FullPathToAssetDataPath(savePath);
            var package = new PackageJson();
            package.name = "com.utoolkit.unknown";
            package.displayName = "未命名";
            package.version = "1.0.0";
            package.description = "描述...";
            package.unity = "2018.1";
            package.type = "tool";
            package.hideInEditor = false;
            package.author = new Author()
            {
                name = "cn_superstar",
                email = string.Empty,
                url = string.Empty,
            };

            package.changelogUrl = string.Empty;
            package.documentationUrl = string.Empty;
            package.keywords = new[]
            {
                "utoolkit",
            };
            package.license = string.Empty;
            package.licensesUrl = string.Empty;
            File.WriteAllText(savePath, JsonUtility.ToJson(package));
            AssetDatabase.Refresh();
            var target = AssetDatabase.LoadAssetAtPath<TextAsset>(savePath);
            if (target != null)
            {
                EditorGUIUtility.PingObject(target);
                Debug.Log($"<color=green>创建成功：{savePath}</color> ");
            }

            return target;
        }

        [MenuItem("Assets/Utoolkit/PublishTool/创建 package.json")]
        private static void CreatePackageJson()
        {
            var path = string.Empty;
            foreach (var g in Selection.assetGUIDs)
            {
                path = AssetDatabase.GUIDToAssetPath(g);
                break;
            }

            if (!string.IsNullOrWhiteSpace(path))
            {
                var savePath = $"{path}/package.json";
                CreatePackageJson(savePath);
            }
        }

        public static string FullPathToAssetDataPath(string path)
        {
            path = path.Replace("\\", "/");
            path = path.Replace(Application.dataPath, "Assets");
            return path;
        }
    }
}