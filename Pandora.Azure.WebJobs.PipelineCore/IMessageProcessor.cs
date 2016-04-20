using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pandora.Azure.WebJobs.PipelineCore
{
    public interface IMessageProcessor
    {
        Task Invoke(IPipelineContext context, Func<Task> next, CancellationToken cancellationToken);
    }
}
