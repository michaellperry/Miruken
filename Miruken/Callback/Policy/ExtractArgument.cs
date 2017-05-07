namespace Miruken.Callback.Policy
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public class ExtractArgument<Cb, Res> : ArgumentRule
    {
        private readonly Func<Cb, Res> _extract;

        public ExtractArgument(Func<Cb, Res> extract)
        {
            if (extract == null)
                throw new ArgumentNullException(nameof(extract));
            _extract = extract;
        }

        public override bool Matches(
            ParameterInfo parameter, DefinitionAttribute attribute,
            IDictionary<string, Type> aliases)
        {
            var paramType = parameter.ParameterType;
            return typeof(Res).IsAssignableFrom(paramType);
        }

        public override object Resolve(object callback, IHandler composer)
        {
            return _extract((Cb)callback);
        }
    }
}