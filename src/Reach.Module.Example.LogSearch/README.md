# Reach Example LogSearch Module

Cloneable example module that starts a log search job and checks its status later.

```
reach search start --query "hello" --server-name "myserver"
reach search status --correlation-id "<correlation-id>"
```

Use this as a template for modules that submit longer-running work and expose a separate status command. For the smallest example module, see `../Reach.Module.Example.Echo/README.md`.
