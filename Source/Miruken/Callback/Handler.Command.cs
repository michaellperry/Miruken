﻿namespace Miruken.Callback
{
    using System;
    using System.Linq;
    using Concurrency;

    public static class HandlerCommandExtensions
    {
        public static void Command(this IHandler handler, object callback)
        {
            if (handler == null) return;
            var command = new Command(callback);
            if (!handler.Handle(command))
                throw new NotSupportedException($"{callback.GetType()} not handled");
            if (command.IsAsync)
                ((Promise)command.Result).Wait();
        }

        public static Promise CommandAsync(this IHandler handler, object callback)
        {
            if (handler == null) return Promise.Empty;
            var command = new Command(callback) { WantsAsync = true };
            return handler.Handle(command)
                 ? (Promise)command.Result
                 : Promise.Rejected(new NotSupportedException(
                      $"{callback.GetType()} not handled"));
        }

        public static Result Command<Result>(this IHandler handler, object callback)
        {
            if (handler == null) return default(Result);
            var command = new Command(callback);
            if (!handler.Handle(command))
                throw new NotSupportedException($"{callback.GetType()} not handled");
            var result = command.Result;
            return command.IsAsync 
                 ? (Result)((Promise)result).Wait()
                 : (Result)result;
        }

        public static Promise<Result> CommandAsync<Result>(
            this IHandler handler, object callback)
        {
            if (handler == null)
                return Promise<Result>.Empty;
            var command = new Command(callback) { WantsAsync = true };
            if (!handler.Handle(command))
                throw new NotSupportedException($"{callback.GetType()} not handled");
            var promise  = (Promise)command.Result;
            return (Promise<Result>)promise.Coerce(typeof(Promise<Result>));
        }

        public static void CommandAll(this IHandler handler, object callback)
        {
            if (handler == null) return;
            if (!handler.Handle(new Command(callback, true), true))
                throw new NotSupportedException($"{callback.GetType()} not handled");
        }

        public static Promise CommandAllAsync(this IHandler handler, object callback)
        {
            if (handler == null) return Promise.Empty;
            var command = new Command(callback, true) { WantsAsync = true };
            if (!handler.Handle(command, true))
                return Promise.Rejected(new NotSupportedException(
                    $"{callback.GetType()} not handled"));
            return (Promise)command.Result;
        }

        public static Result[] CommandAll<Result>(
             this IHandler handler, object callback)
        {
            if (handler == null)
                return Array.Empty<Result>();
            var command = new Command(callback, true) { WantsAsync = true };
            if (!handler.Handle(command, true))
                throw new NotSupportedException(
                    $"{callback.GetType()} not handled");
            var result = command.Result;
            return command.IsAsync
                 ? ((object[])((Promise)result).Wait()).Cast<Result>().ToArray()
                 : ((object[])result).Cast<Result>().ToArray();
        }

        public static Promise<Result[]> CommandAllAsync<Result>(
            this IHandler handler, object callback)
        {
            if (handler == null)
                return Promise<Result[]>.Empty;
            var command = new Command(callback, true) { WantsAsync = true };
            if (!handler.Handle(command, true))
                return Promise<Result[]>.Rejected(new NotSupportedException(
                    $"{callback.GetType()} not handled"));
            var promise = (Promise)command.Result;
            return promise.Then((results, s) => ((object[])results)
                .Cast<Result>().ToArray());
        }
    }
}
