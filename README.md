(icon here)

# NServiceBus.Extensions.DispatchRetries

NServiceBus.Extensions.DispatchRetries is designed to allow to plug in [Polly](https://github.com/App-vNext/Polly) into the outgoing pipeline to retry message dispatching using Polly policies.

## Basic usage

```csharp
var endpointConfiguration = new EndpointConfigiration("myEndpointName");

//define a Polly policy
var retryPolicy = Policy.RetryAsync(3);

var dispatchRetriesConfig = endpointConfiguration.DispatchRetries();

//enable immediate dispatch retries using the defined policy
dispatchRetriesConfig.EnableImmediateDispatchRetries(retryProlicy);
```

---

Icon [Mail by Flatart](https://thenounproject.com/search/?q=Retry&i=2886080) from the Noun Project
