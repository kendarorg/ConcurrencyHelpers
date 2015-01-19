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
using System.Threading;
using ConcurrencyHelpers.Interfaces;
using ConcurrencyHelpers.Utils;

namespace ConcurrencyHelpers.Containers
{
	/// <summary>
	/// Lock free queue implementation
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LockFreeQueue<T> : IQueue<T>
	{
		private readonly bool _isNullable;

		private readonly ConcurrentInt64 _count;

		/// <summary>
		/// Queue head
		/// </summary>
		private PointerT _head;

		/// <summary>
		/// Queue tail
		/// </summary>
		private PointerT _tail;

		/// <summary>
		/// Constructor
		/// </summary>
		public LockFreeQueue()
		{
			_count = new ConcurrentInt64();
			// ReSharper disable CompareNonConstrainedGenericWithNull
			_isNullable = default(T) == null;
			// ReSharper restore CompareNonConstrainedGenericWithNull
			var node = new NodeT();
			_head._ptr = _tail._ptr = node;
		}

		public long Count
		{
			get { return _count.Value; }
		}

		/// <summary>
		/// Dequeue a single element
		/// </summary>
		/// <param name="t">The element to dequeue</param>
		/// <returns></returns>
		public Boolean Dequeue(ref T t)
		{
			// ReSharper disable TooWideLocalVariableScope
			PointerT head;
			// ReSharper restore TooWideLocalVariableScope

			// Keep trying until deque is done
			Boolean bDequeNotDone = true;
			while (bDequeNotDone)
			{
				// read head
				head = _head;

				// read tail
				PointerT tail = _tail;

				// read next
				PointerT next = head._ptr._next;

				// Are head, tail, and next consistent?
				if (head._count == _head._count && head._ptr == _head._ptr)
				{
					// is tail falling behind
					if (head._ptr == tail._ptr)
					{
						// is the queue empty?
						if (null == next._ptr)
						{
							// queue is empty cannnot dequeue
							return false;
						}

						// Tail is falling behind. try to advance it
						CAS(ref _tail, tail, new PointerT(next._ptr, tail._count + 1));
					} // endif

					else // No need to deal with tail
					{
						// read value before CAS otherwise another deque might try to free the next node
						t = next._ptr._value;

						// try to swing the head to the next node
						if (CAS(ref _head, head, new PointerT(next._ptr, head._count + 1)))
						{
							bDequeNotDone = false;
						}
					}
				} // endif
			} // endloop

			// dispose of head.ptr
			_count.Decrement();
			return true;
		}


		public bool Peek(ref T t)
		{
			// ReSharper disable TooWideLocalVariableScope
			PointerT head;
			// ReSharper restore TooWideLocalVariableScope

			// Keep trying until deque is done
			while (true)
			{
				// read head
				head = _head;

				// read tail
				PointerT tail = _tail;

				// read next
				PointerT next = head._ptr._next;

				// Are head, tail, and next consistent?
				if (head._count == _head._count && head._ptr == _head._ptr)
				{
					// is tail falling behind
					if (head._ptr == tail._ptr)
					{
						// is the queue empty?
						if (null == next._ptr)
						{
							// queue is empty cannnot dequeue
							return false;
						}

						// Tail is falling behind. try to advance it
						CAS(ref _tail, tail, new PointerT(next._ptr, tail._count + 1));
					} // endif
					else // No need to deal with tail
					{
						// read value before CAS otherwise another deque might try to free the next node
						t = next._ptr._value;
						return true;
					}
				} // endif
			} // endloop
		}

		/// <summary>
		/// Enqueue a single element
		/// </summary>
		/// <param name="t"></param>
		public void Enqueue(T t)
		{
			// Allocate a new node from the free list
			var node = new NodeT { _value = t };

			// copy enqueued value into node

			// keep trying until Enqueue is done
			Boolean bEnqueueNotDone = true;

			while (bEnqueueNotDone)
			{
				// read Tail.ptr and Tail.count together
				PointerT tail = _tail;

				// read next ptr and next count together
				PointerT next = tail._ptr._next;

				// are tail and next consistent
				if (tail._count == _tail._count && tail._ptr == _tail._ptr)
				{
					// was tail pointing to the last node?
					if (null == next._ptr)
					{
						if (CAS(ref tail._ptr._next, next, new PointerT(node, next._count + 1)))
						{
							bEnqueueNotDone = false;
						} // endif
					} // endif

					else // tail was not pointing to last node
					{
						// try to swing Tail to the next node
						CAS(ref _tail, tail, new PointerT(next._ptr, tail._count + 1));
					}
				} // endif
			} // endloop
			_count.Increment();
		}

		/// <summary>
		/// Dequeu multiple elements
		/// </summary>
		/// <param name="count">The maximum number of elements to dequeue (default Int64.MaxValue) </param>
		/// <returns></returns>
		public IEnumerable<T> Dequeue(Int64 count = Int64.MaxValue)
		{
			// ReSharper disable ExpressionIsAlwaysNull
			object obj = null;
			if (!_isNullable)
			{
				obj = default(T);
			}
			var nd = (T)obj;
			while (Dequeue(ref nd) && count > 0)
			{
				yield return nd;
				count--;
			}
			// ReSharper restore ExpressionIsAlwaysNull
		}

		public T DequeueSingle()
		{
			foreach (var item in Dequeue(1))
			{
				return item;
			}
			return default(T);
		}

		/// <summary>
		/// Enqueue a list of values
		/// </summary>
		/// <param name="toInsert"></param>
		public void Enqueue(List<T> toInsert)
		{
			for (Int32 i = 0; i < toInsert.Count; i++)
			{
				Enqueue(toInsert[i]);
			}
		}

		public void Clear()
		{
			var cleanUp = new List<T>(Dequeue());
			cleanUp.Clear();
		}

		/// <summary>
		/// CAS
		/// stands for Compare And Swap
		/// Interlocked Compare and Exchange operation
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="compared"></param>
		/// <param name="exchange"></param>
		/// <returns></returns>
		private static Boolean CAS(ref PointerT destination, PointerT compared, PointerT exchange)
		{
			if (compared._ptr == Interlocked.CompareExchange(ref destination._ptr, exchange._ptr, compared._ptr))
			{
				Interlocked.Exchange(ref destination._count, exchange._count);
				return true;
			}
			
			return false;
		}

		/// <summary>
		/// Internal queue node
		/// </summary>
		private class NodeT
		{
			/// <summary>
			/// Next element
			/// </summary>
			public PointerT _next;

			/// <summary>
			/// Contained value
			/// </summary>
			public T _value;
		}

		/// <summary>
		/// Pointer to next node
		/// </summary>
		private struct PointerT
		{
			/// <summary>
			/// Counter
			/// </summary>
			public Int64 _count;

			/// <summary>
			/// Next node
			/// </summary>
			public NodeT _ptr;

			/// <summary>
			/// constructor that allows caller to specify ptr and count
			/// </summary>
			/// <param name="node"></param>
			/// <param name="c"></param>
			public PointerT(NodeT node, long c)
			{
				_ptr = node;
				_count = c;
			}
		}
	}
}
