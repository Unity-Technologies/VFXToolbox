using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.VFXToolbox
{
    internal static class VFXToolboxStyles
    {
        // Custom Toggleable Header (in VFXToolboxGUIUtility)
        public static GUIStyle Header;
        public static GUIStyle HeaderCheckBox;

        // Tab Buttons
        public static GUIStyle TabButtonLeft;
        public static GUIStyle TabButtonSingle;
        public static GUIStyle TabButtonMid;
        public static GUIStyle TabButtonRight;

        // Toolbar Related
        public static GUIStyle toolbarButton;
        public static GUIStyle toolbarTextField;
        public static GUIStyle toolbarLabelLeft;

        // Labels
        public static GUIStyle LargeLabel;
        public static GUIStyle miniLabel;
        public static GUIStyle miniLabelRight;
        public static GUIStyle miniLabelCenter;

        // Misc
        public static GUIStyle RListLabel;

        static VFXToolboxStyles()
        {
            Header = new GUIStyle("ShurikenModuleTitle");
            HeaderCheckBox = new GUIStyle("ShurikenCheckMark");

            Header.font = (new GUIStyle("Label")).font;
            Header.fontSize = 12;
            Header.fontStyle = FontStyle.Bold;
            Header.border = new RectOffset(15, 7, 4, 4);
            Header.margin = new RectOffset(0, 0, 16, 0);
            Header.fixedHeight = 28;
            Header.contentOffset = new Vector2(32f, -2f);

            TabButtonSingle = new GUIStyle(EditorStyles.miniButton);
            TabButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft);
            TabButtonMid = new GUIStyle(EditorStyles.miniButtonMid);
            TabButtonRight = new GUIStyle(EditorStyles.miniButtonRight);
            TabButtonSingle.fontSize = 12;
            TabButtonLeft.fontSize = 12;
            TabButtonMid.fontSize = 12;
            TabButtonRight.fontSize = 12;

            LargeLabel = new GUIStyle(EditorStyles.largeLabel);
            RListLabel = new GUIStyle(EditorStyles.label);

            toolbarButton = new GUIStyle(EditorStyles.toolbarButton);
            toolbarButton.padding = new RectOffset();
            toolbarButton.margin = new RectOffset();

            toolbarLabelLeft = new GUIStyle(EditorStyles.miniLabel);
            toolbarLabelLeft.alignment = TextAnchor.MiddleLeft;
            toolbarLabelLeft.contentOffset = new Vector2(-2, -4);

            toolbarTextField = new GUIStyle(EditorStyles.toolbarTextField);
            toolbarTextField.padding = new RectOffset(2,2,2,2);
            toolbarTextField.margin = new RectOffset(2,2,2,2);

            LargeLabel.alignment = TextAnchor.UpperRight;

            miniLabel = new GUIStyle(EditorStyles.miniLabel);
            miniLabelRight = new GUIStyle(EditorStyles.miniLabel);
            miniLabelRight.alignment = TextAnchor.MiddleRight;
            miniLabelCenter = new GUIStyle(EditorStyles.miniLabel);
            miniLabelCenter.alignment = TextAnchor.MiddleCenter;

        }
    }
}

