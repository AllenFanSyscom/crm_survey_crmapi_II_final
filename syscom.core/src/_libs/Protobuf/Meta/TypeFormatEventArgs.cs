using System;

namespace libs.ProtoBuf.Meta
{
	/// <summary>
	/// Event arguments needed to perform type-formatting functions; this could be resolving a Type to a string suitable for serialization, or could
	/// be requesting a Type from a string. If no changes are made, a default implementation will be used (from the assembly-qualified names).
	/// </summary>
	public class TypeFormatEventArgs : EventArgs
	{
		Type type;
		String formattedName;
		readonly Boolean typeFixed;

		/// <summary>
		/// The type involved in this map; if this is initially null, a Type is expected to be provided for the string in FormattedName.
		/// </summary>
		public Type Type
		{
			get => type;
			set
			{
				if ( type != value )
				{
					if ( typeFixed ) throw new InvalidOperationException( "The type is fixed and cannot be changed" );
					type = value;
				}
			}
		}

		/// <summary>
		/// The formatted-name involved in this map; if this is initially null, a formatted-name is expected from the type in Type.
		/// </summary>
		public String FormattedName
		{
			get => formattedName;
			set
			{
				if ( formattedName != value )
				{
					if ( !typeFixed ) throw new InvalidOperationException( "The formatted-name is fixed and cannot be changed" );
					formattedName = value;
				}
			}
		}

		internal TypeFormatEventArgs( String formattedName )
		{
			if ( Helpers.IsNullOrEmpty( formattedName ) ) throw new ArgumentNullException( "formattedName" );
			this.formattedName = formattedName;
			// typeFixed = false; <== implicit
		}

		internal TypeFormatEventArgs( Type type )
		{
			if ( type == null ) throw new ArgumentNullException( "type" );
			this.type = type;
			typeFixed = true;
		}
	}

	/// <summary>
	/// Delegate type used to perform type-formatting functions; the sender originates as the type-model.
	/// </summary>
	public delegate void TypeFormatEventHandler( Object sender, TypeFormatEventArgs args );
}