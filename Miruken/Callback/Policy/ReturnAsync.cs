﻿namespace Miruken.Callback.Policy
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Concurrency;
    using Infrastructure;

    public class ReturnAsync : ReturnRuleDecorator
    {
        private readonly bool _required;

        public ReturnAsync(ReturnRule rule, bool required = true) 
            : base(rule)
        {
            _required = required;
        }

        public override bool Matches(
            Type returnType, ParameterInfo[] parameters, 
            DefinitionAttribute attribute)
        {
            if (returnType == typeof(void))
                return !_required && base.Matches(returnType, parameters, attribute);
            if (typeof(Promise).IsAssignableFrom(returnType))
            {
                var promiseType = returnType.GetOpenTypeConformance(typeof(Promise<>));
                returnType = promiseType != null
                           ? promiseType.GetGenericArguments()[0]
                           : typeof(object);
            }
            else if (typeof(Task).IsAssignableFrom(returnType))
            {
                var taskType = returnType.GetOpenTypeConformance(typeof(Task<>));
                returnType = taskType != null
                           ? taskType.GetGenericArguments()[0]
                           : typeof(object);
            }
            else if (_required)
                return false;
            return base.Matches(returnType, parameters, attribute);
        }
    }
}