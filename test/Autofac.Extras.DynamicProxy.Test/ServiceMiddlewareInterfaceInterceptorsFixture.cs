// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Castle.DynamicProxy;
using Moq;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace Autofac.Extras.DynamicProxy.Test;

public class ServiceMiddlewareInterfaceInterceptorsFixture
{
    public interface IPublicInterface
    {
        string PublicMethod();
    }

    public interface IDecoratorInterface
    {
        void Decorate();
    }

    [Fact]
    public void InterceptsPublicInterfacesUseGenericMethod()
    {
        Mock<IDecoratorInterface> mockDecorator = new();
        ContainerBuilder builder = new();
        builder.RegisterType<StringMethodInterceptor>();
        builder.RegisterDecorator<Decorator, IPublicInterface>();
        builder.RegisterInstance(mockDecorator.Object);
        builder
            .RegisterType<Interceptable>()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        builder.EnableInterfaceInterceptors<IPublicInterface>();
        IContainer container = builder.Build();
        IPublicInterface obj = container.Resolve<IPublicInterface>();
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
        mockDecorator.Verify(e => e.Decorate(), Times.Never);
    }

    [Fact]
    public void InterceptsPublicInterfacesUseNoneGenericMethod()
    {
        Mock<IDecoratorInterface> mockDecorator = new();
        ContainerBuilder builder = new();
        builder.RegisterType<StringMethodInterceptor>();
        builder.RegisterDecorator<Decorator, IPublicInterface>();
        builder.RegisterInstance(mockDecorator.Object);
        builder
            .RegisterType<Interceptable>()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        builder.EnableInterfaceInterceptors(typeof(IPublicInterface));
        IContainer container = builder.Build();
        IPublicInterface obj = container.Resolve<IPublicInterface>();
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
        mockDecorator.Verify(e => e.Decorate(), Times.Never);
    }

    public class Decorator : IPublicInterface
    {
        private readonly IPublicInterface _decoratedService;
        private readonly IDecoratorInterface _decoratorInterface;

        public Decorator(IPublicInterface decoratedService, IDecoratorInterface decoratorInterface)
        {
            _decoratedService = decoratedService;
            _decoratorInterface = decoratorInterface;
        }

        public string PublicMethod()
        {
            _decoratorInterface.Decorate();
            return _decoratedService.PublicMethod();
        }
    }

    public class Interceptable : IPublicInterface
    {
        public string PublicMethod() => throw new NotImplementedException();
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
