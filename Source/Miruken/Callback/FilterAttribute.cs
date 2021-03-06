﻿namespace Miruken.Callback
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Infrastructure;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method,
        AllowMultiple = true, Inherited = false)]
    public class FilterAttribute : Attribute, IFilterProvider
    {
        private static readonly ConcurrentDictionary<MemberInfo, FilterAttribute[]>
            _filters = new ConcurrentDictionary<MemberInfo, FilterAttribute[]>();

        public FilterAttribute(params Type[] filterTypes)
        {
            ValidateFilters(filterTypes);
            FilterTypes = filterTypes;
        }

        public Type[] FilterTypes { get; }
        public bool   Many        { get; set; }
        public int?   Order       { get; set; }

        public IEnumerable<IFilter> GetFilters(
            Type callbackType, Type logicalResultType, IHandler composer)
        {
            var filters = FilterTypes
                .Select(f => CloseFilterType(f, callbackType, logicalResultType))
                .SelectMany(filterType => Many
                    ? composer.Stop().ResolveAll(filterType)
                    : new[] {composer.Stop().Resolve(filterType)})
                .OfType<IFilter>();

            var relativeOrder = Order;
            foreach (var filter in filters)
            {
                filter.Order = relativeOrder.HasValue ? relativeOrder++ : null;
                yield return filter;
            }
        }

        public static FilterAttribute[] GetFilters(MemberInfo member, bool inherit = false)
        {
            return _filters.GetOrAdd(member, t => Normalize(
                    (FilterAttribute[])member.GetCustomAttributes(
                        typeof(FilterAttribute), inherit)));
        }

        protected virtual void VerifyFilterType(Type filterType)
        {          
        }

        private static FilterAttribute[] Normalize(FilterAttribute[] filters)
        {
            return filters.Length > 0 ? filters : Array.Empty<FilterAttribute>();
        }

        private static Type CloseFilterType(Type filterType, Type callbackType,
            Type logicalResultType)
        {
            if (!filterType.IsGenericTypeDefinition)
                return filterType;
            var openFilterType = typeof(IFilter<,>);
            if (filterType == openFilterType)
                return filterType.MakeGenericType(callbackType, logicalResultType);
            var conformance  = filterType.GetOpenTypeConformance(openFilterType);
            var inferredArgs = conformance.GetGenericArguments();
            return filterType.MakeGenericType(inferredArgs.Select((arg, i) =>
            {
                if (!arg.ContainsGenericParameters) return null;
                return i == 0 ? callbackType : logicalResultType;
            }).Where(arg => arg != null).ToArray());
        }

        private void ValidateFilters(Type[] filterTypes)
        {
            if (filterTypes == null)
                throw new ArgumentNullException(nameof(filterTypes));
            var anyFilter     = typeof(IFilter<,>);
            var dynamicFilter = typeof(IDynamicFilter);
            foreach (var filterType in filterTypes)
            {
                if (filterType == null)
                    throw new ArgumentException("Filter types cannot be nulll");  
                if (filterType == anyFilter ||
                    dynamicFilter.IsAssignableFrom(filterType)) continue;
                var conformance = filterType.GetOpenTypeConformance(anyFilter);
                if (conformance == null)
                    throw new ArgumentException($"{filterType.FullName} does not conform to IFilter<,>");
                if (filterType.IsGenericTypeDefinition && !conformance.ContainsGenericParameters)
                    throw new ArgumentException($"{filterType.FullName} generic args cannot be inferred");
                VerifyFilterType(filterType);
            }
        }
    }
}
