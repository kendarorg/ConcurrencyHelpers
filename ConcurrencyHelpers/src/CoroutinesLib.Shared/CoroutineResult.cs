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


using System.Reflection;
using System.Threading;
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
		static CoroutineResult()
		{
			var type = Type.GetType("CoroutinesLib.RunnerFactory,CoroutinesLib");
			if (type != null)
			{
				_createMethod = type.GetMethod("Create", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			}
		}

		public static Task WaitForCoroutine(ICoroutineThread toRun)
		{
			Exception problem = null;
			var onError = new Action<Exception>((ex) =>
			{
				problem = ex;
			});
			var cmanager = (ICoroutinesManager)_createMethod.Invoke(null, new object[] { });
			cmanager.StartCoroutine(toRun, onError);
			var waitSlim = new ManualResetEventSlim(false);
			var task = new Task(() =>
			{
				while ((long)RunningStatus.NotRunning > (long)toRun.Status)
				{
					waitSlim.Wait(50);
				}
				while (!toRun.Status.Is(RunningStatus.NotRunning))
				{
					waitSlim.Wait(50);
				}
				if (problem != null)
				{
					throw new Exception("Error running subtask", problem);
				}
			}, TaskCreationOptions.AttachedToParent);
			task.Start();
			return task;
		}


		public static IMessageBus MessageBus { get; set; }

		public static CoroutineResult _wait = new ConcreteCoroutineResult(ResultType.Wait);
		private static MethodInfo _createMethod;

		public bool ShouldWait
		{
			get { return ResultType == ResultType.Wait; }
		}

		public static ICoroutineResult Enumerable(IEnumerable<ICoroutineResult> enumerable, string name)
		{
			var result = new CoroutineResultEnumerator(string.Format("Enumerator for '{0}'.", name), enumerable.GetEnumerator());
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
