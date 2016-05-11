using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;

using MargieBot.Models;
using MargieBot.Responders;

using Newtonsoft.Json;

using Match=MargieBot.ExampleResponders.Models.Match;

namespace MargieBot.ExampleResponders.Responders
{
    public class MatchInformationResponder : IResponder
    {
        private const string LIST_TEXT = @"list_match";
        private const string SHOW_TEXT = @"show_match";
        private bool IsListCommand;
        private const string MATCHES_FILE_PATH = @"C:\Users\t.bouman\Documents\slackteambot\Matches.json";
        private List<Match> _matches;
        private StringBuilder _messageBuilder;

        public bool CanRespond(ResponseContext context)
        {
            if (_matches == null)
            {
                using (var r = new StreamReader(MATCHES_FILE_PATH))
                {
                    var json = r.ReadToEnd();
                    _matches = JsonConvert.DeserializeObject<List<Match>>(json) ?? new List<Match>();
                }
            }
            if (_messageBuilder == null)
                _messageBuilder = new StringBuilder();
           
            IsListCommand = context.Message.Text.Contains(LIST_TEXT);
            
            return !context.Message.User.IsSlackbot && (IsListCommand || context.Message.Text.Contains(SHOW_TEXT));
        }

        public BotMessage GetResponse(ResponseContext context)
        {
            if(IsListCommand)
            {
                foreach(var match in _matches.Where(x => x.MatchDate >= DateTime.Today))
                {
                    MatchInfoTextBuilder(match);
                    _messageBuilder.AppendLine();
                }
            }
            else
            {
                MatchInfoTextBuilder(_matches.Find(x => x.MatchId == int.Parse(Regex.Match(context.Message.Text, @"\d+").Value)));
            }

            return new BotMessage {Text = _messageBuilder.ToString()};
        }

        private void MatchInfoTextBuilder(Match match)
        {
            if(match.AllPlayers.Count == 10)
            {
                _messageBuilder.Append($"*Match Id: {match.MatchId} | Match date: {match.MatchDate}*\n*Team 1:*");
                foreach (var player in match.Team1.Players)
                    _messageBuilder.Append($"\n{player.Name} - _{player.Rating}_");
                _messageBuilder.Append("\n\n*Team 2:*");
                foreach (var player in match.Team2.Players)
                    _messageBuilder.Append($"\n{player.Name} - _{player.Rating}_");
            }
            else
            {
                _messageBuilder.Append($"*Match Id: {match.MatchId} | Match date: {match.MatchDate}*");
                _messageBuilder.Append($"Currently {match.AllPlayers.Count} players signed up for this match.");
            }

        }
    }
}
