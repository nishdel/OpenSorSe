using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TidyMind.Application;
using TidyMind.Application.Models;
using TidyMind.Core.Logging;
using TidyMind.Scanner.Models;

namespace TidyMind.Application.Tests;

/// <summary>Verifies process-lifetime processing-session state transitions.</summary>
public sealed class ProcessingSessionManagerTests
{
    /// <summary>Verifies completed sessions receive unique identifiers, terminal state, and explicit close behavior.</summary>
    [Fact]
    public async Task RunAsync_CompletedProcessing_TracksAndClosesSession()
    {
        var manager = new ProcessingSessionManager(new CompletedOrchestrator(), new Logging());

        var result = await manager.RunAsync(Request());

        Assert.Equal(ProcessingSessionStatus.Completed, result.Session.Status);
        Assert.StartsWith("session:", result.Session.Id);
        Assert.NotNull(result.Session.CompletedAtUtc);
        Assert.Single(manager.Sessions);
        Assert.True(manager.TryClose(result.Session.Id));
        Assert.Equal(ProcessingSessionStatus.Closed, manager.Sessions[0].Status);
    }

    /// <summary>Verifies unexpected orchestrator failure remains represented by a user-safe terminal session.</summary>
    [Fact]
    public async Task RunAsync_UnexpectedFailure_ReturnsTrackedFailedSession()
    {
        var manager = new ProcessingSessionManager(new FailingOrchestrator(), new Logging());

        var result = await manager.RunAsync(Request());

        Assert.Equal(ProcessingSessionStatus.Failed, result.Session.Status);
        Assert.Null(result.Processing);
        Assert.Equal("The processing session could not be completed.", result.Session.FailureMessage);
    }

    private static ProcessingRequest Request() => new(new ScanRequest(["C:\\Root"], ScanOptions.Default), []);
    private static ProcessingResult CompletedResult() => new(ProcessingStatus.Completed, new ScanResult([], [], new ScanStatistics(0, 0, 0), [], ScanStatus.Completed, TimeSpan.Zero), null, null, null, null, null, null, null);

    private sealed class CompletedOrchestrator : IProcessingOrchestrator
    {
        public Task<ProcessingResult> ProcessAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default) => Task.FromResult(CompletedResult());
    }
    private sealed class FailingOrchestrator : IProcessingOrchestrator
    {
        public Task<ProcessingResult> ProcessAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default) => Task.FromException<ProcessingResult>(new InvalidOperationException());
    }
    private sealed class Logging : ILoggingService
    {
        public void Initialize(LogLevel minimumLevel) { }
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() { }
    }
}
