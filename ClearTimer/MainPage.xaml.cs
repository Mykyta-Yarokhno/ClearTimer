using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Plugin.Maui.Audio;
using System;
using System.Linq;
using CommunityToolkit;
using CommunityToolkit.Maui.Core;

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
        private IAudioPlayer _audioPlayer;
        private IDispatcherTimer _timer;
        private bool _isRunning;
        private int _totalSeconds;

        // Змінні для Interval Timer
        private TimerMode _currentMode;
        private bool _isWorkTime = true;
        private int _workTimeInSeconds = 0;
        private int _breakTimeInSeconds = 0;
        private int _currentCycle = 0;

        // Змінні для Pan Gesture перемикання
        private double _lastPanX = 0;
        private double _modeLastPanX = 0;
        private bool _hasSwiped = false;

        private string _previousTimeText = "00:00";
        private bool _isLandscape;

        public MainPage()
        {
            InitializeComponent();

            _isLandscape = DeviceDisplay.Current.MainDisplayInfo.Orientation == DisplayOrientation.Landscape;

            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += OnTimerTick;

            StartButtonBorder.IsVisible = true;
            PauseButtonBorder.IsVisible = false;
            StopButtonBorder.IsVisible = false;

            _totalSeconds = 0;

            _currentMode = TimerMode.IntervalTimer;

            DeviceDisplay.MainDisplayInfoChanged += OnMainDisplayInfoChanged;

            UpdateOrientationLayout();

            //UpdateTimerLabel();
            UpdateTimerLabelsForOrientation();

            // Видаляємо виклик UpdateModeUI(), оскільки режими не перемикаються
            // UpdateModeUI();

            var audioManager = AudioManager.Current;
            var audioStream = FileSystem.OpenAppPackageFileAsync("lo-fi-piano-amp-guitar-strum-358389.mp3");
            _audioPlayer = audioManager.CreatePlayer(audioStream.Result);


            DeviceDisplay.KeepScreenOn = true;

        }

        private void OnMainDisplayInfoChanged(object sender, DisplayInfoChangedEventArgs e)
        {
            _isLandscape = e.DisplayInfo.Orientation == DisplayOrientation.Landscape;

            UpdateOrientationLayout();
        }

        private void UpdateOrientationLayout()
        {
            PortraitContainer.IsVisible = !_isLandscape;
            LandscapeContainer.IsVisible = _isLandscape;

            UpdateTimerLabelsForOrientation();
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

            // if (_currentMode == TimerMode.IntervalTimer)
            // {
            _totalSeconds--;

            if (_totalSeconds <= 0)
            {
                PlaySound();

                if (_isWorkTime)
                {
                    _currentCycle++;
                    RepeatLabel.Text = _currentCycle.ToString();

                    if (_breakTimeInSeconds > 0)
                    {
                        _isWorkTime = false;
                        _totalSeconds = _breakTimeInSeconds;
                        UpdateTimerLabelsForOrientation();
                    }
                    else
                    {
                        _isWorkTime = true;
                        _totalSeconds = _workTimeInSeconds;
                        UpdateTimerLabelsForOrientation();
                    }
                }
                else
                {
                    _isWorkTime = true;
                    _totalSeconds = _workTimeInSeconds;
                    UpdateTimerLabelsForOrientation();
                }
            }
            // }
            // else
            // {
            //     if (_currentMode == TimerMode.Timer)
            //     {
            //         _totalSeconds--;
            //         if (_totalSeconds <= 0)
            //         {
            //             PlaySound();
            //             _timer.Stop();
            //             _isRunning = false;
            //             _totalSeconds = 0;
            //             ShowButtons(true, false, false);
            //         }
            //     }
            //     else if (_currentMode == TimerMode.Stopwatch)
            //     {
            //         _totalSeconds++;
            //     }
            // }

            UpdateTimerLabelsForOrientation();
        }

        private async void OnStartClicked(object sender, EventArgs e)
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

                UpdateTimerLabelsForOrientation();
                // UpdateModeUI();
                StartButtonBorder.WidthRequest = 70;
                StartButtonBorder.HeightRequest = 70;
                //ShowButtons(false, true, true);
                await AnimateStartToPauseAndReset();
            }
        }

        private async void OnResetClicked(object sender, EventArgs e)
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

            await AnimatePauseAndResetToStart();

            UpdateTimerLabelsForOrientation();
            StartButtonBorder.WidthRequest = 100;
            StartButtonBorder.HeightRequest = 70;
            //ShowButtons(true, false, false);

            // UpdateModeUI();
        }

        private async void OnPauseClicked(object sender, EventArgs e)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _timer.Stop();

            //ShowButtons(true, false, true);

            await AnimatePauseToResumeAndReset();
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
            UpdateTimerLabelsForOrientation();
        }

        // Закоментовані методи для перемикання режимів
        // private void OnModePanUpdated(object sender, PanUpdatedEventArgs e)
        // {
        //     if (_isRunning) return;
        //     switch (e.StatusType)
        //     {
        //         case GestureStatus.Started:
        //             _modeLastPanX = e.TotalX;
        //             _hasSwiped = false;
        //             break;
        //         case GestureStatus.Running:
        //             if (_hasSwiped) return;
        //             double deltaX = e.TotalX - _modeLastPanX;
        //             int changeFactor = 50;
        //             if (Math.Abs(deltaX) >= changeFactor)
        //             {
        //                 if (deltaX > 0)
        //                 {
        //                     SwitchMode(1);
        //                 }
        //                 else
        //                 {
        //                     SwitchMode(-1);
        //                 }
        //                 _hasSwiped = true;
        //                 _modeLastPanX = e.TotalX;
        //             }
        //             break;
        //         case GestureStatus.Completed:
        //             _modeLastPanX = 0;
        //             break;
        //     }
        // }
        // private void SwitchMode(int direction)
        // {
        //     if (_currentMode == TimerMode.Timer && direction < 0)
        //     {
        //         _currentMode = TimerMode.IntervalTimer;
        //     }
        //     else if (_currentMode == TimerMode.IntervalTimer)
        //     {
        //         if (direction < 0)
        //         {
        //             _currentMode = TimerMode.Stopwatch;
        //         }
        //         else
        //         {
        //             _currentMode = TimerMode.Timer;
        //         }
        //     }
        //     else if (_currentMode == TimerMode.Stopwatch && direction > 0)
        //     {
        //         _currentMode = TimerMode.IntervalTimer;
        //     }
        //     OnResetClicked(this, EventArgs.Empty);
        //     UpdateModeUI();
        // }
        // private void UpdateModeUI()
        // {
        //     switch (_currentMode)
        //     {
        //         case TimerMode.Timer:
        //             ModeLabel.Text = "TIMER";
        //             LeftArrowLabel.IsVisible = true;
        //             RightArrowLabel.IsVisible = false;
        //             RepeatLabel.IsVisible = false;
        //             SetTimeButton.IsVisible = false;
        //             break;
        //         case TimerMode.IntervalTimer:
        //             ModeLabel.Text = "INTERVAL TIMER";
        //             LeftArrowLabel.IsVisible = true;
        //             RightArrowLabel.IsVisible = true;
        //             if (!_isRunning)
        //             {
        //                 SetTimeButton.IsVisible = true;
        //                 RepeatLabel.IsVisible = false;
        //             }
        //             else
        //             {
        //                 SetTimeButton.IsVisible = false;
        //                 RepeatLabel.IsVisible = true;
        //             }
        //             break;
        //         case TimerMode.Stopwatch:
        //             ModeLabel.Text = "STOPWATCH";
        //             LeftArrowLabel.IsVisible = false;
        //             RightArrowLabel.IsVisible = true;
        //             RepeatLabel.IsVisible = false;
        //             SetTimeButton.IsVisible = false;
        //             break;
        //     }
        // }

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

                    UpdateTimerLabelsForOrientation();
                    break;

                case GestureStatus.Completed:
                    _lastPanX = 0;
                    break;
            }
        }

        private void UpdateTimerLabelsForOrientation()
        {
            // Отримуємо поточний час
            TimeSpan time = TimeSpan.FromSeconds(_totalSeconds);
            string newTimeText = time.ToString(@"mm\:ss");

            // Визначаємо, які Label використовувати
            Label currentMinTens, currentMinOnes, currentSecTens, currentSecOnes;
            Label nextMinTens, nextMinOnes, nextSecTens, nextSecOnes;

            if (_isLandscape) // Якщо орієнтація горизонтальна
            {
                currentMinTens = LandscapeCurrentMinTens;
                currentMinOnes = LandscapeCurrentMinOnes;
                currentSecTens = LandscapeCurrentSecTens;
                currentSecOnes = LandscapeCurrentSecOnes;
                nextMinTens = LandscapeNextMinTens;
                nextMinOnes = LandscapeNextMinOnes;
                nextSecTens = LandscapeNextSecTens;
                nextSecOnes = LandscapeNextSecOnes;
            }
            else // Якщо орієнтація вертикальна
            {
                currentMinTens = CurrentMinTens;
                currentMinOnes = CurrentMinOnes;
                currentSecTens = CurrentSecTens;
                currentSecOnes = CurrentSecOnes;
                nextMinTens = NextMinTens;
                nextMinOnes = NextMinOnes;
                nextSecTens = NextSecTens;
                nextSecOnes = NextSecOnes;
            }

            // Запускаємо анімацію, використовуючи правильний набір Label
            if (_isRunning)
            {
                if (_previousTimeText[4] != newTimeText[4]) AnimateDigit(currentSecOnes, nextSecOnes, newTimeText[4].ToString());
                if (_previousTimeText[3] != newTimeText[3]) AnimateDigit(currentSecTens, nextSecTens, newTimeText[3].ToString());
                if (_previousTimeText[1] != newTimeText[1]) AnimateDigit(currentMinOnes, nextMinOnes, newTimeText[1].ToString());
                if (_previousTimeText[0] != newTimeText[0]) AnimateDigit(currentMinTens, nextMinTens, newTimeText[0].ToString());
            }
            else
            {
                currentMinTens.Text = newTimeText[0].ToString();
                currentMinOnes.Text = newTimeText[1].ToString();
                currentSecTens.Text = newTimeText[3].ToString();
                currentSecOnes.Text = newTimeText[4].ToString();
            }

            _previousTimeText = newTimeText;
        }

        private async void AnimateDigit(Label currentLabel, Label nextLabel, string newText)
        {
            // Set the new digit's text and starting state
            nextLabel.Text = newText;
            nextLabel.Opacity = 0;
            nextLabel.TranslationY = 60;

            // Define the animation durations
            uint fadeDuration = 250; // Shorter duration for fading
            uint moveDuration = 450; // Longer duration for moving

            await Task.WhenAll(
                // The old digit "flies" away slowly but fades out quickly
                currentLabel.TranslateTo(0, -60, moveDuration, Easing.CubicIn),
                currentLabel.FadeTo(0, fadeDuration),

                // The new digit "flies" in smoothly and appears fully
                nextLabel.TranslateTo(0, 0, moveDuration, Easing.CubicOut),
                nextLabel.FadeTo(1, moveDuration)
            );

            // Reset the labels to their starting states
            currentLabel.Text = newText;
            currentLabel.TranslationY = 0;
            currentLabel.Opacity = 1;

            nextLabel.Opacity = 0;
            nextLabel.TranslationY = 60;
        }

        private void ShowButtons(bool startVisible, bool pauseVisible, bool stopVisible)
        {
            StartButtonBorder.IsVisible = startVisible;
            PauseButtonBorder.IsVisible = pauseVisible;
            StopButtonBorder.IsVisible = stopVisible;
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            DisplayAlert("Settings", "Here will be ssetings", "OK");
        }

        private void PlaySound()
        {
            if (_audioPlayer != null)
            {
                _audioPlayer.Seek(0);
                _audioPlayer.Play();
            }
        }

        private void StopAndCleanupSound()
        {
            if (_audioPlayer != null && _audioPlayer.IsPlaying)
            {
                _audioPlayer.Stop();
            }
        }

        private async Task AnimateStartToPauseAndReset()
        {
            await StartButtonBorder.ScaleTo(0, 60, Easing.CubicIn);
            ShowButtons(false, true, true);
            await Task.WhenAll(
                PauseButtonBorder.ScaleTo(1, 60, Easing.CubicOut),
                StopButtonBorder.ScaleTo(1, 60, Easing.CubicOut)
            );
        }

        private async Task AnimatePauseAndResetToStart()
        {
            await Task.WhenAll(
                PauseButtonBorder.ScaleTo(0, 60, Easing.CubicIn),
                StopButtonBorder.ScaleTo(0, 60, Easing.CubicIn)
            );
            ShowButtons(true, false, false);
            await StartButtonBorder.ScaleTo(1, 60, Easing.CubicOut);
        }

        private async Task AnimatePauseToResumeAndReset()
        {
            await PauseButtonBorder.ScaleTo(0, 60, Easing.CubicIn);

            ShowButtons(true, false, true);

            await Task.WhenAll(
                StartButtonBorder.ScaleTo(1, 60, Easing.CubicOut),
                StopButtonBorder.ScaleTo(1, 60, Easing.CubicOut)
            );
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopAndCleanupSound();
        }
    }
}