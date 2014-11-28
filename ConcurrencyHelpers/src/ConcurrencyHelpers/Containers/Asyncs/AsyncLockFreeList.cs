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
using System.Collections;
using System.Collections.Generic;
using ConcurrencyHelpers.Interfaces;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.Containers.Asyncs
{
	public class AsyncLockFreeList<T> : IList<T>, IDisposable
	{
		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly ITimer _timer;
		private readonly LockFreeItem<List<T>> _list;
		private readonly List<T> _trustedSource;
		private readonly LockFreeQueue<AsyncCollectionRequest<T>> _requestsQueue;
		public ConcurrentInt64 MaxMessagesPerCycle { get; private set; }
		private ConcurrentInt64 _opverlappingChecker;

		public ITimer Timer
		{
			get
			{
				return _timer;
			}
		}

		public AsyncLockFreeList(ITimer timer = null) :
			this(new List<T>(), timer)
		{

		}

		public AsyncLockFreeList(IEnumerable<T> dictionary, ITimer timer = null)
		{
			MaxMessagesPerCycle = new ConcurrentInt64(100);
			_opverlappingChecker = new ConcurrentInt64();
			_timer = timer ?? new SystemTimer(10, 10);
			_list = new LockFreeItem<List<T>>(new List<T>(dictionary));
			_trustedSource = new List<T>();
			_requestsQueue = new LockFreeQueue<AsyncCollectionRequest<T>>();
			_timer.Elapsed += OnSynchronize;
			_timer.Start();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return new AsyncLockFreeEnumerator<T>(new List<T>(_list.Data));
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Data = item,
				EventType = AsyncCollectionEventType.Add
			});
		}

		public void Clear()
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Data = default(T),
				EventType = AsyncCollectionEventType.Clear
			});
		}

		public bool Contains(T item)
		{
			var data = _list.Data;
			return data.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			var data = _list.Data;
			data.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Data = item,
				EventType = AsyncCollectionEventType.Remove
			});
			return true;
		}

		public int Count { get { return _list.Data.Count; } }
		public bool IsReadOnly { get { return false; } }

		public int IndexOf(T item)
		{
			var data = _list.Data;
			return data.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Index = index,
				Data = item,
				EventType = AsyncCollectionEventType.TryInsert
			});
		}

		private void Update(int index, T item)
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Index = index,
				Data = item,
				EventType = AsyncCollectionEventType.TryUpdate
			});
		}

		public void RemoveAt(int index)
		{
			_requestsQueue.Enqueue(new AsyncCollectionRequest<T>
			{
				Index = index,
				Data = default(T),
				EventType = AsyncCollectionEventType.Remove
			});
		}

		public T this[int index]
		{
			get
			{
				var data = _list.Data;
				return data[index];
			}
			set
			{
				Update(index, value);
			}
		}

		private void OnSynchronize(object sender, ElapsedTimerEventArgs e)
		{
			var changesMade = false;
			if (1 == (int)_opverlappingChecker) return;
			_opverlappingChecker = 1;
			try
			{
				var maxMessages = (int)MaxMessagesPerCycle;
				foreach (var request in _requestsQueue.Dequeue(100))
				{
					if (HandledEvent(request))
					{
						changesMade = true;
					}
					maxMessages--;
					if (maxMessages <= 0) break;
				}
				if (changesMade)
				{
					_list.Data = new List<T>(_trustedSource);
				}
			}
			finally
			{
				_opverlappingChecker = 0;
			}
		}

		private bool HandledEvent(AsyncCollectionRequest<T> request)
		{
			var changesMade = false;
			if (request.EventType == AsyncCollectionEventType.Clear)
			{
				changesMade = true;
				_trustedSource.Clear();
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				return changesMade;
			}

			switch (request.EventType)
			{
				case (AsyncCollectionEventType.Remove):
					if (request.Index >= 0 && _trustedSource.Count > request.Index)
					{
						changesMade = true;
						_trustedSource.RemoveAt(request.Index);
					}
					else if (request.Data != null)
					{
						changesMade = true;
						_trustedSource.Remove((T)request.Data);
					}
					break;
				case (AsyncCollectionEventType.Add):
					changesMade = true;
					_trustedSource.Add((T)request.Data);
					break;
				case (AsyncCollectionEventType.TryUpdate):
					if (request.Index >= 0 && _trustedSource.Count > request.Index)
					{
						changesMade = true;
						_trustedSource[request.Index] = (T)request.Data;
					}

					break;
				case (AsyncCollectionEventType.TryInsert):
					if (request.Index >= 0 && _trustedSource.Count > request.Index)
					{
						changesMade = true;
						_trustedSource.Insert(request.Index, (T)request.Data);
					}

					break;
			}
			return changesMade;
		}

		~AsyncLockFreeList()
		{
			// Finalizer calls Dispose(false)
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// free managed resources
				if (_timer != null)
				{
					_timer.Dispose();
				}
				if (_list != null)
				{
					_list.Dispose();
				}
			}
		}
	}
}
