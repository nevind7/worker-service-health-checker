wip

Worker services health checks

Provides a handy extension method to easily add health checks to your worker services

```c#
services.AddWorkerServiceHealthChecker(opt =>
    {
       opt.Port = 1234; // default = 5000
       opt.Endpoint = "healthz"; 
       opt.UseHttps = false,
       /*opt.Tcp = true; TCP health checker*/
       opt.SilentMode = true; // silence exceptions if service fails to start
    });

services.AddHealthChecks().AddCheck<MyHealthCheck>("MyHealthCheck");
```
