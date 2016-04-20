using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Azure.WebJobs.Host.Executors;

namespace Pandora.Azure.WebJobs.PipelineCore.Tests
{
    [TestClass]
    public class PipelineProcessorTest
    {
        #region constructors
        [TestMethod]
        public void CtorBasicFlow()
        {
            var options = new OnMessageOptions() { AutoComplete = true };

            new PipelineProcessor(options);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CtorNullOptions()
        {
            new PipelineProcessor(null);
        }
        #endregion

        #region basic errors
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task BeginProcessingMessageAsyncNullMessage()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var token = new CancellationTokenSource();

            await pipeline.BeginProcessingMessageAsync(null, token.Token);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteProcessingMessageAsyncNullMessage()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var funcresult = new FunctionResult(true);
            var token = new CancellationTokenSource();

            await pipeline.CompleteProcessingMessageAsync(null, funcresult, token.Token);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CompleteProcessingMessageAsyncNullResult()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();

            await pipeline.CompleteProcessingMessageAsync(message, null, token.Token);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullFunction()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());

            pipeline.Add(null);
        }
        #endregion

        #region processing
        [TestMethod]
        public async Task BeginProcessingMessageAsyncEmptyStages()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(result);
        }
        [TestMethod]
        public async Task BeginProcessingMessageAsyncSingleStage()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre = false;
            var post = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = true;
                await next();
                post = true;
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(pre);
            Assert.IsTrue(result);

            await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);

            Assert.IsTrue(post);
        }
        [TestMethod]
        public async Task BeginProcessingMessageAsyncSingleDoNotProcess()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var entered = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                entered = true;
                await Task.Delay(0);
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsFalse(result);
            Assert.IsTrue(entered);
        }
        [TestMethod]
        public async Task BeginProcessingMessageAsyncDoubleDoNotProcess()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var entered = false;
            var pre = false;
            var post = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = true;
                await next();
                post = true;
            });
            pipeline.Add(async (ctx, next, cancel) =>
            {
                entered = true;
                await Task.Delay(0);
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsFalse(result);
            Assert.IsTrue(entered);
            Assert.IsTrue(pre);
            Assert.IsTrue(post);
        }
        #endregion
    }
}

