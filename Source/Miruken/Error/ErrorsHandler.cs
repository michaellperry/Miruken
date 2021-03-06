﻿using System;
using Miruken.Callback;
using Miruken.Concurrency;
using static Miruken.Protocol;

namespace Miruken.Error
{
    public class ErrorsHandler : Handler, IErrors
    {
        public virtual bool HandleException(Exception exception, object context)
        {
            Console.WriteLine(exception);
            return true;
        }
    }

    public static class ErrorsExtensions
    {
        public static IHandler Recover(this IHandler handler)
        {
            return Recover(handler, null);
        }

        public static IHandler Recover(this IHandler handler, object context)
        {
            return handler.Filter((callback, composer, proceed) => {
                if (callback is Composition)
                    return proceed();
                try
                {
                    var handled = proceed();
                    if (handled)
                    {
                        var cb = callback as ICallback;
                        var promise = cb?.Result as Promise;
                        if (promise != null)
                        {
                            cb.Result = promise.Catch((ex, s) => 
                            {
                                if (ex is CancelledException)
                                    return Promise.Rejected(ex);
                                return  P<IErrors>(composer).HandleException(ex, context)
                                     ? Promise.Rejected(new RejectedException(cb))
                                     : Promise.Rejected(ex);
                            }).Coerce(cb.ResultType);
                        }
                    }
                    return handled;
                }
                catch (Exception exception)
                {
                    if (exception is CancelledException) return true;
                    var handled = P<IErrors>(composer).HandleException(exception, context);
                    if (!handled) throw;
                    return true;
                }
            });
        }         
    }
}
