<img src="assets/icon.png" width="100" />

# NServiceBus.Extensions.DispatchRetries

NServiceBus.Extensions.DispatchRetries is designed to allow to plug in [Polly](https://github.com/App-vNext/Polly) into the outgoing pipeline to retry message dispatching using Polly async policies.

## Unreliable environments

There are cases in which the quality of the connection between the NServiceBus endpoint and the underlying transport infrastructure is poor. In such environments, it might happen that a message dispatch operation fails due to a failure in connecting to the underlying transport.

The most common scenario is handling an incoming HTTP request that needs to be transformed into a message for a background service:

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

In unreliable environments, if the above send operation fails, an exception will be thrown and will eventually result in a HTTP500 returned to the caller. Most of the time, retrying is a good enough countermeasure that will succeed.

NServiceBus.Extensions.DispatchRetries can be configured to introduce a Polly async policy into the NServiceBus outgoing pipeline to atomatically retry dispatch operations:

```csharp
var endpointConfiguration = new EndpointConfiguration("myEndpointName");

//define a Polly policy
var retryPolicy = Policy.Handle<Exception>().RetryAsync(3);
//enable DispatchRetries Feature
endpointConfiguration.DispatchRetries(retryPolicy);
```

With the above configuration in place, all _immediate_ and _batch_ dispatch operations will be retried 3 times in case of dispatch failures.

## Immediate and Batch dispatch operations

A dispatch operation is the final act of the message sending request. The dispatch operation is what connects NServiceBus and the underlying transport infrastructure. There are two kinds of dispatch operations: Immediate and Batch (the default).

### Batch dispatch operations

By default, NServiceBus batches dispatch operations that happen in the context of an incoming message, using the following snippet as a sample:

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

Both `MyOtherMessage1` and `MyOtherMessage2` will be dispatched to the transport in a batch. The actual send operation does not happen immediately, it happens at the very end of the pipeline execution.

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

In the above snippet `MyOtherMessage1` and `MyOtherMessage2` are dispatched immediately, `MyOtherMessage3` and `MyOtherMessage4` are batched.

All message operations outside the context of an incoming message are dispatched immediately.

```csharp
var endpointInstance = await Endpoint.Start(endpointConfiguration);
await endpointInstance.Send(new AMessage());
await endpointInstance.Send(new AnotherMessage());
```

In this last case, both `AMessage` and `AnotherMessage` are always dispatched immediately without using any batching.

## Usage

Basic configuration:

```csharp
var endpointConfiguration = new EndpointConfiguration("myEndpointName");

//define a Polly policy
var retryPolicy = Policy.Handle<Exception>().RetryAsync(3);
//enable DispatchRetries Feature
endpointConfiguration.DispatchRetries(retryPolicy);
```

With the above configuration in place, all dispatch operations that fail will be retried 3 times before failing.

It's possible to configure different policies for immediate or batch operations using the following code:

```csharp
var endpointConfiguration = new EndpointConfiguration("myEndpointName");

//enable DispatchRetries Feature
var dispatchRetriesConfig = endpointConfiguration.DispatchRetries();

//define the immediate Polly policy
var immediateRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
//enable immediate dispatch retries using the defined policy
dispatchRetriesConfig.DefaultImmediateDispatchRetriesPolicy(immediateRetryPolicy);

//define the batch Polly policy
var batchRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
//enable batch dispatch retries using the defined policy
dispatchRetriesConfig.DefaultImmediateDispatchRetriesPolicy(batchRetryPolicy);
```

### A note on retrying batch operations

It's important to keep in mind the impact that retrying batch operations can have. When handling an incoming message there is an implicit timeout surrounding the message processing. This timeout can be enforced by the underlying infrastructure or by surrounding transactions. In this context, retrying a batch dispatch counts towards reaching any infrastructure or transaction timeout. Unless it is strictly necessary, it's better to let the [NServiceBus recoverability](https://docs.particular.net/nservicebus/recoverability/) mechanism to handle the failure. There are cases, though, in which retrying an incoming message to handle a dispatch failure might not be desirable, and it might be better to retry a few times the dispatch operation before reverting to the built-in recoverability mechanism. In essence: handle with care.

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

`OverrideBatchDispatchRetryPolicy` can be used in a similar fashion to override the default batch dispatch retries policy.

#### A note on the NServiceBus Outbox

NServiceBus.Extensions.DispatchRetries works with the [NServiceBus Outbox](https://docs.particular.net/nservicebus/outbox/) as well. Due to the way the outbox is implemented the policy applied to outgoing messages is always the immediate dispatch policy when using the outbox.

#### A note on dispatching messages outside the context of an incoming message

When dispatching messages, using either `IMessageSession` or `IEndpointInstance`, messages are immediately dispatched to the underlying transport without any batching. Due to the way NServiceBus works in this case the policy applied to outgoing messages is always the batch dispatch policy. 

## How to install

Using a package manager, add a nuget reference to [NServiceBus.Extensions.DispatchRetries](https://www.nuget.org/packages/NServiceBus.Extensions.DispatchRetries/).

---

Icon [Mail by Flatart](https://thenounproject.com/search/?q=Retry&i=2886080) from the Noun Project
