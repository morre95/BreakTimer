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
            UpdateTimeText(true);

            // TODO: Möjlighet att spela upp ljud när rasten är slut
            // TODO: Animation när rasten börjar
            /*Example:
            uint duration = 10 * 60 * 1000;
            await Task.WhenAll
            (
              TimeLabel.RotateTo(307 * 360, duration),
              TimeLabel.RotateXTo(251 * 360, duration),
              TimeLabel.RotateYTo(199 * 360, duration)
            );*/
        }

        private async void StartTimerClicked(object sender, EventArgs e)
        {
            Image image = new Image { 
                Source = ImageSource.FromFile("break.png"),
                WidthRequest = 228,
            };
            MainView.Children.Insert(0, image);

            ControllPanel.IsVisible = false;
            TimeLabel.FontSize = 110;
            UpdateTimeText(true);

            while (seconds >= 0)
            {
                UpdateTimeText();
                seconds--;
                await Task.Delay(1000);
            }

            MainView.Children.RemoveAt(0);

            ControllPanel.IsVisible = true;
            TimeLabel.FontSize = 42;
        }

        private void UpdateTimeText(bool info = false)
        {
            TimeLabel.Text = TimeSpan.FromSeconds(seconds).ToString("hh':'mm':'ss");
            if (info) 
            {
                DateTime dateTime = DateTime.Now + TimeSpan.FromSeconds(seconds);
                UpdateInfoTimeText(dateTime);
            }
        }

        private void UpdateInfoTimeText(DateTime dateTime)
        {
            InformationLabel.Text = InformationText.Text.Replace("{time}", dateTime.ToString("HH:mm"));
        }

        private void AddOrSubBtnClick(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string[] split = btn.Text.Split(' ');
                seconds += int.Parse(split[0]) * 60;
                if (seconds < 0) seconds = 0;
                UpdateTimeText(true);
            }
        }

        private void ResetBtnClick(object sender, EventArgs e)
        {
            if (sender is Button) 
            {
                seconds = 0;
                UpdateTimeText(true);
            }
        }

        private void TimeEntryChanged(object sender, EventArgs e)
        {
            if (sender is Entry timeEntry)
            {
                CheckIfTime(timeEntry);
            }
        }

        private void OnTimeEntryCompleted(object sender, EventArgs e)
        {
            if (sender is Entry timeEntry)
            {
                CheckIfTime(timeEntry);
                StartTimerClicked(sender, e);
            }
        }

        private void CheckIfTime(Entry timeEntry)
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
            else if (timeEntry.Text.ToLower().Contains('s'))
            {
                string secoundText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secoundText, out seconds))
                {
                    UpdateTimeText(true);
                }
            }
            else if (timeEntry.Text.ToLower().Contains('m'))
            {
                string secoundText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secoundText, out int minutes))
                {
                    seconds = minutes * 60;
                    UpdateTimeText(true);
                }
            }
            else if (timeEntry.Text.ToLower().Contains('h'))
            {
                string secoundText = timeEntry.Text.Remove(timeEntry.Text.Length - 1);
                if (int.TryParse(secoundText, out int hours))
                {
                    seconds = hours * 60 * 60;
                    UpdateTimeText(true);
                }
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