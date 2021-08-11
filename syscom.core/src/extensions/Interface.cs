using System;
using System.Linq;

public static class InterfaceExtensions
{
	/// <summary>取得該Type所繼承的第一個Interface (不論繼承幾層或幾個，只取得該Type所繼承的第一個)
	/// ex. class BB:A,B,C →取得A
	/// </summary>
	public static Type GetInterfaceOfFirstInhert( this Type t )
	{
		var allInterfaces = t.GetInterfaces();
		var selection = allInterfaces
		                .Where
		                ( x =>
			                  !allInterfaces.Any
			                  ( y =>
				                    y.GetInterfaces().Contains( x )
			                  )
		                ).ToArray();

		return
		(
			t.BaseType != null ? selection.Except( t.BaseType.GetInterfaces() ) : selection
		).FirstOrDefault();
	}

	/// <summary>判斷該繼承的第一個Interface是否為指定型別</summary>
	public static Boolean IsFirstInhertInterfaceBy( this Type t, Type target ) { return t.GetInterfaceOfFirstInhert() == target; }
}