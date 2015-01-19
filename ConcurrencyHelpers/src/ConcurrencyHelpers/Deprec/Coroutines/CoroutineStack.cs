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


using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrencyHelpers.Coroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	public class CoroutineStack
	{
		public CoroutineStack(IEnumerator<Step> root, ICoroutine rootCoroutine)
		{
			_stack = new List<EnumeratorWrapper>();
			_stack.Add(new EnumeratorWrapper { Enum = root });
			_rootCoroutine = rootCoroutine;
		}
		private ICoroutine _rootCoroutine;
		private readonly List<EnumeratorWrapper> _stack;
		private object _current;
		private CultureInfo _currentCulture = null;

		public bool MoveNext()
		{
			return CalculateMoveNext();
		}

		private bool CalculateMoveNext()
		{
			if (_stack.Count == 0) return false;
			var currentEnumerator = _stack[_stack.Count - 1];
			if (currentEnumerator.IsTask)
			{
				if (currentEnumerator.TaskCompleted == false)
				{
					if (currentEnumerator.TaskInstance.Status != TaskStatus.RanToCompletion
						&& currentEnumerator.TaskInstance.Status != TaskStatus.Faulted
						&& currentEnumerator.TaskInstance.Status != TaskStatus.Canceled)
					{
						_current = Step.Current;
						return true;
					}
				}
				_current = null;
				_stack.RemoveAt(_stack.Count - 1);
				if (currentEnumerator.TaskInstance.Exception != null)
				{
					throw new Exception("Sub Task exception", currentEnumerator.TaskInstance.Exception);
				}
				return true;
			}
			if (currentEnumerator.IsCoroutine)
			{
#if THE_OLD_VERSION
				if (currentEnumerator.SubCoroutine.ShouldTerminate == false)
				{
					_current = Step.Current;
					return true;
				}
				_current = null;
				_stack.RemoveAt(_stack.Count - 1);
				if (currentEnumerator.SubCoroutine.ExceptionThrown != null)
				{
					throw new Exception("Sub Coroutine exception", currentEnumerator.TaskInstance.Exception);
				}
				return true;
#else
				if (currentEnumerator.SubCoroutine.ShouldTerminate == true)
				{
					_stack.RemoveAt(_stack.Count - 1);
					if (currentEnumerator.SubCoroutine.ExceptionThrown != null)
					{
						throw new Exception("Sub Coroutine exception", currentEnumerator.SubCoroutine.ExceptionThrown);
					}
					return true;
				}
				else
				{
					_current = Step.Current;
					if (currentEnumerator.SubCoroutine.ExceptionThrown != null)
					{
						throw new Exception("Sub Coroutine exception", currentEnumerator.SubCoroutine.ExceptionThrown);
					}
					return true;
				}
#endif
			}

			CultureInfo prevCulture = Thread.CurrentThread.CurrentCulture;
			if (currentEnumerator.Culture != null)
			{
				Thread.CurrentThread.CurrentCulture = currentEnumerator.Culture;
			}
			else if (_currentCulture != null)
			{
				Thread.CurrentThread.CurrentCulture = _currentCulture;
			}

			var next = currentEnumerator.Enum.MoveNext();

			while (next == false && _stack.Count > 0)
			{
				_stack.RemoveAt(_stack.Count - 1);
				if (_stack.Count == 0)
				{
					Thread.CurrentThread.CurrentCulture = prevCulture;
					return false;
				}
				next = _stack[_stack.Count - 1].Enum.MoveNext();
			}
			if (next)
			{
				_current = CalculateCurrent();
			}
			_currentCulture = Thread.CurrentThread.CurrentCulture;
			Thread.CurrentThread.CurrentCulture = prevCulture;
			return next;
		}

		public object Current
		{
			get
			{
				return _current;
			}
		}

		private object CalculateCurrent()
		{
			if (_stack[_stack.Count - 1].Enum == null) return null;
			var current = _stack[_stack.Count - 1].Enum.Current as Step;
			if (current == null) return current;

			var enumerator = current.Data as EnumeratorWrapper;
			if (enumerator != null)
			{
				_stack.Add(enumerator);
			}
			else
			{
				if (current.HasData)
				{
					if (_stack[_stack.Count - 1].Result != null)
					{
						_stack[_stack.Count - 1].Result.RawData = current.Data;
					}
				}
				return current;
			}
			return current;
		}

		public void Reset()
		{
			foreach (var item in _stack)
			{
				if (item.IsTask)
				{
					item.TaskInstance.Dispose();
				}
				else
				{
					//item.Enum.Reset();
				}
			}
			_stack.Clear();
		}
	}
}