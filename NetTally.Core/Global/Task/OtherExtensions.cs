﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NetTally.Extensions
{
    /// <summary>
    /// Class for other general extension methods.
    /// </summary>
    public static class OtherExtensions
    {
        /// <summary>
        /// Function to allow setting a timeout on an async function that doesn't natively permit it.
        /// From: http://stackoverflow.com/a/22078975/770213
        /// </summary>
        /// <typeparam name="TResult">The return type of the task being awaited.</typeparam>
        /// <param name="task">The task to await.</param>
        /// <param name="timeout">The timeout to wait for the task to complete.</param>
        /// <param name="token">A cancellation token for user-initiated cancellations.</param>
        /// <returns>Returns the awaited task, if it completed in less than the timeout period.</returns>
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken token)
        {
            using (CancellationTokenSource timeoutTokenSource = new CancellationTokenSource(timeout))
            {
                using (CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutTokenSource.Token))
                {
                    var completedTask = await Task.WhenAny(task, Task.Delay(timeout, linkedTokenSource.Token));

                    if (completedTask == task)
                    {
                        return await task;  // Very important in order to propagate exceptions
                    }
                    else if (token.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(token);
                    }
                    else
                    {
                        throw new TimeoutException("The operation has timed out.");
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the specified collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection.</param>
        public static void Sort<T>(this ObservableCollection<T> collection) where T : IComparable
        {
            var sorted = collection.OrderBy(x => x).ToList();
            for (int i = 0; i < sorted.Count(); i++)
            {
                int src = collection.IndexOf(sorted[i]);
                if (src != i)
                    collection.Move(src, i);
            }
        }

    }
}
