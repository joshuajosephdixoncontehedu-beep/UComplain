using Xunit;

// WebApplicationFactory<Program> relies on a process-wide diagnostic listener
// (HostFactoryResolver.HostingListener) that intercepts "the next" host build. xUnit
// parallelizes test classes (collections) by default, so two integration test classes
// (AuthEndpointsTests, ReportsAndVerificationTests) each spinning up their own
// WebApplicationFactory can race on that shared hook — one factory's host build steals
// the interception meant for the other, which then fails with "The entry point exited
// without ever building an IHost." Disabling parallelization for this assembly makes
// that race impossible; the suite is small enough that running sequentially costs a
// few seconds, not minutes.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
