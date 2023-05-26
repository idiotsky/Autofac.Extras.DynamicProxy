﻿// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy.Test.SatelliteAssembly;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class ClassInterceptorsFixture
{
    [Fact]
    public void EnableClassInterceptors_NullRegistration()
    {
        IRegistrationBuilder<D, ConcreteReflectionActivatorData, SingleRegistrationStyle> concrete = null;
        IRegistrationBuilder<D, Features.Scanning.ScanningActivatorData, SingleRegistrationStyle> scanning = null;
        var options = new ProxyGenerationOptions();
        Assert.Throws<ArgumentNullException>(() => concrete.EnableClassInterceptors());
        Assert.Throws<ArgumentNullException>(() => concrete.EnableClassInterceptors(options));
        Assert.Throws<ArgumentNullException>(() => scanning.EnableClassInterceptors());
        Assert.Throws<ArgumentNullException>(() => scanning.EnableClassInterceptors(options));
    }

    [Fact]
    public void InterceptorCanBeWiredUsingInterceptedBy()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<D>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(AddOneInterceptor));
        builder.RegisterType<AddOneInterceptor>();
        var container = builder.Build();
        var i = 10;
        var c = container.Resolve<D>(TypedParameter.From(i));
        var got = c.GetValueByMethod();
        Assert.Equal(i + 1, got);
    }

    [Fact]
    public void InterceptsReflectionBasedComponent()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<C>().EnableClassInterceptors();
        builder.RegisterType<AddOneInterceptor>();
        var container = builder.Build();
        var i = 10;
        var c = container.Resolve<C>(TypedParameter.From(i));
        var got = c.GetValueByMethod();
        Assert.Equal(i + 1, got);
    }

    [Fact]
    public void ThrowsIfParametersAreNotMet()
    {
        // Issue #14: Resolving an intercepted type where dependencies aren't met should throw
        var builder = new ContainerBuilder();
        builder.RegisterType<C>().EnableClassInterceptors();
        builder.RegisterType<AddOneInterceptor>();
        var container = builder.Build();
        Assert.Throws<DependencyResolutionException>(() => container.Resolve<C>());
    }

    [Fact]
    public void ResolveFactoryWithInterceptors()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<DoNothingInterceptor>()
            .AsSelf()
            .PropertiesAutowired()
            .InstancePerLifetimeScope();
        builder.RegisterType<ClassWithDelegate>()
            .AsSelf()
            .PropertiesAutowired()
            .InstancePerDependency()
            .EnableClassInterceptors().InterceptedBy(typeof(DoNothingInterceptor));
        builder.RegisterType<ClassWithDelegateFactory>()
            .AsSelf()
            .PropertiesAutowired()
            .InstancePerDependency()
            .EnableClassInterceptors().InterceptedBy(typeof(DoNothingInterceptor));

        var container = builder.Build();

        const int i = 123;

        using (var scope = container.BeginLifetimeScope())
        {
            var mgr = scope.Resolve<ClassWithDelegateFactory>();
            var byFunc = mgr.CreateByFunc(i);
            var byDelegate = mgr.CreateByDelegate(i);

            Assert.Equal(byFunc.I, byDelegate.I);
        }
    }

    [Fact]
    public void ClassInterceptorsFromAssemblyScanning()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder.RegisterAssemblyTypes(typeof(InterceptablePublicSatellite).Assembly)
            .Where(t => t.Name.Equals(nameof(InterceptablePublicSatellite), StringComparison.Ordinal))
            .EnableClassInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();
        var obj = container.Resolve<InterceptablePublicSatellite>();
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    private class AddOneInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.Method.Name == "GetValueByMethod")
            {
                invocation.ReturnValue = 1 + (int)invocation.ReturnValue;
            }
        }
    }

    [Intercept(typeof(AddOneInterceptor))]
    public class C
    {
        public C(int i)
        {
            Value = i;
        }

        public int Value { get; set; }

        public virtual int GetValueByMethod()
        {
            return Value;
        }
    }

    public class D
    {
        public D(int i)
        {
            Value = i;
        }

        public int Value { get; set; }

        public virtual int GetValueByMethod()
        {
            return Value;
        }
    }

    [SuppressMessage("CA1711", "CA1711", Justification = "This isn't a delegate, the suffix 'Delegate' is descriptive of the test case.")]
    public class ClassWithDelegate
    {
        public delegate ClassWithDelegate Factory(int i);

        public int I { get; set; }

        public ClassWithDelegate(int i)
        {
            I = i;
        }
    }

    public class ClassWithDelegateFactory
    {
        public Func<int, ClassWithDelegate> ObjectFuncFactory { get; set; }

        public ClassWithDelegate.Factory ObjectDelegateFactory { get; set; }

        public virtual ClassWithDelegate CreateByFunc(int i)
        {
            return ObjectFuncFactory(i);
        }

        public virtual ClassWithDelegate CreateByDelegate(int i)
        {
            return ObjectDelegateFactory(i);
        }
    }

    private class DoNothingInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }

    private class StringMethodInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType == typeof(string))
            {
                invocation.ReturnValue = "intercepted-" + invocation.Method.Name;
            }
            else
            {
                invocation.Proceed();
            }
        }
    }
}
