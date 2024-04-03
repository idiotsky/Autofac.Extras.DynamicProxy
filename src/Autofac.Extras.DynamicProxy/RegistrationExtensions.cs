// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Autofac.Features.Scanning;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Adds registration syntax to the <see cref="ContainerBuilder"/> type.
/// </summary>
public static class RegistrationExtensions
{
    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration)
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <param name="additionalInterfaces">Additional interface types. Calls to their members will be proxied as well.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ActivatorData.ConfigurationActions.Add((t, rb) => rb.EnableClassInterceptors(options, additionalInterfaces));
        return registration;
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <param name="additionalInterfaces">Additional interface types. Calls to their members will be proxied as well.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ActivatorData.ImplementationType =
            ProxyHelpers.ProxyGenerator.ProxyBuilder.CreateClassProxyType(
                registration.ActivatorData.ImplementationType,
                additionalInterfaces ?? Type.EmptyTypes,
                options);

        var interceptorServices = ProxyHelpers.GetInterceptorServicesFromAttributes(registration.ActivatorData.ImplementationType);
        AddInterceptorServicesToMetadata(registration, interceptorServices, ProxyHelpers.AttributeInterceptorsPropertyName);

        registration.OnPreparing(e =>
        {
            var proxyParameters = new List<Parameter>();
            int index = 0;

            if (options.HasMixins)
            {
                foreach (var mixin in options.MixinData.Mixins)
                {
                    proxyParameters.Add(new PositionalParameter(index++, mixin));
                }
            }

            proxyParameters.Add(new PositionalParameter(index++, ProxyHelpers.GetInterceptorServices(e.Component, registration.ActivatorData.ImplementationType)
                .Select(s => e.Context.ResolveService(s))
                .Cast<IInterceptor>()
                .ToArray()));

            if (options.Selector != null)
            {
                proxyParameters.Add(new PositionalParameter(index, options.Selector));
            }

            e.Parameters = proxyParameters.Concat(e.Parameters).ToArray();
        });

        return registration;
    }

    /// <summary>
    /// Enable interface interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or interface, or added with InterceptedBy() calls.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> EnableInterfaceInterceptors<TLimit, TActivatorData, TSingleRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration, ProxyGenerationOptions? options = null)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ConfigurePipeline(p => p.Use(PipelinePhase.Activation, MiddlewareInsertionMode.StartOfPhase, (ctx, next) =>
        {
            next(ctx);

            ProxyHelpers.ApplyProxy(ctx, options);
        }));

        return registration;
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServices">The interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServices"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params Service[] interceptorServices)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (interceptorServices == null || interceptorServices.Any(s => s == null))
        {
            throw new ArgumentNullException(nameof(interceptorServices));
        }

        AddInterceptorServicesToMetadata(builder, interceptorServices, ProxyHelpers.InterceptorsPropertyName);

        return builder;
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServiceNames">The names of the interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServiceNames"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params string[] interceptorServiceNames)
    {
        if (interceptorServiceNames == null || interceptorServiceNames.Any(n => n == null))
        {
            throw new ArgumentNullException(nameof(interceptorServiceNames));
        }

        return InterceptedBy(builder, interceptorServiceNames.Select(n => new KeyedService(n, typeof(IInterceptor))).ToArray());
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServiceTypes">The types of the interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServiceTypes"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params Type[] interceptorServiceTypes)
    {
        if (interceptorServiceTypes == null || interceptorServiceTypes.Any(t => t == null))
        {
            throw new ArgumentNullException(nameof(interceptorServiceTypes));
        }

        return InterceptedBy(builder, interceptorServiceTypes.Select(t => new TypedService(t)).ToArray());
    }

    private static void AddInterceptorServicesToMetadata<TLimit, TActivatorData, TStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        IEnumerable<Service> interceptorServices,
        string metadataKey)
    {
        if (builder.RegistrationData.Metadata.TryGetValue(metadataKey, out object? existing) && existing is IEnumerable<Service> existingServices)
        {
            builder.RegistrationData.Metadata[metadataKey] =
                existingServices.Concat(interceptorServices).Distinct();
        }
        else
        {
            builder.RegistrationData.Metadata.Add(metadataKey, interceptorServices);
        }
    }
}
