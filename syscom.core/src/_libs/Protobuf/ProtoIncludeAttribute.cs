using System;
using System.ComponentModel;
using libs.ProtoBuf.Meta;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif
namespace libs.ProtoBuf
{
	/// <summary>
	/// Indicates the known-types to support for an individual
	/// message. This serializes each level in the hierarchy as
	/// a nested message to retain wire-compatibility with
	/// other protocol-buffer implementations.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false )]
	public sealed class ProtoIncludeAttribute : Attribute
	{
		///<summary>
		/// Creates a new instance of the ProtoIncludeAttribute.
		/// </summary>
		/// <param name="tag">The unique index (within the type) that will identify this data.</param>
		/// <param name="knownType">The additional type to serialize/deserialize.</param>
		public ProtoIncludeAttribute( Int32 tag, Type knownType )
			: this( tag, knownType == null ? "" : knownType.AssemblyQualifiedName )
		{
		}

		/// <summary>
		/// Creates a new instance of the ProtoIncludeAttribute.
		/// </summary>
		/// <param name="tag">The unique index (within the type) that will identify this data.</param>
		/// <param name="knownTypeName">The additional type to serialize/deserialize.</param>
		public ProtoIncludeAttribute( Int32 tag, String knownTypeName )
		{
			if ( tag <= 0 ) throw new ArgumentOutOfRangeException( "tag", "Tags must be positive integers" );
			if ( Helpers.IsNullOrEmpty( knownTypeName ) ) throw new ArgumentNullException( "knownTypeName", "Known type cannot be blank" );
			this.tag = tag;
			this.knownTypeName = knownTypeName;
		}

		/// <summary>
		/// Gets the unique index (within the type) that will identify this data.
		/// </summary>
		public Int32 Tag => tag;

		readonly Int32 tag;

		/// <summary>
		/// Gets the additional type to serialize/deserialize.
		/// </summary>
		public String KnownTypeName => knownTypeName;

		readonly String knownTypeName;

		/// <summary>
		/// Gets the additional type to serialize/deserialize.
		/// </summary>
		public Type KnownType => TypeModel.ResolveKnownType( KnownTypeName, null, null );

		/// <summary>
		/// Specifies whether the inherited sype's sub-message should be
		/// written with a length-prefix (default), or with group markers.
		/// </summary>
		[DefaultValue( DataFormat.Default )]
		public DataFormat DataFormat { get => dataFormat; set => dataFormat = value; }

		DataFormat dataFormat = DataFormat.Default;
	}
}