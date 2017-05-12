// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.UnityEditorExtensions
{
    /// <summary>
    /// Custom styles for services editor user interface.
    /// </summary>
    public sealed class ZenjectServicesEditorStyles : EditorSingletonScriptableObject
    {
        public static ZenjectServicesEditorStyles Instance {
            get { return EditorSingletonUtility.GetAssetInstance<ZenjectServicesEditorStyles>(); }
        }


        [SerializeField]
        private SkinInfo darkSkin = new SkinInfo();
        [SerializeField]
        private SkinInfo lightSkin = new SkinInfo();


        /// <summary>
        /// Gets the current skin.
        /// </summary>
        public SkinInfo Skin {
            get { return EditorGUIUtility.isProSkin ? this.darkSkin : this.lightSkin; }
        }


        public GUIStyle Panel { get; private set; }
        public GUIStyle PanelToggle { get; private set; }
        public GUIStyle PanelToggleWarning { get; private set; }
        public GUIStyle PanelToggleError { get; private set; }
        public GUIStyle ActivePill { get; private set; }
        public GUIStyle InstallToggle { get; private set; }
        public GUIStyle InstallManyToggle { get; private set; }
        public GUIStyle ProjectUserToggle { get; private set; }
        public GUIStyle InspectButton { get; private set; }

        public GUIStyle InstallerBox { get; private set; }
        public GUIStyle InstallerMenu { get; private set; }


        /// <inheritdoc/>
        protected override void OnInitialize()
        {
            var skin = GUI.skin;


            this.Panel = new GUIStyle();
            this.Panel.normal.background = this.Skin.Panel;
            this.Panel.border = new RectOffset(3, 5, 3, 5);
            this.Panel.margin = new RectOffset(3, 2, 3, 3);
            this.Panel.padding = new RectOffset(2, 4, 2, 4);

            this.PanelToggle = new GUIStyle(EditorStyles.foldout);
            this.PanelToggle.richText = true;

            this.PanelToggleWarning = new GUIStyle(this.PanelToggle);
            this.PanelToggleWarning.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.5f, 0f)
                : new Color(1f, 0.35f, 0f);
            this.PanelToggleWarning.onNormal.textColor = this.PanelToggleWarning.normal.textColor;

            this.PanelToggleError = new GUIStyle(this.PanelToggle);
            this.PanelToggleError.normal.textColor = Color.red;
            this.PanelToggleError.onNormal.textColor = this.PanelToggleError.normal.textColor;

            this.ActivePill = new GUIStyle();
            this.ActivePill.fixedWidth = 46;
            this.ActivePill.fixedHeight = 23;
            this.ActivePill.normal.background = this.Skin.ActivePillOff;
            this.ActivePill.onNormal.background = this.Skin.ActivePillOn;

            this.InstallToggle = new GUIStyle();
            this.InstallToggle.fixedWidth = 21;
            this.InstallToggle.fixedHeight = 22;
            this.InstallToggle.normal.background = this.Skin.InstallToggleAdd;
            this.InstallToggle.onNormal.background = this.Skin.InstallToggleRemove;
            this.InstallToggle.active.background = this.Skin.InstallToggleAddActive;
            this.InstallToggle.onActive.background = this.Skin.InstallToggleRemoveActive;

            this.InstallManyToggle = new GUIStyle();
            this.InstallManyToggle.fixedWidth = 21;
            this.InstallManyToggle.fixedHeight = 22;
            this.InstallManyToggle.normal.background = this.Skin.InstallToggleAddMany;
            this.InstallManyToggle.onNormal.background = this.Skin.InstallToggleRemove;
            this.InstallManyToggle.active.background = this.Skin.InstallToggleAddManyActive;
            this.InstallManyToggle.onActive.background = this.Skin.InstallToggleRemoveActive;

            this.ProjectUserToggle = new GUIStyle();
            this.ProjectUserToggle.fixedWidth = 21;
            this.ProjectUserToggle.fixedHeight = 22;
            this.ProjectUserToggle.normal.background = this.Skin.ProjectUserToggle;
            this.ProjectUserToggle.onNormal.background = this.Skin.ProjectUserToggleOn;
            this.ProjectUserToggle.active.background = this.Skin.ProjectUserToggleActive;
            this.ProjectUserToggle.onActive.background = this.Skin.ProjectUserToggleOnActive;

            this.InspectButton = new GUIStyle();
            this.InspectButton.fixedWidth = 21;
            this.InspectButton.fixedHeight = 22;
            this.InspectButton.normal.background = this.Skin.Inspect;
            this.InspectButton.active.background = this.Skin.InspectActive;


            this.InstallerBox = new GUIStyle();
            this.InstallerBox.normal.background = this.Skin.InstallerBox;
            this.InstallerBox.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color32(34, 34, 34, 255)
                : new Color32(96, 96, 96, 255);
            this.InstallerBox.border = new RectOffset(7, 7, 23, 4);
            this.InstallerBox.contentOffset = new Vector2(-14, -25);
            this.InstallerBox.margin = new RectOffset(8, 8 + 2, 0, 8);
            this.InstallerBox.padding = new RectOffset(14, 14, 30, 9);
            this.InstallerBox.alignment = TextAnchor.UpperLeft;
            this.InstallerBox.fontSize = 11;

            this.InstallerMenu = new GUIStyle();
            this.InstallerMenu.fixedWidth = 15;
            this.InstallerMenu.fixedHeight = 15;
            this.InstallerMenu.normal.background = this.Skin.InstallerMenu;
        }


        [System.Serializable]
        public sealed class SkinInfo
        {
            [SerializeField]
            private Texture2D activePillOff = null;
            [SerializeField]
            private Texture2D activePillOn = null;
            [SerializeField]
            private Texture2D projectUserToggle = null;
            [SerializeField]
            private Texture2D projectUserToggleActive = null;
            [SerializeField]
            private Texture2D projectUserToggleOn = null;
            [SerializeField]
            private Texture2D projectUserToggleOnActive = null;
            [SerializeField]
            private Texture2D installToggleAdd = null;
            [SerializeField]
            private Texture2D installToggleAddActive = null;
            [SerializeField]
            private Texture2D installToggleAddMany = null;
            [SerializeField]
            private Texture2D installToggleAddManyActive = null;
            [SerializeField]
            private Texture2D installToggleRemove = null;
            [SerializeField]
            private Texture2D installToggleRemoveActive = null;
            [SerializeField]
            private Texture2D inspect = null;
            [SerializeField]
            private Texture2D inspectActive = null;
            [SerializeField]
            private Texture2D panel = null;
            [SerializeField]
            private Texture2D installerBox = null;
            [SerializeField]
            private Texture2D installerMenu = null;


            public Texture2D ActivePillOff {
                get { return this.activePillOff; }
            }

            public Texture2D ActivePillOn {
                get { return this.activePillOn; }
            }

            public Texture2D ProjectUserToggle {
                get { return this.projectUserToggle; }
            }

            public Texture2D ProjectUserToggleActive {
                get { return this.projectUserToggleActive; }
            }

            public Texture2D ProjectUserToggleOn {
                get { return this.projectUserToggleOn; }
            }

            public Texture2D ProjectUserToggleOnActive {
                get { return this.projectUserToggleOnActive; }
            }

            public Texture2D InstallToggleAdd {
                get { return this.installToggleAdd; }
            }

            public Texture2D InstallToggleAddActive {
                get { return this.installToggleAddActive; }
            }

            public Texture2D InstallToggleAddMany {
                get { return this.installToggleAddMany; }
            }

            public Texture2D InstallToggleAddManyActive {
                get { return this.installToggleAddManyActive; }
            }

            public Texture2D InstallToggleRemove {
                get { return this.installToggleRemove; }
            }

            public Texture2D InstallToggleRemoveActive {
                get { return this.installToggleRemoveActive; }
            }

            public Texture2D Inspect {
                get { return this.inspect; }
            }

            public Texture2D InspectActive {
                get { return this.inspectActive; }
            }

            public Texture2D Panel {
                get { return this.panel; }
            }

            public Texture2D InstallerBox {
                get { return this.installerBox; }
            }

            public Texture2D InstallerMenu {
                get { return this.installerMenu; }
            }
        }
    }
}
