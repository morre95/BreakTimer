using System.Text.RegularExpressions;

namespace BreakTimer
{
    public partial class MainPage : ContentPage
    {
        private int seconds = 600;

        private bool _timeOfDayIsChecked;
        public bool TimeOfDayIsChecked { get { return _timeOfDayIsChecked; } private set { _timeOfDayIsChecked = !value; } }

        public MainPage()
        {
            InitializeComponent();
            UpdateTimeText(true);
            // TODO: Fixa en Entry att skriva tiden i. Och också möjligheten att välja mellan kloch slaget då rasten är slut éller hur lång den är
            // TODO: Spela upp ljud när rasten är slut
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

        private async void StartTimer_Clicked(object sender, EventArgs e)
        {
            ControllPanel.IsVisible = false;
            TimeLabel.FontSize = 85;
            UpdateTimeText(true);

            while (seconds >= 0)
            {
                UpdateTimeText();
                seconds--;
                await Task.Delay(1000);
            }
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
            //InformationLabel.Text = $"Rasten är över {dateTime.ToString("HH:mm")}";
        }

        private void AddOrSubBtn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                string[] split = btn.Text.Split(' ');
                seconds += int.Parse(split[0]) * (split[1] == "sec" ? 1 : 60);
                if (seconds < 0) seconds = 0;
                UpdateTimeText(true);
            }
        }

        private void ResetBtn_Click(object sender, EventArgs e)
        {
            seconds = 0;
            UpdateTimeText(true);
        }

        private void TimeOfDay_CHeckChanged(object sender, CheckedChangedEventArgs e)
        {
            TimeOfDayIsChecked = TimeOfDayCBox.IsChecked;
            //TimeEntry.IsEnabled = !TimeEntry.IsEnabled;
        }

        private void TimeEntry_Changed(object sender, EventArgs e)
        {
            var timeEntry = sender as Entry;

            if (timeEntry != null)
            {
                if (TimeOfDayIsChecked)
                {
                    TimeSpan time;
                    if (TimeSpan.TryParse(timeEntry.Text, out time))
                    {
                        UpdateInfoTimeText(DateTime.Now + time);
                        seconds = (int)time.TotalSeconds;
                        UpdateTimeText(true);
                    }
                }
                else
                {
                    if (IsValidTime(timeEntry.Text))
                    {
                        DateTime date;
                        if (DateTime.TryParse(timeEntry.Text, out date))
                        {
                            UpdateInfoTimeText(date);
                        }
                    }
                }
            } 
        }

        public bool IsValidTime(string str)
        {
            string strRegex = @"^([01]?[0-9]|2[0-3]):[0-5][0-9]$";
            Regex re = new Regex(strRegex);
            return re.IsMatch(str);
        }

        private void InfoTextChanged(object sender, EventArgs e)
        {
            Entry entry = sender as Entry;
            if (entry != null)
            {
                UpdateTimeText(true);
            }
        }

    }
}