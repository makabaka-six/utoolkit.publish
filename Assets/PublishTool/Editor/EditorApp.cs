using System;
using System.IO;

namespace UToolkit.Publish.Editor
{
    public class EditorApp
    {
        private static string _rootPath = null;

        public static string RootPath
        {
            get
            {
                if (_rootPath == null)
                {
                    var p1 = "Assets/PublishTool";
                    var p2 = "Packages/utoolkit.publish";
                    if (Directory.Exists(p1))
                    {
                        _rootPath = p1;
                    }
                    else if (Directory.Exists(p2))
                    {
                        _rootPath = p2;
                    }
                }

                if (_rootPath == null)
                {
                    throw new Exception("找不到路径");
                }

                return _rootPath;
            }
        }
    }
}