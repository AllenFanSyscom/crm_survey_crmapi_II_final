using syscom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
	public static class TaskExtensions
	{
		/// <summary>設定當發生異常時的處理 (預期單一異常, 預期多個異常請使用OnException's'HandlerBy)</summary>
		public static Task OnExceptionHandleBy( this Task task, Action<Exception> handler )
		{
			if ( handler == null ) return task;

			Action<Task> action = ( t ) =>
			{
				if ( t.Exception == null ) throw Err.Extension( "Task occur exception but refernece is null" );
				var ex = t.Exception.InnerExceptions.FirstOrDefault();
				if ( ex == null ) throw new Exception( "Exception cannot be null" );
				handler( ex );
			};

			task.ContinueWith( action, TaskContinuationOptions.OnlyOnFaulted );
			return task;
		}

		/// <summary>設定當發生異常時的處理 (預期多個異常, 預期單一異常請使用OnExceptionHandlerBy)</summary>
		public static Task OnExceptionsHandleBy( this Task task, Action<List<Exception>> handler )
		{
			if ( handler == null ) return task;

			Action<Task> action = ( t ) =>
			{
				if ( t.Exception == null ) throw Err.Extension( "Task occur exception but refernece is null" );
				var ex = t.Exception.InnerExceptions.ToList();
				handler( ex );
			};

			task.ContinueWith( action, TaskContinuationOptions.OnlyOnFaulted );
			return task;
		}
	}
}