using syscom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	public static class TaskFactoryExtensions
	{
		/// <summary>
		/// Start Periodic Task 啟動周期性任務, 依照傳入的參數進行Task
		/// <para>(ex1: 建立一個只執行2次的任務)</para>
		/// <para>(ex2: 要建立一個每10秒執行一次, 總共只執行6次的任務 )</para>
		/// </summary>
		public static Task StartNewPeriodicBy
		(
			this TaskFactory factory,
			Action action,
			Action<Exception>? actionOnExceptionOccur = null,
			Int32 intervalInMilliseconds = Timeout.Infinite,
			Int32 delayInMilliseconds = 0,
			Int32 maxDurationInMilliseconds = Timeout.Infinite,
			Int32 maxIterations = -1,
			Boolean synchronous = false,
			CancellationToken cancelToken = new CancellationToken(),
			TaskCreationOptions periodicTaskCreationOptions = TaskCreationOptions.None
		)
		{
			var stopWatch = new Stopwatch();
			Action wrapperAction = () =>
			{
				CheckIfCancelled( cancelToken );
				action();
			};

			Action mainAction = () => MainPeriodicTaskAction( intervalInMilliseconds, delayInMilliseconds, maxDurationInMilliseconds, maxIterations, cancelToken, stopWatch, synchronous, wrapperAction, actionOnExceptionOccur, periodicTaskCreationOptions );

			return Task.Factory.StartNew( mainAction, cancelToken, TaskCreationOptions.LongRunning, TaskScheduler.Current );
		}


		static void CheckIfCancelled( CancellationToken cancellationToken )
		{
			if ( cancellationToken == null ) throw new ArgumentNullException( "cancellationToken" );
			cancellationToken.ThrowIfCancellationRequested();
		}


		//synchronous: if set to True exec each period in a blocking fashion and each periodic execution of the task is included in the total duration of the Task.
		static void MainPeriodicTaskAction
		(
			Int32 intervalInMilliseconds,
			Int32 delayInMilliseconds,
			Int32 maxDurationInMilliseconds,
			Int32 maxIterations,
			CancellationToken cancelToken,
			Stopwatch stopWatch,
			Boolean synchronous,
			Action originalAction,
			Action<Exception> actionOnExceptionOccur,
			TaskCreationOptions periodicTaskCreationOptions
		)
		{
			var subTaskCreationOptions = TaskCreationOptions.AttachedToParent | periodicTaskCreationOptions;

			CheckIfCancelled( cancelToken );

			if ( delayInMilliseconds > 0 ) Thread.Sleep( delayInMilliseconds );

			if ( maxIterations == 0 ) return;

			var iteration = 0;

			// Slim ver it's more efficient in small intervals, http://msdn.microsoft.com/en-us/library/vstudio/5hbefs30(v=vs.100).aspx
			using ( var periodResetEvent = new ManualResetEventSlim( false ) )
			{
				while ( true )
				{
					CheckIfCancelled( cancelToken );

					var subTask = Task.Factory.StartNew( originalAction, cancelToken, subTaskCreationOptions, TaskScheduler.Current );
					subTask.ContinueWith( t =>
					{
						actionOnExceptionOccur?.Invoke( subTask.Exception.InnerException );
					}, TaskContinuationOptions.OnlyOnFaulted );

					if ( synchronous )
					{
						stopWatch.Start();
						try { subTask.Wait( cancelToken ); }
						catch
						{
							/* avoid subTask kill periodic task */
						}

						stopWatch.Stop();
					}

					// use the same Timeout setting as the System.Threading.Timer, infinite timeout will execute only one iteration.
					if ( intervalInMilliseconds == Timeout.Infinite ) break;

					iteration++;

					if ( maxIterations > 0 && iteration >= maxIterations ) break;

					try
					{
						stopWatch.Start();
						periodResetEvent.Wait( intervalInMilliseconds, cancelToken );
						stopWatch.Stop();
					}
					finally { periodResetEvent.Reset(); }

					CheckIfCancelled( cancelToken );

					if ( maxDurationInMilliseconds > 0 && stopWatch.ElapsedMilliseconds >= maxDurationInMilliseconds ) break;
				}
			}
		}
	}
}
