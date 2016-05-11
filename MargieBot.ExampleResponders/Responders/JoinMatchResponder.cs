using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using MargieBot.ExampleResponders.Models;
using MargieBot.Models;
using MargieBot.Responders;

using Newtonsoft.Json;

using Match=MargieBot.ExampleResponders.Models.Match;

namespace MargieBot.ExampleResponders.Responders
{
    public class JoinMatchResponder : IResponder
    {
        private const string JOIN_TEXT = @"join_match";
        private const string PLAYER_FILE_PATH = @"C:\Users\t.bouman\Documents\slackteambot\Players.json";
        private const string MATCHES_FILE_PATH = @"C:\Users\t.bouman\Documents\slackteambot\Matches.json";
        private List<Player> _players;
        private List<Match> _matches;
        private StringBuilder _messageBuilder;

        public bool CanRespond(ResponseContext context)
        {
            if(_players == null)
            {
                using(var r = new StreamReader(PLAYER_FILE_PATH))
                {
                    var json = r.ReadToEnd();
                    _players = JsonConvert.DeserializeObject<List<Player>>(json) ?? new List<Player>();
                }
            }
            if(_matches == null)
            {
                using(var r = new StreamReader(MATCHES_FILE_PATH))
                {
                    var json = r.ReadToEnd();
                    _matches = JsonConvert.DeserializeObject<List<Match>>(json) ?? new List<Match>();
                }
            }
            if(_messageBuilder == null)
                _messageBuilder = new StringBuilder();
            return !context.Message.User.IsSlackbot && context.Message.Text.Contains(JOIN_TEXT);
        }

        public BotMessage GetResponse(ResponseContext context)
        {
            var user = context.Message.User;
            var userExists = false;
            Player player = null;
            string name;
            context.UserNameCache.TryGetValue(user.ID, out name);

            foreach(var existingPlayer in _players.Where(existingPlayer => existingPlayer.Id == user.ID))
            {
                userExists = true;
                player = existingPlayer;
            }
            if(!userExists)
                player = CreateUser(user.ID, name);

            int matchId;
            var hasMatchId = int.TryParse(Regex.Match(context.Message.Text, @"\d+").Value, out matchId);
            if(!hasMatchId)
                matchId = 0;

            JoinMatch(player, matchId);


            return new BotMessage {Text = _messageBuilder.ToString()};
        }

        private void JoinMatch(Player player, int matchId)
        {
            Match match = null;
            if(matchId == 0)
            {
                match = _matches.First(x => x.MatchDate >= DateTime.Today);
                //todo fix this while to actually work because I think I'm actualyl retarded
                while(match.AllPlayers.Count > 9 || match.AllPlayers.Contains(player))
                    match = _matches.First(x => x.MatchDate > match.MatchDate);
            }
            else
            {
                try
                {
                    match = _matches.Find(x => x.MatchId == matchId);
                }
                catch(Exception)
                {
                    _messageBuilder.Append(
                        $"Cannot find match with Id: {matchId}, use `list_match` to see all upcoming matches");
                }
            }
            if(match.AllPlayers.Contains(player))
                _messageBuilder.Append($"{player.Name}, you are already in this match");
            else if(match.AllPlayers.Count == 10)
                _messageBuilder.Append($"Sorry {player.Name}, this match is already full");
            else
            {
                match.AllPlayers.Add(player);
                StoreMatch(match);
                _messageBuilder.Append(
                    $"{player.Name} joined the match on {match.MatchDate.ToShortDateString()} _(MatchId: {match.MatchId})_ \n\n");
                if(match.AllPlayers.Count == 10)
                    BuildTeams(match);
            }
        }

        private Player CreateUser(string id, string name)
        {
            var player = new Player
                         {
                             Id = id,
                             Name = name,
                             Rating = 0m
                         };
            _players.Add(player);
            var json = JsonConvert.SerializeObject(_players);
            File.WriteAllText(PLAYER_FILE_PATH, json);

            return player;
        }

        private void BuildTeams(Match match)
        {
            var random = new Random();
            var randomizedList = match.AllPlayers.OrderBy(x => random.Next());
            match.Team1 = new Team {Players = randomizedList.Take(randomizedList.Count() / 2).ToList()};
            match.Team2 = new Team {Players = randomizedList.Skip(randomizedList.Count() / 2).ToList()};

            var builder = new StringBuilder();
            builder.Append($"*The match is full and will be played on: {match.MatchDate}*\n*Team 1:*");
            foreach(var player in match.Team1.Players)
                builder.Append($"\n{player.Name} - _{player.Rating}_");
            builder.Append("\n\n*Team 2:*");
            foreach(var player in match.Team2.Players)
                builder.Append($"\n{player.Name} - _{player.Rating}_");
            _messageBuilder.AppendLine();
            _messageBuilder.Append(builder);
            StoreMatch(match);
        }

        private void StoreMatch(Match match)
        {
            _matches[_matches.FindIndex(ind => ind.MatchId.Equals(match.MatchId))] = match;
            var jsondata = JsonConvert.SerializeObject(_matches);
            File.WriteAllText(MATCHES_FILE_PATH, jsondata);
        }
    }
}
