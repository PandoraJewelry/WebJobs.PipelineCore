// Copyright (c) PandoraJewelry. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

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
        #endregion

        #region add
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullFunction1()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            Type stage = null;

            pipeline.Add(stage);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullFunction2()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            Func<IPipelineContext, Func<Task>, CancellationToken, Task> stage = null;

            pipeline.Add(stage);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddNotMessageProcessor()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            Type stage = typeof(string);

            pipeline.Add(stage);
        } 
        #endregion


        #region exceptions
        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public async Task ExceptionInPipePreTrigger()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);

            pipeline.Add((ctx, next, cancel) =>
            {
                throw new DivideByZeroException();
            });

            await pipeline.BeginProcessingMessageAsync(message, token.Token);
        }
        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public async Task ExceptionInDoublePipePreTrigger()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = true;
                await next();
            });
            pipeline.Add((ctx, next, cancel) =>
            {
                throw new DivideByZeroException();
            });

            try
            {
                await pipeline.BeginProcessingMessageAsync(message, token.Token);
            }
            catch
            {
                Assert.IsTrue(pre);
                throw;
            }
        }
        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public async Task ExceptionInPipePostTrigger()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre = false;
            var result = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = true;
                await next();
                throw new DivideByZeroException();
            });

            try
            {
                result = await pipeline.BeginProcessingMessageAsync(message, token.Token);
                await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);
            }
            catch
            {
                Assert.IsTrue(pre);
                Assert.IsTrue(result);
                throw;
            }
        }
        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public async Task ExceptionInDoublePipePostTrigger1()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre1 = false;
            var pre2 = false;
            var post = false;
            var result = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre1 = true;
                await next();
                post = true;
            });
            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre2 = true;
                await next();
                throw new DivideByZeroException();
            });

            try
            {
                result = await pipeline.BeginProcessingMessageAsync(message, token.Token);
                await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);
            }
            catch
            {
                Assert.IsTrue(pre1);
                Assert.IsTrue(pre2);
                Assert.IsFalse(post);
                Assert.IsTrue(result);
                throw;
            }
        }
        [TestMethod]
        [ExpectedException(typeof(DivideByZeroException))]
        public async Task ExceptionInDoublePipePostTrigger2()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre1 = false;
            var pre2 = false;
            var post = false;
            var result = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre1 = true;
                await next();
                throw new DivideByZeroException();
            });
            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre2 = true;
                await next();
                post = true;
            });

            try
            {
                result = await pipeline.BeginProcessingMessageAsync(message, token.Token);
                await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);
            }
            catch
            {
                Assert.IsTrue(pre1);
                Assert.IsTrue(pre2);
                Assert.IsTrue(post);
                Assert.IsTrue(result);
                throw;
            }
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

        #region context
        [TestMethod]
        public async Task ContextPassesToDifferentStagesForward()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var key = "xxx";
            var actual = "zzz";
            var expected = actual + actual;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                ctx.Enviorment[key] = expected;
                await next();
            });
            pipeline.Add(async (ctx, next, cancel) =>
            {
                if (ctx.Enviorment.ContainsKey(key))
                    actual = ctx.Enviorment[key] as string;
                await next();
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(result);

            await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);

            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public async Task ContextPassesToDifferentStagesReverse()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var key = "xxx";
            var actual = "zzz";
            var expected = actual + actual;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                await next();
                if (ctx.Enviorment.ContainsKey(key))
                    actual = ctx.Enviorment[key] as string;
            });
            pipeline.Add(async (ctx, next, cancel) =>
            {

                await next();
                ctx.Enviorment[key] = expected;
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(result);

            await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);

            Assert.AreEqual(expected, actual);
        }
        [TestMethod]
        public async Task ResultAvailableAfterTrigger()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            var pre = false;
            var post = false;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = ctx.Result == null;
                await next();
                post = ctx.Result != null;
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(result);

            await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);

            Assert.IsTrue(pre);
            Assert.IsTrue(post);
        }
        [TestMethod]
        public async Task RawMessageAvailable()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var message = new BrokeredMessage();
            var token = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);
            BrokeredMessage pre = null;
            BrokeredMessage post = null;

            pipeline.Add(async (ctx, next, cancel) =>
            {
                pre = ctx.Message;
                await next();
                post = ctx.Message;
            });

            var result = await pipeline.BeginProcessingMessageAsync(message, token.Token);

            Assert.IsTrue(result);

            await pipeline.CompleteProcessingMessageAsync(message, funcresult, token.Token);

            Assert.AreSame(message, pre);
            Assert.AreSame(message, post);
        }
        #endregion

        #region threads
        [TestMethod]
        public async Task ContextsDontMixMessages1()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var message1 = new BrokeredMessage() { MessageId = id1 };
            var message2 = new BrokeredMessage() { MessageId = id2 };
            var token1 = new CancellationTokenSource();
            var token2 = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);

            pipeline.Add(async (ctx, next, cancel) =>
            {
                ctx.Enviorment["id"] = ctx.Message.MessageId;
                await next();
                ctx.Message.MessageId = ctx.Enviorment["id"] as string;
            });

            var result1 = await pipeline.BeginProcessingMessageAsync(message1, token1.Token);
            var result2 = await pipeline.BeginProcessingMessageAsync(message2, token1.Token);

            await pipeline.CompleteProcessingMessageAsync(message1, funcresult, token1.Token);
            await pipeline.CompleteProcessingMessageAsync(message2, funcresult, token2.Token);

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
            Assert.AreEqual(id1, message1.MessageId);
            Assert.AreEqual(id2, message2.MessageId);
        }
        [TestMethod]
        public async Task ContextsDontMixMessages2()
        {
            var pipeline = new PipelineProcessor(new OnMessageOptions());
            var id1 = Guid.NewGuid().ToString();
            var id2 = Guid.NewGuid().ToString();
            var message1 = new BrokeredMessage() { MessageId = id1 };
            var message2 = new BrokeredMessage() { MessageId = id2 };
            var token1 = new CancellationTokenSource();
            var token2 = new CancellationTokenSource();
            var funcresult = new FunctionResult(true);

            pipeline.Add(async (ctx, next, cancel) =>
            {
                ctx.Enviorment["id"] = ctx.Message.MessageId;
                await next();
                ctx.Message.MessageId = ctx.Enviorment["id"] as string;
            });

            var result1 = await pipeline.BeginProcessingMessageAsync(message1, token1.Token);
            var result2 = await pipeline.BeginProcessingMessageAsync(message2, token1.Token);

            await pipeline.CompleteProcessingMessageAsync(message2, funcresult, token2.Token);
            await pipeline.CompleteProcessingMessageAsync(message1, funcresult, token1.Token);            

            Assert.IsTrue(result1);
            Assert.IsTrue(result2);
            Assert.AreEqual(id1, message1.MessageId);
            Assert.AreEqual(id2, message2.MessageId);
        }
        #endregion

    }
}

