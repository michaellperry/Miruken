﻿namespace Miruken.Mediator
{
    using System;
    using Callback;
    using Concurrency;

    /// <summary>
    /// Miruken.Mediator imitates the behavior of the 'MediatR' package from Jimmy Bogard
    /// https://github.com/jbogard/MediatR
    /// </summary>
    public static class HandlerMediatorExtensions
    {
        public static Promise Send(this IHandler handler, IRequest request)
        {
            if (handler == null)
                return Promise.Empty;
            var command = new Command(request)
            {
                WantsAsync = true,
                Policy     = MediatesAttribute.Policy
            };
            if (!handler.Handle(command))
                return Promise.Rejected(new NotSupportedException(
                    $"{request.GetType()} not handled"));
            return command.IsAsync ? (Promise)command.Result : Promise.Empty;
        }

        public static Promise<Resp> Send<Resp>(
            this IHandler handler, IRequest<Resp> request)
        {
            if (handler == null)
                return Promise<Resp>.Empty;
            var command = new Command(request)
            {
                WantsAsync = true,
                Policy     = MediatesAttribute.Policy
            };
            if (!handler.Handle(command))
                return Promise<Resp>.Rejected(new NotSupportedException(
                    $"{request.GetType()} not handled"));
            var result  = command.Result;
            var promise = command.IsAsync
                        ? (Promise)result
                        : Promise.Resolved(result);
            return (Promise<Resp>)promise.Coerce(typeof(Promise<Resp>));
        }

        public static Promise Publish(this IHandler handler, INotification notification)
        {
            if (handler == null)
                return Promise.Empty;
            var command = new Command(notification, true)
            {
                WantsAsync = true,
                Policy     = MediatesAttribute.Policy
            };
            return handler.Handle(command, true) && command.IsAsync
                 ? (Promise)command.Result
                 : Promise.Empty;
        }
    }
}