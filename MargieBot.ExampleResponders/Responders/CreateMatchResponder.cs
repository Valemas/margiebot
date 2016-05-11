using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MargieBot.ExampleResponders.Models;
using MargieBot.Models;
using MargieBot.Responders;

using Newtonsoft.Json;

namespace MargieBot.ExampleResponders.Responders
{
    public class CreateMatchResponder : IResponder
    {
        private const string CREATE_TEXT = @"create_match";
        private const string MATCHES_FILE_PATH = @"C:\Users\t.bouman\Documents\slackteambot\Matches.json";
        private List<Match> _matches;
        private StringBuilder _messageBuilder;
        private int _matchId;
        public bool CanRespond(ResponseContext context)
        {
            if (_matches == null)
            {
                using (var r = new StreamReader(MATCHES_FILE_PATH))
                {
                    var json = r.ReadToEnd();
                    _matches = JsonConvert.DeserializeObject<List<Match>>(json) ?? new List<Match>();
                    var highestMatchId = _matches.Select(existingMatch => existingMatch.MatchId).Concat(new[] { 0 }).Min();
                    _matchId = highestMatchId + 1;
                }
            }
            if (_messageBuilder == null)
                _messageBuilder = new StringBuilder();
            return !context.Message.User.IsSlackbot && context.Message.Text.Contains(CREATE_TEXT);
        }

        public BotMessage GetResponse(ResponseContext context)
        {
            //todo enable picking of date but fuck that for now
            var date = DateTime.Today;
            var highestMatchId = _matches.Select(existingMatch => existingMatch.MatchId).Concat(new[] {0}).Min();
            
            var match = new Match
                        {
                            MatchId = _matchId,
                            MatchDate = date
                        };
            _matches.Add(match);
            var jsondata = JsonConvert.SerializeObject(_matches);
            File.WriteAllText(MATCHES_FILE_PATH, jsondata);
            var message = $"New match for {match.MatchDate.ToShortDateString()} created! _Match ID: {match.MatchId}_";
            _matchId++;
            return new BotMessage {Text = message};
        }
    }
}
