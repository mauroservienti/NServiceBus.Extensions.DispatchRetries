﻿using NServiceBus.AcceptanceTesting.Support;

namespace NServiceBus.AttributeRouting.AcceptanceTests
{
    public static class ConfigureExtensions
    {
        public static void RegisterComponentsAndInheritanceHierarchy(this EndpointConfiguration builder, RunDescriptor runDescriptor)
        {
            builder.RegisterComponents(r => 
            {
                var type = runDescriptor.ScenarioContext.GetType();
                while (type != typeof(object))
                {
                    r.RegisterSingleton(type, runDescriptor.ScenarioContext);
                    type = type.BaseType;
                }
            });
        }
    }
}
