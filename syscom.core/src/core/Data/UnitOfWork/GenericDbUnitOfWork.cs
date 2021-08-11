using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom.logging;
using syscom.data;

namespace syscom.data.UnitOfWork
{
	internal sealed class GenericDbUnitOfWork : IDbUnitOfWork
	{
		static Exception NewErrorBy( String message, Exception ex = null ) { return Err.Module( "DB", message, ex ); }
		IsolationLevel _IsolationLevel;
		readonly IDbConnection _Connection;
		IDbTransaction? _Transaction;

		internal GenericDbUnitOfWork( IDbConnection conn, Boolean useTransaction = false, IsolationLevel isoLv = IsolationLevel.ReadCommitted )
		{
			_Connection = conn;
			_IsolationLevel = isoLv;
			if ( useTransaction )
			{
				try
				{
					if ( _Connection.State != ConnectionState.Open ) _Connection.Open();
				}
				catch ( Exception ex ) { throw NewErrorBy( $"開啟DB連線發生異常, {ex.Message}, 連線字串[ {_Connection.ConnectionString} ]", ex ); }

				_Transaction = _Connection.BeginTransaction( _IsolationLevel );
			}
		}

		public IDbConnection Connection => _Connection;
		public IDbTransaction Transaction => _Transaction;
		public Action OnDispose { get; set; }
		public Boolean IsTransaction => _Transaction != null;

		public void RollBack()
		{
			if ( _Transaction == null ) throw NewErrorBy( "無法在沒有Transaction的狀態下執行Rollback, 請確認您是否使用了Transaction的方式" );

			_Transaction.Rollback();
			_Transaction = _Connection.BeginTransaction();
		}

		public void Commit( Boolean hasNext = false )
		{
			if ( _Transaction == null ) throw NewErrorBy( "無法在沒有Transaction的狀態下執行Commit, 請確認您是否使用了Transaction的方式" );

			try
			{
				_Transaction.Commit();
				_Transaction = hasNext ? _Connection.BeginTransaction() : null;
			}
			catch ( Exception ex ) { throw NewErrorBy( "資料Commit異常, 請查詢Log以取得詳細資訊", ex ); }
		}

		public void Dispose()
		{
			_Transaction?.Dispose();
			OnDispose?.Invoke();
		}

		public IDbCommand CreateCommand()
		{
			try
			{
				if ( _Connection.State != ConnectionState.Open ) _Connection.Open();
			}
			catch ( Exception ex ) { throw NewErrorBy( $"開啟DB連線發生異常, {ex.Message}, 連線字串[ {_Connection.ConnectionString} ]", ex ); }

			var cmd = _Connection.CreateCommand();

			if ( _Transaction != null ) cmd.Transaction = _Transaction;

			return cmd;
		}

		public TReturn ExecuteCmdBy<TReturn>( String id, IEnumerable<IDbDataParameter> args, Func<IDbCommand, TReturn> onCmd )
		{
			try
			{
				if ( _Connection.State != ConnectionState.Open ) _Connection.Open();
			}
			catch ( Exception ex ) { throw NewErrorBy( $"資料庫執行[{id}] 開啟DB連線發生異常, {ex.Message}, connStr[ {_Connection.ConnectionString} ]", ex ); }

			try
			{
				using ( var cmd = CreateCommand() )
				{
					if ( args != null )
					{
						foreach ( var arg in args )
						{
							//取代null值為DBNull
							if( arg.Value == null ) arg.Value = DBNull.Value;
							cmd.Parameters.Add( arg );
						}
					}

					return onCmd( cmd );
				}
			}
			catch ( Exception ex )
			{
				var msg = $"資料庫執行[{id}]發生異常, {ex.Message}, 參數: {args.DumpValues()}";
				throw NewErrorBy( msg, ex );
			}
		}

		public Int32 ExecuteSQLBy( String sql, params IDbDataParameter[] args ) { return ExecuteSQLBy( sql, (IEnumerable<IDbDataParameter>) args ); }

		public Int32 ExecuteSQLBy( String sql, IEnumerable<IDbDataParameter> args )
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				return cmd.ExecuteNonQuery();
			} );
		}

		public Int32 ExecuteSPBy( String name, params IDbDataParameter[] args ) { return ExecuteSPBy( name, (IEnumerable<IDbDataParameter>) args ); }

		public Int32 ExecuteSPBy( String name, IEnumerable<IDbDataParameter> args )
		{
			return ExecuteCmdBy( $"SP: {name}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.StoredProcedure;
				cmd.CommandText = name;
				return cmd.ExecuteNonQuery();
			} );
		}

		public Object ExecuteGetSingleResultBy( String sql, params IDbDataParameter[] args ) { return ExecuteGetSingleResultBy( sql, (IEnumerable<IDbDataParameter>) args ); }

		public Object ExecuteGetSingleResultBy( String sql, IEnumerable<IDbDataParameter> args )
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				return cmd.ExecuteScalar();
			} );
		}

		public IDataReader ExecuteGetDataReaderBy( String sql, params IDbDataParameter[] args ) { return ExecuteGetDataReaderBy( sql, (IEnumerable<IDbDataParameter>) args ); }

		public IDataReader ExecuteGetDataReaderBy( String sql, IEnumerable<IDbDataParameter> args )
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				return cmd.ExecuteReader();
			} );
		}

		public DataTable ExecuteGetDataTableBy( String sql, params IDbDataParameter[] args ) { return ExecuteGetDataTableBy( sql, (IEnumerable<IDbDataParameter>) args ); }

		public DataTable ExecuteGetDataTableBy( String sql, IEnumerable<IDbDataParameter> args )
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				var dt = new DataTable();

				using var reader = cmd.ExecuteReader();
				dt.Load( reader );

				return dt;
			} );
		}

		public TModel ExecuteGetMapModelBy<TModel>( String sql, params IDbDataParameter[] args ) where TModel : class { return ExecuteGetMapModelBy<TModel>( sql, (IEnumerable<IDbDataParameter>) args ); }

		public TModel ExecuteGetMapModelBy<TModel>( String sql, IEnumerable<IDbDataParameter> args ) where TModel : class
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				using var reader = cmd.ExecuteReader();
				return reader.AutoReadGetMappedModel<TModel>();
			} );
		}

		public IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql ) where TModel : class { return ExecuteGetMapModelsBy<TModel>( sql, (IEnumerable<IDbDataParameter>) null, null ); }
		public IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, params IDbDataParameter[] args ) where TModel : class { return ExecuteGetMapModelsBy<TModel>( sql, args, null ); }
		public IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, params String[] skipColumns ) where TModel : class { return ExecuteGetMapModelsBy<TModel>( sql, null, skipColumns ); }

		public IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, IEnumerable<IDbDataParameter> args, IList<String> skipColumns ) where TModel : class
		{
			return ExecuteCmdBy( $"SQL: {sql}", args, ( cmd ) =>
			{
				cmd.CommandType = CommandType.Text;
				cmd.CommandText = sql;
				IDataReader? reader = null;
				try
				{
					reader = cmd.ExecuteReader();
				}
				catch ( Exception ex )
				{
					throw NewErrorBy( "執行資料庫取得DataReader時異常, " + ex.Message, ex );
				}

				try
				{
					using ( reader )
					{
						return reader.AutoReadGetMappedModels<TModel>( skipColumns: skipColumns );
					}
				}
				catch ( Exception ex )
				{
					throw NewErrorBy( "執行DataReader自動MappingModel時異常, " + ex.Message, ex );
				}
			} );
		}
	}
}
