using App.Services;
using Sori.Core.Models;

namespace Tests;

public class QueueServiceTests
{
    [Fact]
    public void MoveNext_WithRepeatOff_StopsAtEnd()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }, new Song { Id = "2", Title = "B" }], 0);

        Assert.Equal("A", queue.Current?.Title);
        Assert.Equal("B", queue.MoveNext()?.Title);
        Assert.Null(queue.MoveNext());
    }

    [Fact]
    public void MoveNext_WithRepeatAll_WrapsToStart()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }, new Song { Id = "2", Title = "B" }], 0);
        queue.SetRepeatMode(RepeatMode.All);

        Assert.Equal("A", queue.Current?.Title);
        Assert.Equal("B", queue.MoveNext()?.Title);
        var wrapped = queue.MoveNext();

        Assert.Equal("A", wrapped?.Title);
    }

    [Fact]
    public void MoveNext_WithRepeatOne_ReturnsCurrent()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }, new Song { Id = "2", Title = "B" }], 0);
        queue.SetRepeatMode(RepeatMode.One);

        var current = queue.Current;
        var next = queue.MoveNext();

        Assert.Equal(current?.Id, next?.Id);
    }

    [Fact]
    public void MoveNext_WithShuffle_DoesNotReturnSameTrackWhenPossible()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);
        queue.SetShuffle(true);

        var next = queue.MoveNext();

        Assert.NotNull(next);
        Assert.NotEqual("A", next?.Title);
    }

    [Fact]
    public void MovePrevious_WithShuffle_UsesHistory()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);
        queue.SetShuffle(true);

        queue.MoveNext();
        var previous = queue.MovePrevious();

        Assert.Equal("A", previous?.Title);
    }

    [Fact]
    public void CycleRepeatMode_CyclesOffAllOneOff()
    {
        var queue = new QueueService();

        Assert.Equal(RepeatMode.Off, queue.RepeatMode);

        queue.CycleRepeatMode();
        Assert.Equal(RepeatMode.All, queue.RepeatMode);

        queue.CycleRepeatMode();
        Assert.Equal(RepeatMode.One, queue.RepeatMode);

        queue.CycleRepeatMode();
        Assert.Equal(RepeatMode.Off, queue.RepeatMode);
    }

    [Fact]
    public void ToggleShuffle_ChangesState()
    {
        var queue = new QueueService();

        Assert.False(queue.ShuffleEnabled);

        queue.ToggleShuffle();
        Assert.True(queue.ShuffleEnabled);

        queue.ToggleShuffle();
        Assert.False(queue.ShuffleEnabled);
    }

    [Fact]
    public void PeekNext_WithRepeatAllAtEnd_ReturnsFirst()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }, new Song { Id = "2", Title = "B" }], 1);
        queue.SetRepeatMode(RepeatMode.All);

        var peeked = queue.PeekNext();

        Assert.Equal("A", peeked?.Title);
    }

    [Fact]
    public void PeekNext_WithShuffle_DoesNotStorePlanned()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);
        queue.SetShuffle(true);

        var peeked = queue.PeekNext();
        var next = queue.MoveNext();

        // PeekNext no longer stores planned; MoveNext may pick a different track.
        Assert.NotNull(peeked);
        Assert.NotNull(next);
        Assert.NotEqual("A", next?.Title);
    }

    [Fact]
    public void Remove_BeforeCurrent_DecrementsCurrentIndex()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 1);

        queue.Remove(new Song { Id = "1", Title = "A" });

        Assert.Equal(0, queue.CurrentIndex);
        Assert.Equal("B", queue.Current?.Title);
    }

    [Fact]
    public void Remove_Current_SkipsToNext()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);

        queue.Remove(new Song { Id = "1", Title = "A" });

        Assert.Equal(0, queue.CurrentIndex);
        Assert.Equal("B", queue.Current?.Title);
    }

    [Fact]
    public void AddNext_InShuffle_SetsPlannedNext()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);
        queue.SetShuffle(true);

        queue.AddNext(new Song { Id = "4", Title = "D" });

        var peeked = queue.PeekNext();
        var next = queue.MoveNext();

        Assert.Equal("D", peeked?.Title);
        Assert.Equal("D", next?.Title);
    }

    [Fact]
    public void PeekPlannedNext_InShuffle_PlanAndMoveNextAgree()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);
        queue.SetShuffle(true);

        var planned = queue.PeekPlannedNext();
        var next = queue.MoveNext();

        Assert.NotNull(planned);
        Assert.Equal(planned?.Id, next?.Id);
    }

    [Fact]
    public void PeekPlannedNext_WithoutShuffle_ReturnsNextInOrder()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" },
            new Song { Id = "3", Title = "C" }
        ], 0);

        var planned = queue.PeekPlannedNext();

        Assert.Equal("B", planned?.Title);
    }

    [Fact]
    public void SetRadioQueue_AddsItemsAfterUserQueue()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }, new Song { Id = "2", Title = "B" }], 0);
        queue.SetRadioQueue([new Song { Id = "3", Title = "C" }, new Song { Id = "4", Title = "D" }]);

        Assert.Equal(4, queue.Items.Count);
        Assert.Equal("A", queue.Items[0].Title);
        Assert.Equal("C", queue.Items[2].Title);
    }

    [Fact]
    public void MoveNext_AtEndOfUserQueue_GoesToRadio()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);
        queue.SetRadioQueue([new Song { Id = "2", Title = "B" }]);

        var next = queue.MoveNext();
        Assert.Equal("B", next?.Title);
    }

    [Fact]
    public void AddNext_AddsBeforeRadioItems()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);
        queue.SetRadioQueue([new Song { Id = "2", Title = "B" }]);
        queue.AddNext(new Song { Id = "3", Title = "C" });

        Assert.Equal("C", queue.Items[1].Title);
        Assert.Equal("B", queue.Items[2].Title);
    }

    [Fact]
    public void ToggleRadio_Off_ClearsRadioItems()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);
        queue.SetRadioQueue([new Song { Id = "2", Title = "B" }]);
        queue.ToggleRadio();

        Assert.False(queue.RadioEnabled);
        Assert.Single(queue.Items);
    }

    [Fact]
    public void MovePrevious_FromRadio_GoesToLastUserItem()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);
        queue.SetRadioQueue([new Song { Id = "2", Title = "B" }]);
        queue.MoveNext(); // to radio

        var prev = queue.MovePrevious();
        Assert.Equal("A", prev?.Title);
    }

    [Fact]
    public void AddNext_AllowsDuplicates()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);

        queue.AddNext(new Song { Id = "1", Title = "A" });
        queue.AddNext(new Song { Id = "1", Title = "A" });

        Assert.Equal(3, queue.Items.Count);
        Assert.Equal("A", queue.Items[1].Title);
        Assert.Equal("A", queue.Items[2].Title);
    }

    [Fact]
    public void AddToTheEnd_AllowsDuplicates()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);

        queue.AddToTheEnd(new Song { Id = "1", Title = "A" });
        queue.AddToTheEnd(new Song { Id = "1", Title = "A" });

        Assert.Equal(3, queue.Items.Count);
    }

    [Fact]
    public void PlayNow_AllowsDuplicates()
    {
        var queue = new QueueService();
        queue.SetQueue([new Song { Id = "1", Title = "A" }], 0);

        queue.PlayNow(new Song { Id = "1", Title = "A" });

        Assert.Equal(2, queue.Items.Count);
        Assert.Equal(0, queue.CurrentIndex);
    }

    [Fact]
    public void Remove_RemovesOnlyFirstOccurrence()
    {
        var queue = new QueueService();
        queue.SetQueue([
            new Song { Id = "1", Title = "A" },
            new Song { Id = "1", Title = "A" },
            new Song { Id = "2", Title = "B" }
        ], 0);

        queue.Remove(new Song { Id = "1", Title = "A" });

        Assert.Equal(2, queue.Items.Count);
        Assert.Equal("A", queue.Items[0].Title); // second A still there
        Assert.Equal("B", queue.Items[1].Title);
    }
}
