﻿namespace Miruken.Validate.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Callback;
    using Concurrency;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;
    using static Protocol;

    [TestClass]
    public class ValidationTests
    {
        [TestMethod]
        public void Should_Validate_Target()
        {
            var handler = new ValidationHandler()
                        + new ValidatePlayer();
            var player  = new Player
            {
                DOB = new DateTime(2005, 6, 14)
            };
            var outcome = P<IValidator>(handler).Validate(player);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            Assert.AreEqual("First name is required", outcome["FirstName"]);
            Assert.AreEqual("Last name is required", outcome["LastName"]);
        }

        [TestMethod]
        public void Should_Validate_Target_For_Scope()
        {
            var handler = new ValidationHandler()
                        + new ValidatePlayer();
            var player  = new Player
            {
                DOB = new DateTime(2005, 6, 14)
            };
            var outcome = P<IValidator>(handler).Validate(player, null, "Recreational");
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            Assert.AreEqual("Age must be 10 or younger", outcome["DOB"]);
        }

        [TestMethod]
        public async Task Should_Validate_Target_Async()
        {
            var handler = new ValidationHandler()
                        + new ValidateTeam();
            var team    = new Team();
            var outcome = await P<IValidator>(handler).ValidateAsync(team);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, team.ValidationOutcome);
            Assert.AreEqual("Name is required", outcome["Name"]);
        }

        [TestMethod]
        public async Task Should_Validate_Target_For_Scope_Async()
        {
            var handler = new ValidationHandler()
                        + new ValidateTeam();
            var team    = new Team
            {
                Coach = new Coach()
            };
            var outcome = await P<IValidator>(handler)
                .ValidateAsync(team, null, "ECNL");
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, team.ValidationOutcome);
            var coach = outcome.GetOutcome("Coach");
            Assert.IsFalse(coach.IsValid);
            Assert.AreEqual("Licensed Coach is required", coach["License"]);
        }

        [TestMethod]
        public async Task Should_Handle_Method_If_Valid()
        {
            var handler = new ValidationHandler()
                        + new ManageTeamHandler()
                        + new ValidatePlayer();
            var team    = new Team();
            var player  = new Player
            {
                FirstName = "Wayne",
                LastName  = "Rooney",
                DOB       = new DateTime(1985, 10,24)
            };
            await handler.Valid(player).P<IManageTeam>().AddPlayer(player, team);
            CollectionAssert.Contains(team.Players, player);
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException),
            AllowDerivedTypes = true)]
        public async Task Should_Reject_Method_If_Invalid()
        {
            var handler = new ValidationHandler()
                        + new ManageTeamHandler()
                        + new ValidatePlayer();
            var team    = new Team();
            var player  = new Player();
            await handler.Valid(player).P<IManageTeam>().AddPlayer(player, team);
        }

        [TestMethod,
         ExpectedException(typeof(OperationCanceledException),
            AllowDerivedTypes = true)]
        public async Task Should_Reject_Method_If_Invalid_Async()
        {
            var handler = new ValidationHandler()
                        + new ManageTeamHandler()
                        + new ValidateTeam();
            var team    = new Team();
            var player  = new Player();
            await handler.ValidAsync(team).P<IManageTeam>().AddPlayer(player, team);
        }

        public interface IManageTeam
        {
            Promise AddPlayer(Player player, Team team);
        }

        public class ManageTeamHandler : Handler, IManageTeam
        {
            public Promise AddPlayer(Player player, Team team)
            {
                team.Players = (team.Players ?? new Player[0])
                    .Concat(new [] {player}).ToArray();
                return Promise.Empty;
            }
        }

        public class ValidateTeam: Handler
        {
            [Validates]
            public async Task ShouldHaveName(Team player, ValidationOutcome outcome)
            {
                await Task.Delay(10);
                if (string.IsNullOrEmpty(player.Name))
                    outcome.AddError("Name", "Name is required");
            }

            [Validates(Scope = "ECNL")]
            public Promise ShouldHaveLicensesCoach(Team team, ValidationOutcome outcome)
            {
                return Promise.Delay(TimeSpan.FromMilliseconds(10)).Then((d, s) =>
                {
                    var coach = team.Coach;
                    if (coach == null)
                        outcome.AddError("Coach", "Coach is required");
                    else if (string.IsNullOrEmpty(coach.License))
                        outcome.AddError("Coach.License", "Licensed Coach is required");
                });
            }
        }

        public class ValidatePlayer : Handler
        {
            [Validates]
            public void ShouldHaveFullName(Player player, ValidationOutcome outcome)
            {
                if (string.IsNullOrEmpty(player.FirstName))
                    outcome.AddError("FirstName", "First name is required");

                if (string.IsNullOrEmpty(player.LastName))
                    outcome.AddError("LastName", "Last name is required");

                if (!player.DOB.HasValue)
                    outcome.AddError("DOB", "DOB is required");
            }

            [Validates(Scope = "Recreational")]
            public void MustBeTenOrUnder(Validation validation, IHandler composer)
            {
                var outcome = validation.Outcome;
                var player = (Player) validation.Target;

                var age = (int) (DateTime.Today.Subtract(player.DOB.Value.Date)
                                     .TotalDays / 365.242199);
                if (age > 10)
                    outcome.AddError("DOB", "Age must be 10 or younger");
            }
        }
    }
}
