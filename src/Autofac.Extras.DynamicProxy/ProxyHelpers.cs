// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Reflection;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// A helper class for proxy operations in an Autofac Interceptors context.
/// </summary>
internal static class ProxyHelpers
{
    /// <summary>
    /// The property name for the interceptors in Autofac registration extensions.
    /// </summary>
    internal const string InterceptorsPropertyName = "Autofac.Extras.DynamicProxy.RegistrationExtensions.InterceptorsPropertyName";

    /// <summary>
    /// The property name for the attribute interceptors in Autofac registration extensions.
    /// </summary>
    internal const string AttributeInterceptorsPropertyName = "Autofac.Extras.DynamicProxy.RegistrationExtensions.AttributeInterceptorsPropertyName";

    /// <summary>
    /// An empty set of services for when no interceptors are registered.
    /// </summary>
    private static readonly IEnumerable<Service> EmptyServices = Enumerable.Empty<Service>();

    /// <summary>
    /// The global proxy generator for Castle's DynamicProxy library.
    /// This is used to create new proxy instances when interceptors are applied.
    /// </summary>
    internal static readonly ProxyGenerator ProxyGenerator = new();

    /// <summary>
    /// Applies the proxy to the given ResolveRequestContext.
    /// </summary>
    /// <param name="ctx">The ResolveRequestContext to which the proxy should be applied.</param>
    /// <param name="options">Optional configurations for the proxy generation. If left null, default configurations will be used.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided context is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if interface proxying is attempted on a type that is not interface or is not accessible.</exception>
    public static void ApplyProxy(ResolveRequestContext ctx, ProxyGenerationOptions? options = null)
    {
        if (ctx is null)
        {
            throw new ArgumentNullException(nameof(ctx));
        }

        EnsureInterfaceInterceptionApplies(ctx.Registration);

        // The instance won't ever _practically_ be null by the time it gets here.
        var proxiedInterfaces = ctx.Instance!
            .GetType()
            .GetInterfaces()
            .Where(ProxyUtil.IsAccessible)
            .ToArray();

        if (!proxiedInterfaces.Any())
        {
            return;
        }

        var theInterface = proxiedInterfaces.First();
        var interfaces = proxiedInterfaces.Skip(1).ToArray();

        var interceptors = GetInterceptorServices(ctx.Registration, ctx.Instance.GetType())
            .Select(ctx.ResolveService)
            .Cast<IInterceptor>()
            .ToArray();

        ctx.Instance = options == null
            ? ProxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, interceptors)
            : ProxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, options, interceptors);
    }

    /// <summary>
    /// Checks if the component registration can be used with interface proxying.
    /// </summary>
    /// <param name="componentRegistration">The component registration to check.</param>
    /// <exception cref="InvalidOperationException">Thrown if interface proxying is attempted on a type that is not an interface or is not accessible.</exception>
    internal static void EnsureInterfaceInterceptionApplies(IComponentRegistration componentRegistration)
    {
        if (componentRegistration.Services
            .OfType<IServiceWithType>()
            .Select(s => new Tuple<Type, TypeInfo>(s.ServiceType, s.ServiceType.GetTypeInfo()))
            .Any(s => !s.Item2.IsInterface || !ProxyUtil.IsAccessible(s.Item1)))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RegistrationExtensionsResources.InterfaceProxyingOnlySupportsInterfaceServices,
                    componentRegistration));
        }
    }

    /// <summary>
    /// Retrieves the interceptor services from a component registration and implementation type.
    /// </summary>
    /// <param name="registration">The component registration where the interceptor services are stored.</param>
    /// <param name="implType">The implementation type which potentially has interceptor attributes.</param>
    /// <returns>A sequence of services that represent the interceptors.</returns>
    internal static IEnumerable<Service> GetInterceptorServices(IComponentRegistration registration, Type implType)
    {
        var result = EmptyServices;

        if (registration.Metadata.TryGetValue(InterceptorsPropertyName, out object? services) && services is IEnumerable<Service> existingPropertyServices)
        {
            result = result.Concat(existingPropertyServices);
        }

        return registration.Metadata.TryGetValue(AttributeInterceptorsPropertyName, out services) && services is IEnumerable<Service> existingAttributeServices
            ? result.Concat(existingAttributeServices)
            : result.Concat(GetInterceptorServicesFromAttributes(implType));
    }

    /// <summary>
    /// Retrieves the interceptor services from attributes on a class and its implemented interfaces.
    /// </summary>
    /// <param name="implType">The implementation type which potentially has interceptor attributes.</param>
    /// <returns>A sequence of services that represent the interceptors.</returns>
    internal static IEnumerable<Service> GetInterceptorServicesFromAttributes(Type implType)
    {
        var implTypeInfo = implType.GetTypeInfo();

        var classAttributeServices = implTypeInfo
            .GetCustomAttributes(typeof(InterceptAttribute), true)
            .Cast<InterceptAttribute>()
            .Select(att => att.InterceptorService);

        var interfaceAttributeServices = implType
            .GetInterfaces()
            .SelectMany(i => i.GetTypeInfo().GetCustomAttributes(typeof(InterceptAttribute), true))
            .Cast<InterceptAttribute>()
            .Select(att => att.InterceptorService);

        return classAttributeServices.Concat(interfaceAttributeServices);
    }
}
