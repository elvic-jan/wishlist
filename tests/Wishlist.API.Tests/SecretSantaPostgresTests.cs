using Microsoft.Extensions.DependencyInjection;
using Wishlist.Application.DTOs;
using Wishlist.Application.Interfaces;
using Wishlist.Application.UseCases.Auth;
using Wishlist.Application.UseCases.Buddies;
using Wishlist.Application.UseCases.Drawings;
using Wishlist.Application.UseCases.Groups;
using Wishlist.Domain.Entities;
using Wishlist.Domain.Enums;
using Wishlist.Domain.Exceptions;

namespace Wishlist.API.Tests;

/// <summary>Secret Santa drawings end to end on real Postgres (openspec: secret-santa, groups, wishlists-overview).</summary>
[Collection(PostgresCollection.Name)]
public class SecretSantaPostgresTests
{
    private const string Password = "Passw0rd!long";

    private sealed class Clock(DateTime now) : TimeProvider
    {
        public DateTime Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private static readonly DateTime Start = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly EndDate = new(2026, 12, 24);

    [PostgresFact]
    public async Task A_drawing_from_invitation_to_cleanup()
    {
        var clock = new Clock(Start);
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!, clock);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;

        var users = s.GetRequiredService<IUserRepository>();
        var groups = s.GetRequiredService<IGroupRepository>();
        var jan = await users.CreateAsync("jan@example.test", "Jan", Password);
        var anna = await users.CreateAsync("anna@example.test", "Anna", Password);
        var mia = await users.CreateAsync("mia@example.test", "Mia", Password);
        var sofie = await users.CreateAsync("sofie@example.test", "Sofie", Password);
        var piet = await users.CreateAsync("piet@example.test", "Piet", Password, "nl");
        var family = await groups.CreateAsync("Family", jan.Id);
        foreach (var u in new[] { jan, anna, mia, sofie })
            await groups.AddMemberAsync(family.Id, u.Id, GroupMemberRole.Member);
        await s.GetRequiredService<ManageBuddiesUseCase>().AddAsync(mia.Id, jan.Id);

        // Create: the family circle, Piet's address (he has an account, shares no circle) and a newcomer.
        var create = s.GetRequiredService<CreateDrawingUseCase>();
        await Assert.ThrowsAsync<DomainException>(() => create.ExecuteAsync(jan.Id, new CreateDrawingRequest("X", null, null)));
        await Assert.ThrowsAsync<DomainException>(() => create.ExecuteAsync(jan.Id, new CreateDrawingRequest("X", new DateOnly(2026, 11, 1), null)));
        api.Emails.Clear();
        var id = await create.ExecuteAsync(jan.Id, new CreateDrawingRequest("Christmas 2026", EndDate, "€30", true,
            new AddDrawingParticipantsRequest(
                [new DrawingCircleSelection(family.Id)],
                [anna.Id],
                [new DrawingEmailInvite("Pieter", "PIET@example.test"), new DrawingEmailInvite("Newbie", "new@example.test")])));
        Assert.Equal(5, api.Emails.Count);
        Assert.Contains(api.Emails, m => m.To == "piet@example.test" && m.Subject.Contains("lootjestrekking"));
        Assert.Contains(api.Emails, m => m.To == "new@example.test" && m.PlainTextBody.Contains("/join/"));

        // Adding the same people again adds nobody.
        var manage = s.GetRequiredService<ManageDrawingParticipantsUseCase>();
        Assert.Equal(0, await manage.AddAsync(jan.Id, id, new AddDrawingParticipantsRequest(
            [new DrawingCircleSelection(family.Id)], [anna.Id, mia.Id], [new DrawingEmailInvite("Anna B", "anna@example.test")])));

        // The organizer sees typed addresses the same way, account or not.
        var get = s.GetRequiredService<GetDrawingUseCase>();
        var view = await get.ExecuteAsync(jan.Id, id);
        Assert.Equal(6, view.Participants.Count);
        var pieter = view.Participants.Single(p => p.Name == "Pieter");
        var newbie = view.Participants.Single(p => p.Name == "Newbie");
        Assert.Equal((pieter.Status, pieter.UserId, pieter.CanJoinForThem), (newbie.Status, newbie.UserId, newbie.CanJoinForThem));
        Assert.Equal("PIET@example.test", pieter.Email);
        Assert.Equal(DrawingStatuses.Joined, view.Participants.Single(p => p.IsMe).Status);

        // Invited people see it on top of their circles and get a card.
        var anyGroups = await s.GetRequiredService<GetUserGroupsUseCase>().ExecuteAsync(anna.Id);
        Assert.Equal(id, anyGroups[0].Id);
        Assert.Equal(DrawingStatuses.Invited, anyGroups[0].Drawing!.MyStatus);
        Assert.Single(await s.GetRequiredService<GetMyDrawingInvitationsUseCase>().ExecuteAsync(anna.Id));

        // Anna joins; Jan joins for Mia as her buddy; Anna can't do that for Sofie.
        var answer = s.GetRequiredService<AnswerDrawingUseCase>();
        Guid Pid(DrawingDto v, string name) => v.Participants.Single(p => p.Name == name).Id;
        await answer.JoinAsync(anna.Id, id, Pid(view, "Anna"));
        await answer.JoinAsync(jan.Id, id, Pid(view, "Mia"));
        await Assert.ThrowsAsync<ForbiddenException>(() => answer.JoinAsync(anna.Id, id, Pid(view, "Sofie")));

        // Names only until both joined: Anna can't open Piet's list.
        var annaView = await get.ExecuteAsync(anna.Id, id);
        Assert.Null(annaView.Participants.Single(p => p.Name == "Pieter").UserId);
        Assert.Null(annaView.Participants.Single(p => p.Name == "Pieter").Email);
        Assert.False(await groups.SharesGroupAsync(anna.Id, piet.Id));

        // Exclusions, then the draw: unanswered people need a confirmation.
        await manage.SetExclusionsAsync(jan.Id, id, new SetDrawingExclusionsRequest([new DrawingExclusionPair(Pid(view, "Jan"), Pid(view, "Sofie"))]));
        var draw = s.GetRequiredService<DrawNamesUseCase>();
        await Assert.ThrowsAsync<ForbiddenException>(() => draw.ExecuteAsync(anna.Id, id, new DrawNamesRequest(true)));
        await Assert.ThrowsAsync<ConflictException>(() => draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest()));
        api.Emails.Clear();
        await draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest(true));
        await Assert.ThrowsAsync<ConflictException>(() => draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest(true)));
        Assert.Equal(6, api.Emails.Count(m => m.PlainTextBody.Contains("Secret Santa")));
        await Assert.ThrowsAsync<ConflictException>(() => manage.RemoveAsync(jan.Id, id, Pid(view, "Newbie")));

        // Everyone sees only their own recipient; Jan didn't draw Sofie.
        var janView = await get.ExecuteAsync(jan.Id, id);
        Assert.NotNull(janView.MyRecipient);
        Assert.NotEqual("Sofie", janView.MyRecipient!.Name);
        var recipients = await s.GetRequiredService<GetMyRecipientsUseCase>().ExecuteAsync(jan.Id);
        Assert.Equal(janView.MyRecipient.Name, Assert.Single(recipients).Name);

        // Piet answers with his own link while signed in; the newcomer registers with theirs.
        var codes = await DrawingCodesAsync(s, id);
        await Assert.ThrowsAsync<ConflictException>(() =>
            s.GetRequiredService<RegisterUserUseCase>().ExecuteAsync(new RegisterRequest("other@example.test", "Other", Password, codes["Pieter"])));
        await Assert.ThrowsAsync<InvitationNotFoundException>(() =>
            s.GetRequiredService<JoinGroupViaInvitationUseCase>().ExecuteAsync(anna.Id, codes["Pieter"]));
        Assert.Equal(id, await s.GetRequiredService<JoinGroupViaInvitationUseCase>().ExecuteAsync(piet.Id, codes["Pieter"]));
        var (registered, _) = await s.GetRequiredService<RegisterUserUseCase>().ExecuteAsync(
            new RegisterRequest("new@example.test", "Nina", Password, codes["Newbie"]));
        Assert.Equal(id, registered.JoinedGroupId);

        // Joined both: Anna can open Piet's list now, and sees his own name.
        annaView = await get.ExecuteAsync(anna.Id, id);
        Assert.Equal(piet.Id, annaView.Participants.Single(p => p.Name == "Piet").UserId);
        Assert.Contains(annaView.Participants, p => p.Name == "Nina");
        Assert.True(await groups.SharesGroupAsync(anna.Id, piet.Id));

        // Sofie leaves after the draw: the organizer learns who has no Secret Santa now; her giver needn't buy.
        api.Emails.Clear();
        await answer.JoinAsync(sofie.Id, id, Pid(view, "Sofie"));
        await answer.DeclineAsync(sofie.Id, id);
        var toOrganizer = Assert.Single(api.Emails, m => m.To == jan.Email && m.Subject.Contains("Sofie"));
        Assert.Contains("no longer has a Secret Santa", toOrganizer.PlainTextBody);
        // Jan is excluded from Sofie, so the other email is for her Secret Santa (possibly in Dutch).
        Assert.Single(api.Emails, m => m.To != jan.Email && m.Subject.Contains("Sofie"));
        await Assert.ThrowsAsync<NotFoundException>(() => get.ExecuteAsync(sofie.Id, id));

        // Cancel hides it from participants, not from the organizer; undo brings it back.
        var cancel = s.GetRequiredService<CancelDrawingUseCase>();
        api.Emails.Clear();
        await cancel.CancelAsync(jan.Id, id);
        Assert.Equal(4, api.Emails.Count); // Anna, Mia, Piet, Nina; not Jan, not Sofie.
        await Assert.ThrowsAsync<NotFoundException>(() => get.ExecuteAsync(anna.Id, id));
        Assert.DoesNotContain(await s.GetRequiredService<GetUserGroupsUseCase>().ExecuteAsync(anna.Id), g => g.Id == id);
        Assert.False(await groups.SharesGroupAsync(anna.Id, piet.Id));
        Assert.Empty(await s.GetRequiredService<GetMyRecipientsUseCase>().ExecuteAsync(jan.Id));
        Assert.True((await get.ExecuteAsync(jan.Id, id)).Cancelled);
        await cancel.UndoAsync(jan.Id, id);
        Assert.Equal(8, api.Emails.Count);
        Assert.True(await groups.SharesGroupAsync(anna.Id, piet.Id));
        Assert.NotNull((await get.ExecuteAsync(anna.Id, id)).MyRecipient);

        // After the end date it is shown as ended for a month, then hidden and cleaned up.
        clock.Now = new DateTime(2027, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        Assert.True((await get.ExecuteAsync(anna.Id, id)).Ended);
        var cleanup = s.GetRequiredService<CleanUpDrawingsUseCase>();
        Assert.Equal(0, await cleanup.ExecuteAsync());
        clock.Now = new DateTime(2027, 1, 25, 0, 0, 0, DateTimeKind.Utc);
        await Assert.ThrowsAsync<NotFoundException>(() => get.ExecuteAsync(jan.Id, id));
        Assert.False(await groups.SharesGroupAsync(anna.Id, piet.Id));
        Assert.Equal(1, await cleanup.ExecuteAsync());
        Assert.Null(await groups.GetByIdAsync(id));
        Assert.NotNull(await groups.GetByIdAsync(family.Id));
    }

    [PostgresFact]
    public async Task Organizer_manages_people_until_the_draw()
    {
        var clock = new Clock(Start);
        await using var api = new PostgresApi(Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!, clock);
        await api.ResetDatabaseAsync();
        await using var scope = api.Services.CreateAsyncScope();
        var s = scope.ServiceProvider;

        var users = s.GetRequiredService<IUserRepository>();
        var jan = await users.CreateAsync("jan@example.test", "Jan", Password);
        var create = s.GetRequiredService<CreateDrawingUseCase>();
        var manage = s.GetRequiredService<ManageDrawingParticipantsUseCase>();
        var draw = s.GetRequiredService<DrawNamesUseCase>();
        var get = s.GetRequiredService<GetDrawingUseCase>();

        // The organizer can leave themself out; bad addresses change nothing.
        await Assert.ThrowsAsync<DomainException>(() => create.ExecuteAsync(jan.Id, new CreateDrawingRequest("Kids", EndDate, null, false,
            new AddDrawingParticipantsRequest(Invites: [new DrawingEmailInvite("A", "not an address")]))));
        var id = await create.ExecuteAsync(jan.Id, new CreateDrawingRequest("Kids", EndDate, null, false,
            new AddDrawingParticipantsRequest(Invites: [new("A", "a@example.test"), new("B", "b@example.test"), new("C", "c@example.test")])));
        var view = await get.ExecuteAsync(jan.Id, id);
        Assert.Null(view.MyParticipantId);
        Assert.Equal(["A", "B", "C"], view.Participants.Select(p => p.Name));

        // Three people of whom two are excluded can't be drawn.
        var a = view.Participants[0].Id;
        var b = view.Participants[1].Id;
        await Assert.ThrowsAsync<DomainException>(() =>
            manage.SetExclusionsAsync(jan.Id, id, new SetDrawingExclusionsRequest([new DrawingExclusionPair(a, a)])));
        await manage.SetExclusionsAsync(jan.Id, id, new SetDrawingExclusionsRequest([new DrawingExclusionPair(a, b)]));
        var impossible = await Assert.ThrowsAsync<DomainException>(() => draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest(true)));
        Assert.Contains("exclusions", impossible.Message);

        // Removing someone drops their exclusions; then there are too few.
        var codes = await DrawingCodesAsync(s, id);
        await manage.RemoveAsync(jan.Id, id, a);
        view = await get.ExecuteAsync(jan.Id, id);
        Assert.Equal(["B", "C"], view.Participants.Select(p => p.Name));
        Assert.Empty(view.Exclusions);
        await Assert.ThrowsAsync<DomainException>(() => draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest(true)));

        // A removed person's link no longer works.
        var resolver = s.GetRequiredService<InvitationResolver>();
        await Assert.ThrowsAsync<InvitationNotFoundException>(() => resolver.ResolveAsync(codes["A"]));

        // Someone without an account declines from the email link; their link then stops working.
        Assert.Equal(2, await manage.AddAsync(jan.Id, id, new AddDrawingParticipantsRequest(
            Invites: [new("D", "d@example.test"), new("E", "e@example.test")])));
        var answer = s.GetRequiredService<AnswerDrawingUseCase>();
        var eCode = (await DrawingCodesAsync(s, id))["E"];
        await answer.DeclineWithCodeAsync(null, await resolver.ResolveAsync(eCode));
        await Assert.ThrowsAsync<InvitationNotFoundException>(() => resolver.ResolveAsync(eCode));
        Assert.Equal(DrawingStatuses.Declined, (await get.ExecuteAsync(jan.Id, id)).Participants.Single(p => p.Name == "E").Status);

        await draw.ExecuteAsync(jan.Id, id, new DrawNamesRequest(true));
        Assert.True((await get.ExecuteAsync(jan.Id, id)).Drawn);
        Assert.Null((await get.ExecuteAsync(jan.Id, id)).MyRecipient);
    }

    private static async Task<Dictionary<string, string>> DrawingCodesAsync(IServiceProvider s, Guid drawingId) =>
        (await s.GetRequiredService<IDrawingRepository>().GetAsync(drawingId))!.Participants.ToDictionary(p => p.Name, p => p.InviteCode);
}
