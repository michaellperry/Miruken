﻿namespace Miruken.Concurrency
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITaskConversion
    {
        Task ToTask();
    }

    public partial class Promise : ITaskConversion
    {
        public Promise Then(ResolveCallback<Task> then)
        {
            return Then(then != null ? (ResolveCallback<Promise>)(
                (r, s) => then(r, s).ToPromise()) : null);
        }

        public Promise<R> Then<R>(ResolveCallback<Task<R>> then)
        {
            return Then(then != null ? (ResolveCallback<Promise<R>>)(
                (r, s) => then(r, s).ToPromise()) : null);
        }

        public Promise Catch(RejectCallback<Task> fail)
        {
            return Catch(fail != null ? (RejectCallback<Promise>)(
                (ex, s) => fail(ex, s).ToPromise()) : null);
        }

        public Task<object> ToTask()
        {
            var tcs = new TaskCompletionSource<object>();
            Then((result, s) => tcs.SetResult(result))
            .Catch((exception, s) => tcs.SetException(exception))
            .Cancelled(cancel => tcs.SetCanceled());
            return tcs.Task;
        }

        public TaskAwaiter<object> GetAwaiter()
        {
            return ToTask().GetAwaiter();
        }

        Task ITaskConversion.ToTask()
        {
            return ToTaskInternal();
        }

        protected virtual Task ToTaskInternal()
        {
            return ToTask();
        }

        public static implicit operator Task<object>(Promise promise)
        {
            return promise.ToTask();
        }

        public static implicit operator Promise(Task task)
        {
            return task.ToPromise();
        }
    }

    public partial class Promise<T>
    {
        public Promise(Task<T> task,
            CancellationTokenSource cancellationTokenSource = null,
            ChildCancelMode mode = ChildCancelMode.All)
            : this(mode, (resolve, reject, onCancel) =>
            {
                if (cancellationTokenSource != null)
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        reject(new CancelledException("Task cancellation requested"), false);
                        return;
                    }
                    onCancel(cancellationTokenSource.Cancel);
                }
                if (!task.IsCompleted)
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            reject(ExtractException(t), false);
                        else if (t.IsCanceled)
                            reject(new CancelledException("Task was cancelled"), false);
                        else
                            resolve(t.Result, false);
                    });
                else if (task.IsFaulted)
                    reject(ExtractException(task), true);
                else if (task.IsCanceled)
                    reject(new CancelledException("Task was cancelled"), true);
                else
                    resolve(task.Result, true);
            })
        {
            cancellationTokenSource?.Token.Register(Cancel, false);
        }

        public Promise(Task<T> task,
            CancellationToken cancellationToken = default(CancellationToken),
            ChildCancelMode mode = ChildCancelMode.All)
            : this(task, null, mode)
        {
            cancellationToken.Register(Cancel, false);
        }

        public Promise<R> Then<R>(ResolveCallbackT<Task<R>> then)
        {
            return Then(then, null);
        }

        public Promise<R> Then<R>(ResolveCallbackT<Task<R>> then, RejectCallback<Task<R>> fail)
        {
            return Then(then != null ? (ResolveCallbackT<Promise<R>>)(
                (r, s) => then(r, s).ToPromise()) : null, fail != null 
                ? (RejectCallback<Promise<R>>)((ex, s) => fail(ex, s).ToPromise()) 
                : null);
        }

        public Promise<R> Catch<R>(RejectCallback<Task<R>> fail)
        {
            return Catch(fail != null
                 ? (RejectCallback<Promise<R>>) ((ex, s) => fail(ex, s).ToPromise())
                 : null);
        }

        public new Task<T> ToTask()
        {
            var tcs = new TaskCompletionSource<T>();
            Then((result, s) => tcs.SetResult(result))
            .Catch((exception, s) => tcs.SetException(exception))
            .Cancelled(cancel => tcs.SetCanceled());
            return tcs.Task;
        }

        public new TaskAwaiter<T> GetAwaiter()
        {
            return ToTask().GetAwaiter();
        }

        protected override Task ToTaskInternal()
        {
            return ToTask();
        }

        public static implicit operator Task<T>(Promise<T> promise)
        {
            return promise.ToTask();
        }

        public static implicit operator Task(Promise<T> promise)
        {
            return promise.ToTask();
        }

        public static implicit operator Promise<T>(Task<T> task)
        {
            return task.ToPromise();
        }

        private static Exception ExtractException(Task task)
        {
            return task.Exception?.Flatten().InnerException;
        }
    }
}
