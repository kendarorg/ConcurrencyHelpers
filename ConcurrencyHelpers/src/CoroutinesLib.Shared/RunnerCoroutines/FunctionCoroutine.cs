// ===========================================================
// Copyright (c) 2014-2015, Enrico Da Ros/kendar.org
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
// 
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
