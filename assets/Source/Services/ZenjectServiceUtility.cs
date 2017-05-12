// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Rotorz.Games.Services
{
    /// <summary>
    /// Utility functionality to assist with the installation of zenject services.
    /// </summary>
    public static class ZenjectServiceUtility
    {
        /// <summary>
        /// Installs a given Zenject service to a given Zenject container.
        /// </summary>
        /// <param name="container">Inversion of control container.</param>
        /// <param name="serviceConfiguration">The target service configuration.</param>
        /// <exception cref="System.InvalidOperationException">
        /// <list type="bullet">
        /// <item>If one or more inherited configurations inherit the same configuration!</item>
        /// <item>If multiple installers have been specified for the same service (overloaded installers).</item>
        /// <item>If one or more dependent services do not have installers.</item>
        /// </list>
        /// </exception>
        public static void Install(DiContainer container, ZenjectServiceConfiguration serviceConfiguration)
        {
            VerifyCyclicInheritance(serviceConfiguration);

            Install(container, serviceConfiguration.AllInstallers.Cast<IServiceInstaller>());
        }

        /// <summary>
        /// Installs any given services to a given Zenject container.
        /// </summary>
        /// <param name="container">Inversion of control container.</param>
        /// <param name="installers">A collection of service installers.</param>
        /// <exception cref="System.InvalidOperationException">
        /// <list type="bullet">
        /// <item>If multiple installers have been specified for the same service (overloaded installers).</item>
        /// <item>If one or more dependent services do not have installers.</item>
        /// </list>
        /// </exception>
        public static void Install(DiContainer container, IEnumerable<IServiceInstaller> installers)
        {
            VerifyOverloadedInstallers(installers);

            var serviceInstallerMap = installers.ToDictionary(x => x.TargetService, x => x as IServiceInstaller);
            VerifyDependencyInstallers(serviceInstallerMap);

            var sorter = new ServiceDependencySorter();
            var sortResults = sorter.SortDependencies(serviceInstallerMap.Values);

            if (sortResults.HasCircularDependency) {
                var errorMessage = sortResults.GenerateCircularDependencyErrorMessage();
                throw new Exception(errorMessage);
            }

            InjectInstallers(container, sortResults.SortedInstallers);
            ExecuteInstallers(sortResults.SortedInstallers);
        }

        /// <summary>
        /// Builds a lookup table that can be used to lookup all of the dependents of
        /// any given services.
        /// </summary>
        public static Dictionary<IServiceDescriptor, HashSet<IServiceDescriptor>> BuildServiceDependentsLookup(IEnumerable<ZenjectServiceInstaller> installers)
        {
            return BuildServiceDependentsLookup(installers.Cast<IServiceInstaller>());
        }

        /// <summary>
        /// Builds a lookup table that can be used to lookup all of the dependents of
        /// any given services.
        /// </summary>
        public static Dictionary<IServiceDescriptor, HashSet<IServiceDescriptor>> BuildServiceDependentsLookup(IEnumerable<IServiceInstaller> installers)
        {
            var lookup = new Dictionary<IServiceDescriptor, HashSet<IServiceDescriptor>>();
            foreach (var installer in installers) {
                HashSet<IServiceDescriptor> dependents;
                foreach (var dependency in installer.GetDependencies()) {
                    if (!lookup.TryGetValue(dependency, out dependents)) {
                        dependents = new HashSet<IServiceDescriptor>();
                        lookup[dependency] = dependents;
                    }
                    dependents.Add(installer.TargetService);
                }
            }
            return lookup;
        }


        private static void VerifyCyclicInheritance(ZenjectServiceConfiguration serviceConfiguration)
        {
            if (serviceConfiguration.HasCyclicInheritance()) {
                throw new InvalidOperationException("One or more inherited service configurations inherit the same configuration!");
            }
        }

        private static void VerifyOverloadedInstallers(IEnumerable<IServiceInstaller> installers)
        {
            var counters = installers.GroupBy(installer => installer.TargetService)
                .Where(group => group.Count() > 1)
                .ToDictionary(x => x.Key, group => group.Count());

            if (counters.Count > 0) {
                var overloadedServiceNames = counters.Keys.Select(x => x.ServiceType.Name).ToArray();
                throw new InvalidOperationException("Multiple installers have been specified for the same service (overloaded installers):\n > " + string.Join("\n > ", overloadedServiceNames) + "\n");
            }
        }

        private static IEnumerable<IServiceDescriptor> GetMissingDependencyInstallers(Dictionary<IServiceDescriptor, IServiceInstaller> services)
        {
            var missingInstallers = new HashSet<IServiceDescriptor>();
            foreach (var installer in services.Values) {
                missingInstallers.UnionWith(installer.GetDependencies().Where(x => !services.ContainsKey(x)));
            }
            return missingInstallers;
        }

        private static void VerifyDependencyInstallers(Dictionary<IServiceDescriptor, IServiceInstaller> services)
        {
            var missingInstallers = GetMissingDependencyInstallers(services);
            if (missingInstallers.Any()) {
                var missingInstallerNames = missingInstallers.Select(x => x.ServiceType.FullName).ToArray();
                throw new InvalidOperationException("One or more dependent services do not have installers:\n > " + string.Join("\n > ", missingInstallerNames) + "\n");
            }
        }


        private static void InjectInstallers(DiContainer container, IEnumerable<IServiceInstaller> installers)
        {
            foreach (var installer in installers) {
                container.Inject(installer);
            }
        }

        private static void ExecuteInstallers(IEnumerable<IServiceInstaller> installers)
        {
            foreach (var installer in installers) {
                installer.InstallBindings();
            }
        }
    }
}
