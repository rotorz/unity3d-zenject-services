// Copyright (c) Rotorz Limited. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root.

namespace Rotorz.Games.Services
{
    /// <summary>
    /// Base class of a Zenject service installer of the parameterized service type.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    public abstract class ZenjectServiceInstaller<TService> : ZenjectServiceInstaller
        where TService : Service
    {
        /// <inheritdoc/>
        public override IServiceDescriptor TargetService {
            get { return ServiceDescriptor.For<TService>(); }
        }
    }
}
