using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using syscom;
using syscom.logging;

namespace syscom.logging.transports
{
	public partial class FileTransport : ILoggerTransporter
	{
		public static readonly ConcurrentQueue<ILogMessage> Messages = new ConcurrentQueue<ILogMessage>();

		public DirectoryInfo Dir { get; internal set; }
		public String FileNamePattern { get; internal set; }

		public Int32 IntervalWrite { get; set; }

		public FileTransport( String pathDir, String fileNamePattern, Int32 intervalWrite = 500 )
		{
			Dir = new DirectoryInfo( pathDir );
			FileNamePattern = fileNamePattern;
			IntervalWrite = intervalWrite;

			StartWriterThread();
		}

		public void OnMessage( ILogMessage message )
		{
			Messages.Enqueue( message );
		}

		public void Dispose()
		{
			writerCanceller?.Cancel();
		}
	}



	partial class FileTransport
	{
		CancellationTokenSource writerCanceller;
		Thread writer;

		public void StartWriterThread()
		{
			if ( writer != null ) return;
			lock ( this )
			{
				if ( writer != null ) return;

				writerCanceller = new CancellationTokenSource();
				writer = Utils.CreateWriterThreadBy( $"Log:File:{ FileNamePattern }", writerCanceller, Messages );

				AppDomain.CurrentDomain.ProcessExit += ( sender, args ) =>
				{
					writerCanceller?.Cancel();
				};
			}
		}
	}
}
