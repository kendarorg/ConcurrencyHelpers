using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CoroutinesLib.Shared.Enums;
using CoroutinesLib.Shared.Exceptions;
using CoroutinesLib.Shared.Logging;
using CoroutinesLib.Shared.RunnerCoroutines;

namespace CoroutinesLib.Shared
{
	public class FluentResultBuilder : IOnResponseMessage, IOnFunctionResult, IForEachItem, IOnComplete, INamedItem, ILoggable
	{
		private ICoroutineThread _coroutine;

		public FluentResultBuilder()
		{
			OnCompleteWithoutResultDo = () => { };
			Timeout = TimeSpan.FromMilliseconds(500);
		}
		public object Result { get; private set; }
		public IWaitOrCoroutine OnError(Func<Exception, bool> onError)
		{
			Type |= FluentResultType.WithOnError;
			OnErrorDo = onError;
			return this;
		}

		public IOnError WithTimeout(TimeSpan timeout)
		{
			Type |= FluentResultType.WithTimeout;
			Timeout = timeout;
			return this;
		}

		public IOnError WithTimeout(int milliseconds)
		{
			return WithTimeout(TimeSpan.FromMilliseconds(milliseconds));
		}

		public TimeSpan Timeout { get; set; }

		public FluentResultType Type { get; set; }
		public IOneTimeMessage OneTimeMessage { get; set; }
		public Action<IMessage> OnMessageResponse { get; set; }
		public Func<Exception, bool> OnErrorDo { get; set; }

		public ResultType ResultType
		{
			get { return ResultType.FluentBuilder; }
		}

		public IMessageBus MessageBus { get; set; }
		public IEnumerator<ICoroutineResult> Enumerator { get; set; }

		public IWithTimeout OnResponse(Action<IMessage> onResponse)
		{
			OnComplete(onResponse);
			return this;
		}

		public IWithTimeout OnComplete<T>(Action<T> onComplete)
		{
			OnCompleteWithResults = a => onComplete((T)a.Result);
			Type |= FluentResultType.WaitForFunctionReturn;
			return this;
		}

		public IWithTimeout OnComplete(Action<ICoroutineResult> onComplete)
		{
			OnCompleteWithResults = onComplete;
			Type |= FluentResultType.WaitForFunctionReturn;
			return this;
		}

		public Action<ICoroutineResult> OnCompleteWithResults { get; set; }
		public IOnComplete Do<T>(Func<T, bool> onEach)
		{
			OnEachItem = (a) =>
			{
				if (a.ResultType == ResultType.YieldReturn)
				{
					return onEach((T)a.Result);
				}
				return true;
			};
			Type |= FluentResultType.WaitForFunctionReturn;
			return this;
		}

		public IOnComplete Do(Func<ICoroutineResult, bool> onEach)
		{
			OnEachItem = (a) =>
			{
				if (a.ResultType == ResultType.YieldReturn)
				{
					return onEach(a);
				}
				return true;
			};
			Type |= FluentResultType.WaitForFunctionReturn;
			return this;
		}

		public Func<ICoroutineResult, bool> OnEachItem { get; set; }
		public IWithTimeout OnComplete(Action onComplete)
		{
			OnCompleteWithoutResultDo = onComplete;
			Type |= FluentResultType.WaitForCompletion;
			return this;
		}

		public Action OnCompleteWithoutResultDo { get; set; }
		public Task Task { get; set; }

		private static IEnumerable<ICoroutineResult> CoroutineWaiter(ICoroutineThread coroutine)
		{
			while (!coroutine.Status.Is(RunningStatus.NotRunning))
			{
				yield return CoroutineResult.Wait;
			}
		}
		public ICoroutineThread Coroutine
		{
			get { return _coroutine; }
			set
			{
				_coroutine = value;
				Enumerator = _coroutine.Execute().GetEnumerator();
			}
		}

		public IMessage Message { get; set; }

		public ICoroutineResult AndWait()
		{
			Type |= FluentResultType.Waiting;
			return this;
		}



		public ICoroutineThread AsCoroutine()
		{
			FunctionCoroutine result = null;
			Type |= FluentResultType.AsCoroutine;
			Type = Type & (~FluentResultType.Waiting);
			var timeout = DateTime.UtcNow + Timeout;
			if (Type.HasFlag(FluentResultType.FunctionWithResult))
			{
				result = new FunctionCoroutine(Enumerator, (a) => OnCompleteWithResults(a), (e) => OnErrorDo(e), timeout);
			}
			else if (Type.HasFlag(FluentResultType.ForeachFunction))
			{
				result = new FunctionCoroutine(Enumerator, () => OnCompleteWithoutResultDo(), (e) => OnErrorDo(e), timeout,
					(a) => OnEachItem(a));

			}
			else
			{
				result = new FunctionCoroutine(Enumerator, () => OnCompleteWithoutResultDo(), (e) => OnErrorDo(e), timeout);
			}
			result.InstanceName = InstanceName;
			return result;
		}

		public IEnumerable<ICoroutineResult> RunEnumerator()
		{
			var moveNext = true;
			var timeout = DateTime.UtcNow + Timeout;
			while (moveNext)
			{
				ICoroutineResult current;
				try
				{
					if (DateTime.UtcNow > timeout)
					{
						throw new CoroutineTimeoutException(string.Format("Timeout on '{0}'", InstanceName));
					}
					moveNext = Enumerator.MoveNext();
					if (!moveNext)
					{
						if (_coroutine != null)
						{
							_coroutine.OnDestroy();
							_coroutine = null;
						}
						break;
					}
					current = Enumerator.Current;
					if (current.ResultType == ResultType.Return)
					{
						if (OnCompleteWithResults != null) OnCompleteWithResults(current);
						break;
					}
					if (current.ResultType == ResultType.YieldReturn)
					{
						moveNext = OnEachItem == null || OnEachItem(current);
						current = CoroutineResult.Wait;
					}
					else if (current.ResultType == ResultType.YieldBreak)
					{
						Enumerator.MoveNext();
						break;
					}
				}
				catch (Exception ex)
				{
					if (OnErrorDo != null)
					{
						if (OnErrorDo(ex))
						{
							if (_coroutine != null)
							{
								_coroutine.OnDestroy();
								_coroutine = null;
							}
							break;
						}
					}
					if (_coroutine != null)
					{
						_coroutine.OnDestroy();
						_coroutine = null;
					}
					throw new CoroutineTimeoutException("Error running ", ex);
				}
				yield return current;
			}
			if (OnCompleteWithoutResultDo != null)
			{
				try
				{
					OnCompleteWithoutResultDo();
				}
				catch (Exception ex)
				{
					if (OnErrorDo != null) OnErrorDo(ex);
				}
			}
		}

		public string InstanceName { get; set; }
		public ILogger Log { get; set; }

		
	}
}