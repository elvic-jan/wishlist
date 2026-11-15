using System.Security.Cryptography;

namespace Wishlist.Domain;

/// <summary>
/// Draws names as one loop (A→B→C→…→A) in which nobody draws themself or their
/// exclusion partner. A depth-first search over a randomly ordered graph, with
/// a step cap so impossible exclusions end with "no draw" instead of spinning.
/// </summary>
public static class SecretSanta
{
    public const int MinimumParticipants = 3;
    public const int MaxSteps = 200_000;

    /// <summary>
    /// Giver → receiver for every participant, forming a single loop; null when
    /// the exclusions allow none (or none was found within the step cap).
    /// </summary>
    public static IReadOnlyDictionary<Guid, Guid>? Draw(
        IReadOnlyCollection<Guid> participants,
        IReadOnlyCollection<(Guid A, Guid B)> exclusions,
        Func<int, int>? random = null)
    {
        if (participants.Count < MinimumParticipants)
            return null;
        random ??= RandomNumberGenerator.GetInt32;

        var people = Shuffle(participants.Distinct().ToList(), random);
        var excluded = new HashSet<(Guid, Guid)>();
        foreach (var (a, b) in exclusions)
        {
            excluded.Add((a, b));
            excluded.Add((b, a));
        }

        bool Allowed(Guid giver, Guid receiver) => giver != receiver && !excluded.Contains((giver, receiver));

        // Each person's possible receivers, in random order.
        var options = people.ToDictionary(p => p, p => Shuffle(people.Where(q => Allowed(p, q)).ToList(), random));

        var start = people[0];
        var path = new List<Guid> { start };
        var used = new HashSet<Guid> { start };
        var steps = 0;

        bool Extend()
        {
            if (++steps > MaxSteps) return false;
            var current = path[^1];
            if (path.Count == people.Count)
                return Allowed(current, start);

            foreach (var next in options[current])
            {
                if (used.Contains(next)) continue;
                path.Add(next);
                used.Add(next);
                if (Extend()) return true;
                path.RemoveAt(path.Count - 1);
                used.Remove(next);
                if (steps > MaxSteps) return false;
            }
            return false;
        }

        if (!Extend())
            return null;

        var result = new Dictionary<Guid, Guid>();
        for (var i = 0; i < path.Count; i++)
            result[path[i]] = path[(i + 1) % path.Count];
        return result;
    }

    private static List<T> Shuffle<T>(List<T> items, Func<int, int> random)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
        return items;
    }
}
