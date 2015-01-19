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

namespace ConcurrencyHelpers.Interfaces
{
	public interface IQueue<T>
	{
		long Count { get; }

		/// <summary>
		/// Dequeue a single element
		/// </summary>
		/// <param name="t">The element to dequeue</param>
		/// <returns></returns>
		Boolean Dequeue(ref T t);

		/// <summary>
		/// Enqueue a single element
		/// </summary>
		/// <param name="t"></param>
		void Enqueue(T t);

		/// <summary>
		/// Dequeu multiple elements
		/// </summary>
		/// <param name="count">The maximum number of elements to dequeue (default Int64.MaxValue) </param>
		/// <returns></returns>
		IEnumerable<T> Dequeue(Int64 count = Int64.MaxValue);

		T DequeueSingle();

		/// <summary>
		/// Enqueue a list of values
		/// </summary>
		/// <param name="toInsert"></param>
		void Enqueue(List<T> toInsert);

		void Clear();

		bool Peek(ref T t);
	}
}