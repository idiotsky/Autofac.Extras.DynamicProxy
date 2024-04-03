// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core.Resolving.Pipeline;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Provides a set of static methods for registering middleware services.
/// </summary>
public static class ServiceMiddlewareRegistrationExtensions
{
    /// <summary>
    /// Represents an extension method on the <see cref="ContainerBuilder"/> for enabling interface interceptors.
    /// </summary>
    /// <typeparam name="TService">The type of the service to enable interceptors for.</typeparam>
    /// <param name="builder">The container builder.</param>
    /// <param name="options">The proxy generation options.</param>
    public static void EnableInterfaceInterceptors<TService>(this ContainerBuilder builder, ProxyGenerationOptions? options = null)
    {
        builder.RegisterServiceMiddleware<TService>(PipelinePhase.ScopeSelection, (context, next) =>
        {
            next(context);
            ProxyHelpers.ApplyProxy(context, options);
        });
    }

    /// <summary>
    /// Represents an extension method on the <see cref="ContainerBuilder"/> for enabling interface interceptors.
    /// </summary>
    /// <param name="builder">The container builder.</param>
    /// <param name="serviceType">The type of the service to enable interceptors for.</param>
    /// <param name="options">The proxy generation options.</param>
    public static void EnableInterfaceInterceptors(this ContainerBuilder builder, Type serviceType, ProxyGenerationOptions? options = null)
    {
        builder.RegisterServiceMiddleware(serviceType, PipelinePhase.ScopeSelection, (context, next) =>
        {
            next(context);
            ProxyHelpers.ApplyProxy(context, options);
        });
    }
}
