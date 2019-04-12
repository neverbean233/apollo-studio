﻿using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Apollo.Components;
using Apollo.Core;
using Apollo.Devices;

namespace Apollo.DeviceViewers {
    public class OverrideViewer: UserControl {
        public static readonly string DeviceIdentifier = "override";

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
        
        Override _override;
        Dial Target;

        private void Update_Target(int value) => Target.RawValue = value + 1;

        public OverrideViewer(Override o) {
            InitializeComponent();
            
            _override = o;
            _override.TargetChanged += Update_Target;

            Target = this.Get<Dial>("Target");
            Target.Maximum = Program.Project.Tracks.Count;
            Target.RawValue = _override.Target;
        }

        private void Target_Changed(double value) => _override.Target = (int)value - 1;
    }
}