using System.Diagnostics;
using Wishlist.Domain;
using Wishlist.Domain.Entities;

namespace Wishlist.API.Tests;

/// <summary>The name draw: one loop, exclusions respected, impossible cases detected (openspec: secret-santa).</summary>
public class SecretSantaTests
{
    private static List<Guid> People(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    private static void AssertSingleLoop(IReadOnlyList<Guid> people, IReadOnlyDictionary<Guid, Guid> draw)
    {
        Assert.Equal(people.Count, draw.Count);
        Assert.Equal(people.ToHashSet(), draw.Values.ToHashSet());
        Assert.All(draw, pair => Assert.NotEqual(pair.Key, pair.Value));

        // Following the arrows from anyone visits everyone before coming back.
        var current = people[0];
        for (var step = 1; step < people.Count; step++)
        {
            current = draw[current];
            Assert.NotEqual(people[0], current);
        }
        Assert.Equal(people[0], draw[current]);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(25)]
    public void Everyone_draws_one_person_in_a_single_loop(int count)
    {
        var people = People(count);
        AssertSingleLoop(people, SecretSanta.Draw(people, [])!);
    }

    [Fact]
    public void Exclusions_work_both_ways()
    {
        var people = People(6);
        var pairs = new List<(Guid, Guid)> { (people[0], people[1]), (people[2], people[3]), (people[4], people[5]) };
        for (var run = 0; run < 50; run++)
        {
            var draw = SecretSanta.Draw(people, pairs)!;
            AssertSingleLoop(people, draw);
            foreach (var (a, b) in pairs)
            {
                Assert.NotEqual(b, draw[a]);
                Assert.NotEqual(a, draw[b]);
            }
        }
    }

    [Fact]
    public void Impossible_exclusions_give_no_draw()
    {
        // Three people form one loop that uses every pair: excluding one pair leaves none.
        var people = People(3);
        Assert.Null(SecretSanta.Draw(people, [(people[0], people[1])]));
    }

    [Fact]
    public void Fewer_than_three_people_give_no_draw()
    {
        Assert.Null(SecretSanta.Draw(People(2), []));
    }

    [Fact]
    public void Draws_differ_between_runs()
    {
        var people = People(5);
        var seen = Enumerable.Range(0, 100)
            .Select(_ => string.Join(",", people.Select(p => SecretSanta.Draw(people, [])![p])))
            .ToHashSet();
        Assert.True(seen.Count > 5, $"Only {seen.Count} different draws in 100 runs.");
    }

    [Fact]
    public void A_hundred_people_with_exclusions_draw_quickly()
    {
        var people = People(100);
        var pairs = Enumerable.Range(0, 50).Select(i => (people[2 * i], people[2 * i + 1])).ToList();
        var watch = Stopwatch.StartNew();
        AssertSingleLoop(people, SecretSanta.Draw(people, pairs)!);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Visibility_follows_the_end_date_and_cancellation()
    {
        var drawing = new Drawing { EndDate = new DateOnly(2026, 12, 24) };

        Assert.False(drawing.IsEnded(new DateOnly(2026, 12, 24)));
        Assert.True(drawing.IsEnded(new DateOnly(2026, 12, 25)));
        Assert.True(drawing.IsActive(new DateOnly(2027, 1, 24)));
        Assert.False(drawing.IsActive(new DateOnly(2027, 1, 25)));
        Assert.True(drawing.IsDueForCleanup(new DateTime(2027, 1, 25, 0, 0, 0, DateTimeKind.Utc)));

        drawing.CancelledAt = new DateTime(2026, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(drawing.IsActive(new DateOnly(2026, 12, 2)));
        Assert.True(drawing.IsVisibleToOrganizer(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)));
        Assert.False(drawing.IsVisibleToOrganizer(new DateTime(2027, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
    }
}
