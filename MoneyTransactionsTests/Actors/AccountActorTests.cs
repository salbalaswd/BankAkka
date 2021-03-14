﻿using Akka.Actor;
using Akka.TestKit.Xunit2;
using MoneyTransactions;
using MoneyTransactions.Actors;
using MoneyTransactions.Actors.Messages;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MoneyTransactionsTests.Actors
{
    public class AccountActorTests : TestKit
    {
        [Fact]
        public void Should_transfer_correct_amount_when_balance_is_enough()
        {
            var accountId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            decimal balance = 100m;
            var client = new Client(clientId, "Jonh", "Doe");
            var account = new Account(accountId, balance, client);

            var subject = Sys.ActorOf(Props.Create(() => new AccountActor(account)));

            decimal amountToTransfer = 50m;
            var destinationAccount = new Account(Guid.NewGuid(), balance, new Client(Guid.NewGuid(), "Jane", "Doe"));
            var destinationActor = Sys.ActorOf(Props.Create(() => new AccountActor(destinationAccount)));
            subject.Tell(new TransferMoney(amountToTransfer, destinationActor));

            var transferSucceeded = ExpectMsg<TransferSucceeded>();
            Assert.Equal(balance - amountToTransfer, transferSucceeded.NewBalance);

            destinationActor.Tell(new CheckBalance());
            var currentBalance = ExpectMsg<BalanceStatus>();
            Assert.Equal(balance + amountToTransfer, currentBalance.Balance);
        }

        [Fact]
        public void Deposit_should_succeed_when_requested_with_correct_values()
        {
            var accountId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            decimal balance = 100m;
            var client = new Client(clientId, "Jonh", "Doe");
            var account = new Account(accountId, balance, client);

            var subject = Sys.ActorOf(Props.Create(() => new AccountActor(account)));
            
            decimal amount = 50m;
            subject.Tell(new Deposit(amount, Guid.NewGuid(), "Jane Doe"));
            ExpectMsg<DepositConfirmed>();
            
            subject.Tell(new CheckBalance());
            ExpectMsg<BalanceStatus>(msg => Assert.Equal(balance + amount, msg.Balance ));
        }

        [Fact]
        public void Should_accept_new_transfer_while_processing_others()
        {
            var accountId = Guid.NewGuid();
            var clientId = Guid.NewGuid();
            decimal balance = 100m;
            var client = new Client(clientId, "Jonh", "Doe");
            var account = new Account(accountId, balance, client);

            var subject = Sys.ActorOf(Props.Create(() => new AccountActor(account)));

            decimal amountToTransfer = 50m;
            var destinationAccount = new Account(Guid.NewGuid(), balance, new Client(Guid.NewGuid(), "Jane", "Doe"));
            var destinationActor = Sys.ActorOf(Props.Create(() => new AccountActor(destinationAccount)));
            subject.Tell(new TransferMoney(amountToTransfer, destinationActor));
            subject.Tell(new TransferMoney(amountToTransfer, destinationActor));

            ExpectMsg<TransferSucceeded>(msg => Assert.Equal(balance - amountToTransfer, msg.NewBalance));
            ExpectMsg<TransferSucceeded>(msg => Assert.Equal(balance - amountToTransfer - amountToTransfer, msg.NewBalance));

            destinationActor.Tell(new CheckBalance());
            var currentBalance = ExpectMsg<BalanceStatus>();
            Assert.Equal(balance + amountToTransfer + amountToTransfer, currentBalance.Balance);
        }
    }
}
