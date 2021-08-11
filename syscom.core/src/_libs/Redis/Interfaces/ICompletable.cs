using System.Text;

namespace libs.Redis
{
    internal interface ICompletable
    {
        void AppendStormLog(StringBuilder sb);

        bool TryComplete(bool isAsync);
    }
}
