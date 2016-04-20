// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;

namespace Pandora.Azure.WebJobs.PipelineCore
{
    public interface IPipelineContext
    {
        BrokeredMessage Message { get; }
        Dictionary<string, object> Enviorment { get; }
        FunctionResult Result { get; }
    }
}
