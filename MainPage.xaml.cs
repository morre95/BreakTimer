﻿namespace BreakTimer
{
    public partial class MainPage : ContentPage
    {
        int seconds = 600;

        public MainPage()
        {
            InitializeComponent();
            UpdateTimeText(true);
        }

        private async void StartTimer_Clicked(object sender, EventArgs e)
        {
            ControllPanel.IsVisible = false;
            TimeLabel.FontSize = 85;
            UpdateTimeText(true);

            while (seconds > 0)
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
                InformationLabel.Text = $"Rasten är över {dateTime.ToString("HH:mm")}";
            }
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

    }
}