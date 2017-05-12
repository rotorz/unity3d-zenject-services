// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using Rotorz.Games.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Rotorz.Games.Services
{
    /// <summary>
    /// Base class for Zenject service installers.
    /// </summary>
    /// <seealso cref="ZenjectServiceInstaller{TService}"/>
    public abstract class ZenjectServiceInstaller : ScriptableObjectInstaller, IServiceInstaller
    {
        private static Type[] s_ServiceInstallerTypes;


        /// <summary>
        /// Discovers all of the available installer types for a given service.
        /// </summary>
        /// <param name="service">The service.</param>
        /// <returns>
        /// An array of zero-or-more non-abstract installer types.
        /// </returns>
        public static Type[] DiscoverInstallerTypes(IServiceDescriptor service)
        {
            if (s_ServiceInstallerTypes == null) {
                s_ServiceInstallerTypes = TypeMeta.DiscoverImplementations<ZenjectServiceInstaller>();
            }

            var genericInstallerType = typeof(ZenjectServiceInstaller<>).MakeGenericType(service.ServiceType);
            return s_ServiceInstallerTypes
                .Where(installerType => genericInstallerType.IsAssignableFrom(installerType))
                .Where(installerType => !installerType.IsAbstract && installerType.IsClass)
                .ToArray();
        }


        /// <summary>
        /// When <see langword="true"/> marks that the <see cref="ZenjectServiceInstaller"/>
        /// is a dependency of the project.
        /// </summary>
        [SerializeField, HideInInspector]
        public bool IsProjectDependency = false;


        /// <inheritdoc/>
        public abstract IServiceDescriptor TargetService { get; }


        /// <inheritdoc/>
        public virtual IEnumerable<IServiceDescriptor> GetDependencies()
        {
            var installerType = this.GetType();

            return this.TargetService.GetDependencies()
                .Union(ServiceDescriptor.ForDependenciesOf(installerType));
        }

        /// <inheritdoc/>
        public override void InstallBindings()
        {
        }
    }
}
