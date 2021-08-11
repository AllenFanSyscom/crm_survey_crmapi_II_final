using System;

namespace libs.ProtoBuf
{
	/// <summary>
	/// Indicates that a type is defined for protocol-buffer serialization.
	/// </summary>
	[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface,
	                 AllowMultiple = false, Inherited = false )]
	public sealed class ProtoContractAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the defined name of the type.
		/// </summary>
		public String Name { get => name; set => name = value; }

		String name;

		/// <summary>
		/// Gets or sets the fist offset to use with implicit field tags;
		/// only uesd if ImplicitFields is set.
		/// </summary>
		public Int32 ImplicitFirstTag
		{
			get => implicitFirstTag;
			set
			{
				if ( value < 1 ) throw new ArgumentOutOfRangeException( "ImplicitFirstTag" );
				implicitFirstTag = value;
			}
		}

		Int32 implicitFirstTag;

		/// <summary>
		/// If specified, alternative contract markers (such as markers for XmlSerailizer or DataContractSerializer) are ignored.
		/// </summary>
		public Boolean UseProtoMembersOnly { get => HasFlag( OPTIONS_UseProtoMembersOnly ); set => SetFlag( OPTIONS_UseProtoMembersOnly, value ); }

		/// <summary>
		/// If specified, do NOT treat this type as a list, even if it looks like one.
		/// </summary>
		public Boolean IgnoreListHandling { get => HasFlag( OPTIONS_IgnoreListHandling ); set => SetFlag( OPTIONS_IgnoreListHandling, value ); }


		/// <summary>
		/// Gets or sets the mechanism used to automatically infer field tags
		/// for members. This option should be used in advanced scenarios only.
		/// Please review the important notes against the ImplicitFields enumeration.
		/// </summary>
		public ImplicitFields ImplicitFields { get => implicitFields; set => implicitFields = value; }

		ImplicitFields implicitFields;


		/// <summary>
		/// Enables/disables automatic tag generation based on the existing name / order
		/// of the defined members. This option is not used for members marked
		/// with ProtoMemberAttribute, as intended to provide compatibility with
		/// WCF serialization. WARNING: when adding new fields you must take
		/// care to increase the Order for new elements, otherwise data corruption
		/// may occur.
		/// </summary>
		/// <remarks>If not explicitly specified, the default is assumed from Serializer.GlobalOptions.InferTagFromName.</remarks>
		public Boolean InferTagFromName
		{
			get => HasFlag( OPTIONS_InferTagFromName );
			set
			{
				SetFlag( OPTIONS_InferTagFromName, value );
				SetFlag( OPTIONS_InferTagFromNameHasValue, true );
			}
		}

		/// <summary>
		/// Has a InferTagFromName value been explicitly set? if not, the default from the type-model is assumed.
		/// </summary>
		internal Boolean InferTagFromNameHasValue =>
			// note that this property is accessed via reflection and should not be removed
			HasFlag( OPTIONS_InferTagFromNameHasValue );

		Int32 dataMemberOffset;

		/// <summary>
		/// Specifies an offset to apply to [DataMember(Order=...)] markers;
		/// this is useful when working with mex-generated classes that have
		/// a different origin (usually 1 vs 0) than the original data-contract.
		/// 
		/// This value is added to the Order of each member.
		/// </summary>
		public Int32 DataMemberOffset { get => dataMemberOffset; set => dataMemberOffset = value; }


		/// <summary>
		/// If true, the constructor for the type is bypassed during deserialization, meaning any field initializers
		/// or other initialization code is skipped.
		/// </summary>
		public Boolean SkipConstructor { get => HasFlag( OPTIONS_SkipConstructor ); set => SetFlag( OPTIONS_SkipConstructor, value ); }

		/// <summary>
		/// Should this type be treated as a reference by default? Please also see the implications of this,
		/// as recorded on ProtoMemberAttribute.AsReference
		/// </summary>
		public Boolean AsReferenceDefault { get => HasFlag( OPTIONS_AsReferenceDefault ); set => SetFlag( OPTIONS_AsReferenceDefault, value ); }

		Boolean HasFlag( Byte flag ) { return ( flags & flag ) == flag; }

		void SetFlag( Byte flag, Boolean value )
		{
			if ( value ) flags |= flag;
			else flags = (Byte) ( flags & ~flag );
		}

		Byte flags;

		const Byte
			OPTIONS_InferTagFromName = 1,
			OPTIONS_InferTagFromNameHasValue = 2,
			OPTIONS_UseProtoMembersOnly = 4,
			OPTIONS_SkipConstructor = 8,
			OPTIONS_IgnoreListHandling = 16,
			OPTIONS_AsReferenceDefault = 32,
			OPTIONS_EnumPassthru = 64,
			OPTIONS_EnumPassthruHasValue = 128;

		/// <summary>
		/// Applies only to enums (not to DTO classes themselves); gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
		/// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
		/// </summary>
		public Boolean EnumPassthru
		{
			get => HasFlag( OPTIONS_EnumPassthru );
			set
			{
				SetFlag( OPTIONS_EnumPassthru, value );
				SetFlag( OPTIONS_EnumPassthruHasValue, true );
			}
		}

		/// <summary>
		/// Has a EnumPassthru value been explicitly set?
		/// </summary>
		internal Boolean EnumPassthruHasValue =>
			// note that this property is accessed via reflection and should not be removed
			HasFlag( OPTIONS_EnumPassthruHasValue );
	}
}