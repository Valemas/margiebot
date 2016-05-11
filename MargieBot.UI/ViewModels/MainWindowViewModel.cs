using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Bazam.Wpf.UIHelpers;
using Bazam.Wpf.ViewModels;
using MargieBot.Models;
using MargieBot.ExampleResponders.Models;
using MargieBot.ExampleResponders.Responders;
using MargieBot.Responders;
using System.Configuration;
using MargieBot.UI.Properties;

namespace MargieBot.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
    {
        private Bot _Margie;

        private string _AuthKeySlack = ConfigurationManager.AppSettings["slackAccountApiKey"];
        public string AuthKeySlack
        {
            get { return _AuthKeySlack; }
            set { ChangeProperty(vm => vm.AuthKeySlack, value); }
        }

        private string _BotUserID = string.Empty;
        public string BotUserID
        {
            get { return _BotUserID; }
            set { ChangeProperty(vm => vm.BotUserID, value); }
        }

        private string _BotUserName = string.Empty;
        public string BotUserName
        {
            get { return _BotUserName; }
            set { ChangeProperty(vm => vm.BotUserName, value); }
        }

        private IReadOnlyList<SlackChatHub> _ConnectedHubs;
        public IReadOnlyList<SlackChatHub> ConnectedHubs
        {
            get { return _ConnectedHubs; }
            set { ChangeProperty(vm => vm.ConnectedHubs, value); }
        }

        private DateTime? _ConnectedSince = null;
        public DateTime? ConnectedSince
        {
            get { return _ConnectedSince; }
            set { ChangeProperty(vm => vm.ConnectedSince, value); }
        }

        private bool _ConnectionStatus = false;
        public bool ConnectionStatus
        {
            get { return _ConnectionStatus; }
            set { ChangeProperty(vm => vm.ConnectionStatus, value); }
        }

        private List<string> _Messages = new List<string>();
        public IEnumerable<string> Messages
        {
            get { return _Messages; }
        }

        private string _MessageToSend = string.Empty;
        public string MessageToSend
        {
            get { return _MessageToSend; }
            set { ChangeProperty(vm => vm.MessageToSend, value); }
        }

        private SlackChatHub _SelectedChatHub;
        public SlackChatHub SelectedChatHub
        {
            get { return _SelectedChatHub; }
            set { ChangeProperty(vm => vm.SelectedChatHub, value); }
        }

        private string _TeamName = string.Empty;
        public string TeamName
        {
            get { return _TeamName; }
            set { ChangeProperty(vm => vm.TeamName, value); }
        }

        public ICommand ConnectCommand
        {
            get { 
                return new RelayCommand(async () => {
                    if (_Margie != null && ConnectionStatus) {
                        SelectedChatHub = null;
                        ConnectedHubs = null;
                        _Margie.Disconnect();
                    }
                    else {
                        // let's margie
                        _Margie = new Bot();
                        _Margie.Aliases = GetAliases();
                        
                        // RESPONDER WIREUP
                        _Margie.Responders.AddRange(GetResponders());

                        _Margie.ConnectionStatusChanged += (bool isConnected) => {
                            ConnectionStatus = isConnected;

                            if (isConnected) {
                                // now that we're connected, build list of connected hubs for great glory
                                List<SlackChatHub> hubs = new List<SlackChatHub>();
                                hubs.AddRange(_Margie.ConnectedChannels);
                                hubs.AddRange(_Margie.ConnectedGroups);
                                hubs.AddRange(_Margie.ConnectedDMs);
                                ConnectedHubs = hubs;

                                if (ConnectedHubs.Count > 0) {
                                    SelectedChatHub = ConnectedHubs[0];
                                }

                                // also set other cool properties
                                BotUserID = _Margie.UserID;
                                BotUserName = _Margie.UserName;
                                ConnectedSince = _Margie.ConnectedSince;
                                TeamName = _Margie.TeamName;
                            }
                            else {
                                ConnectedHubs = null;
                                BotUserID = null;
                                BotUserName = null;
                                ConnectedSince = null;
                                TeamName = null;
                            }
                        };

                        _Margie.MessageReceived += (string message) => {
                            int messageCount = _Messages.Count - 500;
                            for (int i = 0; i < messageCount; i++) {
                                _Messages.RemoveAt(0);
                            }

                            _Messages.Add(message);
                            RaisePropertyChanged("Messages");
                        };

                        await _Margie.Connect(AuthKeySlack);

                        // if we're here, we're connected, so store the key as our last slack key in settings
                        Settings.Default.LastSlackKey = AuthKeySlack;
                        Settings.Default.Save();
                    }
                }); 
            }
        }

        public ICommand TalkCommand
        {
            get
            {
                return new RelayCommand(async () => {
                    await _Margie.Say(new BotMessage() { Text = MessageToSend, ChatHub = SelectedChatHub });
                    MessageToSend = string.Empty;
                });
            }
        }

        public MainWindowViewModel()
        {
            AuthKeySlack = Settings.Default.LastSlackKey;
        }
     
        private IReadOnlyList<string> GetAliases()
        {
            return new List<string> { "soccer_bot", "soccerbot", "bot" };
        }

        private IList<IResponder> GetResponders()
        {
            List<IResponder> responders = new List<IResponder>
                                          {
                                              new JoinMatchResponder(),
                                              new CreateMatchResponder(),
                                              new MatchInformationResponder()
                                          };

            //            _tomGaBot.RespondsTo(" ", true)
            //                     .With("Fuck you");

            return responders;
        }
    }
}