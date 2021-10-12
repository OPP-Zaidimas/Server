﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using API.Lobby;

namespace API.Hubs
{
    public class MonstersItemsSpellsHub : Hub
    {
        //Dict gameid -> game
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("Connected");
            return base.OnConnectedAsync();
        }

        public async Task CreateNewGame(string username)
        {
            var game = new Game();
            game.AddPlayer(Clients.Caller, username);
            GamesManager.Instance.RegisterGame(game);
#if DEBUG
            foreach (int id in GamesManager.Instance.GetGameIds())
            {
                Console.WriteLine("[API] Game id: " + id);
            }
#endif
            //send
            await Clients.Caller.SendAsync("ReceiveCode", game.Id);
        }

        public async Task JoinGame(int matchId, string username)
        {
            Game game;
            try
            {
                game = GamesManager.Instance.GetGame(matchId);
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("ReceiveFailure",
                    "Game with such id does not exist");
                return;
            }

            if (game.AddPlayer(Clients.Caller, username))
            {
                //start game for each client in game

                //retrieve data of game players
                var player1 = game.GetPlayer(0);
                var player2 = game.GetPlayer(1);
                /*
                 * @arg1 - opponent name
                 */
                await player1.GetClient().SendAsync("StartGame", player2.GetUsername(), matchId);
                await player2.GetClient().SendAsync("StartGame", player1.GetUsername(), matchId);
            }
            else
            {
                //failure to join due to too many players
                await Clients.Caller.SendAsync("ReceiveFailure",
                    "Game is already full and in progress");
            }
        }

        public async Task PlaceCard(int matchId, int cardId, string username)
        {
            //Retrieve game
            Game game;
            try
            {
                game = GamesManager.Instance.GetGame(matchId);
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync("ReceiveFailure",
                    "Game with such id does not exist");
                return;
            }

            //Get player by username
            var player = game.GetPlayerByUsername(username);
            if (player != null)
            {
                //Place card into card deck of a respective player
                if (!player.AddToNearest(cardId))
                {
                    await Clients.Caller.SendAsync("ReceiveFailure",
                        "Hand is already full");
                    return;
                }

                //Send back both card decks to players
                var player1 = game.GetPlayer(0);
                var player2 = game.GetPlayer(1);
                await player1.GetClient().SendAsync("ReceiveCardDecks", player1.Cards, player2.Cards);
                await player2.GetClient().SendAsync("ReceiveCardDecks", player2.Cards, player1.Cards);
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveFailure",
                    "Something went wrong with username detection");
            }
        }
    }
}
