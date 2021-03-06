﻿namespace Miruken.Callback
{
    using System;

    public delegate bool ProviderDelegate(Inquiry inquiry, IHandler composer);

    public class Provider : Handler
    {
        private readonly ProviderDelegate _provider;

        public Provider(ProviderDelegate provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));
            _provider = provider;
        }

        protected override bool HandleCallback(
            object callback, bool greedy, IHandler composer)
        {
            var compose = callback as Composition;
            if (compose != null)
                callback = compose.Callback;

            var inquiry = callback as Inquiry;
            return inquiry != null && _provider(inquiry, composer);
        }
    }
}
