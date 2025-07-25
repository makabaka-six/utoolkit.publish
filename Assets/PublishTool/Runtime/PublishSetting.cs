using UnityEngine;

namespace UToolkit.PublishTool
{
    [CreateAssetMenu(menuName = "UToolkit/PublishTool/创建UPM发布配置")]
    public class PublishSetting : ScriptableObject
    {
        //package.json
        public TextAsset PackageJson;

        //分支
        public string Branch = "npm";

        //加入最大标签
        public bool AddTagLast = true;

        //NPM注册地址
        public string NpmRegistryUrl = "https://registry.npmjs.org";

        //版本日志
        public VersionChangeLog VersionChangeLog;

        [HideInInspector]

        //迭代日志展开标记
        public bool VersionChangeLogFoldout = false;

        [HideInInspector]

        //版本日志写入到version_log
        public bool WriteToPackageJsonVersionLog = false;

        [HideInInspector] public bool MajorUpdateFlag = false;
    }
}