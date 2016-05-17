﻿using System;
using System.Collections.Generic;
using LightInject;

namespace Umbraco.Core.DependencyInjection
{
    

    internal static class LightInjectExtensions
    {
        /// <summary>
        /// Registers the TService with the factory that describes the dependencies of the service, as a singleton.
        /// </summary>
        public static void RegisterSingleton<TService>(this IServiceRegistry container, Func<IServiceFactory, TService> factory, string serviceName)
        {
            container.Register(factory, serviceName, new PerContainerLifetime());
        }

        /// <summary>
        /// Registers the TService with the TImplementation as a singleton.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        public static void RegisterSingleton<TService, TImplementation>(this IServiceRegistry container) 
            where TImplementation : TService
        {
            container.Register<TService, TImplementation>(new PerContainerLifetime());
        }

        /// <summary>
        /// Registers a concrete type as a singleton service.
        /// </summary>
        /// <typeparam name="TImplementation"></typeparam>
        /// <param name="container"></param>
        public static void RegisterSingleton<TImplementation>(this IServiceRegistry container)
        {
            container.Register<TImplementation>(new PerContainerLifetime());
        }

        /// <summary>
        /// Registers the TService with the factory that describes the dependencies of the service, as a singleton.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="container"></param>
        /// <param name="factory"></param>
        public static void RegisterSingleton<TService>(this IServiceRegistry container, Func<IServiceFactory, TService> factory)
        {
            container.Register(factory, new PerContainerLifetime());
        }

        /// <summary>
        /// In order for LightInject to deal with enumerables of the same type, each one needs to be named individually
        /// </summary>
        /// <typeparam name="TLifetime"></typeparam>
        /// <param name="container"></param>
        /// <param name="implementationTypes"></param>
        public static void RegisterCollection<TLifetime>(this IServiceContainer container, IEnumerable<Type> implementationTypes)
            where TLifetime : ILifetime
        {
            //var i = 0;
            foreach (var type in implementationTypes)
            {
                //This works as of 3.0.2.2: https://github.com/seesharper/LightInject/issues/68#issuecomment-70611055
                // but means that the explicit type is registered, not the implementing type
                container.Register(type, Activator.CreateInstance<TLifetime>());

                //NOTE: This doesn't work, but it would be nice if it did (autofac supports thsi)
                //container.Register(typeof(TService), type,
                //    Activator.CreateInstance<TLifetime>());

                //This does work, but requires a unique name per service
                //container.Register(typeof(TService), type,
                //    //need to name it, we'll keep the name tiny
                //    i.ToString(CultureInfo.InvariantCulture),
                //    Activator.CreateInstance<TLifetime>());
                //i++;
            }
        }

        /// <summary>
        /// In order for LightInject to deal with enumerables of the same type, each one needs to be named individually
        /// </summary>
        /// <param name="container"></param>
        /// <param name="implementationTypes"></param>
        public static void RegisterCollection(this IServiceContainer container, IEnumerable<Type> implementationTypes)
        {
            //var i = 0;
            foreach (var type in implementationTypes)
            {
                //This works as of 3.0.2.2: https://github.com/seesharper/LightInject/issues/68#issuecomment-70611055
                // but means that the explicit type is registered, not the implementing type
                container.Register(type);

                //NOTE: This doesn't work, but it would be nice if it did (autofac supports thsi)
                //container.Register(typeof(TService), type);

                //This does work, but requires a unique name per service
                //container.Register(typeof(TService), type,
                //    //need to name it, we'll keep the name tiny
                //    i.ToString(CultureInfo.InvariantCulture));
                //i++;
            }
        }

        /// <summary>
        /// Creates a child container from the parent container
        /// </summary>
        /// <param name="parentContainer"></param>
        /// <returns></returns>
        public static ServiceContainer CreateChildContainer(this IServiceContainer parentContainer)
        {
            var child = new ChildContainer(parentContainer);
            return child;
        }

        private class ChildContainer : ServiceContainer
        {
            public ChildContainer(IServiceRegistry parentContainer)
            {
                foreach (var svc in parentContainer.AvailableServices)
                {
                    Register(svc);
                }
            }
        }        
    }
}
