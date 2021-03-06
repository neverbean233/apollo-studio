﻿using System;
using System.Collections.Generic;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;
using Apollo.Elements;
using Apollo.Structures;

namespace Apollo.DeviceViewers {
    public class HoldViewer: UserControl {
        public static readonly string DeviceIdentifier = "hold";

        void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);

            Duration = this.Get<Dial>("Duration");
            Gate = this.Get<Dial>("Gate");
            Infinite = this.Get<CheckBox>("Infinite");
            Release = this.Get<CheckBox>("Release");
        }
        
        Hold _hold;

        Dial Duration, Gate;
        CheckBox Infinite, Release;

        public HoldViewer() => new InvalidOperationException();

        public HoldViewer(Hold hold) {
            InitializeComponent();

            _hold = hold;

            Duration.UsingSteps = _hold.Time.Mode;
            Duration.Length = _hold.Time.Length;
            Duration.RawValue = _hold.Time.Free;

            Gate.RawValue = _hold.Gate * 100;

            SetInfinite(_hold.Infinite); // required to set Dial Enabled properties

            Release.IsChecked = _hold.Release;
        }

        void Unloaded(object sender, VisualTreeAttachmentEventArgs e) => _hold = null;

        void Duration_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                int u = (int)old.Value;
                int r = (int)value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Changed to {r}{Duration.Unit}", () => {
                    Track.TraversePath<Hold>(path).Time.Free = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Time.Free = r;
                });
            }

            _hold.Time.Free = (int)value;
        }

        public void SetDurationValue(int duration) => Duration.RawValue = duration;

        void Duration_ModeChanged(bool value, bool? old) {
            if (old != null && old != value) {
                bool u = old.Value;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Switched to {(r? "Steps" : "Free")}", () => {
                    Track.TraversePath<Hold>(path).Time.Mode = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Time.Mode = r;
                });
            }

            _hold.Time.Mode = value;
        }

        public void SetMode(bool mode) => Duration.UsingSteps = mode;

        void Duration_StepChanged(int value, int? old) {
            if (old != null && old != value) {
                int u = old.Value;
                int r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Duration Changed to {Length.Steps[r]}", () => {
                    Track.TraversePath<Hold>(path).Time.Length.Step = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Time.Length.Step = r;
                });
            }
        }

        public void SetDurationStep(Length duration) => Duration.Length = duration;

        void Gate_Changed(Dial sender, double value, double? old) {
            if (old != null && old != value) {
                double u = old.Value / 100;
                double r = value / 100;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Gate Changed to {value}{Gate.Unit}", () => {
                    Track.TraversePath<Hold>(path).Gate = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Gate = r;
                });
            }

            _hold.Gate = value / 100;
        }

        public void SetGate(double gate) => Gate.RawValue = gate * 100;

        void Infinite_Changed(object sender, RoutedEventArgs e) {
            bool value = Infinite.IsChecked.Value;

            if (_hold.Infinite != value) {
                bool u = _hold.Infinite;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Infinite Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Hold>(path).Infinite = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Infinite = r;
                });

                _hold.Infinite = value;
            }
        }

        public void SetInfinite(bool value) {
            Infinite.IsChecked = value;
            Duration.Enabled = Gate.Enabled = !value;
        }

        void Release_Changed(object sender, RoutedEventArgs e) {
            bool value = Release.IsChecked.Value;

            if (_hold.Release != value) {
                bool u = _hold.Release;
                bool r = value;
                List<int> path = Track.GetPath(_hold);

                Program.Project.Undo.Add($"Hold Release Changed to {(r? "Enabled" : "Disabled")}", () => {
                    Track.TraversePath<Hold>(path).Release = u;
                }, () => {
                    Track.TraversePath<Hold>(path).Release = r;
                });

                _hold.Release = value;
            }
        }

        public void SetRelease(bool value) => Release.IsChecked = value;
    }
}
