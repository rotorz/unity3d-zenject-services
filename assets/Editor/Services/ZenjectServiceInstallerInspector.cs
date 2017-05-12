// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEditor;
using UnityEngine;

namespace Rotorz.Games.Services
{
    /// <exclude/>
    [CustomEditor(typeof(ZenjectServiceInstaller), editorForChildClasses: true, isFallback = true)]
    [CanEditMultipleObjects]
    public sealed class ZenjectServiceInstallerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            // Get first property (the script field) in the serialized object.
            var propertyIterator = this.serializedObject.GetIterator();
            propertyIterator.NextVisible(true);

            // If there is no next property then this is one boring inspector!
            if (!propertyIterator.NextVisible(false)) {
                GUILayout.Label("This service installer has no properties!", EditorStyles.miniLabel);
            }
            else {
                // Draw properties using the default inspector.
                do {
                    EditorGUILayout.PropertyField(propertyIterator, includeChildren: true);
                }
                while (propertyIterator.NextVisible(false));
            }

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
