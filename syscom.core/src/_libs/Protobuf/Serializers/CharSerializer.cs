#if !NO_RUNTIME
using System;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif


namespace libs.ProtoBuf.Serializers
{
	internal sealed class CharSerializer : UInt16Serializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Char );
#endif
		public CharSerializer( Meta.TypeModel model ) : base( model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(char));
#endif
		}

		public override Type ExpectedType => expectedType;

#if !FEAT_IKVM
		public override void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteUInt16( (UInt16) (Char) value, dest ); }
		public override Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return (Char) source.ReadUInt16();
		}
#endif
		// no need for any special IL here; ushort and char are
		// interchangeable as long as there is no boxing/unboxing
	}
}
#endif