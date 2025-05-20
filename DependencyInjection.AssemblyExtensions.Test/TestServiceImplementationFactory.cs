using DependencyInjection.ImplementationFactory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DependencyInjection.AssemblyExtensions.Test;

public class TestServiceImplementationFactory : IServiceImplementationFactory
{
    public object GetService(IServiceProvider serviceProvider, Type serviceType, object? key)
    {
        throw new NotImplementedException();
    }

    public object GetService(IServiceProvider serviceProvider, Type serviceType)
    {
        throw new NotImplementedException();
    }
}
