// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pandora.Azure.WebJobs.PipelineCore
{
    public class PipelineProcessor
    {
        #region fields
        private static readonly TraceSource _trace = new TraceSource(Consts.TraceName, SourceLevels.Error);
        private readonly OnMessageOptions _options;
        private readonly List<object> _stages;
        #endregion

        #region constructors
        public PipelineProcessor(OnMessageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
            _stages = new List<object>();
        }
        #endregion

        #region add
        public void Add<TStage>() where TStage : IMessageProcessor
        {
            _stages.Add(typeof(TStage));
        }
        public void Add(Type stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            var imp = typeof(IMessageProcessor);
            if (!stage.GetInterfaces().Contains(imp))
                throw new ArgumentException(nameof(stage));

            _stages.Add(stage);
        }
        public void Add(Func<IPipelineContext, Func<Task>, CancellationToken, Task> stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            _stages.Add(stage);
        }
        #endregion

        #region apm style async processing
        public async Task<bool> BeginProcessingMessageAsync(BrokeredMessage message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (_stages.Count > 0)
            {
                var context = new PipelineContext() { Message = message };
                message.Properties[Consts.PipelineContext] = context;
                await context.PipelineToTrigger.WaitAsync();

                Func<Task> inside = async () => await InnerMostStage(context);
                Func<Task> pipeline = ConstructPipeline(inside, context, cancellationToken);
                Func<Task> outside = async () => await OuterMostStage(context, pipeline);

                var t1 = context.PipelineToTrigger.WaitAsync();
                var t2 = outside();

                await Task.WhenAny(t1, t2);

                if (context.IsFaulted)
                    throw context.Exception;

                return t1.Status == TaskStatus.RanToCompletion;
            }
            else
                return true;
        }
        public async Task CompleteProcessingMessageAsync(BrokeredMessage message, FunctionResult result, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _trace.TraceEvent(TraceEventType.Verbose, 3, "Pipeline - Trigger done({1}) - {0}", message.MessageId, result.Succeeded);

            await MessageCleanupAsync(message, result, cancellationToken);

            _trace.TraceEvent(TraceEventType.Verbose, 4, "Pipeline - ServiceBus updated - {0}", message.MessageId);

            var context = message.Properties[Consts.PipelineContext] as PipelineContext;

            if (context == null)
                throw new ApplicationException(Consts.LostContext);

            context.Result = result;
            context.TriggerRan.Release();
            await context.TriggerToPipeline.WaitAsync();

            if (context.IsFaulted)
                throw context.Exception;
        }
        #endregion

        #region tools
        private async Task MessageCleanupAsync(BrokeredMessage message, FunctionResult result, CancellationToken cancellationToken)
        {
            if (result.Succeeded)
            {
                if (!_options.AutoComplete)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await message.CompleteAsync();
                }
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                await message.AbandonAsync();
            }
        }
        internal async Task InnerMostStage(PipelineContext context)
        {
            _trace.TraceEvent(TraceEventType.Verbose, 2, "Pipeline - At trigger - {0}", context.Message.MessageId);
            context.PipelineToTrigger.Release();
            await context.TriggerRan.WaitAsync();
            _trace.TraceEvent(TraceEventType.Verbose, 5, "Pipeline - Continuing - {0}", context.Message.MessageId);
        }
        internal async Task OuterMostStage(PipelineContext context, Func<Task> pipeline)
        {
            _trace.TraceEvent(TraceEventType.Verbose, 1, "Pipeline - Starting - {0}", context.Message.MessageId);

            await context.TriggerRan.WaitAsync();
            await context.TriggerToPipeline.WaitAsync();

            try
            {
                await pipeline();
            }
            catch (Exception ex)
            {
                context.IsFaulted = true;
                context.Exception = ex;
            }
            finally
            {
                context.TriggerRan.Release();
                context.TriggerToPipeline.Release();
            }
        }
        internal Func<Task> ConstructPipeline(Func<Task> innerMostTask, PipelineContext context, CancellationToken cancellationToken)
        {
            foreach (var t in _stages.Reverse<object>())
            {
                var next = innerMostTask;
                if (t is Type)
                {
                    var f = Activator.CreateInstance((Type)t) as IMessageProcessor;
                    innerMostTask = async () =>
                    {
                        await f.Invoke(context, next, cancellationToken);
                    };
                }
                else
                {
                    var f = (Func<IPipelineContext, Func<Task>, CancellationToken, Task>)t;
                    innerMostTask = async () =>
                    {
                        await f(context, next, cancellationToken);
                    };
                }
            }

            return innerMostTask;
        }
        #endregion
    }
}
