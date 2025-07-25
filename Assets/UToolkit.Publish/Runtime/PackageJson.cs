using System;

namespace UToolkit.PublishTool
{
    [Serializable]
    public class PackageJson
    {
        public string name;
        public string displayName;
        public string version;
        public string description;
        public string unity;
        public string type;
        public bool hideInEditor;
        public Author author;
        public string changelogUrl;
        public string documentationUrl;
        public string[] keywords;
        public string license;
        public string licensesUrl;
        public DependenciesItem[] customDependencies;

        // public ScopedRegisterItem[] scopedRegistries;
        public string version_log;    //版本日志
        public bool major_flag;       //是否为重大修复
        public long write_time_stamp; //写入时间戳
        public MapData others;
    }

    [Serializable]
    public class ScopedRegisterItem
    {
        public string name;
        public string url;
        public string[] scopes;
    }

    [Serializable]
    public class Author
    {
        public string name;
        public string email;
        public string url;
    }

    [Serializable]
    public class DependenciesItem
    {
        public string PackageName;
        public string Value;
    }

    [Serializable]
    public class MapData
    {
        public MapDataItem[] items;
    }

    [Serializable]
    public class MapDataItem
    {
        public string key;
        public string value;
    }
}