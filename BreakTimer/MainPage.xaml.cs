using MediaManager;
using Microsoft.Extensions.Logging;
using SQLite;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BreakTimer
{
    public partial class MainPage : ContentPage
    {
        private int seconds = 0;

        private bool _timeOfDayIsChecked;
        public bool TimeOfDayIsChecked { get { return _timeOfDayIsChecked; } private set { _timeOfDayIsChecked = !value; } }

        private TimeTableDatabase Database { get; set; }

        private ILogger<MainPage> Logger { get; set; }

        public MainPage(ILogger<MainPage> logger)
        {
            InitializeComponent();
            Database = new();

            Logger = logger;
        }

        private async void StartTimerClicked(object sender, EventArgs e)
        {

            var items = await Database.GetItemsAsync();
            foreach (var item in items)
            {
                Debug.WriteLine($"#{item.ID}, {item.Seconds}, {item.Duration.ToString()}");
                Logger.LogDebug($"#{item.ID}, Sec: {item.Seconds}, Duration: {item.Duration.ToString()}, sound: {item.Sound}");
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

                int oldSec = seconds;

                Image image = new Image
                {
                    Source = ImageSource.FromFile("break_icon.png"),
                    WidthRequest = 228,
                };
                MainView.Children.Insert(0, image);

                ControllPanel.IsVisible = false;
                TimeLabel.FontSize = 110;
                UpdateTimeText();


                //Implementera denna kod
                Debug.WriteLine(oldSec + " Sec");

                Stopwatch stopwatch = new Stopwatch();
                var timer = Dispatcher.CreateTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += TimerAsync;
                stopwatch.Start();
                timer.Start();

                /*while (seconds >= 0)
                {
                    UpdateTimeText();
                    seconds--;
                    await Task.Delay(1000);
                }

                //await Task.Run(() => PlaySoundAsync(@"Gong_Sound.mp3"));
                string soundFile = "Gong_Sound.wav";
                await PlaySoundAsync(soundFile);

                await Database.SaveItemAsync(new()
                {
                    Seconds = oldSec,
                    Sound = soundFile,
                    Timestamp = DateTime.Now,
                    Duration = TimeSpan.FromSeconds(oldSec)
                });


                MainView.Children.RemoveAt(0);

                ControllPanel.IsVisible = true;
                TimeLabel.FontSize = 42;*/

            }
        }

        private async void TimerAsync(object? sender, EventArgs e)
        {
            UpdateTimeText();
            seconds--;

            if (seconds <= 0 && sender is IDispatcherTimer t) 
            {
                t.Stop();
                string soundFile = "Gong_Sound.wav";
                await PlaySoundAsync(soundFile);

                await Database.SaveItemAsync(new()
                {
                    Seconds = seconds,
                    Sound = soundFile,
                    Timestamp = DateTime.Now,
                    Duration = TimeSpan.FromSeconds(seconds)
                });


                MainView.Children.RemoveAt(0);

                ControllPanel.IsVisible = true;
                TimeLabel.FontSize = 42;
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
                Debug.WriteLine($"Duration: {CrossMediaManager.Current.Duration.ToString()}, Position: {CrossMediaManager.Current.Position.ToString()}, Buffered: {CrossMediaManager.Current.Buffered.ToString()}");
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
                CheckIfTimeAsync(timeEntry);
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

        private void CheckIfTimeAsync(Entry timeEntry)
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
                    }
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('s'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out seconds))
                {
                    UpdateTimeText(true);
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('m'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out int minutes))
                {
                    seconds = minutes * 60;
                    UpdateTimeText(true);
                }
            }
            else if (timeEntry.Text.ToLower().EndsWith('h'))
            {
                string secondText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secondText, out int hours))
                {
                    seconds = hours * 60 * 60;
                    UpdateTimeText(true);
                }
            }
            else if (!string.IsNullOrEmpty(timeEntry.Text) && timeEntry.Text.Length >= 4)
            {
                seconds = -1;
                //await DisplayAlert("Alert", $"'{timeEntry.Text}' is not a vaild time", "OK");
            }
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

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);
    }

    public class TimeTable
    {
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
        }

        async Task Init()
        {
            if (Database is not null)
                return;

            Database = new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
            var result = await Database.CreateTableAsync<TimeTable>();
        }

        public async Task<List<TimeTable>> GetItemsAsync()
        {
            await Init();
            return await Database.Table<TimeTable>().ToListAsync();
        }

        public async Task<TimeTable> GetItemAsync(int id)
        {
            await Init();
            return await Database.Table<TimeTable>().Where(i => i.ID == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveItemAsync(TimeTable item)
        {
            await Init();
            if (item.ID != 0)
                return await Database.UpdateAsync(item);
            else
                return await Database.InsertAsync(item);
        }

        public async Task<int> DeleteItemAsync(TimeTable item)
        {
            await Init();
            return await Database.DeleteAsync(item);
        }

    }

}
