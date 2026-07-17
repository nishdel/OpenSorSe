using OpenSorSe.Application;
using OpenSorSe.Application.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>Verifies the controller delegates UI-agnostic processing requests to sessions.</summary>
public sealed class ApplicationControllerTests
{
    /// <summary>Verifies a controller forwards the original request and returns the session result unchanged.</summary>
    [Fact]
    public async Task StartProcessingAsync_ForwardsRequestToSessionManager()
    {
        var manager = new RecordingSessionManager();
        var controller = new ApplicationController(manager);
        var request = new ProcessingRequest(new ScanRequest(["C:\\Root"], ScanOptions.Default), []);

        var result = await controller.StartProcessingAsync(request);

        Assert.Same(request, manager.Request);
        Assert.Same(manager.Result, result);
    }

    private sealed class RecordingSessionManager : IProcessingSessionManager
    {
        public ProcessingRequest? Request { get; private set; }
        public ProcessingSessionResult Result { get; } = new(new ProcessingSession("session:test", new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null, ProcessingSessionStatus.Cancelled, null), null);
        public IReadOnlyList<ProcessingSession> Sessions => [];
        public event EventHandler<ProcessingSession>? SessionChanged
        {
            add { }
            remove { }
        }
        public Task<ProcessingSessionResult> RunAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default) { Request = request; return Task.FromResult(Result); }
        public bool TryClose(string sessionId) => false;
    }
}
