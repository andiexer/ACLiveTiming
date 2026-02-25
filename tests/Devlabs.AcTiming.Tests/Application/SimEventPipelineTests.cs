using Devlabs.AcTiming.Application.EventRouting.Pipeline;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Abstractions;
using Devlabs.AcTiming.Application.EventRouting.Pipeline.Sink;
using Devlabs.AcTiming.Application.Shared;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging.Abstractions;

namespace Devlabs.AcTiming.Tests.Application;

public class SimEventPipelineTests
{
    [Fact]
    public async Task SimEventPipeline_WithPreEnricher_ShouldEnrichAndPublish()
    {
        // Arrange
        var preEnricher = new PreEnricher();
        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [preEnricher],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            sink.PublishedEvents.Count.Should().Be(2);
            sink.PublishedEvents[0]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("pre-event");
            sink.PublishedEvents[1]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("original-event");
        }
    }

    [Fact]
    public async Task SimEventPipeline_WithNoEnrichers_ShouldPublishOriginalEventOnly()
    {
        // Arrange
        var sink = new ListSink();
        var pipeline = new SimEventPipeline(NullLogger<SimEventPipeline>.Instance, [], sink);

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        sink.PublishedEvents.Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<TestEvent>()
            .Which.Identifier.Should()
            .Be("original-event");
    }

    [Fact]
    public async Task SimEventPipeline_WithPostEnricher_ShouldEnrichAndPublishInOrder()
    {
        // Arrange
        var postEnricher = new PostEnricher();
        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [postEnricher],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            sink.PublishedEvents.Count.Should().Be(2);
            sink.PublishedEvents[0]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("original-event");
            sink.PublishedEvents[1]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("post-event");
        }
    }

    [Fact]
    public async Task SimEventPipeline_WithPreAndPostEnrichers_ShouldEnrichAndPublishInCorrectOrder()
    {
        // Arrange
        var preEnricher = new PreEnricher();
        var postEnricher = new PostEnricher();
        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [preEnricher, postEnricher],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            sink.PublishedEvents.Count.Should().Be(3);
            sink.PublishedEvents[0]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("pre-event");
            sink.PublishedEvents[1]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("original-event");
            sink.PublishedEvents[2]
                .Should()
                .BeOfType<TestEvent>()
                .Which.Identifier.Should()
                .Be("post-event");
        }
    }

    [Fact]
    public async Task SimEventPipeline_WithMultiplePreEnrichers_ShouldPreserveRegistrationOrder()
    {
        // Arrange
        var pre1 = new InlineEnricher(EnricherPhase.Pre, "pre-1");
        var pre2 = new InlineEnricher(EnricherPhase.Pre, "pre-2");
        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [pre1, pre2],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        sink.PublishedEvents.Select(e => ((TestEvent)e).Identifier)
            .Should()
            .Equal("pre-1", "pre-2", "original-event");
    }

    [Fact]
    public async Task SimEventPipeline_WhenAnEnricherThrows_ShouldContinueWithMainEventAndOtherEnrichers()
    {
        // Arrange
        var failingPre = new ThrowingEnricher(EnricherPhase.Pre);
        var succeedingPre = new InlineEnricher(EnricherPhase.Pre, "pre-ok");
        var post = new InlineEnricher(EnricherPhase.Post, "post-ok");

        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [failingPre, succeedingPre, post],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        sink.PublishedEvents.Select(e => ((TestEvent)e).Identifier)
            .Should()
            .Equal("pre-ok", "original-event", "post-ok");
    }

    [Fact]
    public async Task SimEventPipeline_WhenSinkThrowsWhilePublishingEnrichedEvent_ShouldSkipRestOfThatEnricherButStillPublishMainEvent()
    {
        // Arrange
        var pre = new MultiEventEnricher(
            EnricherPhase.Pre,
            new TestEvent("bad"), // will throw in sink
            new TestEvent("pre-ok") // should NOT be published because the enricher aborts after exception
        );

        var post = new InlineEnricher(EnricherPhase.Post, "post-ok");

        var sink = new ConditionalThrowSink(ev => ev is TestEvent { Identifier: "bad" });
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [pre, post],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        sink.PublishedEvents.Select(e => ((TestEvent)e).Identifier)
            .Should()
            .Equal("original-event", "post-ok");
    }

    [Fact]
    public async Task SimEventPipeline_ShouldPassOriginalEventToEnrichers()
    {
        // Arrange
        var capturingPre = new CapturingEnricher(EnricherPhase.Pre, "pre-ok");
        var capturingPost = new CapturingEnricher(EnricherPhase.Post, "post-ok");

        var sink = new ListSink();
        var pipeline = new SimEventPipeline(
            NullLogger<SimEventPipeline>.Instance,
            [capturingPre, capturingPost],
            sink
        );

        var inputEvent = new TestEvent("original-event");

        // Act
        await pipeline.RouteAsync(inputEvent, CancellationToken.None);

        // Assert
        using (new AssertionScope())
        {
            capturingPre.ReceivedEvent.Should().BeSameAs(inputEvent);
            capturingPost.ReceivedEvent.Should().BeSameAs(inputEvent);
        }
    }

    private record TestEvent(string Identifier) : SimEvent;

    private class PreEnricher : ISimEventEnricher
    {
        public EnricherPhase Phase => EnricherPhase.Pre;

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct)
        {
            var preEvent = new TestEvent("pre-event");
            return ValueTask.FromResult<IReadOnlyList<SimEvent>>(new List<SimEvent> { preEvent });
        }
    }

    private class PostEnricher : ISimEventEnricher
    {
        public EnricherPhase Phase => EnricherPhase.Post;

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct)
        {
            var postEvent = new TestEvent("post-event");
            return ValueTask.FromResult<IReadOnlyList<SimEvent>>(new List<SimEvent> { postEvent });
        }
    }

    private sealed class InlineEnricher(EnricherPhase phase, string enrichedIdentifier)
        : ISimEventEnricher
    {
        public EnricherPhase Phase => phase;

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct)
        {
            IReadOnlyList<SimEvent> events = [new TestEvent(enrichedIdentifier)];
            return ValueTask.FromResult(events);
        }
    }

    private sealed class MultiEventEnricher(EnricherPhase phase, params SimEvent[] enrichedEvents)
        : ISimEventEnricher
    {
        public EnricherPhase Phase => phase;

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(
            SimEvent @event,
            CancellationToken ct
        ) => ValueTask.FromResult<IReadOnlyList<SimEvent>>(enrichedEvents);
    }

    private sealed class ThrowingEnricher(EnricherPhase phase) : ISimEventEnricher
    {
        public EnricherPhase Phase => phase;

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(
            SimEvent @event,
            CancellationToken ct
        ) => throw new InvalidOperationException("Boom from enricher");
    }

    private sealed class CapturingEnricher(EnricherPhase phase, string enrichedIdentifier)
        : ISimEventEnricher
    {
        public EnricherPhase Phase => phase;

        public SimEvent? ReceivedEvent { get; private set; }

        public ValueTask<IReadOnlyList<SimEvent>> EnrichAsync(SimEvent @event, CancellationToken ct)
        {
            ReceivedEvent = @event;
            IReadOnlyList<SimEvent> events = [new TestEvent(enrichedIdentifier)];
            return ValueTask.FromResult(events);
        }
    }

    private class ListSink : ISimEventSink
    {
        public List<SimEvent> PublishedEvents { get; } = new();

        public ValueTask PublishAsync(SimEvent ev, CancellationToken ct)
        {
            PublishedEvents.Add(ev);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ConditionalThrowSink(Func<SimEvent, bool> shouldThrow) : ISimEventSink
    {
        public List<SimEvent> PublishedEvents { get; } = new();

        public ValueTask PublishAsync(SimEvent ev, CancellationToken ct)
        {
            if (shouldThrow(ev))
                throw new InvalidOperationException("Boom from sink");

            PublishedEvents.Add(ev);
            return ValueTask.CompletedTask;
        }
    }
}
