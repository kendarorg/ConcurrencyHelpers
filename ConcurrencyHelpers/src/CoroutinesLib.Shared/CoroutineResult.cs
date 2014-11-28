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


using CoroutinesLib.Shared.Enumerators;
using CoroutinesLib.Shared.Enums;
using CoroutinesLib.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoroutinesLib.Shared
{
	public abstract class CoroutineResult : ICoroutineResult
	{
		public static IMessageBus MessageBus { get; set; }

		public static CoroutineResult _wait = new ConcreteCoroutineResult(ResultType.Wait);

		public bool ShouldWait
		{
			get { return ResultType == ResultType.Wait; }
		}

		public static ICoroutineResult Enumerable(IEnumerable<ICoroutineResult> enumerable, string name)
		{
			var result = new CoroutineResultEnumerator(string.Format("Enumerator for '{0}'.", name),enumerable.GetEnumerator());
			return result;
		}

		/// <summary>
		/// Tells the caller to wait for further result
		/// </summary>
		public static CoroutineResult Wait { get { return _wait; } }

		/// <summary>
		/// Create a "real" return. No current function code will be exectued after this point.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CoroutineResult Return(object value)
		{
			return new ConcreteCoroutineResult(ResultType.Return, value);
		}

		/// <summary>
		/// Create a "yield return" like return value.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static CoroutineResult YieldReturn(object value)
		{
			return new ConcreteCoroutineResult(ResultType.YieldReturn, value);
		}

		/// <summary>
		/// Like "yield break" stops the execution of the function.
		/// </summary>
		/// <returns></returns>
		public static CoroutineResult YieldBreak()
		{
			return new ConcreteCoroutineResult(ResultType.YieldBreak);
		}

		/// <summary>
		/// The type of result of the coroutine
		/// </summary>
		public ResultType ResultType { get; protected set; }

		/// <summary>
		/// The result content
		/// </summary>
		public object Result { get; protected set; }

		private static IEnumerable<ICoroutineResult> WaitForMessageSent(IMessage message)
		{
			var timeout = message.Timeout;
			var now = DateTime.UtcNow;
			while (now < timeout)
			{
				if (message.IsCompleted.Value)
				{
					if (message.Response != null)
					{
						yield return Return(message.Response);
					}
				}
				yield return Wait;
				now = DateTime.UtcNow;
			}
			throw new CoroutineTimeoutException((long)(now - timeout).TotalMilliseconds);
		}

		public static IOnResponseMessage SendMessage(IMessage message)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.Message,
				Message = message,
				MessageBus = MessageBus
			};
			return result;
		}

		public static ICoroutineResult PostMessage(IMessage message)
		{
			MessageBus.Post(message);
			return Wait;
		}

		public static IOnFunctionResult RunAndGetResult(IEnumerable<ICoroutineResult> waitForItem, string name = null)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.FunctionWithResult,
				Enumerator = waitForItem.GetEnumerator(),
				InstanceName = name
			};
			return result;
		}

		public static IOnComplete Run(IEnumerable<ICoroutineResult> waitForItem, string name = null)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.FunctionWithoutResult,
				Enumerator = waitForItem.GetEnumerator(),
				InstanceName = name
			};
			return result;
		}

		public static IForEachItem ForEachItem(IEnumerable<ICoroutineResult> waitForItem, string name = null)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.ForeachFunction,
				Enumerator = waitForItem.GetEnumerator(),
				InstanceName = name
			};
			return result;
		}

		public static IOnComplete RunTask(Task taskToRun, string name = null)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.FunctionWithoutResult,
				Task = taskToRun,
				Enumerator = RunTaskFunction(taskToRun).GetEnumerator(),
				InstanceName = name
			};
			return result;
		}

		private static IEnumerable<ICoroutineResult> RunTaskFunction(Task task)
		{
			if (!task.IsCompleted && !task.IsFaulted && !task.IsCanceled &&
				task.Status != TaskStatus.Running &&
				//task.Status != TaskStatus.WaitingForActivation &&
				task.Status != TaskStatus.WaitingForChildrenToComplete &&
				//task.Status != TaskStatus.WaitingToRun &&
				task.Status != TaskStatus.RanToCompletion)
			{
				// ReSharper disable EmptyGeneralCatchClause
				try
				{
					task.Start();
				}
				catch (Exception)
				{
					
				}
				// ReSharper restore EmptyGeneralCatchClause
			}
			while (!task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
			{
				yield return Wait;
			}
			if (task.IsFaulted)
			{
				throw new CoroutineTaskException("Error executing task", task.Exception);
			}
		}

		public static IOnComplete RunCoroutine(ICoroutineThread coroutine, string name = null)
		{
			var result = new FluentResultBuilder
			{
				Type = FluentResultType.CoroutineFunction,
				Coroutine = coroutine,
				InstanceName = name
			};
			return result;
		}

	}
}
