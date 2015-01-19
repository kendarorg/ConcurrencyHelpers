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
