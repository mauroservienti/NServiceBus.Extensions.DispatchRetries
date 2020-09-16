(icon here)

# NServiceBus.Extensions.DispatchRetries

NServiceBus.Extensions.DispatchRetries is designed to allow to plug in [Polly](https://github.com/App-vNext/Polly) into the outgoing pipeline to retry message dispatching using Polly policies.

## Basic usage

```
var retryPolicy = Policy.RetryAsync(3);

var endpointConfiguration = new EndpointConfigiration("myEndpointName");
var dispatchRetriesConfig = endpointConfiguration.DispatchRetries();
dispatchRetriesConfig.EnableImmediateDispatchRetries(retryProlicy);
```

---
(icon credits)
