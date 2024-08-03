using MediaManager;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BreakTimer
{
    public partial class MainPage : ContentPage
    {
        private int seconds = 0;

        private bool _timeOfDayIsChecked;
        public bool TimeOfDayIsChecked { get { return _timeOfDayIsChecked; } private set { _timeOfDayIsChecked = !value; } }

        public MainPage()
        {
            InitializeComponent();
        }

        private async void StartTimerClicked(object sender, EventArgs e)
        {
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


                Image image = new Image
                {
                    Source = ImageSource.FromFile("break_icon.png"),
                    WidthRequest = 228,
                };
                MainView.Children.Insert(0, image);

                ControllPanel.IsVisible = false;
                TimeLabel.FontSize = 110;
                UpdateTimeText();

                while (seconds >= 0)
                {
                    UpdateTimeText();
                    seconds--;
                    await Task.Delay(1000);
                }

                await PlaySoundAsync(@"Gong_Sound.mp3");

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
                //await CrossMediaManager.Current.Play("https://ia800806.us.archive.org/15/items/Mp3Playlist_555/AaronNeville-CrazyLove.mp3");
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

}
