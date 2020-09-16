<img src="assets/icon.png" width="100" />

# NServiceBus.Extensions.DispatchRetries

NServiceBus.Extensions.DispatchRetries is designed to allow to plug in [Polly](https://github.com/App-vNext/Polly) into the outgoing pipeline to retry message dispatching using Polly async policies.

## Unreliable environments

There are cases in which the quality of the connection between the NServiceBus endpoint and the underlying transport infrastructure is poor. In such environments it might happen that a message dispatch operation fails due to a failure in connecting to the underlying transport.

The most common scenario being handling an incoming HTTP request that needs to be tranformed into a message for a background service:

```csharp
class MyController
{
    IMessageSession _messageSession;

    public MyController(IMessageSession messageSession)
    {
        _messageSession = messageSession;
    }

    [HttpPost]
    public IActionResult Checkout(int cartId)
    {
        _messageSession.Send(new StartCheckoutProcess(cartId));
    }
}
```

In unrealiable environments if the above send operation fails an excpetion will be thrown and will eventually result in a HTTP500 returned to the caller. Most of the times retrying is a good enough countermeasure that will succeed.

NServiceBus.Extensions.DispatchRetries can be configured to introduce a Polly async policy into the NServiceBus outgoing pipeline to atomatically retry dispatch operations:

```csharp
var endpointConfiguration = new EndpointConfigiration("myEndpointName");

//Enable DispatchRetries Feature
var dispatchRetriesConfig = endpointConfiguration.DispatchRetries();

//define a Polly policy
var retryPolicy = Policy.RetryAsync(3);

//enable immediate dispatch retries using the defined policy
dispatchRetriesConfig.DefaultImmediateDispatchRetriesPolicy(retryProlicy);
```

With the above configuration in place, all _immediate_ dispatch operations will be retried 3 times in case of dispatch failures.

## Immediate and Batch dispatch operations

A dispatch operation is the final act of the message sending request. The dispatch operation is what connects NServiceBus and the underlying transport infrastructure. There are two kinds of dispatch operations: Immediate and Batch (the default).

### Batch dispatch operations

By default NServiceBus batches dispatch operations that happen in the context of an incoming message, using the following snippet as a sample:

```csharp
class MyMessageHandler : IHandleMessages<MyMessage>
{
    public async Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        await context.Send(new MyOtherMessage1());
        await context.Send(new MyOtherMessage2());
    }
}
```

Both `MyOtherMessage1` and `MyOtherMessage2` will be dispatch to the transport in a batch. The actual send operation does not happen immidiately, it happens when messages are dispatched to the transport at the very end of the pipeline execution.

### Immediate dispatch operations

NServiceBus allows to influence the message dispatching behavior using `SendOptions`:

```csharp
class MyMessageHandler : IHandleMessages<MyMessage>
{
    public async Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        var sendOptionsForMessage1 = new SendOptions();
        sendOptionsForMessage1.RequireImmediateDispatch();
        await context.Send(new MyOtherMessage1(), sendOptionsForMessage1);

        var sendOptionsForMessage2 = new SendOptions();
        sendOptionsForMessage2.RequireImmediateDispatch();
        await context.Send(new MyOtherMessage2(), sendOptionsForMessage2);

        await context.Send(new MyOtherMessage3());
        await context.Send(new MyOtherMessage4());
    }
}
```

In the above snippet `MyOtherMessage1` and `MyOtherMessage2` are dispatched immidiately, `MyOtherMessage3` and `MyOtherMessage4` are batched.

All message operations outside the context of an incoming message are dispatched immidiately.

```csharp
var endpointInstance = await Endpoint.Start(endpointConfiguration);
await endpointInstance.Send(new AMessage());
await endpointInstance.Send(new AnotherMessage());
```

In this last case, both `AMessage` and `AnotherMessage` are always dispatched immidiately without using any batching.

## Usage

Basic configuration:

```csharp
var endpointConfiguration = new EndpointConfigiration("myEndpointName");

//Enable DispatchRetries Feature
var dispatchRetriesConfig = endpointConfiguration.DispatchRetries();

//define a Polly policy
var retryPolicy = Policy.RetryAsync(3);

//enable immediate dispatch retries using the defined policy
dispatchRetriesConfig.DefaultImmediateDispatchRetriesPolicy(retryProlicy);
```

### Retrying batch operations

NServiceBus.Extensions.DispatchRetries can be configured to retry batch operations as well:

```csharp
//define a Polly policy
var retryPolicy = Policy.RetryAsync(3);

//enable immediate dispatch retries using the defined policy
dispatchRetriesConfig.DefaultBatchDispatchRetriesPolicy(retryProlicy);
```

It's important to keepo in mind the impact that retrying batch operations can have. When handling an incoming message there an implicit timeout surrounding the message processing. This timeout can be enforced by the underlying infrastructure or by surrounding transactions. In this context retrying a batch dispatch counts towards reaching any infrastructure or transaction timeout. Unless it is strictly necessary it's better to let the [NServiceBus recoverability](https://docs.particular.net/nservicebus/recoverability/) mechanism to handle the failure. There cases, though, in which retrying an incoming message to handle a dispatch failure might not be desirable and it might be better to retry a few times the dispatch operation before reverting to the built-in recoverability mechanism. In essence: handle with care.

### Overrides

When in the context of an incoming message, it might happen that the default configured retry policy doesn't satisfy the requirements. It's possible to override the default configured policy at the message handling context level:

```csharp
class MyMessageHandler : IHandleMessages<MyMessage>
{
    public async Task Handle(MyMessage message, IMessageHandlerContext context)
    {
        var customPolicy = Policy.RetryAsync(8);
        context.OverrideImmediateDispatchRetryPolicy(customPolicy)

        var options = new SendOptions();
        options.RequireImmediateDispatch();
        await context.Send(new MyOtherMessage1(), options);
    }
}
```

## A not on the NServiceBus Outbox

NServiceBus.Extensions.DispatchRetries works with the [NServiceBus Outbox](https://docs.particular.net/nservicebus/outbox/) as well. Due to the way the outbox is implemented the policy applied to outgoing messages is always the immediate dispatch policy when using the outbox.

---

Icon [Mail by Flatart](https://thenounproject.com/search/?q=Retry&i=2886080) from the Noun Project
