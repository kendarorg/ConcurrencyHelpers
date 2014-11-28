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