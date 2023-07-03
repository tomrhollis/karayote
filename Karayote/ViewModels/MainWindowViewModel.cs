using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Google.Apis.YouTube.v3.Data;
using Karayote.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace Karayote.ViewModels
{
    internal class MainWindowViewModel : ObservableObject
    {
        private SongQueue songQueue;
        private KarayoteBot karayote;
        private static readonly object _lock = new object();

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

        public ObservableCollection<SelectedSong> RemainingQueue { get; set; } = new ObservableCollection<SelectedSong>();

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

        private SelectedSong? selectedQueueSong;
        public SelectedSong? SelectedQueueSong
        {
            get => selectedQueueSong;
            set => SetProperty(ref selectedQueueSong, value);
        }

        public int SelectedIndex { get; set; }
        
        public RelayCommand AddSongCommand { get; private set; }

        public RelayCommand DeleteSongCommand { get; private set; }

        public RelayCommand RemoveNextCommand { get; private set; }

        public RelayCommand SongDoneCommand { get; private set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                               // (Fields are set in the UpdateElements() method)
        public MainWindowViewModel(IHost host)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            BindingOperations.EnableCollectionSynchronization(RemainingQueue, _lock);

            songQueue = host.Services.GetRequiredService<SongQueue>();
            this.karayote = host.Services.GetRequiredService<KarayoteBot>();
            UpdateElements();
            songQueue.TheQueue.CollectionChanged += UpdateElements;
            RemainingQueue.CollectionChanged += UpdateKarayote;

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
                    MessageBox.Show("In the app, the user would have seen this error:\n\n" + response, "Error");

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
                // TODO: make sure there are inputs in both text boxes
                return true;
            });

            DeleteSongCommand = new RelayCommand(execute: async () =>
            {
                if (SelectedQueueSong is null) return;
                await karayote.DeleteSong(SelectedQueueSong);
                SelectedQueueSong = null;
            },
            canExecute: () =>
            {
                // TODO: make sure something is selected
                return true;
            });

            RemoveNextCommand = new RelayCommand(execute: async () =>
            {
                if (string.IsNullOrEmpty(NextUp)) return;
                await karayote.DeleteSong(songQueue.NextUp!);            
            },
            canExecute: () =>
            {
                // TODO: make sure there is a NextUp song
                return true;
            });

            SongDoneCommand = new RelayCommand(execute: async () =>
            {
                if (string.IsNullOrEmpty(NowPlaying)) return;
                MessageBoxResult sung = MessageBox.Show("Did they actually sing it?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);

                await karayote.AdvanceQueue(sung == MessageBoxResult.Yes);
            },
            canExecute: () =>
            {
                // TODO: make sure there is a NextUp song
                return true;
            });
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
            NowPlaying = songQueue.NowPlaying is null ? "" : songQueue.NowPlaying.UIString;

            NextUp = songQueue.NextUp is null ? "" : songQueue.NextUp.UIString;

            lock (_lock)
            {
                RemainingQueue.Clear();
                if (songQueue.Count > 2)
                {
                    for (int i = 2; i < songQueue.Count; i++)
                    {
                        RemainingQueue.Add(songQueue.TheQueue[i]);
                    }
                }
            }
            if (SelectedQueueSong is not null && !RemainingQueue.Contains(SelectedQueueSong))
                SelectedQueueSong = null;
        }

        private async void UpdateKarayote(object? sender, NotifyCollectionChangedEventArgs e)
        {
            songQueue.TheQueue.CollectionChanged -= UpdateElements; // disable event for queue changing or there's a conflict

            if(e.Action == NotifyCollectionChangedAction.Move)      // handle move actions
                await karayote.MoveSong(e.OldStartingIndex + 2, e.NewStartingIndex + 2);

            songQueue.TheQueue.CollectionChanged += UpdateElements; // re-enable event to update UI when queue changes from other sources
        }
    }
}
