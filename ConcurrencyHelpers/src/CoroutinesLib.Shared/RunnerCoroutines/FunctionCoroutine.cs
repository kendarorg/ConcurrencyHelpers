// ===========================================================
// Copyright (C) 2014-2015 Kendar.org
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
// files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
// is furnished to do so, subject to the following conditions:
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS 
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF 
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ===========================================================


using CoroutinesLib.Shared.Enums;
using CoroutinesLib.Shared.Exceptions;
using CoroutinesLib.Shared.Logging;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoroutinesLib.Shared.RunnerCoroutines
{
	internal class FunctionCoroutine : ICoroutineThread,ILoggable
	{
		public string InstanceName { get; set; }
		private readonly Action<Exception> _onError;
		private Func<ICoroutineResult, bool> _onEach;
		private readonly DateTime _timeout = DateTime.MaxValue;
		//private Func<ICoroutineResult> func;
		private readonly IEnumerator _enumerator;
		private readonly Action<ICoroutineResult> _onCompleteWithResult;
		private Action _onCompleteWithoutResult;
		public RunningStatus Status { get; private set; }
		public object Result { get; set; }
		public ICoroutinesManager CoroutinesManager { get; set; }

		public void OnDestroy()
		{

		}

		public Guid Id { get; private set; }
		public Guid StackId { get; set; }

		public FunctionCoroutine(IEnumerator enumerator, Action<ICoroutineResult> onComplete, Action<Exception> onError, DateTime timeout, Func<ICoroutineResult, bool> onEach = null)
		{
			_onEach = onEach;
			_enumerator = enumerator;
			_onCompleteWithResult = onComplete;
			_onError = onError;
			_timeout = timeout;
		}

		public FunctionCoroutine(IEnumerator enumerator, Action onComplete, Action<Exception> onError, DateTime timeout, Func<ICoroutineResult, bool> onEach = null)
		{
			_onEach = onEach;
			_enumerator = enumerator;
			_onCompleteWithoutResult = onComplete;
			_onError = onError;
			_timeout = timeout;
		}

		public IEnumerable<ICoroutineResult> Execute()
		{
			bool shouldRunCompleteWithoutResult = true;
			while (_enumerator.MoveNext())
			{
				var now = DateTime.UtcNow;
				if (now > _timeout)
				{
					throw new CoroutineTimeoutException(now - _timeout);
				}
				var item = (ICoroutineResult)_enumerator.Current;
				if (item.ResultType == ResultType.Return)
				{
					if (_onCompleteWithResult != null) _onCompleteWithResult(item);
					shouldRunCompleteWithoutResult = false;
					break;
				}
				if (item.ResultType == ResultType.YieldReturn)
				{
					if (_onEach != null)if (!_onEach(item)) break;
				}
				if (item.ResultType == ResultType.YieldBreak)
				{
					_enumerator.MoveNext();
					if (_onCompleteWithoutResult != null) _onCompleteWithoutResult();
					shouldRunCompleteWithoutResult = false;
					break;
				}
				if (item.ResultType == ResultType.FluentBuilder)
				{
					yield return item;
				}
				else
				{
					yield return CoroutineResult.Wait;
				}
			}
			if (shouldRunCompleteWithoutResult)
			{
				if (_onCompleteWithoutResult != null) _onCompleteWithoutResult();
			}
			
			Status = RunningStatus.Stopped;
		}

		public bool OnError(Exception exception)
		{
			if (_onError != null)
			{
				_onError(exception);
				return true;
			}
			throw new CoroutineFunctionCallException("Task exception in '" + InstanceName + "'", exception);
		}

		public ILogger Log { get; set; }
	}
}
