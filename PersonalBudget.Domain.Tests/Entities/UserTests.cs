using System;
using FluentAssertions;
using Xunit;

public class UserTests
    {
        // =========================
        // HELPERS
        // =========================

        private User CreateValidUser()
        {
            return new User(
                name: "Igor",
                email: new Email("igor@email.com"),
                passwordHash: new PasswordHash("hashed-password")
            );
        }

        // =========================
        // CONSTRUCTION
        // =========================

        [Fact]
        public void User_Should_Be_Created_Active()
        {
            var user = CreateValidUser();

            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void User_Should_Have_Valid_Id()
        {
            var user = CreateValidUser();

            user.Id.Should().NotBe(Guid.Empty);
        }

        [Fact]
        public void User_Without_Name_Should_Fail()
        {
            Action act = () =>
                new User(
                    "",
                    new Email("test@email.com"),
                    new PasswordHash("hash")
                );

            act.Should().Throw<DomainException>();
        }

        // =========================
        // AUTHENTICATION
        // =========================

        [Fact]
        public void Active_User_With_Correct_Password_Can_Authenticate()
        {
            var user = CreateValidUser();

            var result = user.CanAuthenticate(new PasswordHash("hashed-password"));

            result.Should().BeTrue();
        }

        [Fact]
        public void Active_User_With_Wrong_Password_Cannot_Authenticate()
        {
            var user = CreateValidUser();

            var result = user.CanAuthenticate(new PasswordHash("wrong-hash"));

            result.Should().BeFalse();
        }

        [Fact]
        public void Inactive_User_Cannot_Authenticate()
        {
            var user = CreateValidUser();
            user.Deactivate();

            var result = user.CanAuthenticate(new PasswordHash("hashed-password"));

            result.Should().BeFalse();
        }

        // =========================
        // PASSWORD CHANGE
        // =========================

        [Fact]
        public void Active_User_Can_Change_Password()
        {
            var user = CreateValidUser();

            user.ChangePassword(new PasswordHash("new-hash"));

            user.CanAuthenticate(new PasswordHash("new-hash")).Should().BeTrue();
        }

        [Fact]
        public void Inactive_User_Cannot_Change_Password()
        {
            var user = CreateValidUser();
            user.Deactivate();

            Action act = () =>
                user.ChangePassword(new PasswordHash("new-hash"));

            act.Should().Throw<DomainException>();
        }
    }