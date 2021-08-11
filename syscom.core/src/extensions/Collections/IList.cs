using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System
{
	public static class IListExtensions
	{
		public static IReadOnlyList<TModel> ToReadOnlyList<TModel>( this IList<TModel> targetList, Boolean cloneMode = true )
		{
			if ( targetList == null ) throw new ArgumentNullException( nameof( targetList ) );

			IReadOnlyList<TModel>? readonlyList = null;
			if ( !cloneMode )
			{
				readonlyList = targetList as IReadOnlyList<TModel>;
				if ( readonlyList != null ) return readonlyList;
			}

			return new ReadOnlyWrapper<TModel>( targetList, cloneMode );
		}

		sealed class ReadOnlyWrapper<TModel> : IReadOnlyList<TModel>
		{
			readonly IList<TModel> _source;

			public Int32 Count => _source.Count;
			public TModel this[ Int32 index ] => _source[index];

			public ReadOnlyWrapper( IList<TModel> source, Boolean cloneMode = false )
			{
				if ( cloneMode )
					_source = source.ToList();
				else
					_source = source;
			}

			public IEnumerator<TModel> GetEnumerator() { return _source.GetEnumerator(); }
			IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
		}
	}
}
