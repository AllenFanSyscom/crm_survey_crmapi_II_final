using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace syscom.Threading
{
	public class MultiThreadWorker : IDisposable
	{
		readonly List<Thread> _workers;
		readonly Int32 _workerSize;

		String _threadNamePrefix;
		CancellationTokenSource _canceller;
		public Int32 CountOfWorkers => _workers.Count;

		public Action<CancellationToken> WorkAction { get; private set; }
		public Action<Thread, Exception> OnWorkException { get; set; }
		public Action OnDispose { get; set; }
		public Boolean IsRunning => _canceller != null;

		public TimeSpan? StopThreadTimeout { get; set; }

		public MultiThreadWorker( Int32 workerSize, String threadNamePrefix = "TWorker" )
		{
			_threadNamePrefix = threadNamePrefix;
			_workerSize = workerSize;
			_workers = new List<Thread>( _workerSize );
		}

		public void StartBy( Action<CancellationToken> action, Boolean isBackground = true, ThreadPriority threadProiority = ThreadPriority.Lowest )
		{
			if ( action == null ) throw new ArgumentNullException( "action", "必需指定要執行的方法" );
			if ( _canceller != null ) throw new InvalidOperationException( "Workers尚在執行中, 無法再度啟動" );
			lock ( _workers )
			{
				if ( _canceller != null ) throw new InvalidOperationException( "Workers尚在執行中, 無法再度啟動" );
				_canceller = new CancellationTokenSource();
				WorkAction = action;

				for ( var i = 0; i < _workerSize; i++ )
				{
					var tName = $"{_threadNamePrefix}-{i + 1}";
					var t = new Thread( _threadWorkAction ) { Name = tName, IsBackground = isBackground, Priority = threadProiority };
					_workers.Add( t );
				}

				foreach ( var t in _workers ) t.Start();
			}
		}

		public void Stop( TimeSpan stopTimeout )
		{
			StopThreadTimeout = stopTimeout;
			Stop();
		}

		public void Stop()
		{
			if ( _canceller == null ) return;
			lock ( _workers )
			{
				if ( _canceller == null ) return;
				_canceller.Cancel();

				for ( var idx = 0; idx < _workers.Count; idx++ )
				{
					var thread = _workers.FirstOrDefault();
					if ( thread == null ) break;

					_workers.Remove( thread );

					if ( !StopThreadTimeout.HasValue )
					{
						thread.Join();
					}
					else
					{
						var success = thread.Join( StopThreadTimeout.Value );
						if ( success == false ) throw new TimeoutException( $"無法停止Thread Name[{thread.Name}] State[{thread.ThreadState}]" );
					}
				}

				_canceller = null;
				WorkAction = null;
			}
		}

		void _threadWorkAction()
		{
			RestartWork:
			if ( _canceller == null || _canceller.IsCancellationRequested ) return;
			try
			{
				WorkAction( _canceller.Token );
			}
			catch ( Exception ex )
			{
				if ( OnWorkException == null ) throw new NotImplementedException( $"Worker[{Thread.CurrentThread.Name}] 發生未處理的異常, ", ex );
				OnWorkException( Thread.CurrentThread, ex );
			}

			goto RestartWork;
		}

		public void Dispose()
		{
			OnDispose?.Invoke();
			Stop();
		}
	}
}