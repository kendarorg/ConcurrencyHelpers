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