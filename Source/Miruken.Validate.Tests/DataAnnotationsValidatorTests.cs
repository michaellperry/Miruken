﻿namespace Miruken.Validate.Tests
{
    using System;
    using Callback;
    using DataAnnotations;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Model;
    using static Protocol;

    [TestClass]
    public class DataAnnotationsValidatorTests
    {
        [TestMethod]
        public void Should_Validate_Target()
        {
            var handler = new ValidationHandler()
                        + new DataAnnotationsValidator();
            var player = new Player
            {
                DOB = new DateTime(2007, 6, 14)
            };
            var outcome = P<IValidator>(handler).Validate(player);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, player.ValidationOutcome);
            Assert.AreEqual("The FirstName field is required.", outcome["FirstName"]);
            Assert.AreEqual("The LastName field is required.", outcome["LastName"]);
        }

        [TestMethod]
        public void Should_Compose_Validation()
        {
            var handler = new ValidationHandler()
                        + new DataAnnotationsValidator();
            var team = new Team
            {
                Division = "10",
                Coach    = new Coach(),
                Players  = new [] {
                    new Player(),
                    new Player
                    {
                        FirstName = "Cristiano",
                        LastName  = "Ronaldo",
                        DOB       = new DateTime(1985, 2, 5)
                    },
                    new Player { FirstName = "Lionel"}
                }
            };
            var outcome = P<IValidator>(handler).Validate(team);
            Assert.IsFalse(outcome.IsValid);
            Assert.AreSame(outcome, team.ValidationOutcome);
            CollectionAssert.AreEquivalent(
                new[] { "Name", "Division", "Coach", "Players" },
                outcome.Culprits);

            Assert.AreEqual("The Name field is required.", outcome["Name"]);
            Assert.AreEqual("The Division must match U followed by age.", outcome["Division"]);

            var coach = outcome.GetOutcome("Coach");
            Assert.IsFalse(coach.IsValid);
            Assert.AreSame(coach, team.Coach.ValidationOutcome);
            Assert.AreEqual("The FirstName field is required.", coach["FirstName"]);
            Assert.AreEqual("The LastName field is required.", coach["LastName"]);
            Assert.AreEqual("The License field is required.", coach["License"]);

            var players = outcome.GetOutcome("Players");
            Assert.IsFalse(players.IsValid);
            CollectionAssert.AreEquivalent(new [] { "0", "2" }, players.Culprits);
            var player1 = players.GetOutcome("0");
            Assert.AreSame(player1, team.Players[0].ValidationOutcome);
            Assert.AreEqual("The FirstName field is required.", player1["FirstName"]);
            Assert.AreEqual("The LastName field is required.", player1["LastName"]);
            Assert.AreEqual("The DOB field is required.", player1["DOB"]);
            Assert.IsNull(players.GetOutcome("1"));
            var player3 = players.GetOutcome("2");
            Assert.AreSame(player3, team.Players[2].ValidationOutcome);
            Assert.AreEqual("", player3["FirstName"]);
            Assert.AreEqual("The LastName field is required.", player3["LastName"]);
            Assert.AreEqual("The DOB field is required.", player3["DOB"]);
        }

        [TestMethod,
         ExpectedException(typeof(RejectedException))]
        public void Should_Reject_Operation_If_Invalid()
        {
            var handler = new ValidationHandler()
                        + new DataAnnotationsValidator()
                        + new RegistrationHandler();

            var team = new Team();
            P<IRegistration>(handler.Valid(team)).RegisterTeam(team);
        }

        public interface IRegistration
        {
            void RegisterTeam(Team team);
        }

        public class RegistrationHandler : Handler, IRegistration
        {
            void IRegistration.RegisterTeam(Team team)
            {
                team.Registered = true;
            }
        }
    }
}
