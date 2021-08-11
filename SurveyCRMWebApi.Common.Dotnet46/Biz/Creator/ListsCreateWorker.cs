using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using libs.DryIoc.Experimental;

namespace Shortener.Biz
{
	public static class ListsCreateWorker
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLogger( "ListsWorker" );
		static Task WorkerTask;
		static CancellationTokenSource WorkerCanceller;

		public static void Start()
		{
			if ( WorkerTask != null ) return;

			WorkerCanceller = new CancellationTokenSource();
			WorkerTask = Task.Factory.StartNew( () => Loop( WorkerCanceller.Token ), TaskCreationOptions.LongRunning );
			WorkerTask.OnExceptionHandleBy( ( ex ) =>
			{
				log.Error( $"[Worker] Task Error, {ex.Message}, Try Restart...", ex );
				Stop();
				Start();
			} );

			log.Info( $"[Worker] The Task Started..." );
		}

		public static void Stop()
		{
			if ( WorkerTask == null ) return;

			WorkerCanceller.Cancel();
			WorkerTask.Wait();

			WorkerTask = null;
		}

		static readonly List<WorkItem_List> Items = new List<WorkItem_List>();

		static void SortItems()
		{
			//沒時間的, 時間愈大的 愈靠前
			Items.Sort( ( a, b ) =>
			{
				if ( a.DTProcess == null ) return -1;
				if ( b.DTProcess == null ) return +1;

				if ( a.DTProcess > b.DTProcess ) return -1;
				if ( a.DTProcess < b.DTProcess ) return +1;

				return 0;
			} );
		}

		public static void Add(Guid newListMemberId, IList<Models.Entitys> entitys, Int32 type, String userid,List<Guid> listids)
		{
			if ( Items.Any( i => i.NewListMemberId == newListMemberId && i.Entitys == entitys && i.Type == type && i.UserId == userid) )
			{
				log.Warn( $"[Worker] The AddItem OPID[] already on Queue..." );
				return;
			}

			Items.Add( new WorkItem_List(newListMemberId, entitys, type, userid, listids) );
			log.Info( $"[Worker] Queued ... NewListMemberId[ {newListMemberId} ], Entitys[ {entitys} ], Type[{type}] Items[{Items.Count}]" );

			Start();
		}

		static void Loop( CancellationToken token )
		{
			while ( true )
			{
				if ( token.IsCancellationRequested )
				{
					log.Info( $"[Worker] IsCancelled, break loop" );
					break;
				}

				SpinWait.SpinUntil( () => Items.Count > 0 );
				if ( Items.Count > 1 ) SortItems();

				var item = Items.FirstOrDefault();
				if ( item == null ) continue;

				try
				{
					log.Debug( $"[Worker] Start...[{item.ToJson()}]" );
					Processor.Entry_List.TryMakeListsBy(item.NewListMemberId, item.Entitys, item.Type, item.UserId,item.ListIds);

					item.DTProcess = DateTime.Now;
					log.Debug( $"[Worker] Finish Item[{item.ToJson()}]" );
					Items.Remove( item );
				}
				catch ( Exception ex )
				{
					item.DTProcess = DateTime.Now;
					log.Error( $"[Worker] 處理Work任務時發生異常, {ex.Message}", ex );
				}
			}

			log.Info( $"[Worker] The Loop Exited" );
		}
	}
}
