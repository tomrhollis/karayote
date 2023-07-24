using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Karayote.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace Karayote.ViewModels
{
    /// <summary>
    /// Display data for the main window
    /// </summary>
    internal class MainWindowViewModel : ObservableObject
    {
        private SongQueue songQueue;
        private KarayoteBot karayote;
        private static readonly object _lock = new object();

        private string nowPlaying;
        /// <summary>
        /// The title of the currently playing song.
        /// This is separated from RemainingQueue because its user will have already been notified that they are singing
        /// </summary>
        public string NowPlaying 
        {
            get => nowPlaying;
            private set
            {
                SetProperty(ref nowPlaying, value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SongDoneCommand?.NotifyCanExecuteChanged();
                    LoadCommand?.NotifyCanExecuteChanged();
                });
            }
        }

        private string nextUp;
        /// <summary>
        /// The title of the next song up in the queue
        /// This is separated from RemainingQueue because its user will have already been notified that they are next
        /// </summary>
        public string NextUp 
        {
            get => nextUp;
            private set
            {
                SetProperty(ref nextUp, value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RemoveNextCommand?.NotifyCanExecuteChanged();
                });
                
            }
        }

        /// <summary>
        /// All remaining songs after the current and next up songs, which can be rearranged freely
        /// </summary>
        public ObservableCollection<SelectedSong> RemainingQueue { get; set; } = new ObservableCollection<SelectedSong>();

        private string singerName;
        /// <summary>
        /// The name of a singer to add a song for
        /// </summary>
        public string SingerName
        {
            get => singerName;
            set
            {
                SetProperty(ref singerName, value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AddSongCommand?.NotifyCanExecuteChanged();
                });                
            }
        }

        private string songTitle;
        /// <summary>
        /// Title of a song to add
        /// </summary>
        public string SongTitle
        {
            get => songTitle;
            set
            {
                SetProperty(ref songTitle, value);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AddSongCommand?.NotifyCanExecuteChanged();
                });
            }            
        }

        private SelectedSong? selectedQueueSong;
        /// <summary>
        /// The song currently selected in the RemainingQueue list
        /// </summary>
        public SelectedSong? SelectedQueueSong
        {
            get => selectedQueueSong;
            set
            {
                SetProperty(ref selectedQueueSong, value);                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DeleteSongCommand?.NotifyCanExecuteChanged();
                });
            }
        }

        /// <summary>
        /// The index of the song currently selected in the RemainingQueue list
        /// </summary>
        public int SelectedIndex { get; set; }
        
        /// <summary>
        /// The command triggered by pressing the button to add a new song
        /// </summary>
        public RelayCommand? AddSongCommand { get; private set; }

        /// <summary>
        /// The command triggered by pressing the delete button under the RemainingQueue list
        /// </summary>
        public RelayCommand? DeleteSongCommand { get; private set; }

        /// <summary>
        /// The command triggered by pressing the remove button under the NextUp song
        /// </summary>
        public RelayCommand? RemoveNextCommand { get; private set; }

        /// <summary>
        /// The command triggered by pressing the Done button under the NowPlaying song
        /// </summary>
        public RelayCommand? SongDoneCommand { get; private set; }

        /// <summary>
        /// The command triggered by pressing the Load button under the NowPlaying song
        /// </summary>
        public RelayCommand? LoadCommand { get; private set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                               // (Fields are set in the UpdateElements() method)
        public MainWindowViewModel(IHost host)
#pragma warning restore CS8618
        {
            BindingOperations.EnableCollectionSynchronization(RemainingQueue, _lock); // Get the UI updates on the right thread

            songQueue = host.Services.GetRequiredService<SongQueue>();
            this.karayote = host.Services.GetRequiredService<KarayoteBot>();
            UpdateElements();
            songQueue.TheQueue.CollectionChanged += UpdateElements; // update UI when queue changes externally
            RemainingQueue.CollectionChanged += UpdateKarayote;     // update queue when changed in the UI

            // Add a song to the queue using the singer and song title from the right side of the UI
            AddSongCommand = new RelayCommand(execute: async () =>
            {
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
                // Make sure there are inputs in both text boxes
                return !string.IsNullOrEmpty(SongTitle) && !string.IsNullOrEmpty(SingerName);
            });

            // Delete the song selected in the queue listbox
            DeleteSongCommand = new RelayCommand(execute: async () =>
            {
                await karayote.DeleteSong(SelectedQueueSong!);
                SelectedQueueSong = null;
            },
            canExecute: () =>
            {
                // Make sure something is selected
                return SelectedQueueSong is not null;
            });

            // Remove the song that's next up (if that singer has a reserve song it will be slotted back in here)
            RemoveNextCommand = new RelayCommand(execute: async () =>
            {
                await karayote.DeleteSong(songQueue.NextUp!);            
            },
            canExecute: () =>
            {
                // Make sure there is a NextUp song
                return !string.IsNullOrEmpty(NextUp);
            });

            // Signal that the current song is done and ask if it was actually sung or not
            // (songs that were not actually sung may be selected again by other people)
            SongDoneCommand = new RelayCommand(execute: async () =>
            {
                if (!karayote.currentSession.IsStarted) // TODO: cause session updates to also update UI so button isn't enabled until start
                {
                    MessageBox.Show("The session hasn't started yet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBoxResult sung = MessageBox.Show("Did they actually sing it?", "", MessageBoxButton.YesNo, MessageBoxImage.Question);
                await karayote.AdvanceQueue(sung == MessageBoxResult.Yes);
            },
            canExecute: () =>
            {
                // make sure there is a NowPlaying song
                return !string.IsNullOrEmpty(NowPlaying);
            });

            // Load the NowPlaying song in Karafun or in a browser, depending on the type of song it is
            LoadCommand = new RelayCommand(execute: () =>
            {
                if (!karayote.currentSession.IsStarted) // TODO: cause session updates to also update UI so button isn't enabled until start
                { 
                    MessageBox.Show("The session hasn't started yet", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if(songQueue.NowPlaying is KarafunSong)
                {
                    uint id;
                    uint.TryParse(songQueue.NowPlaying.Id, out id);

                    karayote.karafun.AddToQueue(id, singer: songQueue.NowPlaying.User.Name);
                }

                else if (songQueue.NowPlaying is YoutubeSong)
                {
                    string targetURL = ((YoutubeSong)songQueue.NowPlaying).Link;
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = targetURL,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }                
            },
            canExecute: () =>
            {
                // Can execute if there's a now playing song and the song is a type we have support for loading with the button
                return !string.IsNullOrEmpty(NowPlaying) && songQueue.NowPlaying is not PlaceholderSong;
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

        /// <summary>
        /// Update the data bound to UI elements
        /// </summary>
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

        /// <summary>
        /// Update the actual queue after a change in the UI listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateKarayote(object? sender, NotifyCollectionChangedEventArgs e)
        {
            songQueue.TheQueue.CollectionChanged -= UpdateElements; // disable event for queue changing or there's a conflict

            if(e.Action == NotifyCollectionChangedAction.Move)      // handle move actions
                await karayote.MoveSong(e.OldStartingIndex + 2, e.NewStartingIndex + 2);

            songQueue.TheQueue.CollectionChanged += UpdateElements; // re-enable event to update UI when queue changes from other sources
        }
    }
}
