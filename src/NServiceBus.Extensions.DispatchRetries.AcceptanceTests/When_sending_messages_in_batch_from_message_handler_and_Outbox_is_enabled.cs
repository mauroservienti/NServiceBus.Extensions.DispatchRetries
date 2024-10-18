using System;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AttributeRouting.AcceptanceTests;
using NUnit.Framework;
using Polly;
using Polly.Retry;

namespace NServiceBus.Extensions.DispatchRetries.AcceptanceTests
{
    public class When_sending_messages_in_batch_from_message_handler_and_Outbox_is_enabled
    {
        const string postgres_connection = "Server=localhost;Port=5432;Database=acceptance_test_database;User Id=test_user;Password=magical_password;";

        [OneTimeSetUp]
        public async Task Setup()
        {
            static Task<bool> checker()
            {
                try
                {
                    /*
                     * database is created by docker compose using the env file
                     * if something goes badly (e.g. container is not ready yet,
                     * we simply fail and return false.
                     */
                    var dbname = "acceptance_test_database";
                    using var connection = new NpgsqlConnection(postgres_connection);

                    using var findDbCommand = new NpgsqlCommand($"SELECT DATNAME FROM pg_catalog.pg_database WHERE DATNAME = '{dbname}'", connection);

                    connection.Open();
                    var foundDbname = findDbCommand.ExecuteScalar();
                    connection.Close();

                    if (foundDbname.ToString() == dbname)
                    {
                        return Task.FromResult(true);
                    }
                }
                catch
                {
                    // ignored
                }

                return Task.FromResult(false);
            }

            await DockerCompose.Up(checker);
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            DockerCompose.Down();
        }

        private static int _numberOfPollyPolicyRetries = 0;
        private static int _numberOfPollyResiliencePipelineRetries = 0;

        [Test]
        public async Task should_be_retried_according_to_policy()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpointWithPolicy));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpointWithPolicy>()
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(_numberOfPollyPolicyRetries, Is.EqualTo(1));
        }
        
        [Test]
        public async Task should_be_retried_according_to_resilience_pipeline()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SenderEndpoint>(g => g.When(b =>
                {
                    var options = new SendOptions();
                    options.SetDestination(nameof(ReceiverEndpointWithResiliencePipeline));

                    return b.Send(new Message(), options);
                }))
                .WithEndpoint<ReceiverEndpointWithResiliencePipeline>()
                .Done(c => c.ReplyMessageReceived)
                .Run();

            Assert.That(context.ReplyMessageReceived, Is.True);
            Assert.That(_numberOfPollyResiliencePipelineRetries, Is.EqualTo(1));
        }

        class Context : ScenarioContext
        {
            public bool ReplyMessageReceived { get; set; }
        }

        class SenderEndpoint : EndpointConfigurationBuilder
        {
            public SenderEndpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                });
            }

            class Handler : IHandleMessages<ReplyMessage>
            {
                private readonly Context _testContext;

                public Handler(Context testContext)
                {
                    _testContext = testContext;
                }

                public Task Handle(ReplyMessage message, IMessageHandlerContext context)
                {
                    _testContext.ReplyMessageReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        class ReceiverEndpointWithPolicy : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithPolicy()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var policy = Policy
                        .Handle<Exception>()
                        .RetryAsync(1, (exception, retryAttempt) =>
                        {
                            _numberOfPollyPolicyRetries++;
                        });

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesPolicy(policy);

                    var persistence = config.UsePersistence<SqlPersistence>();
                    var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
                    dialect.JsonBParameterModifier(parameter =>
                    {
                        var npgsqlParameter = (NpgsqlParameter)parameter;
                        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                    });
                    persistence.TablePrefix("policy");
                    persistence.ConnectionBuilder(() => new NpgsqlConnection(postgres_connection));

                    config.SetReceiveOnlyTransactionMode();
                    config.EnableOutbox();
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    return context.Reply(new ReplyMessage());
                }
            }
        }
        
        class ReceiverEndpointWithResiliencePipeline : EndpointConfigurationBuilder
        {
            public ReceiverEndpointWithResiliencePipeline()
            {
                EndpointSetup<UnreliableServer>(config =>
                {
                    var pipeline = new ResiliencePipelineBuilder()
                        .AddRetry(new RetryStrategyOptions 
                        {
                            MaxRetryAttempts = 1,
                            OnRetry = _ =>
                            {
                                _numberOfPollyResiliencePipelineRetries++;
                                return ValueTask.CompletedTask;
                            }
                        })
                        .Build();

                    var dispatchRetriesOptions = config.DispatchRetries();
                    dispatchRetriesOptions.DefaultBatchDispatchRetriesResiliencePipeline(pipeline);

                    var persistence = config.UsePersistence<SqlPersistence>();
                    var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
                    dialect.JsonBParameterModifier(parameter =>
                    {
                        var npgsqlParameter = (NpgsqlParameter)parameter;
                        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Jsonb;
                    });
                    persistence.TablePrefix("pipeline");
                    persistence.ConnectionBuilder(() => new NpgsqlConnection(postgres_connection));

                    config.SetReceiveOnlyTransactionMode();
                    config.EnableOutbox();
                });
            }

            class Handler : IHandleMessages<Message>
            {
                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    return context.Reply(new ReplyMessage());
                }
            }
        }

        public class Message : IMessage
        {
        }

        public class ReplyMessage : IMessage
        {
        }
    }
}