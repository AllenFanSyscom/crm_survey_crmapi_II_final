using System.Collections.Generic;

namespace libs.Redis
{
    internal interface IMultiMessage
    {
        IEnumerable<Message> GetMessages(PhysicalConnection connection);
    }
}
