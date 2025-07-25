using System;

namespace UToolkit.PublishTool
{
    /// <summary>
    /// 版本日志
    /// </summary>
    [Serializable]
    public struct VersionChangeLog
    {
        public string Additions;     //新增项(新增的内容)
        public string Fixes;         //修复（修复bug和一些已知问题等）
        public string Optimizations; //优化项（指对已有功能或设计进行优化和改进的修改）
        public string Deletions;     //删除项（将某些功能或设计从产品中移除或删除的操作）
        public string Other;         //其它项（不好分类的日志信息）
    }
}