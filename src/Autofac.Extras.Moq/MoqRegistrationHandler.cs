// This software is part of the Autofac IoC container
// Copyright (c) 2007 - 2008 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Autofac.Builder;
using Autofac.Core;
using Moq;

namespace Autofac.Extras.Moq
{
    /// <summary>
    /// Resolves unknown interfaces and mocks using the <see cref="MockRepository"/> from the scope.
    /// </summary>
    internal class MoqRegistrationHandler : IRegistrationSource
    {
        private readonly IList<Type> _createdServiceTypes;

        private readonly MethodInfo _createMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoqRegistrationHandler"/> class.
        /// </summary>
        /// <param name="createdServiceTypes">A list of root services that have been created.</param>
        [SuppressMessage("CA1825", "CA1825", Justification = "netstandard1.3 doesn't support Array.Empty<T>().")]
        public MoqRegistrationHandler(IList<Type> createdServiceTypes)
        {
            this._createdServiceTypes = createdServiceTypes;
            var factoryType = typeof(MockRepository);
            this._createMethod = factoryType.GetMethod(nameof(MockRepository.Create), new Type[0]);
        }

        /// <summary>
        /// Gets a value indicating whether the registrations provided by
        /// this source are 1:1 adapters on top of other components (i.e. like Meta, Func or Owned).
        /// </summary>
        /// <value>
        /// Always returns <see langword="false" />.
        /// </value>
        public bool IsAdapterForIndividualComponents => false;

        /// <summary>
        /// Retrieve a registration for an unregistered service, to be used
        /// by the container.
        /// </summary>
        /// <param name="service">The service that was requested.</param>
        /// <param name="registrationAccessor">Not used; required by the interface</param>
        /// <returns>
        /// Registrations for the service.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="service" /> is <see langword="null" />.
        /// </exception>
        public IEnumerable<IComponentRegistration> RegistrationsFor(
            Service service,
            Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var typedService = service as TypedService;
            if (typedService == null || !this.CanMockService(typedService))
            {
                return Enumerable.Empty<IComponentRegistration>();
            }

            var rb = RegistrationBuilder.ForDelegate((c, p) => this.CreateMock(c, typedService))
                .As(service)
                .InstancePerLifetimeScope();

            return new[] { rb.CreateRegistration() };
        }

        private static bool IsIEnumerable(IServiceWithType typedService)
        {
            // We handle most generics, but we don't handle IEnumerable because that has special
            // meaning in Autofac
            return typedService.ServiceType.GetTypeInfo().IsGenericType &&
                   typedService.ServiceType.GetTypeInfo().GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static bool IsIStartable(IServiceWithType typedService)
        {
            return typeof(IStartable).IsAssignableFrom(typedService.ServiceType);
        }

        private static bool ServiceIsAbstractOrNonSealedOrInterface(IServiceWithType typedService)
        {
            var serverTypeInfo = typedService.ServiceType.GetTypeInfo();

            return serverTypeInfo.IsInterface
                || serverTypeInfo.IsAbstract
                || (serverTypeInfo.IsClass && !serverTypeInfo.IsSealed);
        }

        private bool CanMockService(IServiceWithType typedService)
        {
            return !this._createdServiceTypes.Contains(typedService.ServiceType) &&
                   ServiceIsAbstractOrNonSealedOrInterface(typedService) &&
                   !IsIEnumerable(typedService) &&
                   !IsIStartable(typedService);
        }

        /// <summary>
        /// Creates a mock object.
        /// </summary>
        /// <param name="context">The component context.</param>
        /// <param name="typedService">The typed service.</param>
        /// <returns>
        /// The mock object from the repository.
        /// </returns>
        private object CreateMock(IComponentContext context, TypedService typedService)
        {
            var specificCreateMethod = this._createMethod.MakeGenericMethod(new[] { typedService.ServiceType });
            var mock = (Mock)specificCreateMethod.Invoke(context.Resolve<MockRepository>(), null);
            return mock.Object;
        }
    }
}
