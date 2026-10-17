# Testing modules in isolation

Run a module's TUnit executable from the repository root:

```sh
dotnet run --project tests/Reach.Module.Example.Echo.Tests
dotnet run --project tests/Reach.Module.Example.LogSearch.Tests
```

A failed test returns a nonzero exit code. These tests instantiate the real work classes directly and need no Reach processes, relay, credentials, or database setup.

`EchoFormatterTests` demonstrates data-driven checks, including empty messages and preserved whitespace. `FileSearcherTests` calls the real `FileSearcher` with a null logger and checks its current simulated result and cancellation. The simulation takes up to five seconds and does not search files. Its result assertions are temporary checks, not proof that file searching works.

## Adding tests for a module

1. Create `tests/<Module>.Tests` using either example project, reference the module, and add the project to `Reach.slnx`.
2. If the implementation is internal, add an `InternalsVisibleTo` item for the matching test assembly in the module project.
3. Construct the implementation directly with explicit dependencies. When an interface such as `IFileSearcher` exists, exercise its real implementation. Keep command dispatch and transport out of these tests.
4. Assert observable results, relevant edge cases, and cancellation. Await TUnit assertions. Pass `TestContext.Current!.Execution.CancellationToken` to asynchronous work unless the test owns a cancellation source.
5. Run the module's test project, then `dotnet csharpier format .` and `dotnet build` before finishing.

When real file searching is implemented, replace the simulation test with tests that create a unique directory using `Directory.CreateTempSubdirectory`, write known file contents, and pass that directory to the searcher. Assert matching content and line numbers, no matches, and any supported case-sensitivity rules. Delete the directory recursively in a `finally` block so cleanup runs after assertion failures too. Each test should own its files because TUnit can run tests concurrently.
