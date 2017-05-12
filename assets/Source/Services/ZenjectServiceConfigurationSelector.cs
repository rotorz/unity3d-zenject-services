// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEngine;

namespace Rotorz.Games.Services
{
    /// <summary>
    /// An asset that selects a specific <see cref="ZenjectServiceConfiguration"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Rotorz.Games/Zenject Service Configuration Selector")]
    public sealed class ZenjectServiceConfigurationSelector : ScriptableObject
    {
        [SerializeField]
        private ZenjectServiceConfiguration selectedConfiguration = null;


        /// <summary>
        /// Gets or sets the selected configuration.
        /// </summary>
        public ZenjectServiceConfiguration SelectedConfiguration {
            get { return this.selectedConfiguration; }
            set { this.selectedConfiguration = value; }
        }
    }
}
