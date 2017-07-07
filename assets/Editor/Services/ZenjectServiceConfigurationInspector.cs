// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.Collections;
using Rotorz.Games.Reflection;
using Rotorz.Games.UnityEditorExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Rotorz.Games.Services
{
    /// <exclude/>
    [CustomEditor(typeof(ZenjectServiceConfiguration))]
    public sealed class ZenjectServiceConfigurationInspector : Editor
    {
        private static HashSet<IServiceDescriptor> s_ExpandedServices = new HashSet<IServiceDescriptor>();
        private static ZenjectServiceInstaller s_ClipboardReference;

        private HashSet<IServiceDescriptor> discoveredServices;

        private ZenjectServiceConfiguration targetConfigurationObject;
        private Dictionary<IServiceDescriptor, ZenjectServiceInstaller> targetInstallerComponents;
        private HashSet<ZenjectServiceInstaller> targetActiveInstallers;
        //private HashSet<ZenjectServiceInstaller> targetInheritedInstallers;
        private Dictionary<IServiceDescriptor, HashSet<IServiceDescriptor>> serviceDependentsLookup;
        private Dictionary<IServiceDescriptor, IServiceDescriptor[]> sortedServiceDependentsLookup;
        private ServiceEntryGroup[] serviceEntries;

        private SerializedProperty inheritedConfigurationsProperty;
        private SerializedPropertyWithDropTargetAdaptor inheritedConfigurationsListAdaptor;
        private SerializedProperty installersProperty;

        private bool wasPreviousServiceEntryExpanded;


        [SerializeField]
        private string filterTitle = "";


        private void OnEnable()
        {
            this.discoveredServices = new HashSet<IServiceDescriptor>(ServiceDescriptor.AllServices);

            this.Refresh();

            this.inheritedConfigurationsProperty = this.serializedObject.FindProperty("inheritedConfigurations");
            this.inheritedConfigurationsListAdaptor = new SerializedPropertyWithDropTargetAdaptor(this.inheritedConfigurationsProperty, droppableObjectType: typeof(ZenjectServiceConfiguration));
            this.installersProperty = this.serializedObject.FindProperty("installers");

            Undo.undoRedoPerformed += this.UndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= this.UndoRedoPerformed;
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            this.DrawInheritedConfigurations();
            this.DrawServiceListing();

            this.serializedObject.ApplyModifiedProperties();
        }

        private void UndoRedoPerformed()
        {
            this.Refresh();
            this.Repaint();
        }


        private void DrawInheritedConfigurations()
        {
            EditorGUI.BeginChangeCheck();

            ReorderableListGUI.Title("Inherited Configurations");
            ReorderableListGUI.ListField(this.inheritedConfigurationsListAdaptor);

            if (EditorGUI.EndChangeCheck()) {
                this.serializedObject.ApplyModifiedProperties();
                this.Refresh();
            }

            if (this.targetConfigurationObject.HasCyclicInheritance()) {
                EditorGUILayout.HelpBox("Error: Cyclic Reference!\nOne or more inherited service configurations inherit the same configuration!", MessageType.Error);
                EditorGUILayout.Space();
            }
        }

        private void DrawServiceListing()
        {
            EditorGUILayout.Space();
            this.DrawServiceListingFilterInput();
            EditorGUILayout.Space();

            foreach (var group in this.serviceEntries) {
                this.DrawServiceEntryGroup(group);
            }
        }

        private void DrawServiceListingFilterInput()
        {
            bool applyTitleFiltering = !string.IsNullOrEmpty(this.filterTitle);

            GUILayout.BeginHorizontal();

            this.filterTitle = EditorGUILayout.TextField(this.filterTitle, ExtraEditorStyles.Instance.SearchTextField);
            if (GUILayout.Button("", applyTitleFiltering ? ExtraEditorStyles.Instance.SearchCancelButton : ExtraEditorStyles.Instance.SearchCancelButtonEmpty)) {
                this.filterTitle = "";
                GUI.FocusControl(null);
            }

            GUILayout.EndHorizontal();
        }

        private bool MatchesTitleFilter(string title)
        {
            return title != null && title.IndexOf(this.filterTitle, StringComparison.InvariantCultureIgnoreCase) != -1;
        }

        private void DrawServiceEntryGroup(ServiceEntryGroup group)
        {
            bool applyTitleFiltering = !string.IsNullOrEmpty(this.filterTitle);

            this.wasPreviousServiceEntryExpanded = false;

            int listingIndex = 0;
            foreach (var service in group.Entries) {
                if (applyTitleFiltering && !this.MatchesTitleFilter(service.Title) && !this.MatchesTitleFilter(service.TargetInstallerTitle)) {
                    continue;
                }

                // Display the group title above the first service entry.
                if (listingIndex == 0) {
                    GUILayout.Label(group.Title, ExtraEditorStyles.Instance.GroupLabel);
                }

                this.DrawServiceEntry(service, listingIndex);
                ++listingIndex;
            }

            // Add spaces below non-empty groups of service entries.
            if (listingIndex > 0) {
                EditorGUILayout.Space();
            }
        }

        private void DrawServiceEntry(ServiceEntry service, int listingIndex)
        {
            bool isServiceExpanded = this.IsServiceExpanded(service);

            if (listingIndex > 0) {
                GUILayout.Space((!this.wasPreviousServiceEntryExpanded && isServiceExpanded) ? 2 : -3);
            }

            GUILayout.BeginVertical(ZenjectServicesEditorStyles.Instance.Panel);

            this.DrawServiceEntryHeader(service);

            if (isServiceExpanded) {
                this.DrawServiceInstaller(service);
                this.DrawServiceDependents(service);
            }

            GUILayout.EndVertical();

            GUILayout.Space(isServiceExpanded ? 3 : -3);
            this.wasPreviousServiceEntryExpanded = isServiceExpanded;
        }

        private bool HasAnyDependents(ServiceEntry service)
        {
            if (service.DominantInstaller == null) {
                return false;
            }
            if (service.DominantInstaller.IsProjectDependency) {
                return true;
            }

            HashSet<IServiceDescriptor> dependents;
            this.serviceDependentsLookup.TryGetValue(service.Descriptor, out dependents);

            return (dependents != null && dependents.Count != 0);
        }

        private void DrawServiceEntryHeader(ServiceEntry service)
        {
            var fullPosition = GUILayoutUtility.GetRect(0, 25);
            var installTogglePosition = new Rect(fullPosition.xMax - 26, fullPosition.y + 2, 25, 25);
            var projectDependencyTogglePosition = new Rect(installTogglePosition.x - 26, fullPosition.y + 2, 25, 25);
            var activeTogglePosition = new Rect(projectDependencyTogglePosition.x - 51, fullPosition.y + 1, 46, 23);

            var inspectButtonPosition = installTogglePosition;
            var inheritedLabelPosition = new Rect(inspectButtonPosition.x - 71, fullPosition.y + 1, 66, 22);

            var toggleInteractivePosition = new Rect(fullPosition.x + 2, fullPosition.y, activeTogglePosition.x - (fullPosition.x + 2), fullPosition.height);
            var togglePosition = new Rect(toggleInteractivePosition.x, toggleInteractivePosition.y + 4, toggleInteractivePosition.width, toggleInteractivePosition.height - 4);

            int controlID = GUIUtility.GetControlID(FocusType.Passive, fullPosition);

            string title = service.Title;
            bool hasLocalInstaller = (service.Installer != null && service.InstallerActive);
            bool hasInheritedInstaller = service.ClosestInheritedInstaller != null;
            bool hasInstallerEditor = service.Installer != null || service.ClosestInheritedInstaller != null;

            var titleToggleStyle = ZenjectServicesEditorStyles.Instance.PanelToggle;
            Color restoreColor;
            bool forceOpaqueToggle = false;

            if (this.serviceDependentsLookup.ContainsKey(service.Descriptor)) {
                if (!hasLocalInstaller && !hasInheritedInstaller) {
                    titleToggleStyle = ZenjectServicesEditorStyles.Instance.PanelToggleWarning;
                    title += " (<b>Missing</b>)";
                    forceOpaqueToggle = true;
                }
            }

            if ((hasLocalInstaller || hasInheritedInstaller) && !this.HasAnyDependents(service)) {
                titleToggleStyle = ZenjectServicesEditorStyles.Instance.PanelToggleWarning;
                title += " (<b>No Dependents</b>)";
                forceOpaqueToggle = true;
            }

            if (hasInstallerEditor && service.DominantInstaller == null) {
                titleToggleStyle = ZenjectServicesEditorStyles.Instance.PanelToggleWarning;
                title += " (<b>Inactive</b>)";
                forceOpaqueToggle = true;
            }

            if (service.InheritedInstallers.Any()) {
                if (service.InstallerActive || service.InheritedInstallers.Count() > 1) {
                    titleToggleStyle = ZenjectServicesEditorStyles.Instance.PanelToggleError;
                    title += " (<b>Error: Overloaded</b>)";
                    forceOpaqueToggle = true;
                }

                if (service.Installer != null) {
                    float horizontalOffset = (inspectButtonPosition.xMax - inheritedLabelPosition.x) + 5;
                    activeTogglePosition.x -= horizontalOffset;
                    installTogglePosition.x -= horizontalOffset;
                    projectDependencyTogglePosition.x -= horizontalOffset;
                    toggleInteractivePosition.width -= horizontalOffset;
                }
            }

            switch (Event.current.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if (this.CanExpandService(service) && toggleInteractivePosition.Contains(Event.current.mousePosition)) {
                        this.SetServiceExpanded(service, !this.IsServiceExpanded(service));
                        Event.current.Use();
                        GUIUtility.ExitGUI();
                    }
                    break;

                case EventType.Repaint:
                    EditorGUI.BeginDisabledGroup(!forceOpaqueToggle && !this.CanExpandService(service));
                    restoreColor = GUI.backgroundColor;

                    // Make toggle arrow even fainter when the entry cannot be expanded.
                    if (!this.CanExpandService(service)) {
                        GUI.backgroundColor = new Color(restoreColor.r, restoreColor.g, restoreColor.b, restoreColor.a * 0.5f);
                    }

                    titleToggleStyle.Draw(togglePosition, title, false, false, this.IsServiceExpanded(service), false);

                    GUI.backgroundColor = restoreColor;
                    EditorGUI.EndDisabledGroup();
                    break;
            }

            if (service.InheritedInstallers.Any()) {
                GUI.Label(inheritedLabelPosition, "(Inherited)", ExtraEditorStyles.Instance.RightAlignedMiniLabel);
                if (GUI.Button(inspectButtonPosition, GUIContent.none, ZenjectServicesEditorStyles.Instance.InspectButton)) {
                    var inheritedInstaller = this.targetConfigurationObject.FindClosestInheritedInstallerForService(service.Descriptor);
                    Selection.objects = new UnityEngine.Object[] { inheritedInstaller };
                }
            }

            if (service.Installer != null) {
                this.DrawActiveToggle(activeTogglePosition, service);
            }
            if (service.Installer != null || !service.InheritedInstallers.Any()) {
                this.DrawInstallToggle(installTogglePosition, service);
            }
            if (service.Installer != null && !service.InheritedInstallers.Any()) {
                this.DrawProjectDependencyToggle(projectDependencyTogglePosition, service);
            }
        }

        private void DrawActiveToggle(Rect position, ServiceEntry service)
        {
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(position, service.InstallerActive, GUIContent.none, ZenjectServicesEditorStyles.Instance.ActivePill);
            if (EditorGUI.EndChangeCheck()) {
                if (service.InstallerActive) {
                    Undo.SetCurrentGroupName("Deactivate Service Installer");
                    this.RemoveActiveInstaller(service.Installer);
                }
                else {
                    Undo.SetCurrentGroupName("Activate Service Installer");
                    this.AddActiveInstaller(service.Installer);
                }
                this.Refresh();
            }
        }

        private void DrawProjectDependencyToggle(Rect position, ServiceEntry service)
        {
            EditorGUI.BeginChangeCheck();
            GUI.Toggle(position, service.Installer.IsProjectDependency, GUIContent.none, ZenjectServicesEditorStyles.Instance.ProjectUserToggle);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(service.Installer, "Toggle Project Dependency");
                service.Installer.IsProjectDependency = !service.Installer.IsProjectDependency;
            }
        }

        private void DrawInstallToggle(Rect position, ServiceEntry service)
        {
            var installToggleStyle = service.AvailableInstallerTypes.Length > 1
                ? ZenjectServicesEditorStyles.Instance.InstallManyToggle
                : ZenjectServicesEditorStyles.Instance.InstallToggle;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(service.AvailableInstallerTypes.Length == 0);
            GUI.Toggle(position, service.Installer != null, GUIContent.none, installToggleStyle);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck()) {
                if (service.Installer != null) {
                    this.DestroyServiceInstaller(service.Installer);
                }
                else {
                    if (service.AvailableInstallerTypes.Length > 1) {
                        var serviceInstallerMenu = this.BuildServiceInstallerMenu(service);
                        serviceInstallerMenu.ShowAsContext();
                    }
                    else if (service.AvailableInstallerTypes.Length == 1) {
                        this.CreateServiceInstaller(service.AvailableInstallerTypes.First());
                        this.SetServiceExpanded(service, true);
                    }
                }

                GUIUtility.ExitGUI();
            }
        }

        private EditorMenu BuildServiceInstallerMenu(ServiceEntry service)
        {
            int distinctInstallerTypeNamespaces = service.AvailableInstallerTypes
                .Select(type => type.Namespace)
                .Distinct()
                .Count();

            var serviceInstallerMenu = new EditorMenu();
            foreach (var serviceInstallerType in service.AvailableInstallerTypes) {
                string installerTitle = distinctInstallerTypeNamespaces > 1
                    ? NicifyNamespaceQualifiedInstallerTitle(serviceInstallerType)
                    : NicifyInstallerTitle(serviceInstallerType);

                serviceInstallerMenu.AddCommand(installerTitle)
                    .Action(() => {
                        this.CreateServiceInstaller(serviceInstallerType);
                        this.SetServiceExpanded(service, true);
                    });
            }
            return serviceInstallerMenu;
        }

        private void DrawServiceDependents(ServiceEntry service)
        {
            IServiceDescriptor[] sortedDependents;
            this.sortedServiceDependentsLookup.TryGetValue(service.Descriptor, out sortedDependents);

            bool hasProjectDependentUser = (service.Installer != null && service.Installer.IsProjectDependency);
            bool hasServiceDependents = (sortedDependents != null && sortedDependents.Length != 0);
            if (!hasProjectDependentUser && !hasServiceDependents) {
                return;
            }

            ExtraEditorGUI.SeparatorLight(marginTop: 0);

            GUILayout.Label(" Required For:", ExtraEditorStyles.Instance.MetaLabel);
            if (hasProjectDependentUser) {
                GUILayout.Label("(Project)", EditorStyles.miniLabel);
            }
            if (hasServiceDependents) {
                foreach (var dependent in sortedDependents) {
                    GUILayout.Label(dependent.TitleWithNamespace, EditorStyles.miniLabel);
                }
            }
            GUILayout.Space(2);
        }

        private void DrawServiceInstaller(ServiceEntry service)
        {
            if (service.TargetInstaller == null) {
                return;
            }

            if (service.TargetInstallerEditor == null) {
                service.TargetInstallerEditor = Editor.CreateEditor(service.TargetInstaller);
            }

            var positionMarker = GUILayoutUtility.GetRect(0, 0);

            EditorGUI.BeginDisabledGroup(!service.InstallerActive);
            GUILayout.BeginVertical(service.TargetInstallerTitle, ZenjectServicesEditorStyles.Instance.InstallerBox);
            service.TargetInstallerEditor.OnInspectorGUI();
            GUILayout.EndVertical();
            EditorGUI.EndDisabledGroup();

            var installerMenuPosition = new Rect(positionMarker.xMax - 15 - 5, positionMarker.y + 4, 15, 15);
            if (GUI.Button(installerMenuPosition, GUIContent.none, ZenjectServicesEditorStyles.Instance.InstallerMenu)) {
                this.ShowServiceInstallerContextMenu(service.TargetInstaller, service.InstallerActive);
            }
        }

        private void ShowServiceInstallerContextMenu(ZenjectServiceInstaller installer, bool active)
        {
            var menu = new EditorMenu();

            menu.AddCommand("Reset to Default Values")
                .Enabled(active)
                .Action(() => {
                    var serviceInstallerType = installer.GetType();

                    Undo.RecordObject(installer, "Reset to Default Values");
                    var clone = ScriptableObject.CreateInstance(serviceInstallerType);
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    EditorUtility.CopySerialized(clone, installer);
                    DestroyImmediate(clone);
                });

            menu.AddSeparator();

            menu.AddCommand("Copy Values")
                .Action(() => {
                    s_ClipboardReference = installer;
                });
            menu.AddCommand("Paste Values")
                .Enabled(active && s_ClipboardReference != null && s_ClipboardReference != installer && s_ClipboardReference.GetType().IsAssignableFrom(installer.GetType()))
                .Action(() => {
                    Undo.RecordObject(installer, "Paste Values");
                    EditorUtility.CopySerialized(s_ClipboardReference, installer);
                });

            menu.AddSeparator();

            menu.AddCommand("Edit Script")
                .Action(() => {
                    var script = MonoScript.FromScriptableObject(installer);
                    var assetPath = AssetDatabase.GetAssetPath(script);
                    if (!string.IsNullOrEmpty(assetPath)) {
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
                        InternalEditorUtility.OpenFileAtLineExternal(filePath, 0);
                    }
                });

            menu.ShowAsContext();
        }



        private bool CanExpandService(ServiceEntry service)
        {
            bool hasInstallerToShow = service.Installer != null || service.InheritedInstallers.Any();
            bool hasDependentsListToShow = this.serviceDependentsLookup.ContainsKey(service.Descriptor);

            return hasInstallerToShow || hasDependentsListToShow;
        }

        private bool IsServiceExpanded(ServiceEntry service)
        {
            bool expandedState = s_ExpandedServices.Contains(service.Descriptor);

            // It is slightly more user friendly to collapse services when there is nothing
            // to show since over long sessions in the editor the old expanded state can
            // easily be forgotten by the user leading to confusion.
            if (!this.CanExpandService(service)) {
                expandedState = false;
                s_ExpandedServices.Remove(service.Descriptor);
            }

            return expandedState;
        }

        private void SetServiceExpanded(ServiceEntry service, bool expanded)
        {
            if (expanded) {
                s_ExpandedServices.Add(service.Descriptor);
            }
            else {
                s_ExpandedServices.Remove(service.Descriptor);
            }
        }


        private void CreateServiceInstaller(Type serviceInstallerType)
        {
            var serviceInstaller = (ZenjectServiceInstaller)ScriptableObject.CreateInstance(serviceInstallerType);
            serviceInstaller.name = serviceInstallerType.FullName;
            AssetDatabase.AddObjectToAsset(serviceInstaller, this.target);
            Undo.RegisterCreatedObjectUndo(serviceInstaller, "Add Installer");

            this.AddActiveInstaller(serviceInstaller);
            this.serializedObject.ApplyModifiedProperties();

            this.Refresh();
        }

        private void DestroyServiceInstaller(ZenjectServiceInstaller installer)
        {
            Undo.SetCurrentGroupName("Remove Installer");
            this.RemoveActiveInstaller(installer);
            this.serializedObject.ApplyModifiedProperties();
            Undo.DestroyObjectImmediate(installer);

            this.Refresh();
        }

        private void AddActiveInstaller(ZenjectServiceInstaller installer)
        {
            this.installersProperty.arraySize += 1;
            var serviceInstallerElementProperty = this.installersProperty.GetArrayElementAtIndex(this.installersProperty.arraySize - 1);
            serviceInstallerElementProperty.objectReferenceValue = installer;
        }

        private void RemoveActiveInstaller(ZenjectServiceInstaller installer)
        {
            for (int i = 0; i < this.installersProperty.arraySize; ++i) {
                var element = this.installersProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null || element.objectReferenceValue == installer) {
                    this.installersProperty.DeleteArrayElementAtIndex(i);
                    --i;
                }
            }
        }


        private void Refresh()
        {
            this.serializedObject.ApplyModifiedProperties();

            this.targetConfigurationObject = this.target as ZenjectServiceConfiguration;
            this.targetInstallerComponents = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this.target))
                .OfType<ZenjectServiceInstaller>()
                .ToDictionary(x => x.TargetService);
            this.targetActiveInstallers = new HashSet<ZenjectServiceInstaller>(this.targetConfigurationObject.Installers);
            //this.targetInheritedInstallers = new HashSet<ZenjectServiceInstaller>(this.targetConfigurationObject.InheritedInstallers);
            this.serviceDependentsLookup = ZenjectServiceUtility.BuildServiceDependentsLookup(this.targetConfigurationObject.AllInstallers);
            this.sortedServiceDependentsLookup = this.serviceDependentsLookup.ToDictionary(
                entry => entry.Key,
                entry => entry.Value
                    .OrderBy(group => group.TitleWithNamespace, ServiceGroupTitleComparer.Instance)
                    .ToArray()
            );

            this.serviceEntries = this.discoveredServices
                .Select(CreateServiceEntry)
                .OrderBy(entry => entry.Descriptor.ServiceType.Name)
                .GroupBy(service => service.Descriptor.ServiceType.Namespace)
                .Select(group => new ServiceEntryGroup {
                    Title = !string.IsNullOrEmpty(group.Key) ? group.Key : "Global Namespace",
                    Entries = group.ToArray()
                })
                .OrderBy(group => group.Title, ServiceGroupTitleComparer.Instance)
                .ToArray();
        }

        private ServiceEntry CreateServiceEntry(IServiceDescriptor service)
        {
            var entry = new ServiceEntry();

            entry.Title = service.Title;
            entry.Descriptor = service;
            entry.AvailableInstallerTypes = ZenjectServiceInstaller.DiscoverInstallerTypes(service);

            entry.InheritedInstallers = this.targetConfigurationObject.InheritedInstallers
                .Where(x => x.TargetService == service)
                .ToArray();

            this.targetInstallerComponents.TryGetValue(service, out entry.Installer);
            entry.InstallerActive = entry.Installer != null && this.targetActiveInstallers.Contains(entry.Installer);
            entry.ClosestInheritedInstaller = this.targetConfigurationObject.FindClosestInheritedInstallerForService(service);

            entry.TargetInstaller = (entry.ClosestInheritedInstaller != null && !entry.InstallerActive)
                ? entry.ClosestInheritedInstaller
                : entry.Installer;

            if (entry.TargetInstaller != null) {
                entry.TargetInstallerTitle = NicifyNamespaceQualifiedInstallerTitle(entry.TargetInstaller.GetType());
            }

            entry.DominantInstaller = this.targetConfigurationObject.FindDominantInstallerForService(service);

            if (entry.TargetInstaller != null) {
                string targetInstallerHint = NicifyInstallerTitle(entry.TargetInstaller.GetType());
                if (targetInstallerHint.StartsWith(entry.Title)) {
                    targetInstallerHint = targetInstallerHint.Substring(entry.Title.Length).TrimStart(' ', '-');
                }
                if (!string.IsNullOrEmpty(targetInstallerHint)) {
                    if (EditorGUIUtility.isProSkin) {
                        entry.Title += " <color=white>(" + targetInstallerHint + ")</color>";
                    }
                    else {
                        entry.Title += " <color=grey>(" + targetInstallerHint + ")</color>";
                    }
                }
            }

            return entry;
        }

        private static string NicifyNamespaceQualifiedInstallerTitle(Type installerType)
        {
            return TypeMeta.NicifyNamespaceQualifiedName(
                installerType.Namespace,
                NicifyInstallerTitle(installerType)
            );
        }

        private static string NicifyInstallerTitle(Type installerType)
        {
            return TypeMeta.NicifyCompoundName(installerType.Name, unwantedSuffix: "_Installer");
        }


        private sealed class ServiceEntryGroup
        {
            public string Title;
            public ServiceEntry[] Entries;
        }


        private sealed class ServiceEntry
        {
            public string Title;
            public IServiceDescriptor Descriptor;
            public Type[] AvailableInstallerTypes = { };
            public ZenjectServiceInstaller[] InheritedInstallers = { };
            public ZenjectServiceInstaller Installer;
            public bool InstallerActive;
            public ZenjectServiceInstaller TargetInstaller;
            public string TargetInstallerTitle;
            public Editor TargetInstallerEditor;
            public ZenjectServiceInstaller ClosestInheritedInstaller;
            public ZenjectServiceInstaller DominantInstaller;
        }


        private sealed class ServiceGroupTitleComparer : IComparer<string>
        {
            public static readonly ServiceGroupTitleComparer Instance = new ServiceGroupTitleComparer();

            public int Compare(string a, string b)
            {
                // A special case group title for services specific to the project.
                if (a.StartsWith("Project.")) {
                    return -100000;
                }
                if (b.StartsWith("Project.")) {
                    return +100000;
                }

                // A special case group title for services in the global namespace.
                if (a == "Global Namespace") {
                    return -99999;
                }
                if (b == "Global Namespace") {
                    return +99999;
                }

                return a.CompareTo(b);
            }
        }
    }
}
