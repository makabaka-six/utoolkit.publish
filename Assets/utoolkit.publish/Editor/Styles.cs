using System;
using UnityEditor;
using UnityEngine;

namespace UToolkit.Publish.Editor
{
    /// <summary>
    /// GUI样式
    /// </summary>
    internal class Styles
    {
        #region 图片

        private static Texture2D _menu2DActive = null;
        public static Texture2D Menu2DActive => LoadTheme2D(ref _menu2DActive, "menu_active.png");

        private static Texture2D _helpBox2D = null;
        public static Texture2D HelpBox2D => LoadTheme2D(ref _helpBox2D, "help_box.png");

        private static Texture2D _iconWarning = null;
        public static Texture2D IconWarning => LoadIcon2D(ref _iconWarning, "warning.png");

        private static Texture2D _iconInfo = null;

        public static Texture2D IconInfo => LoadIcon2D(ref _iconInfo, "info.png");

        private static Texture2D _iconError = null;
        public static Texture2D IconError => LoadIcon2D(ref _iconError, "error.png");

        #endregion

        #region 默认按钮

        private static GUIStyle _btn;

        public static GUIStyle Btn =>
            CreateStyle(ref _btn, "button", (style) =>
            {
                style.border = new RectOffset(2, 2, 2, 2);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.alignment = TextAnchor.MiddleCenter;
            });

        private static GUIStyle _btnLeft;

        public static GUIStyle BtnLeft =>
            CreateStyle(ref _btnLeft, "button", (style) =>
            {
                style.border = new RectOffset(6, 6, 6, 6);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.alignment = TextAnchor.MiddleLeft;
            });

        private static GUIStyle _btnRight;

        public static GUIStyle BtnRight =>
            CreateStyle(ref _btnRight, "button", (style) =>
            {
                style.border = new RectOffset(2, 2, 2, 2);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.alignment = TextAnchor.MiddleRight;
            });

        #endregion

        #region 菜单栏按钮

        public static GUIStyle MenuButton => BtnLeft;

        private static GUIStyle _menuButtonActive = null;

        public static GUIStyle MenuButtonActive =>
            CreateColorButtonStyle(ref _menuButtonActive, Menu2DActive, Menu2DActive, Menu2DActive, (style) =>
            {
                style.alignment = TextAnchor.MiddleLeft;
                style.fontStyle = FontStyle.Bold;
            });

        #endregion

        #region Label

        private static GUIStyle _helpBox;

        public static GUIStyle HelpBox =>
            CreateStyle(ref _helpBox, GUI.skin.textArea, (style) =>
            {
                style.border = new RectOffset(6, 6, 6, 6);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.normal.background = HelpBox2D;
                style.normal.scaledBackgrounds = Array.Empty<Texture2D>();
                style.alignment = TextAnchor.UpperLeft;
                style.stretchWidth = true;
                style.fontSize = 11;
            });

        private static GUIStyle _boldLabel;

        public static GUIStyle BoldLabel =>
            CreateStyle(ref _boldLabel, "label", (style) =>
            {
                style.border = new RectOffset(2, 2, 2, 2);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.UpperLeft;
            });

        private static GUIStyle _boldLabelMiddle;

        public static GUIStyle BoldLabelMiddle =>
            CreateStyle(ref _boldLabelMiddle, "label", (style) =>
            {
                style.border = new RectOffset(2, 2, 2, 2);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleLeft;
            });

        #endregion

        private static GUIStyle CreateStyle(ref GUIStyle style, string proto, Action<GUIStyle> onCreate)
        {
            if (style == null)
            {
                style = new GUIStyle(proto);
                onCreate?.Invoke(style);
            }

            return style;
        }

        private static GUIStyle CreateStyle(ref GUIStyle style, GUIStyle proto, Action<GUIStyle> onCreate)
        {
            if (style == null)
            {
                style = new GUIStyle(proto);
                onCreate?.Invoke(style);
            }

            return style;
        }

        private static GUIStyle CreateColorButtonStyle(ref GUIStyle style, Texture2D normal, Texture2D hover, Texture2D active, Action<GUIStyle> onCreate)
        {
            return CreateStyle(ref style, "button", (style) =>
            {
                style.border = new RectOffset(6, 6, 6, 6);
                style.margin = new RectOffset(2, 2, 2, 2);
                style.padding = new RectOffset(4, 4, 4, 4);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.background = normal;
                style.normal.scaledBackgrounds = Array.Empty<Texture2D>();

                style.hover.background = hover;
                style.hover.scaledBackgrounds = Array.Empty<Texture2D>();

                style.focused.background = active;
                style.focused.scaledBackgrounds = Array.Empty<Texture2D>();

                style.active.background = active;
                style.active.scaledBackgrounds = Array.Empty<Texture2D>();

                style.onNormal.background = normal;
                style.onNormal.scaledBackgrounds = Array.Empty<Texture2D>();

                style.onHover.background = hover;
                style.onHover.scaledBackgrounds = Array.Empty<Texture2D>();

                style.onActive.background = active;
                style.onActive.scaledBackgrounds = Array.Empty<Texture2D>();

                style.onFocused.background = active;
                style.onFocused.scaledBackgrounds = Array.Empty<Texture2D>();
                onCreate?.Invoke(style);
            });
        }

        private static Texture2D LoadIcon2D(ref Texture2D tex, string name)
        {
            if (tex == null)
            {
                var path = $"{EditorApp.RootPath}/Editor/Images/icon/{name}";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return tex;
        }

        private static Texture2D LoadTheme2D(ref Texture2D tex, string name)
        {
            if (tex == null)
            {
                var path = EditorGUIUtility.isProSkin ? $"{EditorApp.RootPath}/Editor/Images/dark/{name}" : $"{EditorApp.RootPath}/Editor/Images/light/{name}";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }

            return tex;
        }
    }
}