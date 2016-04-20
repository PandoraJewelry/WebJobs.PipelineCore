using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using System.Threading;

namespace Pandora.Azure.WebJobs.PipelineCore
{
    internal class PipelineContext: IPipelineContext
    {
        public BrokeredMessage Message { get; set; }
        public Dictionary<string, object> Enviorment { get; private set; } = new Dictionary<string, object>();
        public FunctionResult Result { get; set; }

        #region context state
        internal SemaphoreSlim PipelineToTrigger { get; private set; } = new SemaphoreSlim(1);
        internal SemaphoreSlim TriggerRan { get; private set; } = new SemaphoreSlim(1);
        internal SemaphoreSlim TriggerToPipeline { get; private set; } = new SemaphoreSlim(1); 
        #endregion
    }
}
