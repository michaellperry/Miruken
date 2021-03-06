﻿using System;
using System.Threading;

namespace Miruken.Concurrency
{
    public abstract class AbstractAsyncResult : IAsyncResult
    {
        private int _completed;
        private readonly AsyncCallback _callback;
        private bool _completedSynchronously;
        private bool _endCalled;
        private object _waitEvent;
        protected Exception _exception;
        protected object _result;

        protected AbstractAsyncResult()
        {
        }

        protected AbstractAsyncResult(AsyncCallback callback, object state)
        {
            AsyncState = state;
            _callback  = callback;
        }

        public object AsyncState { get; protected set; }

        public bool IsCompleted => _completed != 0;

        public bool CompletedSynchronously
        {
            get { return _completedSynchronously; }
            protected set { _completedSynchronously = value; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                var isCompleted = _completed;

                if (_waitEvent == null)
                {
                    Interlocked.CompareExchange(ref _waitEvent,
                        new ManualResetEvent(isCompleted != 0), null);
                }

                var ev = (ManualResetEvent)_waitEvent;

                if ((isCompleted == 0) && (_completed != 0))
                    ev.Set();

                return ev;
            }
        }

        public static object End(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException(nameof(asyncResult));

            var result = asyncResult as AbstractAsyncResult;

            if (result == null)
                throw new ArgumentException("Unrecognized IAsyncResult", nameof(asyncResult));

            if (result._endCalled)
                throw new InvalidOperationException("IAsyncResult has already ended");

            result._endCalled = true;

            if (result._completed == 0)
                result.AsyncWaitHandle.WaitOne();

            if (result._exception != null)
                throw result._exception;

            return result._result;
        }

        protected bool Complete(object result, bool synchronously, Action action = null)
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0) 
                return false;
            _result = result;
            Complete(synchronously, action);
            return true;
        }

        protected bool Complete(Exception exception, bool synchronously, Action action = null)
        {
            if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
                return false;
            _exception = exception;
            Complete(synchronously, action);
            return true;
        }

        private void Complete(bool synchronously, Action action = null)
        {
            _completedSynchronously = synchronously;
            action?.Invoke();
            ((ManualResetEvent) _waitEvent)?.Set();
            _callback?.Invoke(this);
        }
    }
}
