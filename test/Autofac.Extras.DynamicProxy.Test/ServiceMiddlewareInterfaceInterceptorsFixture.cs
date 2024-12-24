// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Castle.DynamicProxy;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace Autofac.Extras.DynamicProxy.Test;

public class ServiceMiddlewareInterfaceInterceptorsFixture
{
    public interface IPublicInterface
    {
        string PublicMethod();
    }

    [Fact]
    public void InterceptsPublicInterfacesUseGenericMethod()
    {
        ContainerBuilder builder = new();
        builder.RegisterType<StringMethodInterceptor>();
        builder.RegisterDecorator<Decorator, IPublicInterface>();
        builder.RegisterType<Interceptable>().InterceptedBy(typeof(StringMethodInterceptor)).As<IPublicInterface>();
        builder.EnableInterfaceInterceptors<IPublicInterface>();
        IContainer container = builder.Build();
        IPublicInterface obj = container.Resolve<IPublicInterface>();
        Assert.Equal("intercepted-decorated-PublicMethod", obj.PublicMethod());
    }

    [Fact]
    public void InterceptsPublicInterfacesUseNoneGenericMethod()
    {
        ContainerBuilder builder = new();
        builder.RegisterType<StringMethodInterceptor>();
        builder.RegisterDecorator<Decorator, IPublicInterface>();
        builder.RegisterType<Interceptable>().InterceptedBy(typeof(StringMethodInterceptor)).As<IPublicInterface>();
        builder.EnableInterfaceInterceptors(typeof(IPublicInterface));
        IContainer container = builder.Build();
        IPublicInterface obj = container.Resolve<IPublicInterface>();
        Assert.Equal("intercepted-decorated-PublicMethod", obj.PublicMethod());
    }

    public class Decorator : IPublicInterface
    {
        private readonly IPublicInterface _decoratedService;

        public Decorator(IPublicInterface decoratedService) => _decoratedService = decoratedService;

        public string PublicMethod() => $"decorated-{_decoratedService.PublicMethod()}";
    }

    public class Interceptable : IPublicInterface
    {
        public string PublicMethod() => "PublicMethod";
    }

    private class StringMethodInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            invocation.ReturnValue = $"intercepted-{invocation.ReturnValue}";
        }
    }
}
