using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.data
{
	public interface IDbUnitOfWork : IDisposable
	{
		IDbConnection Connection { get; }
		IDbTransaction Transaction { get; }

		/// <summary>設定當前操作結束時執行之動作</summary>
		Action OnDispose { get; set; }

		/// <summary>判斷當前UnitOfWork是否以交易形式處理</summary>
		Boolean IsTransaction { get; }


		/// <summary>RollBack</summary>
		void RollBack();

		/// <summary>Commit資料至DB</summary>
		void Commit( Boolean hasNext = false );

		IDbCommand CreateCommand();

		/// <summary>執行SQL指令並取得異動列數</summary>
		Int32 ExecuteSQLBy( String sql, params IDbDataParameter[] args );

		/// <summary>執行SQL指令並取得異動列數</summary>
		Int32 ExecuteSQLBy( String sql, IEnumerable<IDbDataParameter> args );

		/// <summary>執行StoredProcedure並取得異動列數</summary>
		Int32 ExecuteSPBy( String name, params IDbDataParameter[] args );

		/// <summary>執行StoredProcedure並取得異動列數</summary>
		Int32 ExecuteSPBy( String name, IEnumerable<IDbDataParameter> args );

		TReturn ExecuteCmdBy<TReturn>( String id, IEnumerable<IDbDataParameter> args, Func<IDbCommand, TReturn> onCmd );

		/// <summary>執行SQL指令並取得結果</summary>
		Object ExecuteGetSingleResultBy( String sql, params IDbDataParameter[] args );

		/// <summary>執行SQL指令並取得結果</summary>
		Object ExecuteGetSingleResultBy( String sql, IEnumerable<IDbDataParameter> args );

		/// <summary>執行SQL指令並取得DataReader<para>【請注意: DataReader使用完畢需終結】</para></summary>
		IDataReader ExecuteGetDataReaderBy( String sql, params IDbDataParameter[] args );

		/// <summary>執行SQL指令並取得DataReader<para>【請注意: DataReader使用完畢需終結】</para></summary>
		IDataReader ExecuteGetDataReaderBy( String sql, IEnumerable<IDbDataParameter> args );

		/// <summary>執行SQL指令並取得DataTable (注意: DataTable的Name將會為空值)</summary>
		DataTable ExecuteGetDataTableBy( String sql, params IDbDataParameter[] args );

		/// <summary>執行SQL指令並取得DataTable (注意: DataTable的Name將會為空值)</summary>
		DataTable ExecuteGetDataTableBy( String sql, IEnumerable<IDbDataParameter> args );

		///<summary>執行SQL指令並取得單筆TModel</summary>
		TModel? ExecuteGetMapModelBy<TModel>( String sql, params IDbDataParameter[] args ) where TModel : class;

		///<summary>執行SQL指令並取得單筆TModel</summary>
		TModel ExecuteGetMapModelBy<TModel>( String sql, IEnumerable<IDbDataParameter> args ) where TModel : class;


		///<summary>執行SQL指令並取得TModel List, 跳過指定的欄位名不做對應</summary>
		IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql ) where TModel : class;

		///<summary>執行SQL指令並取得TModel List</summary>
		IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, params IDbDataParameter[] args ) where TModel : class;

		///<summary>執行SQL指令並取得TModel List, 跳過指定的欄位名不做對應</summary>
		IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, params String[] skipColumns ) where TModel : class;

		///<summary>執行SQL指令並取得TModel List</summary>
		IList<TModel> ExecuteGetMapModelsBy<TModel>( String sql, IEnumerable<IDbDataParameter> args, IList<String> skipColumns ) where TModel : class;
	}
}
