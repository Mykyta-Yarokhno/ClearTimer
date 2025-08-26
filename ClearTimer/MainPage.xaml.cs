using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;

namespace ClearTimer
{
    public enum TimerMode
    {
        Timer,
        IntervalTimer,
        Stopwatch
    }

    public partial class MainPage : ContentPage
    {
        private IDispatcherTimer _timer;
        private bool _isRunning;
        private int _totalSeconds;

        private TimerMode _currentMode;
        private bool _isWorkTime = true;
        private int _workTimeInSeconds = 0;
        private int _breakTimeInSeconds = 0;
        private int _currentCycle = 0;

        private double _lastPanX = 0;
        private double _modeLastPanX = 0;
        private bool _hasSwiped = false;

        public MainPage()
        {
            InitializeComponent();

            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;

            StartButton.IsVisible = true;
            PauseButton.IsVisible = false;
            StopButton.IsVisible = false;

            _totalSeconds = 0;
            _currentMode = TimerMode.Timer;
            UpdateTimerLabel();
            UpdateModeUI();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            double minDimension = Math.Min(width, height);
            double containerSize = minDimension * 0.9;

            TimerContainer.WidthRequest = containerSize;
            TimerContainer.HeightRequest = containerSize;

            foreach (var child in TimerContainer.Children)
            {
                if (child is Ellipse ellipse)
                {
                    ellipse.WidthRequest = containerSize;
                    ellipse.HeightRequest = containerSize;
                }
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            if (_currentMode == TimerMode.IntervalTimer)
            {
                _totalSeconds--;

                if (_totalSeconds <= 0)
                {
                    if (_isWorkTime)
                    {
                        _currentCycle++;
                        RepeatLabel.Text = _currentCycle.ToString();

                        if (_breakTimeInSeconds > 0)
                        {
                            _isWorkTime = false;
                            _totalSeconds = _breakTimeInSeconds;
                            UpdateTimerLabel();
                        }
                        else
                        {
                            _isWorkTime = true;
                            _totalSeconds = _workTimeInSeconds;
                            UpdateTimerLabel();
                        }
                    }
                    else
                    {
                        _isWorkTime = true;
                        _totalSeconds = _workTimeInSeconds;
                        UpdateTimerLabel();
                    }
                }
            }
            else
            {
                if (_currentMode == TimerMode.Timer)
                {
                    _totalSeconds--;
                    if (_totalSeconds <= 0)
                    {
                        _timer.Stop();
                        _isRunning = false;
                        _totalSeconds = 0;
                        ShowButtons(true, false, false);
                    }
                }
                else if (_currentMode == TimerMode.Stopwatch)
                {
                    _totalSeconds++;
                }
            }

            UpdateTimerLabel();
        }

        private void OnStartClicked(object sender, EventArgs e)
        {
            if (_currentMode == TimerMode.IntervalTimer)
            {
                if (SetTimeButton.Text == "Set break")
                {
                    _workTimeInSeconds = _totalSeconds;
                }
                else
                {
                    _breakTimeInSeconds = _totalSeconds;
                }
            }

            if (_workTimeInSeconds > 0 || _currentMode != TimerMode.IntervalTimer)
            {
                _isRunning = true;
                _timer.Start();

                if (_currentMode == TimerMode.IntervalTimer && _currentCycle == 0)
                {
                    _totalSeconds = _workTimeInSeconds;
                }

                UpdateTimerLabel();
                UpdateModeUI();
                ShowButtons(false, true, true);
            }
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            _isRunning = false;
            _timer.Stop();
            _totalSeconds = 0;
            _isWorkTime = true;
            _currentCycle = 0;
            RepeatLabel.Text = "0";

            _workTimeInSeconds = 0;
            _breakTimeInSeconds = 0;
            SetTimeButton.Text = "Set break";
            _totalSeconds = _workTimeInSeconds;

            UpdateTimerLabel();
            ShowButtons(true, false, false);
            UpdateModeUI();
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _timer.Stop();

            ShowButtons(true, false, true);
        }

        private void OnSetTimeClicked(object sender, EventArgs e)
        {
            if (SetTimeButton.Text == "Set break")
            {
                _workTimeInSeconds = _totalSeconds;
                _totalSeconds = _breakTimeInSeconds;
                SetTimeButton.Text = "Set time";
            }
            else
            {
                _breakTimeInSeconds = _totalSeconds;
                _totalSeconds = _workTimeInSeconds;
                SetTimeButton.Text = "Set break";
            }
            UpdateTimerLabel();
        }

        private void OnModePanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (_isRunning) return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _modeLastPanX = e.TotalX;
                    _hasSwiped = false;
                    break;
                case GestureStatus.Running:
                    if (_hasSwiped) return;

                    double deltaX = e.TotalX - _modeLastPanX;
                    int changeFactor = 50;
                    if (Math.Abs(deltaX) >= changeFactor)
                    {
                        if (deltaX > 0)
                        {
                            SwitchMode(1);
                        }
                        else
                        {
                            SwitchMode(-1);
                        }
                        _hasSwiped = true;
                        _modeLastPanX = e.TotalX;
                    }
                    break;
                case GestureStatus.Completed:
                    _modeLastPanX = 0;
                    break;
            }
        }

        private void SwitchMode(int direction)
        {
            if (_currentMode == TimerMode.Timer && direction < 0)
            {
                _currentMode = TimerMode.IntervalTimer;
            }
            else if (_currentMode == TimerMode.IntervalTimer)
            {
                if (direction < 0)
                {
                    _currentMode = TimerMode.Stopwatch;
                }
                else
                {
                    _currentMode = TimerMode.Timer;
                }
            }
            else if (_currentMode == TimerMode.Stopwatch && direction > 0)
            {
                _currentMode = TimerMode.IntervalTimer;
            }

            OnResetClicked(this, EventArgs.Empty);
            UpdateModeUI();
        }

        private void UpdateModeUI()
        {
            switch (_currentMode)
            {
                case TimerMode.Timer:
                    ModeLabel.Text = "TIMER";
                    LeftArrowLabel.IsVisible = true;
                    RightArrowLabel.IsVisible = false;
                    RepeatLabel.IsVisible = false;
                    SetTimeButton.IsVisible = false;
                    break;
                case TimerMode.IntervalTimer:
                    ModeLabel.Text = "INTERVAL TIMER";
                    LeftArrowLabel.IsVisible = true;
                    RightArrowLabel.IsVisible = true;
                    if (!_isRunning)
                    {
                        SetTimeButton.IsVisible = true;
                        RepeatLabel.IsVisible = false;
                    }
                    else
                    {
                        SetTimeButton.IsVisible = false;
                        RepeatLabel.IsVisible = true;
                    }
                    break;
                case TimerMode.Stopwatch:
                    ModeLabel.Text = "STOPWATCH";
                    LeftArrowLabel.IsVisible = false;
                    RightArrowLabel.IsVisible = true;
                    RepeatLabel.IsVisible = false;
                    SetTimeButton.IsVisible = false;
                    break;
            }
        }

        private void OnTimerPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            if (_isRunning) return;

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    _lastPanX = e.TotalX;
                    break;

                case GestureStatus.Running:
                    double deltaX = e.TotalX - _lastPanX;
                    int changeFactor = 30;
                    if (Math.Abs(deltaX) >= changeFactor)
                    {
                        int secondsToAdd = (int)(deltaX / changeFactor) * 10;
                        _totalSeconds += secondsToAdd;
                        _lastPanX = e.TotalX;
                    }

                    if (_totalSeconds < 0)
                    {
                        _totalSeconds = 0;
                    }

                    UpdateTimerLabel();
                    break;

                case GestureStatus.Completed:
                    _lastPanX = 0;
                    break;
            }
        }

        private void UpdateTimerLabel()
        {
            TimeSpan time = TimeSpan.FromSeconds(_totalSeconds);
            if (_currentMode == TimerMode.Stopwatch)
            {
                TimerLabel.Text = time.ToString(@"mm\:ss\:ff");
            }
            else
            {
                TimerLabel.Text = time.ToString(@"mm\:ss");
            }
        }

        private void ShowButtons(bool startVisible, bool pauseVisible, bool stopVisible)
        {
            StartButton.IsVisible = startVisible;
            PauseButton.IsVisible = pauseVisible;
            StopButton.IsVisible = stopVisible;
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            DisplayAlert("Settings", "placholder for settings", "OK");
        }
    }
}