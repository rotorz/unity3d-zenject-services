// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using UnityEngine;
using UnityEngine.Assertions;
using Zenject;

namespace Rotorz.Games.Services
{
    /// <summary>
    /// Installs a selected <see cref="ZenjectServiceConfiguration"/>.
    /// </summary>
    public sealed class ZenjectServiceConfigurationInstaller : MonoInstaller
    {
        [SerializeField]
        private ZenjectServiceConfigurationSelector configurationSelector = null;


        /// <summary>
        /// Gets or sets the <see cref="ZenjectServiceConfigurationSelector"/> that
        /// identifies the current <see cref="ZenjectServiceConfiguration"/> selection.
        /// </summary>
        public ZenjectServiceConfigurationSelector ConfigurationSelector {
            get { return this.configurationSelector; }
            set { this.configurationSelector = value; }
        }


        /// <inheritdoc/>
        public override void InstallBindings()
        {
            Assert.IsNotNull(this.ConfigurationSelector, "A service configuration selector must be specified.");
            Assert.IsNotNull(this.ConfigurationSelector.SelectedConfiguration, "A service configuration must be selected.");

            ZenjectServiceUtility.Install(this.Container, this.ConfigurationSelector.SelectedConfiguration);
        }
    }
}
