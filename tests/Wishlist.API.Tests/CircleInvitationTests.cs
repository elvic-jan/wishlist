using Microsoft.Extensions.Logging.Abstractions;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Tests;

/// <summary>Inviting people you know from your other circles; they join or decline.</summary>
public class CircleInvitationTests
{
    [Fact]
    public async Task Candidates_are_grouped_by_my_other_circles()
    {
        var w = new World();
        await w.Invite(w.Me, w.Target, w.Bob);

        var circles = await w.Candidates.ExecuteAsync(w.Me.Id, w.Target.Id);

        // Ann is already in the target circle, so she's left out; Bob shows in both circles.
        Assert.Equal(["Family", "Friends"], circles.Select(c => c.Name));
        Assert.Equal(["Bob", "Cleo"], circles[0].Members.Select(m => m.DisplayName));
        Assert.Equal(["Bob"], circles[1].Members.Select(m => m.DisplayName));
        Assert.All(circles.SelectMany(c => c.Members).Where(m => m.UserId == w.Bob.Id), m => Assert.True(m.Invited));
        Assert.False(circles[0].Members.Single(m => m.UserId == w.Cleo.Id).Invited);
    }

    [Fact]
    public async Task Non_member_cannot_see_candidates_or_invite()
    {
        var w = new World();

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Candidates.ExecuteAsync(w.Cleo.Id, w.Target.Id));
        await Assert.ThrowsAsync<ForbiddenException>(() => w.Invite(w.Cleo, w.Target, w.Bob));
    }

    [Fact]
    public async Task Inviting_several_people_emails_each_new_invitee_once()
    {
        var w = new World();

        var first = await w.Invite(w.Me, w.Target, w.Bob, w.Cleo);
        var again = await w.Invite(w.Me, w.Target, w.Bob, w.Ann); // Bob already invited, Ann already a member

        Assert.Equal(new InviteFromCirclesResult(2, 0), first);
        Assert.Equal(new InviteFromCirclesResult(0, 2), again);
        var email = Assert.Single(w.Store.EmailsTo(w.Bob.Email));
        Assert.Equal("Me invited you to join Target", email.Subject);
        Assert.Single(w.Store.EmailsTo(w.Cleo.Email));
        Assert.Empty(w.Store.EmailsTo(w.Ann.Email));
    }

    [Fact]
    public async Task Only_people_from_my_circles_can_be_invited()
    {
        var w = new World();

        await Assert.ThrowsAsync<ForbiddenException>(() => w.Invite(w.Me, w.Target, w.Stranger));
        var mixed = await w.Invite(w.Me, w.Target, w.Stranger, w.Cleo);

        Assert.Equal(new InviteFromCirclesResult(1, 1), mixed);
        Assert.Empty(await w.Mine.ExecuteAsync(w.Stranger.Id));
    }

    [Fact]
    public async Task Empty_or_oversized_requests_are_rejected()
    {
        var w = new World();

        await Assert.ThrowsAsync<DomainException>(() => w.InviteIds(w.Me, w.Target, []));
        await Assert.ThrowsAsync<DomainException>(() => w.InviteIds(w.Me, w.Target, Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList()));
    }

    [Fact]
    public async Task Email_failure_still_creates_the_invitation()
    {
        var w = new World(failEmails: true);

        await w.Invite(w.Me, w.Target, w.Bob);

        Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));
    }

    [Fact]
    public async Task Invitee_sees_and_joins_the_invitation()
    {
        var w = new World();
        await w.Invite(w.Me, w.Target, w.Bob);

        var invitation = Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));
        Assert.Equal(("Target", "Me"), (invitation.CircleName, invitation.InvitedByName));

        var groupId = await w.Answer.JoinAsync(w.Bob.Id, invitation.Id);

        Assert.Equal(w.Target.Id, groupId);
        Assert.True(w.Store.IsMember(w.Target.Id, w.Bob.Id));
        Assert.Empty(await w.Mine.ExecuteAsync(w.Bob.Id));
    }

    [Fact]
    public async Task Declining_closes_it_quietly_and_allows_a_new_invitation()
    {
        var w = new World();
        await w.Invite(w.Me, w.Target, w.Bob);
        var invitation = Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));

        await w.Answer.DeclineAsync(w.Bob.Id, invitation.Id);

        Assert.False(w.Store.IsMember(w.Target.Id, w.Bob.Id));
        Assert.Empty(await w.Mine.ExecuteAsync(w.Bob.Id));
        Assert.Equal(new InviteFromCirclesResult(1, 0), await w.Invite(w.Me, w.Target, w.Bob));
    }

    [Fact]
    public async Task Joining_when_already_a_member_just_closes_it()
    {
        var w = new World();
        await w.Invite(w.Me, w.Target, w.Bob);
        var invitation = Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));
        w.Join(w.Target, w.Bob); // joined by link meanwhile

        await w.Answer.JoinAsync(w.Bob.Id, invitation.Id);

        Assert.Empty(await w.Mine.ExecuteAsync(w.Bob.Id));
    }

    [Fact]
    public async Task Someone_elses_invitation_is_not_found()
    {
        var w = new World();
        await w.Invite(w.Me, w.Target, w.Bob);
        var invitation = Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));

        await Assert.ThrowsAsync<NotFoundException>(() => w.Answer.JoinAsync(w.Cleo.Id, invitation.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => w.Answer.DeclineAsync(w.Cleo.Id, invitation.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => w.Answer.JoinAsync(w.Bob.Id, Guid.NewGuid()));
        Assert.False(w.Store.IsMember(w.Target.Id, w.Cleo.Id));
        Assert.Single(await w.Mine.ExecuteAsync(w.Bob.Id));
    }

    private sealed class World
    {
        public InMemoryStore Store { get; } = new();
        public User Me { get; }
        public User Ann { get; }
        public User Bob { get; }
        public User Cleo { get; }
        public User Stranger { get; }
        public Group Target { get; }
        public GetInviteCandidatesUseCase Candidates { get; }
        public GetMyCircleInvitationsUseCase Mine { get; }
        public AnswerCircleInvitationUseCase Answer { get; }
        private readonly InviteFromCirclesUseCase _invite;

        public World(bool failEmails = false)
        {
            Me = Named("me@example.test", "Me");
            Ann = Named("ann@example.test", "Ann");
            Bob = Named("bob@example.test", "Bob");
            Cleo = Named("cleo@example.test", "Cleo");
            Stranger = Named("stranger@example.test", "Stranger");

            Target = Store.AddGroup("Target");
            var family = Store.AddGroup("Family");
            var friends = Store.AddGroup("Friends");
            Join(Target, Me); Join(Target, Ann);
            Join(family, Me); Join(family, Ann); Join(family, Bob); Join(family, Cleo);
            Join(friends, Me); Join(friends, Bob);

            var groups = new InMemoryGroups(Store);
            var invitations = new Invitations(Store);
            IEmailSender sender = failEmails ? new FailingSender() : new CapturingEmailSender(Store);
            var emails = new CircleInvitationEmails(sender, new Urls(), NullLogger<CircleInvitationEmails>.Instance);
            Candidates = new GetInviteCandidatesUseCase(groups, invitations);
            _invite = new InviteFromCirclesUseCase(groups, invitations, new InMemoryUsers(Store), emails, TimeProvider.System);
            Mine = new GetMyCircleInvitationsUseCase(invitations);
            Answer = new AnswerCircleInvitationUseCase(invitations, groups, new InMemoryUnitOfWork(Store));
        }

        public Task<InviteFromCirclesResult> Invite(User who, Group circle, params User[] people) =>
            InviteIds(who, circle, people.Select(p => p.Id).ToList());

        public Task<InviteFromCirclesResult> InviteIds(User who, Group circle, IReadOnlyList<Guid> ids) =>
            _invite.ExecuteAsync(who.Id, circle.Id, new InviteFromCirclesRequest(ids));

        public void Join(Group circle, User user) =>
            Store.Members[(circle.Id, user.Id)] = new GroupMember { GroupId = circle.Id, UserId = user.Id, Role = GroupMemberRole.Member };

        private User Named(string email, string name)
        {
            var user = Store.AddUser(email);
            user.DisplayName = name;
            return user;
        }
    }

    private sealed class Urls : IAppUrlProvider
    {
        public string PublicBaseUrl => "https://giftcircles.test/";
    }

    private sealed class FailingSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default) =>
            throw new InvalidOperationException("Email service down.");
    }

    /// <summary>Same rules as the EF repository, over the in-memory store.</summary>
    private sealed class Invitations(InMemoryStore store) : ICircleInvitationRepository
    {
        private readonly List<CircleInvitation> _rows = [];

        private IEnumerable<Guid> MembersOf(Guid groupId) => store.Members.Keys.Where(k => k.GroupId == groupId).Select(k => k.UserId);
        private IEnumerable<Guid> GroupsOf(Guid userId) => store.Members.Keys.Where(k => k.UserId == userId).Select(k => k.GroupId);

        public Task<IReadOnlyList<InviteCandidateCircle>> GetCandidatesAsync(Guid groupId, Guid callerId, CancellationToken ct = default)
        {
            var inTarget = MembersOf(groupId).ToHashSet();
            IReadOnlyList<InviteCandidateCircle> result = GroupsOf(callerId).Where(g => g != groupId)
                .Select(g => new InviteCandidateCircle(g, store.Groups[g].Name,
                    MembersOf(g).Where(u => u != callerId && !inTarget.Contains(u))
                        .Select(u => new InviteCandidate(u, store.Users[u].DisplayName, _rows.Any(r => r.GroupId == groupId && r.InviteeId == u)))
                        .OrderBy(m => m.DisplayName).ToList()))
                .Where(c => c.Members.Count > 0)
                .OrderBy(c => c.Name).ToList();
            return Task.FromResult(result);
        }

        public Task<IReadOnlySet<Guid>> GetInvitedAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(_rows.Where(r => r.GroupId == groupId && userIds.Contains(r.InviteeId)).Select(r => r.InviteeId).ToHashSet());

        public Task<IReadOnlySet<Guid>> GetCircleMatesAsync(Guid userId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
        {
            var mine = GroupsOf(userId).ToHashSet();
            return Task.FromResult<IReadOnlySet<Guid>>(userIds.Where(u => GroupsOf(u).Any(mine.Contains)).ToHashSet());
        }

        public Task<IReadOnlySet<Guid>> GetMembersAsync(Guid groupId, IReadOnlyCollection<Guid> userIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlySet<Guid>>(userIds.Where(u => store.IsMember(groupId, u)).ToHashSet());

        public Task<IReadOnlyList<Guid>> CreateAsync(Guid groupId, Guid invitedById, IReadOnlyCollection<Guid> inviteeIds, DateTime now, CancellationToken ct = default)
        {
            foreach (var id in inviteeIds)
                _rows.Add(new CircleInvitation { Id = Guid.NewGuid(), GroupId = groupId, InviteeId = id, InvitedById = invitedById, CreatedAt = now });
            return Task.FromResult<IReadOnlyList<Guid>>(inviteeIds.ToList());
        }

        public Task<IReadOnlyList<MyCircleInvitation>> GetForInviteeAsync(Guid inviteeId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<MyCircleInvitation>>(_rows.Where(r => r.InviteeId == inviteeId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new MyCircleInvitation(r.Id, r.GroupId, store.Groups[r.GroupId].Name,
                    r.InvitedById is { } by ? store.Users[by].DisplayName : null, r.CreatedAt))
                .ToList());

        public Task<CircleInvitation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_rows.FirstOrDefault(r => r.Id == id));

        public Task DeleteAsync(Guid id, CancellationToken ct = default)
        {
            _rows.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }
    }
}
