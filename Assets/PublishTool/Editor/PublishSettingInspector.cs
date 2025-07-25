using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UToolkit.PublishTool;
using Debug = UnityEngine.Debug;

namespace UToolkit.Publish.Editor
{
    [CustomEditor(typeof(PublishSetting))]
    public class PublishSettingInspector : UnityEditor.Editor
    {
        private static Texture2D _errorIcon = null;

        private static Texture2D ErrorIcon
        {
            get
            {
                if (_errorIcon == null)
                {
                    var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
                    _errorIcon = (Texture2D)typeof(EditorGUIUtility).GetMethod("LoadIcon", bindingFlags).Invoke((object)null, new object[1] { (object)"console.erroricon" });
                }

                return _errorIcon;
            }
        }

        //用户名文件夹
        private static string UserRoot => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

        private const string EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME = "EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME";

        //用户名输出目录
        private static string UserNameTxtOutPutFolder => $"{UserRoot}/.utoolkit/temp";

        private PublishSetting Target => target as PublishSetting;

        private string _cachePackageJson;
        private PackageJsonObject _packageJsonObject;
        private UnityEditor.Editor _packageJsonEditor;
        private float _inspectorWidth;
        private string _changelogPath = null;
        private string _smartVersion = null; //智能版本号

        private string SmartVersion
        {
            get
            {
                if (_smartVersion == null)
                {
                    ResetSmartVersion();
                }

                return _smartVersion;
            }
            set => _smartVersion = value;
        }

        private string ChangelogPath
        {
            get
            {
                if (string.IsNullOrEmpty(_changelogPath))
                {
                    var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Target.PackageJson));
                    _changelogPath = $"{dir}/CHANGELOG.md";
                }

                return _changelogPath;
            }
        }

        public UnityEditor.Editor PackJsonEditor
        {
            get
            {
                if (Target.PackageJson == null)
                {
                    return null;
                }

                if (_cachePackageJson != Target.PackageJson.text)
                {
                    _cachePackageJson = Target.PackageJson.text;
                    DestroyImmediate(_packageJsonObject, true);
                    DestroyImmediate(_packageJsonEditor, true);
                    try
                    {
                        _packageJsonEditor = CreateEditor(PackJsonObject);
                    }
                    catch (Exception e)
                    {
                        _packageJsonEditor = null;
                    }
                }

                return _packageJsonEditor;
            }
        }

        private PackageJsonObject PackJsonObject
        {
            get
            {
                if (_packageJsonObject == null)
                {
                    try
                    {
                        var data = JsonUtility.FromJson<PackageJson>(Target.PackageJson.text);
                        if (data != null)
                        {
                            _packageJsonObject = CreateInstance<PackageJsonObject>();
                            _packageJsonObject.PackageJson = data;
                        }
                    }
                    catch (Exception e)
                    {
                        _packageJsonObject = null;
                    }
                }

                return _packageJsonObject;
            }
        }

        public override void OnInspectorGUI()
        {
            if (Target.PackageJson == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("错误：未绑定 package.json", ErrorIcon), EditorStyles.helpBox, GUILayout.Height(32));
                var temColor = GUI.color;
                GUI.color = new Color(0.29f, 0.82f, 0.58f, 1f);

                if (GUILayout.Button("新建", GUILayout.Width(150), GUILayout.Height(32)))
                {
                    var path = EditorUtility.SaveFilePanel("新建 package.json ...", Application.dataPath, "package", "json");
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".json"))
                    {
                        var textAsset = PublishMenuWindow.CreatePackageJson(path);
                        Target.PackageJson = textAsset;
                    }
                }

                GUI.color = temColor;

                GUILayout.EndHorizontal();
            }

            if (string.IsNullOrWhiteSpace(Target.Branch))
            {
                GUILayout.Label(new GUIContent("错误：Branch 不可为空", ErrorIcon), EditorStyles.helpBox);
            }

            base.OnInspectorGUI();

            if (Target.PackageJson != null)
            {
                if (PackJsonEditor != null)
                {
                    GUILayout.Label("", GUI.skin.horizontalSlider, GUILayout.Height(16));
                    GUILayout.Label("包设置", EditorStyles.boldLabel);
                    PackJsonEditor.OnInspectorGUI();
                }
            }

            GUILayout.Label("");

            if (Event.current.type == EventType.Repaint)
            {
                _inspectorWidth = GUILayoutUtility.GetLastRect().width;
            }

            ChangeLogGUI();

            var padding = new RectOffset(10, 10, 0, 0);
            var width = _inspectorWidth;
            var size = width - padding.horizontal;
            var btnWidth = size * 0.5f;
            GUILayout.BeginHorizontal();
            GUILayout.Space(padding.left);
            if (GUILayout.Button("自动版本号+", GUILayout.Width(btnWidth), GUILayout.Height(28)))
            {
                if (PackJsonEditor != null && PackJsonObject != null)
                {
                    //_packageJson
                    var version = PackJsonObject.PackageJson.version;
                    if (string.IsNullOrWhiteSpace(version))
                    {
                        PackJsonObject.PackageJson.version = "1.0.0";
                    }
                    else
                    {
                        try
                        {
                            var str = version.Split('.');
                            int first = int.Parse(str[0]);
                            int two = int.Parse(str[1]);
                            int three = int.Parse(str[2]);
                            PackJsonObject.PackageJson.version = $"{first}.{two}.{three + 1}";
                        }
                        catch (Exception e)
                        {
                            PackJsonObject.PackageJson.version = "1.0.0";
                        }
                    }
                }
            }

            if (GUILayout.Button("保存package信息", GUILayout.Width(btnWidth), GUILayout.Height(28)))
            {
                WritePackageJson();
            }

            GUILayout.Space(padding.right);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(padding.left);
            if (GUILayout.Button("发布到Git分支", GUILayout.Width(btnWidth), GUILayout.Height(28)))
            {
                if (string.IsNullOrWhiteSpace(Target.Branch))
                {
                    Debug.LogError($"branch 不可以为空");
                }

                if (PackJsonEditor == null || PackJsonObject == null)
                {
                    Debug.LogError("发布失败，package不满足规范");
                }
                else
                {
                    var gitPath = GetGitPath();
                    var packagePath = AssetDatabase.GetAssetPath(Target.PackageJson);
                    packagePath = new FileInfo(packagePath).Directory.FullName;
                    var prefix = packagePath.Replace(gitPath, "");
                    var isLast = Target.AddTagLast ? "true" : "false";
                    Publish(gitPath, prefix, Target.Branch, PackJsonObject.PackageJson.version, isLast);
                }
            }

            if (GUILayout.Button("发布到npm", GUILayout.Width(btnWidth), GUILayout.Height(28)))
            {
                if (PackJsonEditor == null || PackJsonObject == null)
                {
                    Debug.LogError("发布失败，package不满足规范");
                }
                else
                {
                    PublishToNpm(Path.GetDirectoryName(AssetDatabase.GetAssetPath(Target.PackageJson)));
                }
            }

            GUILayout.Space(padding.right);
            GUILayout.EndHorizontal();
        }

        //获取git工作路径
        public string GetGitPath()
        {
            if (Target.PackageJson != null)
            {
                var path = AssetDatabase.GetAssetPath(Target.PackageJson);
                FileInfo fileInfo = new FileInfo(path);

                //往上一级找
                var directory = fileInfo.Directory.Parent;
                while (directory != null)
                {
                    if (Directory.Exists($"{directory}/.git"))
                    {
                        return directory.FullName;
                    }
                    else
                    {
                        directory = directory.Parent;
                    }
                }
            }

            throw new Exception("找不到有效的git仓库位置");
        }

        public void Publish(string gitPath, string prefixPath, string branch, string version, string isLast)
        {
            gitPath = gitPath.Replace("\\", "/");
            prefixPath = prefixPath.Replace("\\", "/");
            if (prefixPath[0] == '/')
            {
                prefixPath = prefixPath.Substring(1, prefixPath.Length - 1);
            }

            if (string.IsNullOrWhiteSpace(gitPath))
            {
                Debug.LogError($"发布失败，gitPath 为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(prefixPath))
            {
                Debug.LogError($"发布失败，prefixPath 为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(branch))
            {
                Debug.LogError($"发布失败，branch  为空");
                return;
            }

            if (string.IsNullOrWhiteSpace(version))
            {
                Debug.LogError($"发布失败，version  为空");
                return;
            }

            //publish.bat 路径
            var path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            FileInfo info = new FileInfo(path);
            var batPath = new FileInfo($"{info.Directory.FullName}/publish.bat").FullName;
            using (Process proc = new Process())
            {
                proc.StartInfo.FileName = batPath;
                proc.StartInfo.WorkingDirectory = gitPath;
                proc.StartInfo.Arguments = $"{prefixPath} {branch} {version} {isLast}";
                proc.Start();
                proc.WaitForExit();
            }

            // var runner = new CommandRunner("cmd.exe", gitPath);
            // var output = runner.Run($"git subtree split --prefix={prefixPath} --branch {branch}");
            // Console.WriteLine(output);
            // output = runner.Run($"git tag {version} {branch}");
            // Console.WriteLine(output);
            // output = runner.Run($"git push origin {branch} --tags");
        }

        public async void PublishToNpm(string packageRoot)
        {
            var hasNpmRegistryUrl = !string.IsNullOrEmpty(Target.NpmRegistryUrl);
            if (!hasNpmRegistryUrl)
            {
                var defalutNpmUrl = "https://registry.npmjs.org";
                ShowMessageBox($"NpmRegistryUrl 为空！\n改用npm官方地址：{defalutNpmUrl} ？", () =>
                {
                    Target.NpmRegistryUrl = defalutNpmUrl;
                    EditorUtility.SetDirty(Target);
                    AssetDatabase.SaveAssets();
                });
                return;
            }

            var npmRegistryUrl = Target.NpmRegistryUrl;
            var sb = new StringBuilder();

            #region 生成.npmignore

            sb.Clear();
            var npmIgnore = new[]
            {
                ".npmrc",
                ".gitignore",
                "npm_user.txt",
                "npm-login.bat",
                "npm-logout.bat",
                "npm-whoami.bat",
                "npm-publish.bat",
                "npm_user.txt.meta",
                "npm-login.bat.meta",
                "npm-logout.bat.meta",
                "npm-whoami.bat.meta",
                "npm-publish.bat.meta",
                "npm_user.txt~",
            };
            foreach (var element in npmIgnore)
            {
                sb.AppendLine(element);
            }

            var path_npmignore = $"{packageRoot}/.npmignore";
            Debug.Log($"生成：{path_npmignore}");
            File.WriteAllText(path_npmignore, sb.ToString());

            #endregion

            #region 生成.gitignore

            sb.Clear();
            var gitIgnore = new[]
            {
                ".npmrc",
                "npm_user.txt",
                "npm-login.bat",
                "npm-logout.bat",
                "npm-whoami.bat",
                "npm-publish.bat",
                "npm_user.txt.meta",
                "npm-login.bat.meta",
                "npm-logout.bat.meta",
                "npm-whoami.bat.meta",
                "npm-publish.bat.meta",
                "npm_user.txt~",
                ".npmignore",
            };

            foreach (var element in gitIgnore)
            {
                sb.AppendLine(element);
            }

            var path_gitignore = $"{packageRoot}/.gitignore";
            Debug.Log($"生成：{path_gitignore}");
            File.WriteAllText(path_gitignore, sb.ToString());

            #endregion

            #region 生成bat

            var path_npm_login = $"{packageRoot}/npm-login.bat";
            var login_cmd = $"start npm login --registry={npmRegistryUrl}";
            Debug.Log($"生成: {path_npm_login}");
            File.WriteAllText(path_npm_login, login_cmd);

            //随机一个用户名写入位置
            if (!Directory.Exists(UserNameTxtOutPutFolder))
            {
                Directory.CreateDirectory(UserNameTxtOutPutFolder);
            }

            var userNameCacheFile = $"{UserNameTxtOutPutFolder}/{GUID.Generate()}.txt";
            userNameCacheFile = userNameCacheFile.Replace("/", "\\");
            EditorPrefs.SetString(EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME, userNameCacheFile);
            var path_npm_whoami = $"{packageRoot}/npm-whoami.bat";
            var whoami_cmd = $"npm whoami --registry={npmRegistryUrl} > \"{userNameCacheFile}\"";
            Debug.Log($"生成: {path_npm_whoami}");
            File.WriteAllText(path_npm_whoami, whoami_cmd);

            var path_npm_logout = $"{packageRoot}/npm-logout.bat";
            var logout_cmd = $"npm logout --registry={npmRegistryUrl}";
            Debug.Log($"生成: {path_npm_logout}");
            File.WriteAllText(path_npm_logout, logout_cmd);

            var path_npm_publish = $"{packageRoot}/npm-publish.bat";
            var publish_cmd = $"start npm publish --registry={npmRegistryUrl}";
            Debug.Log($"生成: {path_npm_publish}");
            File.WriteAllText(path_npm_publish, publish_cmd);

            #endregion

            CancellationTokenSource cts = new CancellationTokenSource();
            Task.Run(async () =>
            {
                for (int i = 10; i >= 0; i--)
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }

                    Debug.Log($"发布...检查登录状态...请稍等...{i}");
                    await Task.Delay(1000);
                }

                EditorUtility.ClearProgressBar();
            });

            var output = await RunBat(path_npm_whoami, 10);
            EditorUtility.ClearProgressBar();
            if (output == "timeout")
            {
                Debug.LogError("超时！检查登录失败,请重试");
                cts.Cancel();
                EditorPrefs.DeleteKey(EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME);
            }
            else
            {
                cts.Cancel();

                //读取登录信息
                Debug.Log($"用户：{File.ReadAllText(userNameCacheFile)}");
                if (File.Exists(userNameCacheFile))
                {
                    CheckUserName(packageRoot, npmRegistryUrl, userNameCacheFile);
                }
                else
                {
                    Debug.Log($"发布失败,找不到:{userNameCacheFile}");
                    EditorPrefs.DeleteKey(EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME);
                }
            }
        }

        private async Task WritePackageJson()
        {
            if (PackJsonObject != null && Target.PackageJson != null)
            {
                try
                {
                    var info = await GetInternetTime();
                    var stamp = (long)(info.time - new DateTime(1970, 1, 1).ToLocalTime()).TotalMilliseconds;
                    PackJsonObject.PackageJson.write_time_stamp = stamp;
                    var path = AssetDatabase.GetAssetPath(Target.PackageJson);
                    var json = JsonUtility.ToJson(PackJsonObject.PackageJson);
                    var end = json.LastIndexOf("}");
                    StringBuilder sb = new StringBuilder();
                    sb.Append(",\"dependencies\":{");
                    var dependencies = PackJsonObject.PackageJson.customDependencies;
                    if (dependencies != null)
                    {
                        List<DependenciesItem> valid = new List<DependenciesItem>();
                        foreach (var item in dependencies)
                        {
                            if (!string.IsNullOrWhiteSpace(item.PackageName) && !string.IsNullOrWhiteSpace(item.Value))
                            {
                                valid.Add(item);
                            }
                        }

                        for (int i = 0; i < valid.Count; i++)
                        {
                            var item = valid[i];
                            sb.Append($"\"{item.PackageName}\":\"{item.Value}\"");
                            if (i != valid.Count - 1)
                            {
                                sb.Append(",");
                            }
                        }
                    }

                    sb.Append("}");
                    json = json.Insert(end, sb.ToString());

                    // var registries = PackJsonObject.PackageJson.scopedRegistries;
                    // if (registries != null)
                    // {
                    //     var registriesStringItem = registries.Select(item =>
                    //     {
                    //         var scopesItems = item.scopes.Select(e => $"\"{e}\"");
                    //         var scopesString = string.Join(",", scopesItems);
                    //         var nameString = $"\"name\":\"{item.name}\"";
                    //         var urlString = $"\"url\":\"{item.url}\"";
                    //         scopesString = $"\"scopes\":[{scopesString}]";
                    //         return $"{{{nameString},{urlString},{scopesString}}}";
                    //     });
                    //
                    //     var jsonString = string.Join(",", registriesStringItem);
                    //     sb.Clear();
                    //     sb.Append(",\"scopedRegistries\":[");
                    //     sb.Append(jsonString);
                    //     sb.Append("]");
                    //     end = json.LastIndexOf("}");
                    //     json = json.Insert(end, sb.ToString());
                    // }

                    File.WriteAllText(path, json);
                    AssetDatabase.Refresh();
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssets();
                    Debug.Log("保存成功");
                }
                catch (Exception e)
                {
                }
            }
        }

        private static Task<string> RunBat(string bat, float timeOut = 99999, Action onTimeout = null)
        {
            return Task.Run(() =>
            {
                using (var process = new Process())
                {
                    var file = new FileInfo(bat).FullName;
                    var workDir = Path.GetDirectoryName(file);
                    process.StartInfo.FileName = $"{file}";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.WorkingDirectory = workDir;
                    process.Start();
                    var hasExited = process.WaitForExit((int)(timeOut * 1000));
                    if (!hasExited)
                    {
                        process.Kill(); // 进程未退出，使用 Kill 方法强制关闭
                        onTimeout?.Invoke();
                        return "timeout";
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    return output;
                }
            });
        }

        private static void ShowMessageBox(string content, Action success)
        {
            if (EditorUtility.DisplayDialog("提示", content, "是"))
            {
                success?.Invoke();
            }
        }

        //检查userName
        private void CheckUserName(string packageRoot, string npmUrl, string userNameCacheFile)
        {
            var path_login_bat = $"{packageRoot}/npm-login.bat";
            var total = 10;
            if (File.Exists(userNameCacheFile))
            {
                var user_name = File.ReadAllText(userNameCacheFile);
                EditorPrefs.DeleteKey(EDITOR_PREFS_KEY_TYPHOON_CACHE_NPM_USER_NAME);
                if (string.IsNullOrEmpty(user_name))
                {
                    if (EditorUtility.DisplayDialog("提示", $"未登录npm,去登录?\n(登录完毕后再次发布即可)", "是"))
                    {
                        RunBat(path_login_bat);
                    }
                }
                else
                {
                    var packageName = PackJsonObject.PackageJson.name;
                    var version = PackJsonObject.PackageJson.version;

                    var result = EditorUtility.DisplayDialogComplex("提示", $"以{user_name}身份发布->{npmUrl}？\npackage:{packageName}\nversion:{version}", "是", "否", "换个身份");
                    switch (result)
                    {
                        case 0: //是
                            RunPublishBat();
                            break;

                        case 1: //否
                            break;

                        case 2: //换个身份
                            RunBat(path_login_bat);
                            break;
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public bool CanPublishNpm()
        {
            if (PackJsonEditor == null || PackJsonObject == null)
            {
                return false;
            }

            return true;
        }

        public void ChangeLogGUI()
        {
            if (Target.PackageJson != null)
            {
                if (!File.Exists(ChangelogPath))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(10);
                    GUILayout.Label("不存在CHANGELOG.md", GUILayout.Width(140));
                    if (GUILayout.Button("创建", GUILayout.Width(60)))
                    {
                        Debug.Log(ChangelogPath);
                        var dir = Path.GetDirectoryName(ChangelogPath);
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                            AssetDatabase.Refresh();
                        }

                        File.WriteAllText(ChangelogPath, "");
                        AssetDatabase.Refresh();
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<TextAsset>(ChangelogPath));
                    }

                    GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }
                else
                {
                    GUILayout.Label("撰写日志&发布", BoldLabel);
                    Target.VersionChangeLogFoldout = EditorGUILayout.Foldout(Target.VersionChangeLogFoldout, "补充CHANGELOG日志");

                    if (Target.VersionChangeLogFoldout)
                    {
                        GUILayout.BeginHorizontal();
                        SmartVersion = GUILayout.TextField(SmartVersion, GUILayout.Width(120));
                        if (GUILayout.Button("智能版本号", GUILayout.Width(80)))
                        {
                            ResetSmartVersion();
                        }

                        if (GUILayout.Button("保存", GUILayout.Width(60)))
                        {
                            EditorUtility.SetDirty(Target);
                            AssetDatabase.SaveAssets();
                            GUI.FocusControl("");
                        }

                        if (GUILayout.Button("提交&写入", GUILayout.Width(80)))
                        {
                            PopUpCommit();
                        }

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        Target.MajorUpdateFlag = GUILayout.Toggle(Target.MajorUpdateFlag, "重大更新", GUILayout.Width(80));
                        Target.WriteToPackageJsonVersionLog = GUILayout.Toggle(Target.WriteToPackageJsonVersionLog, "将版本日志补充到package.json", GUILayout.Width(200));
                        GUILayout.EndHorizontal();

                        Target.VersionChangeLog.Additions = DrawTxtArea("新增项(新增的内容)", Target.VersionChangeLog.Additions);
                        Target.VersionChangeLog.Fixes = DrawTxtArea("修复（修复bug和一些已知问题等）", Target.VersionChangeLog.Fixes);
                        Target.VersionChangeLog.Optimizations = DrawTxtArea("优化项（指对已有功能或设计进行优化和改进的修改）", Target.VersionChangeLog.Optimizations);
                        Target.VersionChangeLog.Deletions = DrawTxtArea("删除项（将某些功能或设计从产品中移除或删除的操作）", Target.VersionChangeLog.Deletions);
                        Target.VersionChangeLog.Other = DrawTxtArea("其它项（不好分类的日志信息）", Target.VersionChangeLog.Other);
                    }

                    // //绘制版本日志
                    // GUILayout.Label("补充CHANGELOG日志", BoldLabel);
                }
            }
        }

        private void RunPublishBat()
        {
            var packageRoot = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Target.PackageJson));
            var path_npm_publish = $"{packageRoot}/npm-publish.bat";
            RunBat(path_npm_publish);
        }

        //重置智能版本号
        private void ResetSmartVersion()
        {
            try
            {
                var current = PackJsonObject.PackageJson.version;
                var items = current.Split('.').ToArray();
                items[items.Length - 1] = $"{int.Parse(items.Last()) + 1}";
                _smartVersion = string.Join(".", items);
            }
            catch (Exception e)
            {
                _smartVersion = "1.0.0";
            }
        }

        private string DrawTxtArea(string title, string content)
        {
            var height = CalculateInputHeight(content);
            GUILayout.Label(title, BoldLabel);
            return EditorGUILayout.TextArea(content, GUILayout.Height(height));
        }

        /// <summary>
        /// 计算输入框高度
        /// </summary>
        private float CalculateInputHeight(string content)
        {
            return Mathf.Max(GUI.skin.label.CalcHeight(new GUIContent(content), _inspectorWidth), 44f);
        }

        public async void PopUpCommit(bool overrideVersion = false, string version = "")
        {
            var setting = Target;
            if (setting == null)
            {
                throw new Exception($"找不到{setting}");
            }

            if (Target.PackageJson == null)
            {
                throw new Exception($"找不到PackageJson");
            }

            var changelogPath = ChangelogPath;
            if (!File.Exists(changelogPath))
            {
                throw new Exception($"找不到{changelogPath}");
            }

            EditorUtility.DisplayProgressBar("获取互联网时间...", "对时中", 0);
            var internetTime = await GetInternetTime();
            EditorUtility.ClearProgressBar();
            var sb = new StringBuilder();

            if (internetTime.success)
            {
                sb.AppendLine($"使用互联网时间:{internetTime.time}");
            }
            else
            {
                sb.AppendLine($"对时失败:{internetTime.time}");
            }

            //重写版本号
            if (overrideVersion)
            {
                SmartVersion = version;
            }

            sb.AppendLine("写入迭代记录到package.json");
            sb.AppendLine($"版本号:{SmartVersion}");
            sb.AppendLine($"补充版本日志到package.json:{Target.WriteToPackageJsonVersionLog}?");
            if (EditorUtility.DisplayDialog("提示", sb.ToString(), "是"))
            {
                Debug.Log("发布");
                var writeToVersionLog = Target.WriteToPackageJsonVersionLog;
                var major = Target.MajorUpdateFlag;
                var changelog = Target.VersionChangeLog;

                //写入日志
                WriteChangeLogAndPackageJson(internetTime.time, SmartVersion, changelog, changelogPath, writeToVersionLog, major);

                //清空缓存
                Target.VersionChangeLog = new VersionChangeLog();
                Target.MajorUpdateFlag = false;
                ResetSmartVersion();
                EditorUtility.SetDirty(Target);
                AssetDatabase.SaveAssets();

                //提示是否发布到npm
                if (CanPublishNpm() && EditorUtility.DisplayDialog("提示", "提交成功,是否发布到npm?", "是"))
                {
                    PublishToNpm(Path.GetDirectoryName(AssetDatabase.GetAssetPath(Target.PackageJson)));
                }
            }
            else
            {
                Debug.Log("取消发布");
            }
        }

        public static async Task<(DateTime time, bool success)> GetInternetTime()
        {
            // 获取国家授时中心的时间服务器
            string[] hosts = new string[]
            {
                "https://www.baidu.com",
                "https://www.qq.com",
                "https://www.163.com",
                "https://www.google.com",
                "https://www.microsoft.com",
                "https://www.amazon.com",
            };

            // 遍历时间服务器列表，直到成功获取时间
            foreach (string host in hosts)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string url = host;
                    client.Timeout = TimeSpan.FromSeconds(5);

                    // 发送GET请求并获取响应内容
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    if (response.Headers.Date != null)
                    {
                        return (response.Headers.Date.Value.UtcDateTime.ToLocalTime(), true);
                    }
                }
                catch (Exception ex)
                {
                }
            }

            Debug.Log("返回本地时间");
            return (DateTime.Now, false);
        }

        private static string[] StringToLines(string str)
        {
            if (str == null)
            {
                return new string[0] { };
            }

            return str.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        //写入日志和版本json
        public async Task WriteChangeLogAndPackageJson(DateTime dateTime, string version, VersionChangeLog changeLog, string changelogPath, bool writeToVersionLog, bool major)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"## [{version}] - {dateTime.ToString("yyyy-MM-dd")}");
            sb.AppendLine();
            var cache = changeLog;
            var additions = CreateChangeLogPart("### 新增", cache.Additions);
            var fixes = CreateChangeLogPart("### 修复", cache.Fixes);
            var optimizations = CreateChangeLogPart("### 优化", cache.Optimizations);
            var deletions = CreateChangeLogPart("### 移除", cache.Deletions);
            var other = CreateChangeLogPart("### 其它", cache.Other);
            TryAppendLine(sb, additions);
            TryAppendLine(sb, fixes);
            TryAppendLine(sb, optimizations);
            TryAppendLine(sb, deletions);
            TryAppendLine(sb, other);
            var list = File.ReadLines(changelogPath).ToList();
            for (int i = 0; i < list.Count; i++)
            {
                var element = list[i];
                if (element.StartsWith("# 更新日志"))
                {
                    list.RemoveAt(i);
                    break;
                }
            }

            var final = new StringBuilder();
            final.AppendLine("# 更新日志");
            final.AppendLine(sb.ToString());
            foreach (var s in list)
            {
                final.AppendLine(s);
            }

            var changelog = final.ToString();
            Debug.Log($"写入：{changelogPath}");
            File.WriteAllText(changelogPath, changelog);
            AssetDatabase.Refresh();
            PackJsonObject.PackageJson.version = version;
            PackJsonObject.PackageJson.version_log = "";
            PackJsonObject.PackageJson.major_flag = major;
            if (writeToVersionLog)
            {
                //补充版本日志到
                Debug.Log($"填充version_log");
                PackJsonObject.PackageJson.version_log = sb.ToString();
            }

            await WritePackageJson();
        }

        private void TryAppendLine(StringBuilder sb, string content)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                sb.AppendLine(content);
            }
        }

        private string CreateChangeLogPart(string partTitle, string content)
        {
            var sb = new StringBuilder();
            var lines = StringToLines(content);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                sb.AppendLine($"* {line}");
            }

            if (sb.Length <= 0)
            {
                return "";
            }

            sb.Insert(0, $"{partTitle}\n");
            return sb.ToString();
        }

        private static GUIStyle _boldLabel = null;

        private static GUIStyle BoldLabel
        {
            get
            {
                if (_boldLabel == null)
                {
                    _boldLabel = new GUIStyle("label");
                    _boldLabel.fontStyle = FontStyle.Bold;
                    _boldLabel.padding = new RectOffset(2, 2, 2, 2);
                    _boldLabel.margin = new RectOffset(2, 2, 2, 2);
                    _boldLabel.border = new RectOffset(2, 2, 2, 2);
                }

                return _boldLabel;
            }
        }
    }
}