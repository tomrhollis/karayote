using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karayote.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace Karayote.ViewModels
{
    internal class MainWindowViewModel : ObservableObject//, IDropTarget
    {
        private SongQueue songQueue;
        private KarayoteBot karayote;

        private string nowPlaying;
        public string NowPlaying 
        {
            get => nowPlaying;
            private set => SetProperty(ref nowPlaying, value);
        }

        private string nextUp;
        public string NextUp 
        {
            get => nextUp;
            private set => SetProperty(ref nextUp, value);
        }

        private List<SelectedSong> remainingQueue;
        public List<SelectedSong> RemainingQueue 
        {
            get => remainingQueue;
            set => SetProperty(ref remainingQueue, value);
        }

        private string singerName;
        public string SingerName
        {
            get => singerName;
            set => SetProperty(ref singerName, value);
        }

        private string songTitle;
        public string SongTitle
        {
            get => songTitle;
            set => SetProperty(ref songTitle, value);
        }


        public RelayCommand AddSongCommand { get; private set; }

        public MainWindowViewModel(IHost host)
        {
            songQueue = host.Services.GetRequiredService<SongQueue>();
            this.karayote = host.Services.GetRequiredService<KarayoteBot>();
            UpdateElements();
            songQueue.TheQueue.CollectionChanged += UpdateElements;

            AddSongCommand = new RelayCommand(execute: async () =>
                {
                    // TODO: move to canExecute eventually
                    if (string.IsNullOrEmpty(SongTitle) || string.IsNullOrEmpty(SingerName))
                    {
                        MessageBox.Show("Must have a singer name and a song title", "Error");
                        return;
                    }

                    // register a user with karayote or retrieve an existing one
                    KarayoteUser user = karayote.CreateOrFindUser(SingerName);

                    // send to karayotebot and act on success or failure
                    string response = await karayote.TryAddSong(new PlaceholderSong(user, SongTitle));
                    bool success = response.Substring(0, 5) == "Added";

                    // on failure, notify user
                    if (!success)
                    {
                        MessageBox.Show("In the app, the user would have seen this error:\n\n" + response, "Error");
                    }
                    
                    // on success, clear the text boxes
                    else
                    {
                        SongTitle = string.Empty;
                        SingerName = string.Empty;
                        MessageBox.Show("In the app, the user would have seen this confirmation:\n\n" + response, "Success");
                    }
                },
                canExecute: () => 
                {
                    // make sure there are inputs in both text boxes
                    return true;
                }
            );
        }

        /// <summary>
        /// Method to catch the event parameters and ignore them (they aren't needed right now)
        /// </summary>
        /// <param name="sender">The initiator of the collection changed event</param>
        /// <param name="e">The associated data</param>
        private void UpdateElements(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateElements();
        }

        private void UpdateElements()
        {
            NowPlaying = songQueue.NowPlaying is null ? "" : songQueue.NowPlaying.ToString();

            NextUp = songQueue.NextUp is null ? "" : songQueue.NextUp.ToString();
            
            RemainingQueue = songQueue.Count < 3 ? new List<SelectedSong>() : songQueue.TheQueue.ToList().GetRange(2, songQueue.Count - 2);
        }
        /*
        public void DragOver(IDropInfo dropInfo)
        {
            throw new System.NotImplementedException();
        }

        public void Drop(IDropInfo dropInfo)
        {
            throw new System.NotImplementedException();
        }*/
    }
}
