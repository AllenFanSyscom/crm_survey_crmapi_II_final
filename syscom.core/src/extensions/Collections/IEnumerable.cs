using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace System
{
	public static class IEnumerableExtensions
	{
		//Note: 做一個可以排除異項的Extensions
		//public static IEnumerable<TModel> DistanctBy<TModel>( this IEnumerable<TModel> items, Expression<Func<TModel, Object>> targetProperty )
		//{
		//	List<TModel> items =
		//	Object? value = null;


		//}

		/// <summary>強制將指定Ienumerable轉換為List</summary>
		public static List<TModel> CastToList<TModel>( this IEnumerable enumerable )
		{
			var list = new List<TModel>();
			foreach ( var item in enumerable ) list.Add( (TModel) item );
			return list;
		}
	}
}
