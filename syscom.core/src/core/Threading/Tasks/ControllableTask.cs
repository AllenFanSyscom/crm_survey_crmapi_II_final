using syscom.logging;
using syscom;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	/*
	使用方式:
	----------------------------------------------------
	Action<CancellationToken> action = ( token ) =>
	{
		var count = 0;
		Console.WriteLine( "開始執行Action..." );
		while( !token.IsCancellationRequested )
		{
			Console.WriteLine( "Action執行中 ( "+ (count++) +" ) ..." + DateTime.Now );
			Thread.Sleep( 500 );
			if( count > 5 ) throw new Exception("count is 5.");
		}
		Console.WriteLine( "已離開Action..." );
	};

	var task = new ControllableTask( action );
	task.OnStart = () => { Console.WriteLine( "task準備開始" ); };
	task.OnStarted = () => { Console.WriteLine( "task已經開始" ); };
	task.OnStop = () => { Console.WriteLine( "task準備停止" ); };
	task.OnStoped = () => { Console.WriteLine( "task已經停止" ); };
	task.OnException = ( ex ) =>
	{
		Console.WriteLine("task發生異常, 重新啟動...");
		task.Restart();
	};
	task.Start();
	----------------------------------------------------
	你會得到如下輸出:
	----------------------------------------------------
	task準備開始
	task已經開始
	開始執行Action...
	Action執行中 ( 0 ) ...2015/9/21 下午 05:15:23
	Action執行中 ( 1 ) ...2015/9/21 下午 05:15:23
	Action執行中 ( 2 ) ...2015/9/21 下午 05:15:24
	Action執行中 ( 3 ) ...2015/9/21 下午 05:15:24
	Action執行中 ( 4 ) ...2015/9/21 下午 05:15:25
	Action執行中 ( 5 ) ...2015/9/21 下午 05:15:25
	task發生異常, 重新啟動...
	task準備停止
	task已經停止
	*/
	public class ControllableTask
	{
		static ILogger Log;
		static Boolean _IsNeedDumpTaskStatusChange;

		static ControllableTask()
		{
			Log = LogUtils.GetLogger( "ControllableTask" );
			_IsNeedDumpTaskStatusChange = ConfigUtils.GetAppSettingOr( "syscom.core.ControllableTask.TraceTaskStatusChange", false );
		}

		public Action OnStart { get; set; }
		public Action OnStarted { get; set; }
		public Action OnStop { get; set; }
		public Action OnStoped { get; set; }
		public Action<Exception> OnException { get; set; }

		//TaskFactory _taskFactory;
		List<Task> _tasks;
		Action<CancellationToken> _action;
		Task _task;
		public TaskStatus Status { get; private set; }
		CancellationTokenSource _canceller;
		Object _mutex;


		AutoResetEvent _resetEvent;

		public CancellationToken CancelToken => _canceller.Token;

		public ControllableTask( Action<CancellationToken> cancellableAction )
		{
			//_taskFactory = new TaskFactory( TaskCreationOptions.LongRunning, TaskContinuationOptions.AttachedToParent );
			_tasks = new List<Task>();
			_mutex = new Object();
			_resetEvent = new AutoResetEvent( false );
			_action = cancellableAction;
			Status = TaskStatus.WaitingToRun;

			_parentTask = new Task( CreationCore, _parentCanceller.Token, TaskCreationOptions.LongRunning );
			_parentTask.Start();
		}

		void _UpdateStatus( Task task )
		{
			if ( Monitor.TryEnter( _mutex ) )
			{
				if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[UpdateStatus] GOT :" + task.Status + ", Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
				if ( task.Status == TaskStatus.RanToCompletion )
				{
					Status = _canceller.IsCancellationRequested ? TaskStatus.Canceled : TaskStatus.RanToCompletion;
					_resetEvent.Set();
				}

				if ( task.Status == TaskStatus.Faulted ) Status = TaskStatus.Faulted;
				Monitor.Exit( _mutex );
			}
		}

		CancellationTokenSource _parentCanceller = new CancellationTokenSource();
		Task _parentTask;

		ConcurrentQueue<Boolean> _queueCmds = new ConcurrentQueue<Boolean>();

		void CreationCore()
		{
			var isCreate = false;
			while ( true )
			{
				if ( _queueCmds.Count == 0 && _parentCanceller.IsCancellationRequested )
					if ( Status != TaskStatus.Running )
						break;
				if ( !_queueCmds.TryDequeue( out isCreate ) )
				{
					SpinWait.SpinUntil( () => _queueCmds.Count > 0, 1000 );
					continue;
				}

				if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[CreationCore] Into... Flag: " + isCreate + ", Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
				if ( isCreate )
					//Debug.WriteLine( "Into Start: " + Thread.CurrentThread.ManagedThreadId );
					lock ( _mutex )
					{
						//Debug.WriteLine( "Into Start Locked: " + Thread.CurrentThread.ManagedThreadId );
						if ( _task != null || Status == TaskStatus.Running )
						{
							if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[CreationCore] reject start, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
						}
						else
						{
							if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[CreationCore] start START, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
							_DIRECT_START();
						}
					}
				else
					//Debug.WriteLine( "Into Stop: " + Thread.CurrentThread.ManagedThreadId );
					lock ( _mutex )
					{
						//Debug.WriteLine( "Into Stop Locked: " + Thread.CurrentThread.ManagedThreadId );
						//if ( _task == null || Status == TaskStatus.RanToCompletion )
						if ( _task == null )
						{
							if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[CreationCore] reject stop, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
						}
						else
						{
							if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[CreationCore] start stop, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
							_DIRECT_STOP();
						}
					}

				if ( _queueCmds.Count != 0 || !_parentCanceller.IsCancellationRequested ) continue;
				if ( Status != TaskStatus.Running ) break;
			}
		}

		void _DIRECT_START()
		{
			OnStart?.Invoke();

			_canceller = new CancellationTokenSource();

			_task = new Task( () => _action( _canceller.Token ), _canceller.Token, TaskCreationOptions.AttachedToParent );
			_task.ContinueWith( _UpdateStatus );
			if ( OnException != null ) _task.OnExceptionHandleBy( OnException );
			_task.Start();

			_tasks.Add( _task );

			Status = TaskStatus.Running;

			OnStarted?.Invoke();
		}

		void _DIRECT_STOP()
		{
			OnStop?.Invoke();

			_canceller.Cancel();

			try
			{
				_tasks.Remove( _task );
				_task.Wait();
			}
			catch ( Exception )
			{
				//Debug.WriteLine( "WaitERR: " + ex.InnerException.Message );
			}
			finally
			{
				Status = TaskStatus.Canceled;
				_task = null;
				_resetEvent.Set();
			}

			OnStoped?.Invoke();
		}


		public void Start() { _queueCmds.Enqueue( true ); }

		public void Stop() { _queueCmds.Enqueue( false ); }

		public void Restart()
		{
			if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[RESTART] RESTART - Into Method, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
			lock ( _mutex )
			{
				if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[RESTART] start STOP, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
				_DIRECT_STOP();
				if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[RESTART] start START, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
				_DIRECT_START();
				if ( _IsNeedDumpTaskStatusChange ) Log.Trace( "[RESTART] RESTART - leave, Status[" + Status + "], task: " + ( _task == null ? "null" : _task.Status.ToString() ) );
			}
		}

		public Boolean WaitForComplete( TimeSpan? timeout = null )
		{
			Debug.WriteLine( "Into WaitForComplete, status: " + Status );

			_parentCanceller.Cancel();
			try
			{
				_parentTask.Wait();
			}
			catch { }

			if ( Status == TaskStatus.WaitingToRun ) throw new Exception( "Task尚未啟動, 請先執行Start." );

			if ( Status == TaskStatus.RanToCompletion ) return true;
			var result = !timeout.HasValue ? _resetEvent.WaitOne() : _resetEvent.WaitOne( timeout.Value );
			Debug.WriteLine( "WaitForComplete: " + result + " : " + Thread.CurrentThread.ManagedThreadId + ", Status: " + Status );
			return result;
		}
	}


	#region OtherWay

	public class ControllableTaskScheduler : TaskScheduler
	{
		public Action<Exception> OnException;

		LinkedList<Task> _tasks = new LinkedList<Task>();

		protected sealed override IEnumerable<Task> GetScheduledTasks()
		{
			var lockTaken = false;
			try
			{
				Monitor.TryEnter( _tasks, ref lockTaken );
				if ( lockTaken ) return _tasks;
				else throw new NotSupportedException();
			}
			finally
			{
				if ( lockTaken ) Monitor.Exit( _tasks );
			}
		}

		protected sealed override Boolean TryDequeue( Task task )
		{
			lock ( _tasks )
			{
				return _tasks.Remove( task );
			}
		}

		protected override void QueueTask( Task task )
		{
			lock ( _tasks )
			{
				_tasks.AddLast( task );
				++_delegatesQueuedOrRunning;
				NotifyThreadPoolOfPendingWork();
			}
		}

		protected sealed override Boolean TryExecuteTaskInline( Task task, Boolean taskWasPreviouslyQueued )
		{
			// If this thread isn't already processing a task, we don't support inlining
			if ( !_currentThreadIsProcessingItems ) return false;

			// If the task was previously queued, remove it from the queue
			if ( taskWasPreviouslyQueued )
			{
				// Try to run the task.
				if ( !TryDequeue( task ) ) return false;

				task.OnExceptionHandleBy( OnException );

				return TryExecuteTask( task );
			}

			task.OnExceptionHandleBy( OnException );
			return TryExecuteTask( task );
		}


		[ThreadStatic] static Boolean _currentThreadIsProcessingItems;

		// Indicates whether the scheduler is currently processing work items.
		Int32 _delegatesQueuedOrRunning = 0;

		void NotifyThreadPoolOfPendingWork()
		{
			ThreadPool.UnsafeQueueUserWorkItem( _ =>
			{
				// Note that the current thread is now processing work items.
				// This is necessary to enable inlining of tasks into this thread.
				_currentThreadIsProcessingItems = true;
				try
				{
					// Process all available items in the queue.
					while ( true )
					{
						Task item;
						lock ( _tasks )
						{
							// When there are no more items to be processed,
							// note that we're done processing, and get out.
							if ( _tasks.Count == 0 )
							{
								--_delegatesQueuedOrRunning;
								break;
							}

							// Get the next item from the queue
							item = _tasks.First.Value;
							_tasks.RemoveFirst();
						}

						item.OnExceptionHandleBy( OnException );
						// Execute the task we pulled out of the queue
						TryExecuteTask( item );
					}
				}
				// We're done processing items on the current thread
				finally { _currentThreadIsProcessingItems = false; }
			}, null );
		}
	}

	#endregion
}
