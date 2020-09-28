using System.Threading;
using System.Threading.Tasks;

namespace NationalInstruments.Tools
{
    public interface IProgram<T>
    {
        Task<int> ExecuteAsync(T context, CancellationToken cancellationToken);
    }
}
