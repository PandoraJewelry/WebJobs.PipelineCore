# Azure Webjobs SDK Pipelines for Azure ServiceBus

## Our use case
Webjobs currently allow for limited extension points. Pre/Post message processing requires custom `MessagingProvider` and `MessageProcessor`. This project aims to provide a more OWIN like composable experience.

## Flow control
Currently, flow control looks something like this

    if(ShouldProcess(message))
    {
        var functionResult = RunTrigger(message)
        PostTriggerProcessing(functionResult, message)
    }
    
We want to be able to add in several messages like this

    (context, next) =>
    {
        PreProcessWork()
        next()
        PostProcessWork()
    }
    
So when both these patterns are composed, the flow control should look something like

    PreProcessWork() 1->n
    if(all stages of the pipe are processed)
    {
        var functionResult = RunTrigger(message)
        PostTriggerProcessing(functionResult, message)
        PostProcessWork() n->1
    }

##Tracing
Tracing can be turned on by adding in

	<switches>
	  <add name="Pandora.Azure.WebJobs.PipelineCore" value="Verbose" />
	</switches>
	<sources>
	  <source name="Pandora.Azure.WebJobs.PipelineCore" />
	</sources>

## Installation
You can obtain it [through Nuget](https://www.nuget.org/packages/Pandora.Azure.WebJobs.PipelineCore/) with:

    Install-Package Pandora.Azure.WebJobs.PipelineCore

Or **clone** this repo and reference it.
