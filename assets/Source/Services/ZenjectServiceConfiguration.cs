// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rotorz.Games.Services
{
    /// <summary>
    /// A project configuration asset that defines the installers for any Zenject services
    /// that are required for a configuration target of the project.
    /// </summary>
    [CreateAssetMenu(menuName = "Rotorz.Games/Zenject Service Configuration")]
    public sealed class ZenjectServiceConfiguration : ScriptableObject
    {
        [SerializeField]
        private ZenjectServiceConfiguration[] inheritedConfigurations = { };
        [SerializeField]
        private ZenjectServiceInstaller[] installers = { };


        /// <summary>
        /// Gets the <see cref="ZenjectServiceConfiguration"/> instances that are inherited
        /// by this <see cref="ZenjectServiceConfiguration"/> instance.
        /// </summary>
        /// <seealso cref="HasCyclicInheritance()"/>
        public IEnumerable<ZenjectServiceConfiguration> InheritedConfigurations {
            get {
                return this.inheritedConfigurations
                    .Where(x => x != null && x != this)
                    .Distinct()
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets the <see cref="ZenjectServiceInstaller"/> instances that are inherited from
        /// parent configurations.
        /// </summary>
        /// <seealso cref="InheritedConfigurations"/>
        /// <seealso cref="Installers"/>
        /// <seealso cref="AllInstallers"/>
        public IEnumerable<ZenjectServiceInstaller> InheritedInstallers {
            get { return DiscoverInheritedInstallers(this); }
        }

        /// <summary>
        /// Gets the <see cref="ZenjectServiceInstaller"/> instances that are defined by the
        /// <see cref="ZenjectServiceConfiguration"/>.
        /// </summary>
        /// <seealso cref="InheritedInstallers"/>
        /// <seealso cref="AllInstallers"/>
        public IEnumerable<ZenjectServiceInstaller> Installers {
            get { return this.installers; }
        }

        /// <summary>
        /// Gets all of the <see cref="ZenjectServiceInstaller"/> instances that are define and
        /// inherited by the <see cref="ZenjectServiceConfiguration"/>.
        /// </summary>
        /// <seealso cref="InheritedConfigurations"/>
        /// <seealso cref="InheritedInstallers"/>
        /// <seealso cref="Installers"/>
        public IEnumerable<ZenjectServiceInstaller> AllInstallers {
            get { return DiscoverInstallers(this); }
        }


        /// <summary>
        /// Tests whether the <see cref="ZenjectServiceConfiguration"/> has any cyclic
        /// configuration inheritances.
        /// </summary>
        /// <remarks>
        /// <para>Cyclic inheritance occurs when one or more <see cref="ZenjectServiceConfiguration"/>
        /// instances inherit themselves either directly or from another <see cref="ZenjectServiceConfiguration"/>
        /// that the <see cref="ZenjectServiceConfiguration"/> inherits.</para>
        /// </remarks>
        /// <returns>
        /// A value of <see langword="true"/> if the <see cref="ZenjectServiceConfiguration"/>
        /// has any cyclic configuration inheritances; otherwise, a value of <see langword="false"/>.
        /// </returns>
        public bool HasCyclicInheritance()
        {
            var traversed = new HashSet<ZenjectServiceConfiguration>();
            return this.DetectCyclicInheritance(traversed);
        }

        private bool DetectCyclicInheritance(HashSet<ZenjectServiceConfiguration> traversed)
        {
            if (!traversed.Add(this)) {
                return true;
            }

            foreach (var inheritedConfiguration in this.inheritedConfigurations) {
                if (inheritedConfiguration == null) {
                    continue;
                }
                if (inheritedConfiguration.DetectCyclicInheritance(traversed)) {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Finds the dominant installer for a given service when multiple installers are
        /// defined for the service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns>
        /// A <see cref="ZenjectServiceInstaller"/> instance when one is found; otherwise,
        /// a value of <see langword="null"/>.
        /// </returns>
        public ZenjectServiceInstaller FindDominantInstallerForService(IServiceDescriptor service)
        {
            return this.AllInstallers
                .LastOrDefault(x => x.TargetService == service);
        }

        /// <summary>
        /// Finds the closest inherited installer for a given service.
        /// </summary>
        /// <remarks>
        /// <para>This method will not locate a <see cref="ZenjectServiceInstaller"/>
        /// instance from the <see cref="ZenjectServiceConfiguration"/> instance itself
        /// since it is only searching any inherited <see cref="ZenjectServiceConfiguration"/>
        /// instances.</para>
        /// </remarks>
        /// <param name="service">The service.</param>
        /// <returns>
        /// A <see cref="ZenjectServiceInstaller"/> instance when one is found; otherwise,
        /// a value of <see langword="null"/> since the <see cref="ZenjectServiceConfiguration"/>
        /// does not inherit an installer for the given service.
        /// </returns>
        public ZenjectServiceInstaller FindClosestInheritedInstallerForService(IServiceDescriptor service)
        {
            return this.InheritedInstallers
                .LastOrDefault(x => x.TargetService == service);
        }


        private static IEnumerable<ZenjectServiceInstaller> DiscoverInstallers(ZenjectServiceConfiguration configuration)
        {
            var installerSet = new HashSet<ZenjectServiceInstaller>();
            var installerList = new List<ZenjectServiceInstaller>();
            var processedConfigurations = new HashSet<ZenjectServiceConfiguration>();

            DiscoverInstallers(configuration, installerSet, installerList, processedConfigurations);
            installerList.Reverse();
            return installerList;
        }

        private static void DiscoverInstallers(ZenjectServiceConfiguration configuration, HashSet<ZenjectServiceInstaller> installerSet, List<ZenjectServiceInstaller> installerList, HashSet<ZenjectServiceConfiguration> processedConfigurations)
        {
            if (configuration == null) {
                return;
            }

            if (!processedConfigurations.Add(configuration)) {
                return;
            }

            foreach (var installer in configuration.Installers.Reverse()) {
                if (installer == null) {
                    continue;
                }

                if (installerSet.Add(installer)) {
                    installerList.Add(installer);
                }
            }

            foreach (var inheritedConfiguration in configuration.InheritedConfigurations.Reverse()) {
                DiscoverInstallers(inheritedConfiguration, installerSet, installerList, processedConfigurations);
            }
        }


        private static IEnumerable<ZenjectServiceInstaller> DiscoverInheritedInstallers(ZenjectServiceConfiguration configuration)
        {
            var installerSet = new HashSet<ZenjectServiceInstaller>();
            var installerList = new List<ZenjectServiceInstaller>();
            var processedConfigurations = new HashSet<ZenjectServiceConfiguration>();

            processedConfigurations.Add(configuration);

            foreach (var inheritedConfiguration in configuration.InheritedConfigurations.Reverse()) {
                DiscoverInstallers(inheritedConfiguration, installerSet, installerList, processedConfigurations);
            }
            installerList.Reverse();
            return installerList;
        }
    }
}
