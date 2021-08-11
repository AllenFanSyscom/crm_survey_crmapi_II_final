using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.data
{
	//Todo[Raz]: 2015-03-21 未完成, remember Implement

	/// <summary>
	/// 對應資料庫某一資料表的存取層
	/// </summary>
	public interface IDbRepository<TTableModel> where TTableModel : class
	{
		IList<TTableModel> GetAll( String? orderBy = null );

		DataTable GetAllByDataTable( String? orderBy = null );


		Int32 InsertBy( TTableModel model );
		Int32 InsertBy( IEnumerable<TTableModel> models );

		Int32 UpdateBy( TTableModel model );
		Int32 DeleteBy( TTableModel model );
	}
}
