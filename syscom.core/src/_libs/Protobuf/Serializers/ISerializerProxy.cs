#if !NO_RUNTIME

namespace libs.ProtoBuf.Serializers
{
	internal interface ISerializerProxy
	{
		IProtoSerializer Serializer { get; }
	}
}
#endif