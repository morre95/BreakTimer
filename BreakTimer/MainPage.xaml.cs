﻿using MediaManager;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BreakTimer
{
    public partial class MainPage : ContentPage
    {
        private int seconds = 0;
        private int oldSeconds = 0;

        private bool _timeOfDayIsChecked;
        public bool TimeOfDayIsChecked { get { return _timeOfDayIsChecked; } private set { _timeOfDayIsChecked = !value; } }

        private TimeTableDatabase Database { get; set; }

        private ILogger<MainPage> _logger { get; set; }

        private Stopwatch stopwatch;

        public MainPage(ILogger<MainPage> logger, TimeTableDatabase database)
        {
            InitializeComponent();
            Database = database;
            stopwatch = new();

            _logger = logger;

        }

        private async void StartTimerClicked(object sender, EventArgs e)
        {
            // TODO: Fixa så det går att lista Recources/Raw filerna eller hårdkoda in dem i en lista
            string mainDir = FileSystem.Current.AppDataDirectory;
            Debug.WriteLine(mainDir);
            Stream resourceStream = await FileSystem.OpenAppPackageFileAsync("AboutAssets.txt");

            FileStream? fs = resourceStream as FileStream;
            if (fs != null)
            {
                Debug.WriteLine(fs.ToString());
            }
            else
            {
                Debug.WriteLine("No fs!!!");
            }
            // Slut på test kod

            //await Database.DeleteAll();
            var items = await Database.GetItemsAsync();
            foreach (var item in items)
            {
                Debug.WriteLine($"#{item.ID}, {item.Seconds}, {item.Duration.ToString()}, Item: {JsonSerializer.Serialize(item)}");
                _logger.LogDebug($"#{item.ID}, Item: {JsonSerializer.Serialize(item)}");
            }

            if (sender is Button)
            {
                //await CrossMediaManager.Current.Play("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3");


                //Debug.WriteLine($"TimeEntry.Text: {TimeEntry.Text}, seconds: {seconds}");
                if (seconds <= 0)
                {
                    await DisplayAlert("Alert", $"'{TimeEntry.Text}' is not a vaild time", "OK");
                    return;
                }
                else if (!string.IsNullOrWhiteSpace(TimeEntry.Text))
                {
                    CheckIfTimeAsync(TimeEntry);
                }

                oldSeconds = seconds;

                Image image = new Image
                {
                    Source = ImageSource.FromFile("break_icon.png"),
                    WidthRequest = 228,
                };
                MainView.Children.Insert(0, image);

                ControllPanel.IsVisible = false;
                TimeLabel.FontSize = 110;
                UpdateTimeText();


                var timer = Dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += TimerAsync;

                stopwatch = Stopwatch.StartNew();
                timer.Start();

            }
        }

        private async void TimerAsync(object? sender, EventArgs e)
        {
            seconds--;
            UpdateTimeText();

            if (seconds <= 0 && sender is IDispatcherTimer t) 
            {
                _logger.LogDebug($"Stopwatch: {stopwatch.Elapsed.TotalSeconds}, Seconds: {oldSeconds}");
                stopwatch.Stop();

                t.Stop();
                string soundFile = "Gong_Sound.wav";
                await PlaySoundAsync(soundFile);

                

                await Database.SaveItemAsync(new()
                {
                    Seconds = oldSeconds,
                    Sound = soundFile,
                    Timestamp = DateTime.Now,
                    Duration = TimeSpan.FromSeconds(oldSeconds)
                });

                MainView.Children.RemoveAt(0);

                ControllPanel.IsVisible = true;
                TimeLabel.FontSize = 42;

                seconds = oldSeconds;

                UpdateTimeText(true);
            }
        }

        private async Task PlaySoundAsync(string sound)
        {
            var resourceStream = await FileSystem.OpenAppPackageFileAsync(sound);
            if (resourceStream is Stream stream)
            {
                await CrossMediaManager.Current.Play(stream, MediaManager.Media.MimeType.AudioMp3);
                await Task.Delay(100);
                await Task.Delay(CrossMediaManager.Current.Duration);
                //Debug.WriteLine($"Duration: {CrossMediaManager.Current.Duration.ToString()}, Position: {CrossMediaManager.Current.Position.ToString()}, Buffered: {CrossMediaManager.Current.Buffered.ToString()}");
                _logger.LogInformation($"Duration: {CrossMediaManager.Current.Duration.ToString()}, Position: {CrossMediaManager.Current.Position.ToString()}, Buffered: {CrossMediaManager.Current.Buffered.ToString()}");
            } 
        }

        private void UpdateTimeText(bool info = false)
        {
            TimeLabel.Text = TimeSpan.FromSeconds(seconds).ToString("hh':'mm':'ss");
            if (info)
            {
                // TODO: Ibland räknar detta en minut för kort rast
                DateTime dateTime = DateTime.Now + TimeSpan.FromSeconds(seconds);
                UpdateInfoTimeText(dateTime);

            }
        }

        private void UpdateInfoTimeText(DateTime dateTime)
        {
            InformationLabel.Text = InformationText.Text.Replace("{time}", dateTime.ToString("HH:mm"));
            SemanticScreenReader.Announce(InformationLabel.Text);
        }

        private void AddOrSubBtnClick(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string[] split = btn.Text.Split(' ');
                seconds += int.Parse(split[0]) * 60;
                if (seconds < 0)
                {
                    seconds = 0;
                }
                else if (!string.IsNullOrWhiteSpace(TimeEntry.Text))
                {
                    TimeEntry.Text = (DateTime.Now + TimeSpan.FromSeconds(seconds)).ToString("HH:mm");
                    SemanticScreenReader.Announce(TimeEntry.Text);
                }

                UpdateTimeText(true);
            }
        }

        private void ResetBtnClick(object sender, EventArgs e)
        {
            if (sender is Button)
            {
                seconds = 0;
                TimeEntry.Text = string.Empty;

                SemanticScreenReader.Announce(TimeEntry.Text);

                UpdateTimeText(true);
            }
        }

        private void TimeEntryChanged(object sender, EventArgs e)
        {
            if (sender is Entry timeEntry)
            {
                if (!CheckIfTimeAsync(timeEntry))
                {
                    _logger.LogDebug($"'{timeEntry.Text}' is not a volid time");
                }
            }
        }

        private void OnTimeEntryCompleted(object sender, EventArgs e)
        {
            if (sender is Entry timeEntry)
            {
                //CheckIfTime(timeEntry);
                StartTimerClicked(new Button(), e);
            }
        }

        private bool CheckIfTimeAsync(Entry timeEntry)
        {
            if (IsValidTime(timeEntry.Text))
            {
                if (DateTime.TryParse(timeEntry.Text, out DateTime date))
                {
                    TimeSpan diff = DateTime.Now - date;
                    if (diff.TotalSeconds < 0)
                    {
                        seconds = Math.Abs((int)diff.TotalSeconds);
                        UpdateTimeText();
                        UpdateInfoTimeText(date);
                        return true;
                    }
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('s'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out seconds))
                {
                    UpdateTimeText(true);
                    return true;
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('m'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out int minutes))
                {
                    seconds = minutes * 60;
                    UpdateTimeText(true);
                    return true;
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('h'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out int hours))
                {
                    seconds = hours * 60 * 60;
                    UpdateTimeText(true);
                    return true;
                }
            }
            else if (!string.IsNullOrEmpty(timeEntry.Text) && timeEntry.Text.Length >= 4)
            {
                seconds = 0;
                _logger.LogDebug($"The text '{timeEntry.Text}' is not a volid time");
                //await DisplayAlert("Alert", $"'{timeEntry.Text}' is not a vaild time", "OK");
            }
            return false;
        }

        public static bool IsValidTime(string str)
        {
            string strRegex = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$";
            Regex re = new Regex(strRegex);
            return re.IsMatch(str);
        }

        private void InfoTextChanged(object sender, EventArgs e)
        {
            if (sender is Entry)
            {
                UpdateTimeText(true);
            }
        }


    }

    public static class Constants
    {
        public const string DatabaseFilename = "my_database.db3";

        public const SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLiteOpenFlags.SharedCache;

        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
    }

    public class TimeTable
    {
        [PrimaryKey]
        public int ID { get; set; }
        public string Sound { get; set; }
        public DateTime Timestamp { get; set; }
        public TimeSpan Duration { get; set; }
        public int Seconds { get; set; }
    }

    public class TimeTableDatabase
    {
        SQLiteAsyncConnection Database;

        public TimeTableDatabase()
        {
            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            _ = Database.CreateTableAsync<TimeTable>();
        }

        public async Task<List<TimeTable>> GetItemsAsync()
        {
            return await Database.Table<TimeTable>().ToListAsync();
        }

        public async Task<TimeTable> GetItemAsync(int id)
        {
            return await Database.Table<TimeTable>().Where(i => i.ID == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveItemAsync(TimeTable item)
        {
            if (item.ID != 0)
                return await Database.UpdateAsync(item);
            else
                return await Database.InsertAsync(item);
        }

        public async Task<int> DeleteItemAsync(TimeTable item)
        {
            return await Database.DeleteAsync(item);
        }

        public async Task<int> DeleteItemAsync(int id)
        {
            TimeTable item = await GetItemAsync(id);
            return await Database.DeleteAsync(item);
        }

        public async Task<List<int>> DeleteAll() 
        { 
            List<TimeTable> items = await GetItemsAsync();
            List<int> indexes = new List<int>();
            foreach (TimeTable item in items)
            {
                indexes.Add(await Database.DeleteAsync(item));
            }
            return indexes;
        }

    }

}
