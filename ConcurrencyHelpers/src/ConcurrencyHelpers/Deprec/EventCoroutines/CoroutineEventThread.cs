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


using System.Diagnostics.CodeAnalysis;
using ConcurrencyHelpers.Containers;
using ConcurrencyHelpers.Utils;
using System;
using System.Collections.Generic;
namespace ConcurrencyHelpers.Coroutines
{

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventMessage
	{
		public object Data;
		public string EventName;
	}


	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventManagerCoroutine : Coroutine
	{
		private LockFreeQueue<CoroutineEventDescriptor> _eventDescriptorsQueue;
		private CounterInt64 _counter;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsDescriptors;

		public EventManagerCoroutine(LockFreeQueue<CoroutineEventDescriptor> eventQueue, CounterInt64 counter,
			Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> eventsDescriptors)
		{
			_eventsDescriptors = eventsDescriptors;
			_eventDescriptorsQueue = eventQueue;
			_counter = counter;
		}

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Running)
			{
				foreach (var newEvt in _eventDescriptorsQueue.Dequeue(100))
				{
					if (!_eventsDescriptors.ContainsKey(newEvt.EventName))
					{
						_eventsDescriptors.Add(newEvt.EventName, new Dictionary<Type, CoroutineEventDescriptor>());
					}
					if (!_eventsDescriptors[newEvt.EventName].ContainsKey(newEvt.EventArgType))
					{
						_eventsDescriptors[newEvt.EventName].Add(newEvt.EventArgType, newEvt);
					}
				}
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{

		}
	}

	[Obsolete]
	[ExcludeFromCodeCoverage]
	internal class EventDispatcher : Coroutine
	{
		public EventDispatcher(LockFreeQueue<EventMessage> events, CoroutineThread[] threads, CounterInt64 counter, Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> eventsList)
		{
			_events = events;
			_threads = threads;
			_counter = counter;
			_eventsList = eventsList;
		}

		private CounterInt64 _counter;
		private CoroutineThread[] _threads;
		private LockFreeQueue<EventMessage> _events;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsList;

		public override IEnumerable<Step> Run()
		{
			while (Thread.Status != CoroutineThreadStatus.Running)
			{
				foreach (var newEvt in _events.Dequeue(100))
				{
					if (_eventsList.ContainsKey(newEvt.EventName) && newEvt.Data!=null)
					{
						var type =newEvt.Data.GetType();
						if (_eventsList[newEvt.EventName].ContainsKey(type))
						{
							var handler = _eventsList[newEvt.EventName][type];
							var where = _counter.Value % _threads.Length;
							_threads[where].AddCoroutine(new ExecEventCoroutine(handler, newEvt));
						}
					}
				}
				yield return Step.Current;
			}
		}

		public override void OnError(Exception ex)
		{

		}
	}


	[Obsolete]
	[ExcludeFromCodeCoverage]
	public class CoroutineEventThread
	{
		private LockFreeQueue<EventMessage> _events;
		internal static CoroutineEventThread _eventThread;

		public static void Initialize(int threadsCount = 0, int cycleMaxMs = 10)
		{
			_eventThread = new CoroutineEventThread(threadsCount, cycleMaxMs);
		}

		private readonly int _threadsCount;
		private CoroutineThread[] _threads;
		private CoroutineThread _manager;
		private LockFreeQueue<CoroutineEventDescriptor> _eventQueue;
		private CounterInt64 _counter;
		private Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>> _eventsList;

		protected CoroutineEventThread(int threadsCount, int cycleMaxMs)
		{
			_eventQueue = new LockFreeQueue<CoroutineEventDescriptor>();
			_events = new LockFreeQueue<EventMessage>();
			_eventsList = new Dictionary<string, Dictionary<Type, CoroutineEventDescriptor>>(StringComparer.OrdinalIgnoreCase);
			_counter = new CounterInt64(0);
			if (threadsCount < 1) threadsCount = 1;
			_threadsCount = threadsCount;
			_threads = new CoroutineThread[_threadsCount];
			_manager = new CoroutineThread(1);
			_manager.AddCoroutine(new EventManagerCoroutine(_eventQueue, _counter, _eventsList));

			for (int i = 0; i < _threadsCount; i++)
			{
				_threads[i] = new CoroutineThread(cycleMaxMs);
			}
		}

		public static void AddEntryCoroutines(params ICoroutine[] coroutines)
		{
			_eventThread._manager.AddCoroutine(coroutines);
		}

		/* Action(EventThread,string callerEvent,T eventParameter */
		public static void RegisterEventHandler<T>(string eventName, Action<CoroutineEventThread, string, T> action)
		{
			var eventDescriptor = new CoroutineEventDescriptor(eventName,
				(thread, caller, par) =>
				{
					action(
						_eventThread, caller, (T)par);
				}
				, typeof(T));
			_eventThread._eventQueue.Enqueue(eventDescriptor);
		}

		public static void RegisterEventHandler<T>(string eventName, IEventHandler<T> eventhandler)
		{
			var eventDescriptor = new CoroutineEventDescriptor(eventName,
				(thread, caller, par) =>
				{
					try
					{
						eventhandler.Run(_eventThread, caller, (T)par);
						eventhandler.OnSuccess();
					}
					catch (Exception ex)
					{
						eventhandler.OnError(ex);
					}
				}
				, typeof(T));
			_eventThread._eventQueue.Enqueue(eventDescriptor);
		}

		public static void Publish<T>(string eventName, T eventArgs)
		{
			_eventThread._counter++;
			_eventThread._events.Enqueue(new EventMessage { Data = eventArgs, EventName = eventName });
		}

		public static void Start()
		{
			foreach (var th in _eventThread._threads)
			{
				th.Start();
			}
			_eventThread._manager.Start();
		}

		public static void Stop()
		{
			_eventThread._manager.Stop();
			foreach (var th in _eventThread._threads)
			{
				th.Stop();
			}
		}
	}


	[Obsolete]
	public interface IEventCoroutine
	{
		IEnumerable<CoroutineEventDescriptor> Initialize();
	}
}