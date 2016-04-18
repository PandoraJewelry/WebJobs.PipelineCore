# Azure Webjobs SDK Pipelines for Azure ServiceBus

## Our use case
Webjobs currently allow for limited extension points. Pre/Post message processing requires custom `MessagingProvider` and `MessageProcessor`. This project aims to provide a more OWIN like composable experience.

## Installation

You can obtain it [through Nuget](https://www.nuget.org/packages/Pandora.Azure.WebJobs.PipelineCore/) with:

    Install-Package Pandora.Azure.WebJobs.PipelineCore

Or **clone** this repo and reference it.
